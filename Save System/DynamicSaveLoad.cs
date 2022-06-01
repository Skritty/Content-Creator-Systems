using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System;
using System.Reflection;
using Object = UnityEngine.Object;
using System.IO;
using Newtonsoft.Json.Serialization;
using System.Linq;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using UnityEngine.Rendering;

public static class DynamicSaveLoad
{
    private class SaveFile
    {
        public Dictionary<string, List<JSONClassContainer>> jsonGroups = new Dictionary<string, List<JSONClassContainer>>();
    }

    private class JSONClassContainer//: IConvertible
    {
        public string n;
        public string t;
        public dynamic v;
        public int i = -1;
        public List<JSONClassContainer> c = new List<JSONClassContainer>();
        public Dictionary<string, JSONClassContainer> p = new Dictionary<string, JSONClassContainer>();
        public Dictionary<string, JSONClassContainer> f = new Dictionary<string, JSONClassContainer>();

        public JSONClassContainer() { }
        public JSONClassContainer(object source)
        {
            // If it is null or fake-null, return
            if (source == null || (source.GetType().IsSubclassOf(typeof(Object)) && (Object)source == null)) return;

            Type type = source.GetType();

            // If it is a Component, save reference to it's parent gameobject
            if (type.IsSubclassOf(typeof(Component)))
            {
                //Debug.Log($"{source} is a Component");
                p.Add("gameObject", new JSONClassContainer((source as Component).gameObject));
            }

            // If this is a Unity Object, check to see if it already has been recorded
            if (type.IsSubclassOf(typeof(Object)))
            {
                t = type.AssemblyQualifiedName;
                i = (source as Object).GetInstanceID();
                //Debug.Log($"{source} is an Object, considering it a reference with UID {uid}");
                if (saveUIDs.Contains(i))
                {
                    return;
                }
                else
                {
                    saveUIDs.Add(i);
                }
            }

            // If it is a GameObject, find and create containers for all the components
            if (type == typeof(GameObject))
            {
                //Debug.Log($"{source} is a GameObject, getting components...");
                foreach (Component component in (source as GameObject).GetComponents<Component>())
                {
                    //Debug.Log($"Component on {(source as GameObject).name}: {c.name}");
                    JSONClassContainer container = new JSONClassContainer(component);
                    container.n = component.name;
                    c.Add(container);
                }
            }

            // If it is enumerable, fill the value with a list and add it's entries
            if (source is IEnumerable && type != typeof(string))
            {
                //Debug.Log($"{type.Name} is a enumerable");
                List<JSONClassContainer> collection = new List<JSONClassContainer>();
                foreach (object entry in (IEnumerable)source)
                {
                    collection.Add(new JSONClassContainer(entry));
                }
                v = collection;
                t = type.AssemblyQualifiedName;
                if (type != typeof(Transform))
                    return;
            }

            // If it is a value type, set the value
            else if (type.IsPrimitive || type == typeof(string) || type.IsConstructedGenericType)
            {
                //Debug.Log($"{source} is a value type");
                v = source;
                return;
            }
            
            // Create containers for all readable properties
            foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                try
                {
                    if (!property.CanRead || !property.CanWrite || property.GetIndexParameters().Length > 0 || property.GetCustomAttributes(typeof(ObsoleteAttribute), true).Length > 0) continue;
                    //Debug.Log(property.Name);
                    object value = property.GetValue(source, null);
                    if (value == null || source.Equals(value)) continue;
                    JSONClassContainer container = new JSONClassContainer(value);
                    container.n = property.Name;
                    p.Add(property.Name, container);
                }
                catch
                {
                    throw;
                }
            }

            // Create containers for all fields
            foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                try
                {
                    if (field.GetCustomAttributes(typeof(ObsoleteAttribute), true).Length > 0) continue;
                    object value = field.GetValue(source);
                    //Debug.Log(field.Name +": "+ value);
                    if (value == null || source.Equals(value)) continue;
                    JSONClassContainer container = new JSONClassContainer(value);
                    container.n = field.Name;
                    f.Add(field.Name, container);
                }
                catch
                {
                    throw;
                }
            }

            //Exceptions
            if (type == typeof(Material))
            {
                Material material = (Material)source;
                for (int i = 0; i < material.GetPropertyCount(); i++)
                {
                    var value = material.GetProperty(i);
                    string name = material.GetPropertyName(i);
                    p.Add(name, new JSONClassContainer(value));
                }
            }

            // If it is a texture, obtain its data in a special way
            if (type == typeof(Texture2D))
            {
                Texture2D texture = ((Texture2D)source).Decompress();
                byte[] data = texture.EncodeToPNG();
                v = data;
            }

            if (type == typeof(Mesh))
            {
                Mesh mesh = ((Mesh)source);
                List<SubMeshDescriptor> submeshes = new List<SubMeshDescriptor>();
                for (int i = 0; i < mesh.subMeshCount; i++)
                {
                    submeshes.Add(mesh.GetSubMesh(i));
                }
                p.Add("submeshes", new JSONClassContainer(submeshes));
            }

            t = type.AssemblyQualifiedName;
        }

        public object ToObject()
        {
            // If this object is a reference type and has already been created, return the instance
            if (i != -1 && loadedReferences.ContainsKey(i))
            {
                return loadedReferences[i];
            }

            object obj;
            Type type = null;
            if (t != null)
                type = Type.GetType(t);
            else if (v != null)
                type = ((object)v).GetType();
            else return null;
            //Debug.Log(type);

            // Converters
            if (type == typeof(Material))
            {
                Material material = new Material(Shader.Find("Standard"));
                material.DisableKeyword("_EMISSION");
                if (p.Count == 0) return material;
                for (int i = 0; i < material.GetPropertyCount(); i++)
                {
                    string name = material.GetPropertyName(i);
                    if (!p.ContainsKey(name)) continue;
                    material.SetProperty(i, p[name].ToObject());
                }
                return material;
            }

            if (type == typeof(Texture2D))
            {
                if (v == null) return null;
                Texture2D texture = new Texture2D(1, 1);
                byte[] data = Convert.FromBase64String(v);
                texture.LoadImage(data);
                return texture;
            }

            // If the object has a value
            if (v != null)
            {
                if (v is JArray)
                {
                    if(type.GetInterface(nameof(ICollection)) != null)
                    {
                        Type typeC = type.GetEnumerableType();
                        //Debug.Log($"Array type: {typeC}");
                        List<object> temp = new List<object>();
                        foreach(JObject child in (List<object>)(v as JArray).ToObject(typeof(List<object>)))
                        {
                            temp.Add(child.ToObject<JSONClassContainer>().ToObject());
                        }
                        var temp2 = Array.CreateInstance(typeC, temp.Count);
                        for (int a = 0; a < temp.Count; a++)
                            temp2.SetValue(Convert.ChangeType(temp[a], typeC), a);
                        obj = temp2;

                        if (type.IsGenericList())
                        {
                            IList l = (IList)Activator.CreateInstance(type);
                            foreach (dynamic x in (IEnumerable)obj)
                                l.Add(x);
                            obj = l;
                        }
                        return obj;
                    }
                    else if(type.GetInterface(nameof(IEnumerable)) != null)
                    {
                        foreach(JObject child in (List<dynamic>)(v as JArray).ToObject(typeof(List<dynamic>)))
                        {
                            child.ToObject<JSONClassContainer>().ToObject();
                        }
                    }
                }
                else
                {
                    if (type == typeof(Int64))
                        v = Convert.ToInt32(v);
                    if (type == typeof(ulong))
                        v = Convert.ToUInt32(v);
                    if (type == typeof(Double))
                        v = Convert.ToSingle(v);
                    
                    return v;
                }
            }

            // Create the object instance
            if (type.IsSubclassOf(typeof(Component)))
            {
                // If the object is a component, add this component to it's referenced gameobject instead of creating an instance
                //Debug.Log($"Component1: {type}");
                GameObject gameObject = (GameObject)p["gameObject"].ToObject();
                obj = gameObject.GetComponent(type);
                if ((Object)obj == null)
                    obj = gameObject.AddComponent(type);
                //obj = gameObject.GetComponent(type);
                //Debug.Log($"Component2: {obj}, t: {type}, subclass? {type.IsSubclassOf(typeof(Component))}, GO: {gameObject}");
            }
            else if(type == typeof(Shader))
            {
                obj = Shader.Find("Standard");
            }
            else
            {
                obj = Activator.CreateInstance(type);
            }

            if (i != -1 && loadedReferences.ContainsKey(i) && loadedReferences[i] != obj as Object)
            {
                return loadedReferences[i];
            }
            // Add the instance to loaded references so it won't get loaded again
            if (i != -1 && !loadedReferences.ContainsKey(i))
            {
                loadedReferences.Add(i, obj as Object);
            }

            foreach (KeyValuePair<string, JSONClassContainer> property in p)
            {
                PropertyInfo p = type.GetProperty(property.Key, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (p == null || !p.CanWrite) continue;
                object value = property.Value.ToObject();
                //Debug.Log($"{property.Key}/{type}: {p}, {value}");
                if (p.PropertyType == typeof(int))
                    value = Convert.ToInt32(value);
                if (p.PropertyType == typeof(uint))
                    value = Convert.ToUInt32(value);
                if (p.PropertyType == typeof(float))
                    value = Convert.ToSingle(value);
                if (p.PropertyType.IsGenericList())
                {
                    IList l = (IList)Activator.CreateInstance(p.PropertyType);
                    foreach (dynamic x in (IEnumerable)value)
                        l.Add(x);
                    value = l;
                }
                if (value != null && value.GetType() == typeof(JObject))
                    value = ((JObject)value).ToObject(p.PropertyType);
                if (obj != null && value != null)
                    p.SetValue(obj, value);
            }

            foreach (KeyValuePair<string, JSONClassContainer> field in f)
            {
                FieldInfo f = type.GetField(field.Key, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                //Debug.Log(f.Name);
                if (f == null) continue;
                object value = field.Value.ToObject();
                if (f.FieldType == typeof(int))
                    value = Convert.ToInt32(value);
                if (f.FieldType == typeof(uint))
                    value = Convert.ToUInt32(value);
                if (f.FieldType == typeof(float))
                    value = Convert.ToSingle(value);
                if (f.FieldType.IsGenericList())
                {
                    IList l = (IList)Activator.CreateInstance(f.FieldType);
                    foreach (dynamic x in (IEnumerable)value)
                        l.Add(x);
                    value = l;
                }
                if (value != null && value.GetType() == typeof(JObject))
                    value = ((JObject)value).ToObject(f.FieldType);
                f.SetValue(obj, value);
            }

            if (type == typeof(Mesh))
            {
                Mesh mesh = (Mesh)obj;
                List<SubMeshDescriptor> submeshes = (List<SubMeshDescriptor>)p["submeshes"].ToObject();
                mesh.subMeshCount = submeshes.Count;
                int i = 0;
                foreach (SubMeshDescriptor submesh in submeshes)
                {
                    mesh.SetSubMesh(i++, submesh);
                }
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                mesh.RecalculateTangents();
                return mesh;
            }

            foreach (JSONClassContainer component in c)
            {
                component.ToObject();
            }

            return obj;
        }

        /*#region IConvertible
        public TypeCode GetTypeCode()
        {
            return TypeCode.Object;
        }
        public object ToType(Type conversionType, IFormatProvider provider)
        {
            if (conversionType == typeof(JSONClassContainer))
            {
                return this;
            }

            return null;
        }
        public bool ToBoolean(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }
        public byte ToByte(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }
        public char ToChar(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }
        public DateTime ToDateTime(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }
        public decimal ToDecimal(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }
        public double ToDouble(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }
        public short ToInt16(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }
        public int ToInt32(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }
        public long ToInt64(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }
        public sbyte ToSByte(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }
        public float ToSingle(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }
        public string ToString(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }
        public ushort ToUInt16(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }
        public uint ToUInt32(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }
        public ulong ToUInt64(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }
        #endregion*/
    }
    public static Type GetListType(Type type)
    {
        if (!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(List<>))
            throw new ArgumentException("Type must be List<>, but was " + type.FullName, "someList");

        return type.GetGenericArguments()[0];
    }

    private static SaveFile currentSaveRecord;
    private static SaveFile currentLoadRecord;
    private static HashSet<int> saveUIDs = new HashSet<int>();
    private static Dictionary<int, Object> loadedReferences = new Dictionary<int, Object>();
    public static void ClearSaveGroups() => currentSaveRecord = null;
    public static void RemoveSaveGroup(string groupName) => currentSaveRecord.jsonGroups.Remove(groupName);
    public static void AddToSaveGroup<T>(string groupName, List<T> objectsToSave)
    {
        if(currentSaveRecord == null)
        {
            currentSaveRecord = new SaveFile();
        }

        if (!currentSaveRecord.jsonGroups.ContainsKey(groupName))
        {
            currentSaveRecord.jsonGroups.Add(groupName, new List<JSONClassContainer>());
        }
        
        foreach (object obj in objectsToSave)
        {
            //Debug.Log($"Typeof: {obj.GetType().Name}");
            JSONClassContainer root = new JSONClassContainer(obj);
            currentSaveRecord.jsonGroups[groupName].Add(root);
        }
    }

    public static void AddToSaveGroup(string groupName, object objectToSave)
    {
        if (currentSaveRecord == null)
        {
            currentSaveRecord = new SaveFile();
        }

        if (!currentSaveRecord.jsonGroups.ContainsKey(groupName))
        {
            currentSaveRecord.jsonGroups.Add(groupName, new List<JSONClassContainer>());
        }

        JSONClassContainer root = new JSONClassContainer(objectToSave);
        currentSaveRecord.jsonGroups[groupName].Add(root);
    }

    public static void Save(string filePath)
    {
        JsonSerializerSettings settings = new JsonSerializerSettings();
        settings.Formatting = Formatting.None;
        settings.NullValueHandling = NullValueHandling.Include;
        string json = JsonConvert.SerializeObject(currentSaveRecord, settings);
        File.WriteAllText(filePath, json);
    }

    public static void Load(string filePath)
    {
        string json = File.ReadAllText(filePath);
        currentLoadRecord = JsonConvert.DeserializeObject<SaveFile>(json);
    }

    public static List<T> LoadObjectGroup<T>(string groupName)
    {
        if(currentLoadRecord == null)
        {
            Debug.LogError($"Please use DynamicSaveLoad.Load() before trying to load any objects!");
        }

        if (!currentLoadRecord.jsonGroups.ContainsKey(groupName))
        {
            Debug.LogError($"Group [{groupName}] not found!");
            return null;
        }

        List<T> objects = new List<T>();

        foreach (JSONClassContainer obj in currentLoadRecord.jsonGroups[groupName])
        {
            if(obj.t == null)
            {
                continue;
            }

            if (Type.GetType(obj.t).IsAssignableFrom(typeof(T)))
                objects.Add((T)obj.ToObject());
        }

        return objects;
    }

    public static List<object> LoadAllObjects()
    {
        // TODO
        return null;
    }
}

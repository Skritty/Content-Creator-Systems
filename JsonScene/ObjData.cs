using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using SimpleJSON;
using System.Linq;
using System.Threading.Tasks;

/// <summary>
/// A data storage class that attaches to scene objects to aid in saving to/loading from JSON.
/// </summary>
public class ObjData : MonoBehaviour
{
    #region Stored Data
    public Transform obj;
    public Transform rootObj;
    public string submeshReference;
    public string meshFileName;
    public MeshFilter mf;
    public MeshRenderer rend;
    public Light light;
    public List<Preset<Material>> optionalMaterials = new List<Preset<Material>>();
    public MeshZeroer.Pivot pivot = MeshZeroer.Pivot.Bottom;
    public List<bool> disabledSubmeshes;
    public void DisableSubmesh(int submeshIndex)
    {
        if(mf && mf.mesh && mf.mesh.subMeshCount > submeshIndex)
        {
            if (disabledSubmeshes == null)
            {
                disabledSubmeshes = new List<bool>();
                for (int i = 0; i < mf.mesh.subMeshCount; i++)
                    disabledSubmeshes.Add(false);
            }
             
            disabledSubmeshes[submeshIndex] = true;
            UnityEngine.Rendering.SubMeshDescriptor old = mf.mesh.GetSubMesh(submeshIndex);
            int smi = submeshIndex;
            mf.mesh.SetSubMesh(submeshIndex, new UnityEngine.Rendering.SubMeshDescriptor());
            mf.LogState(
                    () => { return old; },
                    x => { mf.mesh.SetSubMesh(smi, old); }
                );
        }
    }

    // UI helper data
    public Material[] originalMats, uniqueMats, singleUniqueMats;
    public bool[] isUniqueMat;
    #endregion

    #region Instantiation
    /// <summary>
    /// Creates object data for an object and its children. 
    /// Use this before making any hierarchy or name changes to the object in question.
    /// </summary>
    /// <param name="obj">The object to have object data created for</param>
    /// <returns>The root object data</returns>
    public static ObjData CreateObjData(Transform obj, string _meshFilePath, Transform root = null)
    {
        ObjData data = obj.gameObject.AddComponent<ObjData>();
        data.InstantiateData(obj, obj.GetComponent<MeshFilter>(), _meshFilePath);
        if (root) data.rootObj = root;
        else root = data.rootObj = obj;

        foreach (Transform child in obj)
        {
            CreateObjData(child, _meshFilePath, root);
        }

        return data;
    }

    private void InstantiateData(Transform _obj, MeshFilter _submeshReference, string _meshFilePath)
    {
        obj = _obj;
        if (_submeshReference && _submeshReference.sharedMesh)
        {
            submeshReference = _submeshReference.sharedMesh.name.Replace(" Instance", "");
            meshFileName = _meshFilePath;
        }

        mf = _submeshReference;
        rend = obj.GetComponent<MeshRenderer>();
        if (rend)
        {
            //rend.gameObject.AddComponent<MaterialPresetPicker>();
            rend.gameObject.AddComponent<OptionalMaterials>();
            originalMats = rend.sharedMaterials;
            uniqueMats = rend.sharedMaterials;
            singleUniqueMats = rend.sharedMaterials;
            isUniqueMat = new bool[rend.sharedMaterials.Length];
        }
        light = obj.GetComponent<Light>();
    }

    public void UpdateComponents()
    {
        mf = obj.GetComponent<MeshFilter>();
        rend = obj.GetComponent<MeshRenderer>();
        light = obj.GetComponent<Light>();
        if (light) light.useColorTemperature = true;
    }
    #endregion

    /// <summary>
    /// Converts an object and its children to a JSONObject.
    /// </summary>
    /// <param name="referenceObjectPaths">A list of project model paths referenced by objects for construction</param>
    public JSONObject AddObjToJson(List<string> referenceObjectPaths, JsonSceneManagement.JSONReferences references)
    {
        UpdateComponents();
        JSONObject jobj = new JSONObject();

        //Add the object's name
        jobj.Add("Name", obj.gameObject.name);
        jobj.Add("Tag", obj.gameObject.tag);
        JSONArray ds = new JSONArray();

        // Add disabled submeshes
        if(disabledSubmeshes != null)
        {
            foreach (bool b in disabledSubmeshes)
                ds.Add(b);
            jobj.Add("Disabled Submeshes", ds);
        }

        // Add submesh reference (what is the original name of this piece)
        if (submeshReference != null && submeshReference != "")
        {
            jobj.Add("Submesh Reference", submeshReference);
        }

        // Add referenced mesh file path to the rolling list
        if (meshFileName != null && meshFileName != "")
        {
            jobj.Add("Mesh File Name", meshFileName);
            if (!referenceObjectPaths.Contains(meshFileName))
                referenceObjectPaths.Add(meshFileName);
        }

        // Add transform
        JSONObject transform = new JSONObject();
        jobj.Add("Transform", transform);

        JSONArray position = new JSONArray();
        position.Add(obj.position.x);
        position.Add(obj.position.y);
        position.Add(obj.position.z);
        transform.Add("Position", position);
        Debug.Log($"{obj.name} {obj.position}");

        JSONArray localPosition = new JSONArray();
        localPosition.Add(obj.localPosition.x);
        localPosition.Add(obj.localPosition.y);
        localPosition.Add(obj.localPosition.z);
        transform.Add("Local Position", localPosition);

        JSONArray rotation = new JSONArray();
        rotation.Add(obj.rotation.x);
        rotation.Add(obj.rotation.y);
        rotation.Add(obj.rotation.z);
        rotation.Add(obj.rotation.w);
        transform.Add("Rotation", rotation);

        JSONArray scale = new JSONArray();
        scale.Add(obj.localScale.x);
        scale.Add(obj.localScale.y);
        scale.Add(obj.localScale.z);
        transform.Add("Scale", scale);

        //Add material data
        if (rend != null)
        {
            JSONArray meshOffset = new JSONArray();
            meshOffset.Add(mf.sharedMesh.bounds.center.x);
            meshOffset.Add(mf.sharedMesh.bounds.center.y);
            meshOffset.Add(mf.sharedMesh.bounds.center.z);
            jobj.Add("Mesh Offset", meshOffset);

            JSONArray materials = new JSONArray();
            jobj.Add("Materials", materials);
            for (int i = 0; i < rend.sharedMaterials.Length; i++)
            {
                materials.Add("Preset", PresetLibrary.Get(rend.sharedMaterials[i]).name);
            }
        }

        // Add optional materials
        JSONArray jOptionalMaterials = new JSONArray();
        jobj.Add("Optional Materials", jOptionalMaterials);
        foreach(Preset<Material> m in optionalMaterials)
        {
            JSONObject material = new JSONObject();
            jOptionalMaterials.Add("Preset", m.name);
        }

        jobj.Add("Pivot", pivot.ToString());

        //Add lighting data
        if (light != null)
        {
            JSONObject jlight = new JSONObject();
            jobj.Add("Light", jlight);
            jlight.Add("Type", light.type.ToString());
            jlight.Add("Shape", light.shape.ToString());
            JSONArray color = new JSONArray();
            color.Add(light.color.r);
            color.Add(light.color.g);
            color.Add(light.color.b);
            color.Add(light.color.a);
            jlight.Add("Color", color);
            jlight.Add("Intensity", light.intensity);
            jlight.Add("Range", light.range);
            jlight.Add("Spot Angle", light.spotAngle);
            jlight.Add("Bounce Intensity", light.bounceIntensity);
            jlight.Add("Color Temperature", light.colorTemperature);
            jlight.Add("Shadow Type", ((int)light.shadows));
        }

        if (GetComponent<ReflectionProbe>())
            jobj.Add("Reflection Probe", true);

        //Add children
        JSONArray children = new JSONArray();
        jobj.Add("Children", children);
        foreach (Transform child in obj)
        {
            children.Add(child.GetComponent<ObjData>().AddObjToJson(referenceObjectPaths, references));
        }

        return jobj;
    }

    /// <summary>
    /// Takes json object data and builds an object to its exact specifications.
    /// </summary>
    /// <param name="jsonObj">The JSON data for the object</param>
    /// <param name="referenceObjects">The dictionary of objects in the scene to reference when constructing objects</param>
    /// <param name="parent">Do not use</param>
    /// <returns>The created object</returns>
    public static Transform CreateObjectFromJson(JSONNode jsonObj, string projectFolder, JsonSceneManagement.JSONReferences references, Transform parent = null, Transform root = null)
    {
        Transform obj = (new GameObject()).transform;

        //ObjData | Set the object's name and generate object data for it
        obj.gameObject.name = jsonObj["Name"];
        if (jsonObj["Tag"])
            obj.gameObject.tag = jsonObj["Tag"];

        //Transform | Update transform
        obj.parent = parent;
        obj.position = new Vector3(
            jsonObj["Transform"]["Position"][0].AsFloat,
            jsonObj["Transform"]["Position"][1].AsFloat,
            jsonObj["Transform"]["Position"][2].AsFloat);
        obj.rotation = new Quaternion(
            jsonObj["Transform"]["Rotation"][0].AsFloat,
            jsonObj["Transform"]["Rotation"][1].AsFloat,
            jsonObj["Transform"]["Rotation"][2].AsFloat,
            jsonObj["Transform"]["Rotation"][3].AsFloat);
        obj.localScale = new Vector3(
            jsonObj["Transform"]["Scale"][0].AsFloat,
            jsonObj["Transform"]["Scale"][1].AsFloat,
            jsonObj["Transform"]["Scale"][2].AsFloat);

        //MeshFilter/Renderer | Find the referenced mesh
        if (jsonObj["Submesh Reference"] != null && jsonObj["Submesh Reference"] != "")
        {
            Transform reference = references.objects[Path.GetFileNameWithoutExtension(jsonObj["Mesh File Name"]).ToLower()];
            Transform objRef = FindChildInHeirarchy(reference, jsonObj["Submesh Reference"]);
            MeshFilter mf = obj.gameObject.AddComponent<MeshFilter>();
            mf.sharedMesh = objRef.GetComponent<MeshFilter>().sharedMesh;
            obj.gameObject.AddComponent<MeshRenderer>().sharedMaterials = objRef.GetComponent<MeshRenderer>().sharedMaterials;
            obj.gameObject.AddComponent<MeshCollider>().sharedMesh = obj.gameObject.GetComponent<MeshFilter>().sharedMesh;

            //Materials/Textures | Set the correct materials and textures
            if (jsonObj["Materials"] != null)
            {
                int i = 0;
                foreach (JSONNode material in jsonObj["Materials"])
                {
                    if (i >= obj.gameObject.GetComponent<MeshRenderer>().sharedMaterials.Length) break;
                    MaterialPropertyBlock preset = PresetLibrary.GetPreset<Material>(material["Material Name"]);
                    if (preset != null)
                    {
                        obj.gameObject.GetComponent<MeshRenderer>().SetPropertyBlock(preset, i);
                    }
                    i++;
                }
            }
        }

        ObjData data = CreateObjData(obj, jsonObj["Mesh File Name"]);

        if (root) data.rootObj = root;
        else root = data.rootObj = obj;

        if (jsonObj["Disabled Submeshes"] != null)
        {
            int enabled = 0;
            int total = 0;
            for (int i = 0; i < jsonObj["Disabled Submeshes"].AsArray.Count; i++)
            {
                if (jsonObj["Disabled Submeshes"].AsArray[i])
                {
                    data.DisableSubmesh(i);
                }
                else
                {
                    enabled = i;
                    total++;
                }
            }
            if(total == 1)
            {
                Mesh m = MeshSlicer.DisconnectSubmesh(data.mf.mesh, enabled);
                data.mf.mesh = m;
                data.GetComponent<MeshCollider>().sharedMesh = m;
            }
        }

        if (jsonObj["Pivot"] != null)
            data.pivot = (MeshZeroer.Pivot)System.Enum.Parse(typeof(MeshZeroer.Pivot), (string)jsonObj["Pivot"]);

        //Optional Materials/Textures | Set the correct materials and textures
        if (jsonObj["Optional Materials"] != null)
        {
            foreach (JSONNode material in jsonObj["Optional Materials"])
            {
                Preset<Material> preset = PresetLibrary.Get<Material>(material["Material Name"]);
                if (preset != null)
                {
                    data.optionalMaterials.Add(preset);
                }
            }
        }

        //Light | Set lighting data if needed
        if (jsonObj["Light"] != null)
        {
            if (data.light == null) data.light = obj.gameObject.AddComponent<Light>();
            obj.gameObject.AddComponent<LightPresetPicker>();
            LightType type = (LightType)System.Enum.Parse(typeof(LightType), jsonObj["Light"]["Type"]);
            data.light.type = type;
            LightShape shape = (LightShape)System.Enum.Parse(typeof(LightShape), jsonObj["Light"]["Shape"]);
            data.light.shape = shape;
            data.light.color = new Color(
                jsonObj["Light"]["Color"][0],
                jsonObj["Light"]["Color"][1],
                jsonObj["Light"]["Color"][2],
                jsonObj["Light"]["Color"][3]);
            data.light.intensity = jsonObj["Light"]["Intensity"];
            data.light.range = jsonObj["Light"]["Range"];
            data.light.spotAngle = jsonObj["Light"]["Spot Angle"];
            data.light.bounceIntensity = jsonObj["Light"]["Bounce Intensity"];
            data.light.colorTemperature = jsonObj["Light"]["Color Temperature"];
            data.light.shadows = (LightShadows)jsonObj["Light"]["Shadow Type"].AsInt;
            data.light.useColorTemperature = true;
        }

        if (jsonObj["Reflection Probe"])
            obj.gameObject.AddComponent<ReflectionProbe>();

        //Do the same for all children
        foreach (JSONNode child in jsonObj["Children"])
            CreateObjectFromJson(child, projectFolder, references, obj, root);
        return obj;
    }

    #region Helpers
    private static Transform FindChildInHeirarchy(Transform parent, string name)
    {
        if (parent.GetComponent<MeshFilter>() && parent.GetComponent<MeshFilter>().sharedMesh.name.Replace(" Instance", "") == name) return parent;
        Transform found = null;
        foreach (Transform child in parent)
        {
            found = FindChildInHeirarchy(child, name);
            if (found != null) break;
        }
        return found;
    }

    public bool HitByRay(Ray ray)
    {
        if (rend && rend.bounds.IntersectRay(ray)) return true;
        return false;
    }
    #endregion
}
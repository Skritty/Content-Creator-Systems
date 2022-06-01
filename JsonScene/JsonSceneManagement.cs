using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.IO;
using SimpleJSON;
using TriLibCore;

public class JsonSceneManagement
{
    public delegate void Finished();
    public static Finished doneLoading;

    /// <summary>
    /// Contains dictionaries for objects, textures, and materials already in the scene. All keys are the object names.
    /// </summary>
    public class JSONReferences
    {
        public Dictionary<string, Transform> objects = new Dictionary<string, Transform>();
        public Dictionary<string, Texture> textures = new Dictionary<string, Texture>();
        public Dictionary<string, Material> materials = new Dictionary<string, Material>();
    }

    public static IEnumerator LoadJsonScene(List<Transform> sceneObjects, string sceneJSONFilePath, string projectPath, JSONReferences references)
    {
        string jsonString = File.ReadAllText(sceneJSONFilePath);
        JSONNode scene = JSON.Parse(jsonString);

        if (scene == null || scene["Name"] == null)
        {
            doneLoading.Invoke();
            yield break;
        }

        // Create hidden reference objects to copy submeshes from
        foreach (KeyValuePair<string, JSONNode> objectFilePath in scene["Reference Objects"])
        {
            bool loading = true;
            string path = projectPath + "\\Meshes\\" + objectFilePath.Value;

            if (!references.objects.ContainsKey(Path.GetFileNameWithoutExtension(objectFilePath.Value).ToLower()))
            {
                // Load the asset if it doesn't already exist
                AssetLoaderOptions assetLoaderOptions = AssetLoader.CreateDefaultLoaderOptions();
                AssetLoaderContext context = AssetLoader.LoadModelFromFile(path, null, (c) => loading = false, null, (c) => loading = false, null, assetLoaderOptions);
                yield return new WaitWhile(() => loading);
                GameObject asset = context.RootGameObject;
                if (context.RootGameObject == null) continue;
                asset.SetActive(false);
                asset.name = Path.GetFileNameWithoutExtension(objectFilePath.Value);
                MeshZeroer.ZeroObjectMeshes(asset.transform.gameObject);
                asset.transform.DoFunctionToTree(o => {
                    MeshFilter mf = o.GetComponent<MeshFilter>();
                    if (mf)
                        MeshSlicer.SliceMesh(o.gameObject.GetComponent<MeshFilter>(), o.gameObject.GetComponent<MeshRenderer>(), o.lossyScale);
                });
                // Add it to the reference
                references.objects.Add(asset.name.ToLower(), asset.transform);
            }
        }

        // Create all the actual objects
        foreach (JSONObject obj in scene["Objects"])
        {
            //Debug.Log($"Loading {obj["Name"]}");
            //Dictionary<string, Material> previousMats = references.materials;
            //references.materials = new Dictionary<string, Material>();
            Transform newObj = ObjData.CreateObjectFromJson(obj, projectPath, references);
            
            //foreach (KeyValuePair<string, Material> mat in previousMats)
            //    references.materials.Add(mat.Key, mat.Value);
            sceneObjects.Add(newObj);
        }
        doneLoading.Invoke();
        //Debug.Log(sceneObjects.Count);
        //foreach (KeyValuePair<string, Transform> o in referenceObjects) GameObject.DestroyImmediate(o.Value.gameObject);
    }

#if UNITY_EDITOR
    /// <summary>
    /// Clears the old scene and places all the objects of a new scene
    /// </summary>
    /// <param name="sceneObjects"></param>
    /// <param name="sceneJSON"></param>
    public static void LoadJsonSceneEditor(List<Transform> sceneObjects, string sceneJSONFilePath, string projectPath, JSONReferences references)
    {
        string jsonString = File.ReadAllText(sceneJSONFilePath);
        JSONNode scene = JSON.Parse(jsonString);

        if (scene == null || scene["Name"] == null) return;

        // Create hidden reference objects to copy submeshes from
        foreach (KeyValuePair<string, JSONNode> objectFilePath in scene["Reference Objects"])
        {
            string path = "Assets/TempExtracted/" + Path.GetFileName(projectPath) + "/Meshes/" + objectFilePath.Value;

            if (!references.objects.ContainsKey(Path.GetFileNameWithoutExtension(objectFilePath.Value).ToLower()))
            {
                GameObject asset = GameObject.Instantiate<GameObject>((GameObject)AssetDatabase.LoadMainAssetAtPath(path));
                if (asset == null) continue;
                asset.SetActive(false);
                asset.name = Path.GetFileNameWithoutExtension(objectFilePath.Value);

                references.objects.Add(asset.name.ToLower(), asset.transform);
            }
        }

        // Create all the actual objects
        foreach (JSONObject obj in scene["Objects"])
        {
            sceneObjects.Add(ObjData.CreateObjectFromJson(obj, projectPath, references));
        }

        //foreach (KeyValuePair<string, Transform> o in referenceObjects) GameObject.DestroyImmediate(o.Value.gameObject);
    }
#endif
    public static JSONObject SceneToJson(List<Transform> sceneObjects, JSONReferences references, string filePath)
    {
        JSONObject scene = new JSONObject();
        scene.Add("Name", Path.GetFileNameWithoutExtension(filePath));
        JSONArray refObjs = new JSONArray();
        List<string> referenceObjectPaths = new List<string>();
        scene.Add("Reference Objects", refObjs);
        JSONArray objs = new JSONArray();
        scene.Add("Objects", objs);

        foreach (Transform so in sceneObjects)
        {
            Debug.Log($"saving {so.gameObject.name}");
            //MeshZeroer.ZeroObjectMeshes(so.gameObject, MeshZeroer.Pivot.Center);
            objs.Add(so.GetComponent<ObjData>().AddObjToJson(referenceObjectPaths, references));
        }

        foreach (string refPath in referenceObjectPaths)
        {
            refObjs.Add(refPath);
        }

        return scene;
    }
}

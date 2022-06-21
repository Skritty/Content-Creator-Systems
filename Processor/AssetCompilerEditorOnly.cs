using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using AsImpL;
using System.IO;
using LightBuzz.Archiver;
using Amazon.S3.Model;
using UnityEngine.UI;
using Amazon.Util;
using SimpleJSON;
using System.IO.Compression;
using UnityEditor.SceneManagement;
using Unity.EditorCoroutines.Editor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEditor.Compilation;

[InitializeOnLoad]
public class AssetCompilerEditorOnly
{
    public static AssetCompilerEditorOnly refrence; 
    [SerializeField]
    public string projPath;
    public ProjectManager projManager;

    public string projFile;
    public string sceneName;
    public string scenePath;
    [SerializeField]
    private string finalPath;
    [SerializeField]
    public bool run;

    public S3Manage manager;
    private const string awsBucketName = "aireal-realitystudio";
    private const string awsAccessKey = "AKIASJE2LZECD26ZMS7M";
    private const string awsSecretKey = "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";

    // Paths
    string downloadPath = Application.dataPath + "/TempDownload";
    string extractPath = Application.dataPath + "/TempExtracted";
    string defaultScenePath = "Assets/Scenes/DefaultScene.unity";
    string localBuildPath = "ServerData/[BuildTarget]";
    string downloadFilePath = "";

    // Process variables
    List<S3Object> objs = new List<S3Object>();
    private string filename;
    private string currentProjectPath;
    string[] scenes;
    int currentScene = 0;
    List<Transform> sceneObjects = new List<Transform>();
    Dictionary<string, Material> materials = new Dictionary<string, Material>();
    Dictionary<string, string> texturePaths = new Dictionary<string, string>();
    int totalFilesToUpload = 0;
    int filesUploaded = 0;

    // Process update timer
    [SerializeField] float updateDelay = 1f;
    private float timer = 0f;
    private float totalTimeForDelta = 0;

    enum ProcessState { Off, Waiting, Watch, Download, Unzip, Load, SaveScene, UnwrapUVs, BakeLightmaps, WaitForBake, CreateAssetBundle, Upload, Clean}
    ProcessState processState = ProcessState.Off;

    [MenuItem("Processor/Start")]
    private static void EnableProcessor()
    {
        AssetCompilerEditorOnly.refrence.processState = ProcessState.Watch;
    }

    [MenuItem("Processor/Stop")]
    private static void DisableProcessor()
    {
        if(Lightmapping.isRunning)
            Lightmapping.ForceStop();
        AssetCompilerEditorOnly.refrence.processState = ProcessState.Clean;
    }

    static AssetCompilerEditorOnly()
    {
        EditorApplication.update += Update;
        if (AssetCompilerEditorOnly.refrence == null)
        {
            AssetCompilerEditorOnly.refrence = new AssetCompilerEditorOnly();
        }
    }

    public AssetCompilerEditorOnly()
    {
        manager = S3Manage.Instance;
    }

    private static void Update() 
    {
        AssetCompilerEditorOnly.refrence.RunProcess();
    }

    private void RunProcess()
    {
        float deltaTime = (float)EditorApplication.timeSinceStartup - totalTimeForDelta;
        totalTimeForDelta = (float)EditorApplication.timeSinceStartup;
        if ((timer += deltaTime) < updateDelay) return;
        else timer %= updateDelay;

        if (Lightmapping.isRunning)
            Debug.Log($"Bake {(ftRenderLightmap.progressBarPercent).ToString("n2")}% Complete");
        else
            Debug.Log($"Process State: {processState}");
        try
        {
            switch (processState)
            {
                case ProcessState.Watch:
                    Watch();
                    break;
                case ProcessState.Download:
                    Download();
                    break;
                case ProcessState.Unzip:
                    Unzip();
                    break;
                case ProcessState.Load:
                    Load();
                    break;
                case ProcessState.SaveScene:
                    PrepareAndSave();
                    break;
                case ProcessState.UnwrapUVs:
                    //UnwrapUVs();
                    break;
                case ProcessState.BakeLightmaps:
                    BakeLightmaps();
                    break;
                case ProcessState.CreateAssetBundle:
                    BundleAssets();
                    break;
                case ProcessState.Upload:
                    UploadBundle();
                    break;
                case ProcessState.Clean:
                    ClearTempStorage();
                    break;
            }
        }
        catch(System.Exception ex)
        {
            processState = ProcessState.Off;
            EditorSceneManager.OpenScene(defaultScenePath, OpenSceneMode.Single);
            ClearTempStorage();
            throw;
        }
        
    }

    private void Watch()
    {
        _ = S3Manage.ListObjectsBucket(awsBucketName, (result) =>
          {
              if (result.S3Objects.Count > 1)
            {
                  objs = result.S3Objects;
                  processState = ProcessState.Download;
              }
              else
              {
                  processState = ProcessState.Watch;
              }
          });
        processState = ProcessState.Waiting;
    }

    private void Download()
    {
        S3Object obj = objs[1];
        filename = obj.Key.Replace("CC/", "");
        downloadFilePath = downloadPath + '/'+ filename;
        
        if (!Directory.Exists(downloadPath))
            Directory.CreateDirectory(downloadPath);
        if (!Directory.Exists(extractPath))
            Directory.CreateDirectory(extractPath);

        //Pull object from aws bucket
        _ = S3Manage.GetObjectFromBucket(awsBucketName, obj.Key, (result) =>
        {
            //Write data to .zip file
            using (FileStream fs = File.Create(downloadFilePath))
            {
                byte[] buffer = new byte[81920];
                int count;
                while ((count = result.ResponseStream.Read(buffer, 0, buffer.Length)) != 0)
                    fs.Write(buffer, 0, count);
                fs.Flush();
            }
            
            processState = ProcessState.Unzip;

        });
        processState = ProcessState.Waiting;
    }

    private void Unzip()
    {
        if (!Directory.Exists(extractPath + '/' + Path.GetFileNameWithoutExtension(filename))) 
            Directory.CreateDirectory(extractPath + '/' + Path.GetFileNameWithoutExtension(filename));

        Archiver.Decompress(downloadFilePath, extractPath + '/' + Path.GetFileNameWithoutExtension(filename));
        currentProjectPath = extractPath + '/' + Path.GetFileNameWithoutExtension(filename);

        scenes = Directory.GetFiles(currentProjectPath, "*.json");
        currentScene = 0;
        processState = ProcessState.Load;
    }

    private void Load()
    {
        EditorSceneManager.OpenScene(defaultScenePath, OpenSceneMode.Single);
        AssetDatabase.Refresh();

        sceneObjects.Clear();
        texturePaths.Clear();
        materials.Clear();

        JsonSceneManagement.LoadJsonSceneEditor(sceneObjects, texturePaths, materials, scenes[currentScene], Path.GetFileName(currentProjectPath));
        processState = ProcessState.SaveScene;
    }

    private void PrepareAndSave()
    {
        if (!Directory.Exists(currentProjectPath + "/Materials"))
            Directory.CreateDirectory(currentProjectPath + "/Materials");

        // Create material assets
        List<Material> newMats = new List<Material>();
        foreach(MeshRenderer r in GameObject.FindObjectsOfType<MeshRenderer>())
        {
            for (int i = 0; i < r.sharedMaterials.Length; i++)
            {
                Material m = r.sharedMaterials[i];
                m.name = "" + m.GetInstanceID();
                if (m.mainTexture != null)
                {
                    foreach (KeyValuePair<string, string> p in texturePaths) Debug.Log(m.mainTexture.name + " " + p.Key + "|" + p.Value);
                    string path = "Assets/TempExtracted/" + Path.GetFileName(currentProjectPath) + "/Textures/" + Path.GetFileName(texturePaths[m.mainTexture.name]);
                    var texture = AssetDatabase.LoadMainAssetAtPath(path);
                    m.mainTexture = (Texture)texture;

                }

                if (!newMats.Contains(m))
                    newMats.Add(m);
            }
        }    
        foreach (Material m in newMats)
        {
            if (!AssetDatabase.Contains(m))
                AssetDatabase.CreateAsset(m, $"Assets/TempExtracted/{Path.GetFileName(currentProjectPath)}/Materials/{m.GetInstanceID()}.mat");
        }

        // Create mesh assets
        foreach(MeshFilter mf in GameObject.FindObjectsOfType<MeshFilter>())
        {
            Mesh mesh = mf.sharedMesh;
            if (!AssetDatabase.Contains(mesh))
                AssetDatabase.CreateAsset(mesh, $"Assets/TempExtracted/{Path.GetFileName(currentProjectPath)}/Meshes/{mesh.GetInstanceID()}.asset");
        }

        StaticEditorFlags flags = StaticEditorFlags.ContributeGI | StaticEditorFlags.ReflectionProbeStatic;
        foreach(Transform obj in sceneObjects)
        {
            DoFunctionToTree(obj, o => {
                GameObjectUtility.SetStaticEditorFlags(o.gameObject, flags);
                });
        }
        foreach (Transform obj in sceneObjects)
        {
            DoFunctionToTree(obj, o => {
                MeshFilter mf = o.GetComponent<MeshFilter>();
                if (mf != null)
                {
                    Mesh meshCopy = Mesh.Instantiate(mf.sharedMesh) as Mesh;
                    mf.mesh = meshCopy;
                    Unwrapping.GenerateSecondaryUVSet(meshCopy);
                }
            });
        }
        List<Light> lights = new List<Light>();
        foreach(Light l in Object.FindObjectsOfType<Light>())
        {
            lights.Add(l);
            l.lightmapBakeType = LightmapBakeType.Baked;
            BakeryPointLight pl;
            BakeryDirectLight dl;
            switch (l.type)
            {
                case LightType.Point:
                    pl = l.gameObject.AddComponent<BakeryPointLight>();
                    pl.MatchToRealTime();
                    pl.samples = 64;
                    pl.shadowSpread = 0.25f;
                    break;
                case LightType.Spot:
                    pl = l.gameObject.AddComponent<BakeryPointLight>();
                    pl.MatchToRealTime();
                    pl.projMode = BakeryPointLight.ftLightProjectionMode.Cone;
                    pl.samples = 64;
                    pl.shadowSpread = 0.25f;
                    break;
                case LightType.Directional:
                    l.gameObject.AddComponent<BakeryDirectLight>();
                    break;
            }
        }
        for (int i = 0; i < lights.Count; i++)
        {
            Object.DestroyImmediate(lights[i]);
        }
            

        scenePath = currentProjectPath + "/Scenes/" + Path.GetFileNameWithoutExtension(scenes[currentScene]) + ".unity";
        if (!Directory.Exists(currentProjectPath + "/Scenes"))
            Directory.CreateDirectory(currentProjectPath + "/Scenes");

        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), scenePath);
        AssetDatabase.Refresh();
        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        processState = ProcessState.BakeLightmaps;
    }

    private void DoFunctionToTree(Transform root, System.Action<Transform> function)
    {
        function.Invoke(root);
        foreach (Transform child in root)
            DoFunctionToTree(child, function);
    }

    private void BakeLightmaps()
    {
        ftRenderLightmap.ValidateOutputPath();
        var bakeryRuntimePath = ftLightmaps.GetRuntimePath();
        var gstorage = AssetDatabase.LoadAssetAtPath(bakeryRuntimePath + "ftGlobalStorage.asset", typeof(ftGlobalStorage)) as ftGlobalStorage;

        if (gstorage == null)
        {
            Debug.Log("Bakery is not initalized");
            return;
        }

        Lightmapping.ClearLightingDataAsset();
        Lightmapping.ClearDiskCache();
        Lightmapping.Clear();
        var storage = ftRenderLightmap.FindRenderSettingsStorage();
        ftRenderLightmap bakery = ftRenderLightmap.instance != null ? ftRenderLightmap.instance : new ftRenderLightmap();
        ftLightmapsStorage.CopySettings(gstorage, storage);
        EditorUtility.SetDirty(storage);
        bakery.LoadRenderSettings();
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        Debug.Log("Default settings loaded");
        bakery.RenderButton(false);
        
        ftRenderLightmap.OnFinishedFullRender += BakeFinished;

        //Lightmapping.lightingSettings = AssetDatabase.LoadAssetAtPath<LightingSettings>("Assets/Lighting Settings.lighting");
        //if (Lightmapping.BakeAsync())
        //{
        //    Debug.Log("Bake Started");
        //    Lightmapping.bakeCompleted += BakeFinished;
        //}
        //else
        //{
        //    Debug.Log("Bake Failed");
        //    processState = ProcessState.Off;
        //}

        processState = ProcessState.Waiting;
    }

    private void BakeFinished(object sender, System.EventArgs e)
    {
        AddAssetToGroup(scenePath.Replace(currentProjectPath, "Assets/TempExtracted/" + Path.GetFileName(currentProjectPath)), Path.GetFileName(currentProjectPath));
        sceneObjects.Clear();
        Debug.Log("Successfully Baked");
        //Check to see if there are more scenes, if so, go load them and bake them  
        if (currentScene < scenes.Length - 1)
        {
            currentScene++;
            processState = ProcessState.Load;
        }
        else
        {
            EditorSceneManager.OpenScene(defaultScenePath, OpenSceneMode.Single);
            processState = ProcessState.CreateAssetBundle;
        }
    }

    private void BundleAssets()
    {
        string bundlePath = extractPath + "/" + Path.GetFileName(currentProjectPath) + "/Bundles";
        if (!Directory.Exists(bundlePath))
            Directory.CreateDirectory(bundlePath);

        AddressableAssetSettingsDefaultObject.Settings.OverridePlayerVersion = Path.GetFileNameWithoutExtension(scenePath);
        AddressableAssetSettings.BuildPlayerContent();
        //AddressableAssetSettingsDefaultObject.Settings.OverridePlayerVersion = null;
        processState = ProcessState.Upload;
    }

    public void AddAssetToGroup(string path, string groupName)
    {
        var group = AddressableAssetSettingsDefaultObject.Settings.FindGroup(groupName);
        if (!group)
        {
            group = AddressableAssetSettingsDefaultObject.Settings.CreateGroup(groupName, false, false, true, null, typeof(SceneAsset));
            BundledAssetGroupSchema schema = group.AddSchema<BundledAssetGroupSchema>();
            schema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogether;
            schema.BundleNaming = BundledAssetGroupSchema.BundleNamingStyle.NoHash;
            schema.LoadPath.SetVariableByName(AddressableAssetSettingsDefaultObject.Settings, "RemoteLoadPath");
            //group.Settings.profileSettings.SetValue(group.Settings.activeProfileId, "RemoteBuildPath", localBuildPath);
            //group.Settings.profileSettings.SetValue(group.Settings.activeProfileId, "RemoteLoadPath", localBuildPath);
        }
        var entry = AddressableAssetSettingsDefaultObject.Settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(path), group);
        entry.SetLabel("scene", true);
    }

    private void UploadBundle()
    {
        string bundlePath = Application.dataPath.Replace("/Assets","") + "/ServerData/" + EditorUserBuildSettings.activeBuildTarget;
        string[] bundles = Directory.GetFiles(bundlePath);
        totalFilesToUpload = bundles.Length;
        filesUploaded = 0;
        foreach (string bundle in bundles)
        {
            S3Manage.UploadObjectToBucket(bundle, awsBucketName, "App/" + EditorUserBuildSettings.activeBuildTarget + "/" + Path.GetFileName(bundle), (result, error) => { filesUploaded++; }).Wait(1000);
        }
        processState = ProcessState.Clean;
    }

    private void DeleteFileAfterUpload(string path)
    {
        if (File.Exists(path))
            File.Delete(path);
    }

    private void ClearTempStorage()
    {
        if (filesUploaded != totalFilesToUpload) return;

        // Clear downloaded zips
        if (File.Exists(downloadFilePath))
            File.Delete(downloadFilePath);

        // Clear project folders
        if (Directory.Exists(extractPath))
            Directory.Delete(extractPath, true);

        // Clear ServerData folder
        if (Directory.Exists(Application.dataPath.Replace("/Assets", "") + "/ServerData"))
            Directory.Delete(Application.dataPath.Replace("/Assets", "") + "/ServerData", true);

        // Delete Schema and Group
        AddressableAssetSettingsDefaultObject.Settings.RemoveGroup(AddressableAssetSettingsDefaultObject.Settings.FindGroup(g => g.Name == Path.GetFileName(currentProjectPath)));
        File.Delete($"{AddressableAssetSettingsDefaultObject.Settings.GroupSchemaFolder}/{Path.GetFileName(currentProjectPath)}_BundledAssetGroupSchema.asset");

        // Delete zip from s3
        if(objs.Count > 0)
            _ = S3Manage.DeleteObjectOnBucket(objs[1].Key, awsBucketName, (result) => {
            processState = ProcessState.Watch;
        });
        processState = ProcessState.Waiting;

        AssetDatabase.Refresh();
    }

    #region Testing
    [MenuItem("Test/ImportAssetBundles")]
    public static void ImportBundles()
    {
        string bundlePath = @"C:\Users\trevo\Documents\GitHub\RealityStudioProcessorPlugin\ServerData\iOS\contentcreatorproject_scenes_all_24aa239eb8061d9f2398676fa2e94d82.bundle";
        Debug.Log(Application.streamingAssetsPath);
        Debug.Log(bundlePath);
        //AssetBundle myLoadedAssetBundle = AssetBundle.LoadFromFile(bundlePath);
        //Debug.Log(myLoadedAssetBundle.isStreamedSceneAssetBundle);
        /*if (myLoadedAssetBundle == null)
        {
            Debug.Log("Failed to load AssetBundle!");
            return;
        }*/
        SceneAsset scene = Resources.Load("ContentCreatorProject/Scenes/plswork") as SceneAsset;
        EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(Resources.Load("ContentCreatorProject/Scenes/plswork")));//myLoadedAssetBundle.GetAllScenePaths()[0]);
        //myLoadedAssetBundle.Unload(false);
    }
    [MenuItem("Test/BuildSceneLighting")]
    public static void BuildLighting()
    {
        Lightmapping.lightingSettings = AssetDatabase.LoadAssetAtPath<LightingSettings>("Assets/Lighting Settings.lighting");
        Lightmapping.Bake();
    }
    [MenuItem("Test/AddToGroup")]
    public static void AddObjToGroup()
    {
        string path = "Assets/TempExtracted/ContentCreatorProject/Scenes";
        string groupName = "Test";
        Debug.Log(groupName);
        Debug.Log(path + " | " + AssetDatabase.AssetPathToGUID(path));
        var group = AddressableAssetSettingsDefaultObject.Settings.FindGroup(groupName);
        if (!group)
        {
            group = AddressableAssetSettingsDefaultObject.Settings.CreateGroup(groupName, false, false, true, null, typeof(SceneAsset));
        }
        var entry = AddressableAssetSettingsDefaultObject.Settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(path), group);
    }
    [MenuItem("Test/StartProcessFromLoad")]
    public static void StartProcessFromLoad()
    {
        AssetCompilerEditorOnly.refrence.processState = ProcessState.Load;
    }
    #endregion
}
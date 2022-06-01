using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using SimpleJSON;
using TriLibCore.Samples;
using LightBuzz.Archiver;
using TMPro;
using TriLibCore.Extensions;
using TriLibCore;
using RuntimeInspectorNamespace;
using System.Linq;
using HighlightPlus;

public partial class ProjectManager : MonoBehaviour
{
    private void CreateNewProject(string path)
    {
        //if (Directory.GetFiles(path, "*.json").Length > 0 && Path.GetFileNameWithoutExtension(Directory.GetFiles(path, "*.json")[0]) != Path.GetFileName(path)) return;
        initialProjectSelectionPanel?.SetActive(false);

        if (!Directory.Exists(path)) return;
        //Directory.CreateDirectory(path);
        if (scenePath != "") SaveScene(scenePath);

        projectDirectory = new DirectoryInfo(path);
        textureDirectory = Directory.CreateDirectory(path + "/Textures");
        meshDirectory = Directory.CreateDirectory(path + "/Meshes");

        scenePath = $"{path}/{Path.GetFileName(path)}.json";
        File.Create(scenePath);
        currentScene = Path.GetFileNameWithoutExtension(scenePath);
        RecentProjectsUIManager.Instance.UpdateRecentProjects(path);

        foreach (Transform o in sceneObjects) Destroy(o.gameObject);
        sceneObjects.Clear();

        pseudosceneRoot.SceneName = currentScene;
        GameObject awaken = new GameObject();
        awaken.transform.parent = pseudosceneRoot.transform;
        Destroy(awaken);
        //hierarchy.DeleteAllPseudoScenes();
        //hierarchy.CreatePseudoScene(currentScene);

        sceneObjects.Add(Instantiate(mainLight).transform);
        ActionLog.Log($"Created new project {currentScene}");
    }

    public void AddObjectToScene(Transform obj)
    {
        obj.transform.parent = pseudosceneRoot.transform;
        sceneObjects.Add(obj);
    }

    public void RemoveObject(Transform obj)
    {
        sceneObjects.Remove(obj);
        //hierarchy.RemoveFromPseudoScene(currentScene, obj, false);
    }

    public void SaveScene(string path)
    {
        saving = true;
        RecentProjectsUIManager.Instance.TakeThumbnailScreenshot(Camera.main);
        foreach (Transform t in sceneObjects)
            t.DoFunctionToTree(x => DestroyImmediate(x.GetComponent<ObjData>()));
        foreach (Transform t in sceneObjects.Select(x => { x.parent = null; return x; }))
            DynamicSaveLoad.AddToSaveGroup("Scene", t);
        DynamicSaveLoad.AddToSaveGroup("Presets", PresetLibrary.Instance.PresetContainer);
        DynamicSaveLoad.Save(path);
        //JSONObject scene = JsonSceneManagement.SceneToJson(sceneObjects, references, path);
        //File.WriteAllText(path, scene.ToString());
        saving = false;
        ActionLog.Log("Project saved!");
    }

    public IEnumerator LoadProject(string path)
    {
        initialProjectSelectionPanel?.SetActive(false);

        //loadingPanel.SetActive(true);
        //RTG.RTFocusCamera.Get.LoadingUIActive = true;

        if (Directory.GetFiles(path, "*.json").Length == 0 || (Directory.GetFiles(path, "*.json").Length > 0 && Path.GetFileNameWithoutExtension(Directory.GetFiles(path, "*.json")[0]) != Path.GetFileName(path)))
        {
            ActionLog.Log($"Invalid Project Folder");
            DisableLoadingPanel();
            yield break;
        }
        if (scenePath != "") SaveScene(scenePath);

        projectDirectory = new DirectoryInfo(path);
        textureDirectory = new DirectoryInfo(path + "/Textures");
        meshDirectory = new DirectoryInfo(path + "/Meshes");

        scenePath = $"{path}/{Path.GetFileName(path)}.json";
        currentScene = Path.GetFileNameWithoutExtension(scenePath);
        RecentProjectsUIManager.Instance.UpdateRecentProjects(path);

        foreach (Transform o in sceneObjects) Destroy(o.gameObject);
        sceneObjects.Clear();
        pseudosceneRoot.SceneName = currentScene;
        GameObject awaken = new GameObject();
        awaken.transform.parent = pseudosceneRoot.transform;
        Destroy(awaken);
        hierarchy.DeleteAllPseudoScenes();
        hierarchy.CreatePseudoScene(currentScene);

        loading = true;
        DynamicSaveLoad.Load(scenePath);
        //PresetLibrary.Instance.PresetContainer = DynamicSaveLoad.LoadObjectGroup<PresetLibrary.Presets>("Presets")[0];
        sceneObjects.AddRange(DynamicSaveLoad.LoadObjectGroup<Transform>("Scene"));
        ////references = new JsonSceneManagement.JSONReferences();
        //foreach (Transform o in sceneObjects) GameObject.Destroy(o.gameObject);
        //sceneObjects.Clear();
        //JsonSceneManagement.doneLoading += FinishLoadProject;
        //StartCoroutine(JsonSceneManagement.LoadJsonScene(sceneObjects, scenePath, projectDirectory.FullName, references));
        loading = false;
    }

    private void FinishLoadProject()
    {
        JsonSceneManagement.doneLoading -= FinishLoadProject;
        loading = false;
        foreach (Transform obj in sceneObjects)
            obj.parent = pseudosceneRoot.transform;
        //hierarchy.AddToPseudoScene(currentScene, obj);
        gizmos.DisableGizmos();
        if (!GameObject.FindGameObjectWithTag("MainLight"))
            sceneObjects.Add(Instantiate(mainLight).transform);
        else GameObject.FindGameObjectWithTag("MainLight").transform.parent = null;

        ActionLog.Log($"Project {currentScene} loaded");
    }
}

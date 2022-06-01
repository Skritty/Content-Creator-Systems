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
using TriLibCore.SFB;

public partial class ProjectManager : MonoBehaviour
{
    public void NewProject()
    {
        //FileBrowser.SetFilters(false, ".CCProject");
        //FileBrowser.ShowSaveDialog((path) => { TutorialManager.Instance.OnNewProject.Invoke(); CreateNewProject(path[0].Replace(".ccproject", "")); }, null,
        //FileBrowser.PickMode.Files, false, "", "", "Pick Project Location", "Create Project");
        IList<ItemWithStream> paths = StandaloneFileBrowser.OpenFolderPanel("Pick Project Location", "", false);
        if (paths.Count > 0 && Directory.Exists(paths[0].Name))
        {
            TutorialManager.Instance.OnNewProject.Invoke(); 
            CreateNewProject(paths[0].Name.Replace(".ccproject", ""));
        }
    }

    public void OpenProject()
    {
        //FileBrowser.SetFilters(false, ".json");
        //FileBrowser.ShowLoadDialog((path) => { if (Directory.Exists(path[0])) { StartCoroutine(LoadProject(path[0])); } }, null,
        //FileBrowser.PickMode.Folders, false, "", null, "Load Project", "Load");
        IList<ItemWithStream> paths = StandaloneFileBrowser.OpenFolderPanel("Load Project", "", false);
        if (paths.Count > 0 && Directory.Exists(paths[0].Name))
        {
            StartCoroutine(LoadProject(paths[0].Name));
        }
    }

    public void SaveProject()
    {
        if (scenePath != "") SaveScene(scenePath);
        //else SaveProjectAs();
    }

    public void SaveProjectAs()
    {
        //FileBrowser.SetFilters(false, ".json");
        //FileBrowser.ShowSaveDialog((path) => { SaveScene(path[0]); }, null,
        //FileBrowser.PickMode.Files, false, projectDirectory.FullName, "MyScene", "Save Scene File", "Save");
        ItemWithStream paths = StandaloneFileBrowser.SaveFilePanel("Save As", "", currentScene, ".json");
        if (paths != null && paths.Name != "" && Path.GetFileName(paths.Name) != "")
        {
            SaveScene(paths.Name);
        }
    }

    public void UploadProject()
    {
        SaveProject();
        string zipPath = Application.persistentDataPath + Path.GetFileName(projectDirectory.FullName) + ".zip";
        Debug.Log($"Compressing {projectDirectory.FullName} to {zipPath}");
        Archiver.Compress(projectDirectory.FullName, zipPath);
        Debug.Log("Compressed");
        ActionLog.Log("Project uploading...");
        S3Manage.Instance.UploadObjectForBucket(zipPath, awsBucketName, awsZipPath + Path.GetFileName(projectDirectory.FullName) + ".zip", (response, str) => {
            Debug.Log("Upload Complete");
            ActionLog.Log("Upload complete");
            if (File.Exists(zipPath))
                File.Delete(zipPath);
        });
    }

    public void CloseProject(bool save)
    {
        if (save && scenePath != "") SaveScene(scenePath);

        scenePath = "";
        currentScene = "";

        foreach (Transform o in sceneObjects) Destroy(o.gameObject);
        sceneObjects.Clear();

        pseudosceneRoot.SceneName = currentScene;
    }

    public void CreateNewMaterial()
    {
        if (SelectionManager.Instance.IsMultiselecting)
        {
            foreach (Transform obj in SelectionManager.Instance.SelectedTransforms)
            {
                MeshRenderer mr = obj.transform.GetComponent<MeshRenderer>();
                if (mr == null) return;
                Material newMat = new Material(mr.sharedMaterial);
                newMat.name = "CCAsset-" + +mr.material.GetInstanceID();
                Material[] newMats = new Material[mr.sharedMaterials.Length];
                for (int i = 0; i < mr.sharedMaterials.Length; i++)
                {
                    newMats[i] = newMat;
                    /*MaterialPropertyBlock properties = new MaterialPropertyBlock();
                    mr.GetPropertyBlock(properties);
                    mr.SetPropertyBlock(properties, i);*/
                }
                mr.sharedMaterials = newMats;
                ActionLog.Log("Made material unique");
            }
        }
        else
        {
            MeshRenderer mr = hierarchy.CurrentSelection.GetComponent<MeshRenderer>();
            if (mr == null) return;
            Material newMat = new Material(mr.sharedMaterial);
            newMat.name = "CCAsset-" + +mr.material.GetInstanceID();
            Material[] newMats = new Material[mr.sharedMaterials.Length];
            for (int i = 0; i < mr.sharedMaterials.Length; i++)
            {
                newMats[i] = newMat;
                /*MaterialPropertyBlock properties = new MaterialPropertyBlock();
                mr.GetPropertyBlock(properties);
                mr.SetPropertyBlock(properties, i);*/
            }
            mr.sharedMaterials = newMats;
            ActionLog.Log("Made material unique");
        }
    }

    public void DuplicateSelected()
    {
        if (SelectionManager.Instance.IsMultiselecting)
        {
            List<Transform> toSelect = new List<Transform>();
            foreach (Transform obj in SelectionManager.Instance.SelectedTransforms)
            {
                Transform t = obj.transform;
                GameObject copy = Instantiate(t.gameObject);
                copy.name = t.gameObject.name;
                copy.transform.parent = isAdvancedMode ? t.parent : null;
                copy.transform.position = t.position;
                copy.transform.rotation = t.rotation;
                copy.transform.localScale = t.localScale;
                if (copy.transform.parent == pseudosceneRoot.transform) AddObjectToScene(copy.transform);
                toSelect.Add(t);
                toSelect.Add(copy.transform);
                ActionLog.Log("Duplicated selected object");
            }
            toSelect[0].LogState(() => toSelect,
                (v) =>
                {
                    foreach (Transform t in toSelect.ToArray()) Delete(t);
                });
            foreach (Transform t in toSelect)
                SelectionManager.Instance.Select(t);
        }
        else if (hierarchy.CurrentSelection)
        {
            GameObject copy = Instantiate(hierarchy.CurrentSelection.gameObject);
            copy.name = hierarchy.CurrentSelection.gameObject.name;
            copy.transform.parent = isAdvancedMode ? hierarchy.CurrentSelection.parent : null;
            copy.transform.position = hierarchy.CurrentSelection.position;
            copy.transform.rotation = hierarchy.CurrentSelection.rotation;
            copy.transform.localScale = hierarchy.CurrentSelection.localScale;
            if (copy.transform.parent == pseudosceneRoot.transform) AddObjectToScene(copy.transform);
            SelectionManager.Instance.Select(copy.transform);
            copy.LogState(() => copy, (v) => Delete(((GameObject)v).transform));
            ActionLog.Log("Duplicated selected object");
        }
    }

    public void DeleteSelected()
    {
        Dictionary<Transform, Transform> temp = SelectionManager.Instance.SelectedTransforms.ToDictionary(x => x, x => x.parent);
        if (temp.Count == 0) return;
        ChangeHistory.LogState(
            () => 
            {
                Debug.Log("Delete Undo Undone");
                foreach (KeyValuePair<Transform, Transform> t in temp)
                    if (t.Key)
                    {
                        t.Key.gameObject.SetActive(true);
                        sceneObjects.Add(t.Key);
                        t.Key.parent = t.Value;
                        deletedObjects = new Queue<Transform>(deletedObjects.Where(x => x == t.Key));
                        SelectionManager.Instance.DoMultiselect = true;
                        SelectionManager.Instance.Select(t.Key);
                    }
            },
            () => 
            {
                foreach (KeyValuePair<Transform, Transform> t in temp)
                    Delete(t.Key);
            }
        );

        foreach (KeyValuePair <Transform, Transform> t in temp)
        {
            Delete(t.Key);
        }

        RefreshInspector();
    }

    /// <summary>
    /// Places the object in the deletion queue and removes its interactablity. Does not contain undo/redo functionality!
    /// </summary>
    /// <param name="obj">The object to delete</param>
    private void Delete(Transform obj)
    {
        if (obj == null) return;

        obj.gameObject.SetActive(false);
        sceneObjects.Remove(obj);
        obj.parent = null;
        SelectionManager.Instance.Deselect();

        deletedObjects.Enqueue(obj);

        // Actually delete anything past the queue limit
        if (deletedObjects.Count > maxDeletionUndoQueue)
            Destroy(deletedObjects.Dequeue().gameObject);

        ActionLog.Log($"Deleted {obj.gameObject} object");
    }

    public void CreateEmptyObject()
    {
        if (SelectionManager.Instance.IsMultiselecting)
        {
            GameObject newObj = new GameObject("Pivot");
            newObj.transform.localPosition = Vector3.zero;
            newObj.AddComponent<PivotDummy>();
            ObjData.CreateObjData(newObj.transform, null);
            if (newObj.transform.parent == null) AddObjectToScene(newObj.transform);

            newObj.LogState(() => newObj, (v) => Delete(v));
            ActionLog.Log("Created Pivot");

            List<Transform> toSelect = new List<Transform>();
            foreach (Transform obj in SelectionManager.Instance.SelectedTransforms)
            {
                obj.parent = newObj.transform;
                toSelect.Add(obj.transform);
            }
            foreach (Transform t in toSelect)
                SelectionManager.Instance.Select(t);
            SelectionManager.Instance.Select(newObj.transform);
        }
        else
        {
            GameObject newObj = new GameObject("Pivot");
            if (hierarchy.CurrentSelection) newObj.transform.parent = isAdvancedMode ? hierarchy.CurrentSelection : null;
            newObj.transform.localPosition = Vector3.zero;
            newObj.AddComponent<PivotDummy>();
            ObjData.CreateObjData(newObj.transform, null);
            if (newObj.transform.parent == null) AddObjectToScene(newObj.transform);
            SelectionManager.Instance.Select(newObj.transform);
            newObj.LogState(() => newObj, (v) => Delete(v));
            ActionLog.Log("Created Pivot");
        }
    }

    public void CreateViewSpot()
    {
        if (SelectionManager.Instance.IsMultiselecting)
        {
            int i = 1;
            while (GameObject.Find("View " + i)) i++;
            GameObject newObj = new GameObject("View " + i);
            newObj.transform.localPosition = Vector3.zero;
            newObj.tag = "View";
            newObj.AddComponent<ViewDummy>();
            ObjData.CreateObjData(newObj.transform, null);
            if (newObj.transform.parent == null) AddObjectToScene(newObj.transform);
            SelectionManager.Instance.Select(newObj.transform);
            newObj.LogState(() => newObj, (v) => Delete(v));
            ActionLog.Log("Created View");
        }
        else
        {
            int i = 1;
            while (GameObject.Find("View " + i)) i++;
            GameObject newObj = new GameObject("View " + i);
            if (hierarchy.CurrentSelection) newObj.transform.parent = isAdvancedMode ? hierarchy.CurrentSelection : null;
            newObj.transform.localPosition = Vector3.zero;
            newObj.tag = "View";
            newObj.AddComponent<ViewDummy>();
            ObjData.CreateObjData(newObj.transform, null);
            if (newObj.transform.parent == null) AddObjectToScene(newObj.transform);
            SelectionManager.Instance.Select(newObj.transform);
            newObj.LogState(() => newObj, (v) => Delete(v));
            ActionLog.Log("Created View");
        }
    }

    public void CreateSimpleLight()
    {
        if (SelectionManager.Instance.IsMultiselecting)
        {
            GameObject newObj = Instantiate(defaultLight);
            newObj.name = "Light";
            newObj.transform.localPosition = Vector3.zero;
            ObjData.CreateObjData(newObj.transform, null);
            if (newObj.transform.parent == null) AddObjectToScene(newObj.transform);
            SelectionManager.Instance.Select(newObj.transform);
            newObj.LogState(() => newObj, (v) => Delete(v));
            ActionLog.Log("Created Light");
        }
        else
        {
            GameObject newObj = Instantiate(defaultLight);
            newObj.name = "Light";
            if (hierarchy.CurrentSelection) newObj.transform.parent = isAdvancedMode ? hierarchy.CurrentSelection : null;
            newObj.transform.localPosition = Vector3.zero;
            newObj.transform.Rotate(Vector3.right, 90f);
            ObjData.CreateObjData(newObj.transform, null);
            if (newObj.transform.parent == null) AddObjectToScene(newObj.transform);
            SelectionManager.Instance.Select(newObj.transform);
            newObj.LogState(() => newObj, (v) => Delete(v));
            ActionLog.Log("Created Light");
        }
    }

    public void CreateReflectionProbe()
    {
        if (SelectionManager.Instance.IsMultiselecting)
        {
            GameObject newObj = Instantiate(defaultReflectionProbe);
            newObj.transform.localPosition = Vector3.zero;
            ObjData.CreateObjData(newObj.transform, null);
            if (newObj.transform.parent == null) AddObjectToScene(newObj.transform);
            SelectionManager.Instance.Select(newObj.transform);
            newObj.LogState(() => newObj, (v) => Delete(v));
            ActionLog.Log("Created Reflection Probe");
        }
        else
        {
            GameObject newObj = Instantiate(defaultReflectionProbe);
            if (hierarchy.CurrentSelection) newObj.transform.parent = isAdvancedMode ? hierarchy.CurrentSelection : null;
            newObj.transform.localPosition = Vector3.zero;
            ObjData.CreateObjData(newObj.transform, null);
            if (newObj.transform.parent == null) AddObjectToScene(newObj.transform);
            SelectionManager.Instance.Select(newObj.transform);
            newObj.LogState(() => newObj, (v) => Delete(v));
            ActionLog.Log("Created Reflection Probe");
        }
    }

    public void ToggleMainLight()
    {
        GameObject obj = GameObject.FindGameObjectWithTag("MainLight");
        if (obj == null)
        {
            mainLightButtonOn.SetActive(false);
            mainLightButtonOff.SetActive(true);
            return;
        }
        ActionLog.Log("Toggled Main Light");
        bool on = !obj.GetComponent<Light>().enabled;
        obj.GetComponent<Light>().enabled = on;
        mainLightButtonOn.SetActive(on);
        mainLightButtonOff.SetActive(!on);
    }
    public void ToggleMainLight(bool on)
    {
        GameObject obj = GameObject.FindGameObjectWithTag("MainLight");
        if (obj == null)
        {
            mainLightButtonOn.SetActive(false);
            mainLightButtonOff.SetActive(true);
            return;
        }
        ActionLog.Log("Toggled Main Light");
        mainLightButtonOn.SetActive(on);
        mainLightButtonOff.SetActive(!on);
        obj.GetComponent<Light>().enabled = on;
    }

    public void TogglePersonForScale()
    {
        if (scalePerson == null)
        {
            personButtonOn.SetActive(false);
            personButtonOff.SetActive(true);
            return;
        }
        ActionLog.Log("Toggled Scale Person");
        bool on = !scalePerson.activeSelf;
        scalePerson.SetActive(on);
        personButtonOn.SetActive(on);
        personButtonOff.SetActive(!on);
    }

    public void ToggleAdvancedMode()
    {
        isAdvancedMode = !isAdvancedMode;
        RefreshInspector();
    }

    public void ToggleSurfaceSnapping()
    {
        surfaceSnapping = !surfaceSnapping;
        surfaceSnapOff.SetActive(!surfaceSnapping);
        surfaceSnapOn.SetActive(surfaceSnapping);
    }

    public void DetachSubmesh()
    {
        if (SelectionManager.Instance.mode == SelectionManager.SelectionMode.Submesh && hierarchy.CurrentSelection && hierarchy.CurrentSelection.GetComponent<MeshFilter>())
        {
            GameObject o = Instantiate(hierarchy.CurrentSelection.gameObject);
            ObjData data = o.GetComponent<ObjData>();
            MeshFilter mf = o.GetComponent<MeshFilter>();
            MeshRenderer mr = o.GetComponent<MeshRenderer>();
            MeshCollider mc = o.GetComponent<MeshCollider>();
            Mesh m = MeshSlicer.DisconnectSubmesh(hierarchy.CurrentSelection.GetComponent<MeshFilter>(), SelectionManager.Instance.CurrentSubmeshIndex);
            data.disabledSubmeshes.Clear();
            for (int i = 0; i < hierarchy.CurrentSelection.GetComponent<MeshFilter>().mesh.subMeshCount; i++)
            {
                data.disabledSubmeshes.Add(true);
            }
            data.disabledSubmeshes[SelectionManager.Instance.CurrentSubmeshIndex] = false;
            mr.sharedMaterials = new Material[] { mr.sharedMaterials[SelectionManager.Instance.CurrentSubmeshIndex] };
            mf.mesh = m;
            mc.sharedMesh = m;
            AddObjectToScene(o.transform);
            hierarchy.CurrentSelection.GetComponent<ObjData>().DisableSubmesh(1);
        }
    }

    public void CalculateSquarefootage()
    {
        sqftCalc.text = $"Calculating...";
        StartCoroutine(Delay());

        IEnumerator Delay()
        {
            yield return new WaitForSeconds(0.2f);
            int sqft = SquarefootageCalculator.CalculateSquarefootage(sceneObjects);
            sqftCalc.text = $"{sqft} sqft";
            Debug.Log($"{sqft} sqft");
            ActionLog.Log($"{sqft} sqft");
        }
    }

    public void RefreshInspector()
    {
        object selectedObject = hierarchy.ConnectedInspector.InspectedObject;
        hierarchy.ConnectedInspector.Inspect(null);
        hierarchy.ConnectedInspector.Inspect(selectedObject);
    }

    public void CloseApplication()
    {
        Application.Quit();
    }
}

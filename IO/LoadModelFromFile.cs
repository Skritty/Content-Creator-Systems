#pragma warning disable 649
using TriLibCore.General;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TriLibCore.Extensions;
using UnityEngine.UI;
using System.IO;
using System.Threading.Tasks;
using TriLibCore.SFB;

namespace TriLibCore.Samples
{
    /// <summary>
    /// Represents a sample that loads a Model from a file-picker.
    /// </summary>
    public class LoadModelFromFile : MonoBehaviour
    {
        [SerializeField] GameObject importLoadingPanel;

        private AssetLoaderOptions assetLoaderOptions;

        /// <summary>
        /// The last loaded GameObject.
        /// </summary>
        public GameObject _loadedGameObject;
        private bool loading = false;

        /// <summary>
        /// The progress indicator Text;
        /// </summary>
        [SerializeField]
        private Text _progressText;

        private string projFilePath;

 
        //opens simple file browser to select a file
        public void startLoadModel()
        {
            //FileBrowser.SetFilters(false, new string[] { ".fbx", ".obj" });
            //FileBrowser.ShowLoadDialog((path) => { StartCoroutine(LoadModel(path)); }, null,
            //FileBrowser.PickMode.Files, true, "Load", "Load");

            var extensions = new[] {
            new ExtensionFilter("Mesh Files", "fbx", "obj"),
            new ExtensionFilter("All Files", "*" ),
        };
            IList<ItemWithStream> paths = StandaloneFileBrowser.OpenFilePanel("Import", "", extensions, false);
            if (paths.Count > 0)
            {
                StartCoroutine(LoadModel(paths));
            }
        }

        public IEnumerator LoadModel(IList<ItemWithStream> paths)
        {
            TutorialManager.Instance.OnObjectStartImport.Invoke();
            foreach (ItemWithStream path in paths)
            {
                string filePath = path.Name;
                string fileName = Path.GetFileName(path.Name);
                if (fileName == "") break;
                importLoadingPanel.SetActive(true);
                RTG.RTFocusCamera.Get.LoadingUIActive = true;
                RTG.RTFocusCamera.Get.Settings.CanProcessInput = false;
                loading = true;
                projFilePath = ProjectManager.Instance.meshDirectory.FullName + "\\" + fileName;
                if (!File.Exists(projFilePath)) File.Copy(path.Name, projFilePath, true);

                if (!ProjectManager.Instance.references.objects.ContainsKey(Path.GetFileNameWithoutExtension(fileName).ToLower()))
                {
                    ProjectManager.Instance.referenceObjectPaths.Add(projFilePath);
                    // now we have a handle to the file
                    assetLoaderOptions = AssetLoader.CreateDefaultLoaderOptions();
                    AssetLoader.LoadModelFromFile(projFilePath, OnLoad, OnMaterialsLoad, OnProgress, OnError, null, assetLoaderOptions);
                    yield return new WaitWhile(() => loading);
                }
                else
                {
                    GenerateBetterObject(ProjectManager.Instance.references.objects[Path.GetFileNameWithoutExtension(fileName).ToLower()], null);
                }
            }
        }

        /// <summary>
        /// Called when any error occurs.
        /// </summary>
        /// <param name="obj">The contextualized error, containing the original exception and the context passed to the method where the error was thrown.</param>
        private void OnError(IContextualizedError obj)
        {
            loading = false;
            Debug.LogError($"An error occurred while loading your Model: {obj.GetInnerException()}");
        }

        /// <summary>
        /// Called when the Model loading progress changes.
        /// </summary>
        /// <param name="assetLoaderContext">The context used to load the Model.</param>
        /// <param name="progress">The loading progress.</param>
        private void OnProgress(AssetLoaderContext assetLoaderContext, float progress)
        {
            //Debug.Log($"Loading Model. Progress: {progress:P}");
        }

        /// <summary>
        /// Called when the Model (including Textures and Materials) has been fully loaded, or after any error occurs.
        /// </summary>
        /// <remarks>The loaded GameObject is available on the assetLoaderContext.RootGameObject field.</remarks>
        /// <param name="assetLoaderContext">The context used to load the Model.</param>
        private void OnMaterialsLoad(AssetLoaderContext assetLoaderContext)
        {
            loading = false;
            Transform obj = assetLoaderContext.RootGameObject.transform;
            obj.DoFunctionToTree(o => {
                MeshFilter mf = o.GetComponent<MeshFilter>();
                if (mf)
                    MeshSlicer.SliceMesh(o.gameObject.GetComponent<MeshFilter>(), o.gameObject.GetComponent<MeshRenderer>(), o.lossyScale);
            });
            MeshZeroer.ZeroObjectMeshes(obj.gameObject);
            obj.gameObject.SetActive(false);
            ProjectManager.Instance.references.objects.Add(obj.name.ToLower(), obj);
            GenerateBetterObject(obj, null);
            Debug.Log($"Loaded model: {obj.gameObject.name}");
        }

        /// <summary>
        /// Called when the Model Meshes and hierarchy are loaded.
        /// </summary>
        /// <remarks>The loaded GameObject is available on the assetLoaderContext.RootGameObject field.</remarks>
        /// <param name="assetLoaderContext">The context used to load the Model.</param>
        private void OnLoad(AssetLoaderContext assetLoaderContext)
        {
            //Debug.Log("Model loaded. Loading materials.");
        }

        private void GenerateBetterObject(Transform root, Transform parent)
        {
            Transform obj = (new GameObject()).transform;

            obj.gameObject.name = root.name;
            obj.position = root.position;
            obj.parent = parent;
            obj.localScale = root.localScale;
            obj.rotation = root.rotation;

            if (root.GetComponent<MeshFilter>() && root.GetComponent<MeshRenderer>())
            {
                obj.gameObject.AddComponent<MeshFilter>().sharedMesh = root.GetComponent<MeshFilter>().sharedMesh;
                obj.gameObject.AddComponent<MeshRenderer>().sharedMaterials = root.GetComponent<MeshRenderer>().sharedMaterials;
                obj.gameObject.AddComponent<MeshCollider>().sharedMesh = root.GetComponent<MeshFilter>().sharedMesh;

                foreach (Material m in root.GetComponent<MeshRenderer>().sharedMaterials)
                {
                    m.name = "CCAsset-" + + m.GetInstanceID();
                    if (m.color.a < 1) m.SwitchBlendMode(BlendMode.Transparent);
                }
            }

            foreach (Transform child in root)
            {
                GenerateBetterObject(child, obj);
            }

            if(parent == null)
            {
                ObjData data = ObjData.CreateObjData(obj, Path.GetFileName(projFilePath));
                //MeshZeroer.MovePivotSimple(obj.gameObject, data.pivot);
                //obj.DoFunctionToTree(o => Debug.Log($"{o.gameObject}: {o.position}"));
                ProjectManager.Instance.AddObjectToScene(obj);
                SelectionManager.Instance.Select(obj);
                obj.gameObject.SetActive(true);
                obj.parent = ProjectManager.Instance.pseudosceneRoot.transform;
                ActionLog.Log($"{obj.name} imported");
                TutorialManager.Instance.OnObjectFinishImport.Invoke();
                importLoadingPanel.SetActive(false);
                RTG.RTFocusCamera.Get.LoadingUIActive = false;
                RTG.RTFocusCamera.Get.Settings.CanProcessInput = true;
            }
        }
    }

}

public static class TransformExtension
{
    public static List<Transform> GetAllChildren(this Transform parent, List<Transform> transformList = null)
    {
        if (transformList == null) transformList = new List<Transform>();

        foreach (Transform child in parent)
        {
            transformList.Add(child);
            child.GetAllChildren(transformList);
        }
        return transformList;
    }
}
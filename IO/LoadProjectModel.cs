#pragma warning disable 649
using TriLibCore.General;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TriLibCore.Extensions;
using UnityEngine.UI;
using System.IO;
using System.Threading;

namespace TriLibCore.Samples
{
    /// <summary>
    /// Represents a sample that loads a Model from a file-picker.
    /// </summary>
    public class LoadProjectModel : MonoBehaviour
    {
        public ProjectManager projManager;

        private AssetLoaderOptions assetLoaderOptions;

        /// <summary>
        /// The last loaded GameObject.
        /// </summary>
        public GameObject _loadedGameObject;


        /// <summary>
        /// The progress indicator Text;
        /// </summary>
        [SerializeField]
        private Text _progressText;

        private string projFilePath;


        public bool loading;



        public void startLoad()
        {
            StartCoroutine("LoadModels");
        }
        /*public IEnumerator LoadModels()
        {
            Debug.Log("Mario");
            foreach (string path in projManager.objectManager.modelPaths)
            {
                


                loading = true;
                Debug.Log("yum");
                // now we have a handle to the file
                assetLoaderOptions = AssetLoader.CreateDefaultLoaderOptions();
                // assetLoaderOptions.ExternalDataMapper = ScriptableObject.CreateInstance<ExternalDataMapperSample>();
                // assetLoaderOptions.TextureMapper = ScriptableObject.CreateInstance<TextureMapperSample>();
                // assetLoaderOptions.ImportColors = true;
                // assetLoaderOptions.MergeVertices = true;
                // assetLoaderOptions.OptimizeMeshes = true;
                // assetLoaderOptions.ImportTextures = true;
                // assetLoaderOptions.MarkTexturesNoLongerReadable = false;
                // assetLoaderOptions.UseUnityNativeTextureLoader = false;
                // assetLoaderOptions.TextureCompressionQuality = TextureCompressionQuality.NoCompression;
                AssetLoader.LoadModelFromFile(path, OnLoad, OnMaterialsLoad, OnProgress, OnError, null, assetLoaderOptions);
                yield return new WaitWhile(() => loading);
            }
            //CreateObjects();
                yield return null;
        }*/

        /// <summary>
        /// Called when any error occurs.
        /// </summary>
        /// <param name="obj">The contextualized error, containing the original exception and the context passed to the method where the error was thrown.</param>
        private void OnError(IContextualizedError obj)
        {
            Debug.LogError($"An error occurred while loading your Model: {obj.GetInnerException()}");
        }

        /// <summary>
        /// Called when the Model loading progress changes.
        /// </summary>
        /// <param name="assetLoaderContext">The context used to load the Model.</param>
        /// <param name="progress">The loading progress.</param>
        private void OnProgress(AssetLoaderContext assetLoaderContext, float progress)
        {
            Debug.Log($"Loading Model. Progress: {progress:P}");
        }

        /// <summary>
        /// Called when the Model (including Textures and Materials) has been fully loaded, or after any error occurs.
        /// </summary>
        /// <remarks>The loaded GameObject is available on the assetLoaderContext.RootGameObject field.</remarks>
        /// <param name="assetLoaderContext">The context used to load the Model.</param>
        private void OnMaterialsLoad(AssetLoaderContext assetLoaderContext)
        {
            Debug.Log("Materials loaded. Model fully loaded.");
            //startArchive(assetLoaderContext);
        }

        /// <summary>
        /// Called when the Model Meshes and hierarchy are loaded.
        /// </summary>
        /// <remarks>The loaded GameObject is available on the assetLoaderContext.RootGameObject field.</remarks>
        /// <param name="assetLoaderContext">The context used to load the Model.</param>
        private void OnLoad(AssetLoaderContext assetLoaderContext)
        {
            Debug.Log("Model loaded. Loading materials.");
        }

        //the old archive code

        /*private void startArchive(AssetLoaderContext assetLoaderContext)
        {


            //creates the root object data
            ObjData rootData = new ObjData();
            //rootData.objectID = System.Guid.NewGuid().ToString();
            //rootData.objectName = assetLoaderContext.RootGameObject.name;
            rootData.gameObj = assetLoaderContext.RootGameObject;
            projManager.objectManager.objectArchive.Add(rootData.gameObj);
            //rootData.objIndex = projManager.objectManager.objectArchive.Count - 1;


            var curGUID = rootData.gameObj.AddComponent<GUID>();
            curGUID.guid = rootData.objectID;

            //adds the objects permanent instance id as a readable component for referencing later
            var guid = rootData.gameObj.AddComponent<GUID>();
            guid.guid = rootData.objectID;

            Debug.Log(rootData.gameObj);

            if (rootData.gameObj.transform.parent)
                rootData.objectParent = rootData.gameObj.transform.parent.name;

            rootData.hasLight = false;


            if (rootData.gameObj.GetComponent<MeshRenderer>())
            {
                rootData.hasMesh = true;
                //rootData.meshName = rootData.gameObj.GetComponent<MeshFilter>().mesh.name;
                projManager.objectManager.meshArchive.Add(rootData.gameObj.GetComponent<MeshFilter>().mesh);
                //rootData.materialIndices.Add(projManager.objectManager.meshArchive.Count - 1);


                foreach (Material mat in rootData.gameObj.GetComponent<MeshRenderer>().sharedMaterials)
                {
                    //rootData.meshMatNames.Add(mat.name);
                    projManager.objectManager.materialArchive.Add(mat);
                    //rootData.materialIndices.Add(projManager.objectManager.materialArchive.Count - 1);
                }
            }
            else
                rootData.hasMesh = false;

            //projManager.objectManager.objectDataArchive.Add(rootData);




            //creates all child object data
            foreach (Transform child in assetLoaderContext.RootGameObject.transform.GetAllChildren())
            {
                ObjData currentData = new ObjData();
                //currentData.objectID = System.Guid.NewGuid().ToString();
                //currentData.objectName = child.name;
                currentData.gameObj = child.gameObject;

                projManager.objectManager.objectArchive.Add(currentData.gameObj);
                //currentData.objIndex = projManager.objectManager.objectArchive.Count - 1;

                //curGUID = currentData.gameObj.AddComponent<GUID>();
                //curGUID.guid = currentData.objectID;


                if (child.transform.parent)
                    currentData.objectParent = child.parent.name;

                currentData.hasLight = false;
                if (child.GetComponent<MeshRenderer>())
                {
                    currentData.hasMesh = true;
                    //currentData.meshName = child.GetComponent<MeshFilter>().mesh.name;
                    projManager.objectManager.meshArchive.Add(currentData.gameObj.GetComponent<MeshFilter>().mesh);
                    //currentData.materialIndices.Add(projManager.objectManager.meshArchive.Count - 1);


                    foreach (Material mat in child.GetComponent<MeshRenderer>().sharedMaterials)
                    {
                        //currentData.meshMatNames.Add(mat.name);
                        projManager.objectManager.materialArchive.Add(mat);
                        //currentData.materialIndices.Add(projManager.objectManager.materialArchive.Count - 1);
                    }
                }
                //projManager.objectManager.objectDataArchive.Add(currentData);
            }


            loading = false;
        }

        private void CreateObjects()
        {
            foreach(ObjData data in projManager.objectManager.objectDataArchive)
            {
                GameObject newObj = new GameObject();
                newObj.name = data.objectName;
                if (data.hasMesh)
                {
                    MeshFilter filter = newObj.AddComponent<MeshFilter>();
                    filter.mesh = projManager.objectManager.meshArchive[data.meshIndex];
                    MeshRenderer renderer = newObj.GetComponent<MeshRenderer>();

                    for (int i = 0; i < renderer.sharedMaterials.Length; i++)
                    {
                        renderer.sharedMaterials[i] = projManager.objectManager.materialArchive[data.materialIndices[i]];
                    }
                }
                if (data.hasLight)
                {
                    Light newLight = newObj.AddComponent<Light>();
                    newLight.range = data.lightRange;
                    newLight.spotAngle = data.spotLightAngle;
                    newLight.useColorTemperature = true;
                    newLight.colorTemperature = data.colorTemp;
                    newLight.intensity = data.intensity;
                    newLight.bounceIntensity = data.bounceIntensity;
                }

                GUID objGUID = newObj.AddComponent<GUID>();
                objGUID.guid = data.objectID;

                GameObject instanceObject = Object.Instantiate(newObj);
                instanceObject.name = data.objectName;
                instanceObject.transform.localPosition = data.localPosition;
                instanceObject.transform.rotation = data.localRotation;
                instanceObject.transform.localScale = data.localScale;


                projManager.objectManager.objectArchive.Add(instanceObject);

                if(data.objectParent != null)
                    instanceObject.transform.parent = projManager.objectManager.objectArchive[data.objIndex].transform;

            }

        }*/

    }

}


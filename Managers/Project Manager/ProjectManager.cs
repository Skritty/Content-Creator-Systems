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
    public void Start()
    {
        if (!Instance) Instance = this;
        hierarchy.OnDrop += CheckIfRoot;
        //JsonSceneManagement.doneLoading += DisableLoadingPanel;
        SingleSelectionButton.interactable = false;
        UnityEngine.Rendering.GraphicsSettings.lightsUseColorTemperature = true;
        UnityEngine.Rendering.GraphicsSettings.lightsUseLinearIntensity = true;
        HierarchyField.OnFieldUpdated += ChangeFields;
        void ChangeFields(HierarchyField field)
        {
            if (!isAdvancedMode)
                field.SetExpandedState(false);
            field.ExpandToggleActiveState(isAdvancedMode && field.Data.CanExpand);
        }
    }

    private void Update()
    {
        
        if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.S)) SaveProject();
        if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.D)) DuplicateSelected();
        if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.M)) ToggleMainLight();
        if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.U)) CreateNewMaterial();
        if (Input.GetKeyDown(KeyCode.Delete)) DeleteSelected();
        if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.E)) CreateEmptyObject();
        if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.L)) CreateSimpleLight();
        if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.P)) CreateViewSpot();

        
    }

    private void CheckIfRoot(Transform t)
    {
        if (t.parent == pseudosceneRoot.transform && !sceneObjects.Contains(t))
            sceneObjects.Add(t);
        else if (t.parent != pseudosceneRoot.transform && sceneObjects.Contains(t))
            sceneObjects.Remove(t);
    }

    [System.Serializable]
    public class MaterialData
    {
        public string matName;
        public string mainTexPath;
    }

    public void StartTextureImport()
    {
        var extensions = new[] {
            new ExtensionFilter("Image Files", "png", "jpg", "jpeg" ),
            new ExtensionFilter("All Files", "*" ),
        };
        IList<ItemWithStream> paths = StandaloneFileBrowser.OpenFilePanel("Import", "", extensions, false);
        if (paths.Count > 0)
        {
            TextureImport(paths);
        }
    }

    public void TextureImport(IList<ItemWithStream> paths)
    {
        foreach(ItemWithStream path in paths)
        {
            //file setup
            string fileName = Path.GetFileName(path.Name);
            if (fileName == "") break;
            string sourceFile = path.Name;
            string targetPath = projectDirectory.FullName + "/Textures";
            string destFile = Path.Combine(targetPath, fileName);

            //copy the file to the project folder structure
            if (!File.Exists(destFile))
                File.Copy(sourceFile, destFile, true);
            Debug.Log("file coppied to " + destFile);

            MaterialData matData = new MaterialData();
            matData.matName = "CCAsset-" + Path.GetFileName(destFile);
            matData.mainTexPath = destFile;
            if (references.textures.ContainsKey(matData.matName)) continue;
            CreateMaterial(matData);
        }
    }

    public void CreateMaterial(MaterialData matData)
    {
        //create a new material
        Material newMat = new Material(Shader.Find("Standard"));
        newMat.name = matData.matName;

        //read the texture from file
        byte[] image = File.ReadAllBytes(matData.mainTexPath);
        // Create a texture. Texture size does not matter, since
        // LoadImage will replace with with incoming image size.
        Texture2D loadedImage = new Texture2D(2, 2);
        loadedImage.name = matData.matName;
        loadedImage.LoadImage(image);

        //assign texture to new the new material
        newMat.mainTexture = loadedImage;
        references.textures.Add(matData.matName, newMat.mainTexture);
        ActionLog.Log($"Texture {loadedImage.name} imported");
    }

    public void ApplyMats(ObjData obj, Material m)
    {
        if (obj.rend != null)
            obj.rend.sharedMaterial = m;
        else foreach (Transform d in obj.obj) ApplyMats(d.GetComponent<ObjData>(), m);
    }
}

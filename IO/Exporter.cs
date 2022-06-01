using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TriLibCore.SFB;

public class Exporter : MonoBehaviour
{
    public GameObject objectToExport;
    public void startExport()
    {
        var extensions = new[] {
            new ExtensionFilter(".obj"),
            new ExtensionFilter("All Files", "*" ),
        };
        IList<ItemWithStream> paths = StandaloneFileBrowser.OpenFilePanel("Save", "", extensions, true);
        ExportModel(paths);
    }

    private void ExportModel(IList<ItemWithStream> paths)
    {
        ObjExporter.MeshToFile(objectToExport.GetComponent<MeshFilter>(), paths[0].Name);

    }

}

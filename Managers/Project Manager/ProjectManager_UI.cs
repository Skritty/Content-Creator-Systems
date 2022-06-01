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
    private void DisableLoadingPanel()
    {
        loadingPanel.SetActive(false);
        RTG.RTFocusCamera.Get.LoadingUIActive = false;
    }
}

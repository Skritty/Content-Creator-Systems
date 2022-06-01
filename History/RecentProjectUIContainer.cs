using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class RecentProjectUIContainer : MonoBehaviour
{
    public TMPro.TextMeshProUGUI name;
    public TMPro.TextMeshProUGUI time;
    public RawImage thumbnail;
    public string projectPath;
    public string thumbnailPath;
    public string dateTime;

    public void OpenRecentProject()
    {
        if (Directory.Exists(projectPath))
        {
            StartCoroutine(ProjectManager.Instance.LoadProject(projectPath));
        }
        else
        {
            ActionLog.Log("Invalid Project");
        }
    }
}

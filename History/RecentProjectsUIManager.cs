using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using UnityEngine.UI;

public class RecentProjectsUIManager : MonoBehaviour
{
    public static RecentProjectsUIManager Instance;

    [SerializeField]
    private ScrollRect scroll;
    [SerializeField]
    private GameObject defaultRecentProjectButton;

    [SerializeField]
    private Vector2Int topLeftMostPixel;
    [SerializeField]
    private Vector2Int pixelDistanceBetween;
    [SerializeField]
    private int perRow;

    private List<RecentProjectUIContainer> recentProjectUIContainers = new List<RecentProjectUIContainer>();

    private void Start()
    {
        if (Instance)
            Destroy(this);
        else Instance = this;

        GenerateUI();
    }

    public void GenerateUI()
    {
        //foreach (RecentProjectUIContainer container in recentProjectUIContainers.ToArray())
        //    Destroy(container.gameObject);

        int i = -1;
        int invalid = 0;
        while (PlayerPrefs.HasKey($"RecentProjectPath{++i}"))
        {
            // Check if it exists
            string path = PlayerPrefs.GetString($"RecentProjectPath{i}");
            if (!Directory.Exists(path) || !File.Exists($"{path}\\{Path.GetFileNameWithoutExtension(path)}.json"))
            {
                invalid++;
                continue;
            }  

            // Move to the correct spot
            RecentProjectUIContainer container;
            if (recentProjectUIContainers.Count > i)
                container = recentProjectUIContainers[i];
            else
            {
                container = Instantiate(defaultRecentProjectButton).GetComponent<RecentProjectUIContainer>();
                recentProjectUIContainers.Add(container);
            }
                
            RectTransform t = container.GetComponent<RectTransform>();
            t.SetParent(scroll.content, true);
            t.anchoredPosition = new Vector2(topLeftMostPixel.x + (i - invalid) % perRow * pixelDistanceBetween.x, 
                topLeftMostPixel.y + (i - invalid) / perRow * pixelDistanceBetween.y);

            // Set the project name
            container.projectPath = path;
            container.name.text = Path.GetFileNameWithoutExtension(path);

            // Set the project time
            container.time.text = PlayerPrefs.GetString($"RecentProjectTime{i}");
            container.dateTime = container.time.text;

            // Set the project thumbnail
            path = PlayerPrefs.GetString($"RecentProjectThumbnailPath{i}");

            if (File.Exists(path))
            {
                container.thumbnailPath = path;
                container.thumbnail.color = Color.white;
                container.thumbnail.texture = LoadPNG(path);
            }
            else
            {
                container.thumbnailPath = "";
                container.thumbnail.color = Color.white;
                container.thumbnail.texture = null;
            }

            
        }
        Vector2 content = scroll.content.sizeDelta;
        content.y = -(topLeftMostPixel.y + ((i - invalid) / perRow + 1) * pixelDistanceBetween.y);
        scroll.content.sizeDelta = content;
    }

    public static Texture2D LoadPNG(string filePath)
    {

        Texture2D tex = null;
        byte[] fileData;

        if (File.Exists(filePath))
        {
            fileData = File.ReadAllBytes(filePath);
            tex = new Texture2D(2, 2);
            tex.LoadImage(fileData); //..this will auto-resize the texture dimensions.
        }
        return tex;
    }

    public void TakeThumbnailScreenshot(Camera camera)
    {
        string thumbnailPath = Path.Combine(Application.persistentDataPath, Path.GetFileNameWithoutExtension(ProjectManager.Instance.scenePath) + ".png");
        Debug.Log(thumbnailPath);
        RenderTexture rt = new RenderTexture(341, 252, 0);
        camera.targetTexture = rt;
        camera.Render();
        camera.targetTexture = null;
        File.WriteAllBytes(thumbnailPath, rt.toTexture2D().EncodeToPNG());
        RecentProjectUIContainer container = recentProjectUIContainers.Find(x => x.name.text == Path.GetFileNameWithoutExtension(ProjectManager.Instance.scenePath));
        container.thumbnailPath = thumbnailPath;
        UpdateRecentProjects(container.projectPath);
    }

    public void UpdateRecentProjects(string path)
    {
        PlayerPrefs.DeleteAll();

        int i = 1;
        foreach(RecentProjectUIContainer container in recentProjectUIContainers.ToArray())
        {
            if (container.projectPath != path)
            {
                PlayerPrefs.SetString($"RecentProjectPath{i}", container.projectPath);
                PlayerPrefs.SetString($"RecentProjectTime{i}", container.dateTime);
                PlayerPrefs.SetString($"RecentProjectThumbnailPath{i}", container.thumbnailPath);
                i++;
            }
        }

        RecentProjectUIContainer container2 = recentProjectUIContainers.Find(x => x.projectPath == ProjectManager.Instance.projectDirectory.FullName);
        PlayerPrefs.SetString($"RecentProjectPath{0}", ProjectManager.Instance.projectDirectory.FullName);
        PlayerPrefs.SetString($"RecentProjectTime{0}", System.DateTime.Now.ToString());
        if (container2)
            PlayerPrefs.SetString($"RecentProjectThumbnailPath{0}", container2.thumbnailPath);

        PlayerPrefs.Save();
        GenerateUI();
    }
}

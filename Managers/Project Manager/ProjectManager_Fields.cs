using System.Collections.Generic;
using UnityEngine;
using System.IO;
using TriLibCore.Samples;
using RuntimeInspectorNamespace;

public partial class ProjectManager : MonoBehaviour
{
    public static ProjectManager Instance;

    [Header("Project Management")]
    private const string awsBucketName = "aireal-realitystudio";
    private const string awsZipPath = "CC/";
    public DirectoryInfo projectDirectory;
    public DirectoryInfo textureDirectory;
    public DirectoryInfo meshDirectory;
    public string scenePath = "";
    public string currentScene;
    public List<Transform> sceneObjects = new List<Transform>();
    public float loadComplete = 0;
    public bool saving = false;
    public bool loading = false;
    private Transform previousSelection;
    public bool isAdvancedMode = false;
    public bool surfaceSnapping = false;

    [Header("Object Management")]
    public PseudoSceneSourceTransform transformer;
    public Color selectedColor;
    [SerializeField]
    private int maxDeletionUndoQueue = 200;

    [Header("References")]
    public RuntimeHierarchy hierarchy;
    public PseudoSceneSourceTransform pseudosceneRoot;
    [SerializeField] LoadModelFromFile importer;
    [SerializeField] LoadProjectModel projImporter;
    [SerializeField] public GizmoManager gizmos;
    [SerializeField] GameObject defaultLight;
    [SerializeField] GameObject initialProjectSelectionPanel;
    [SerializeField] GameObject loadingPanel;
    [SerializeField] GameObject mainLight;
    [SerializeField] GameObject mainLightButtonOn;
    [SerializeField] GameObject mainLightButtonOff;
    [SerializeField] GameObject personButtonOn;
    [SerializeField] GameObject personButtonOff;
    [SerializeField] GameObject scalePerson;
    [SerializeField] GameObject defaultReflectionProbe;
    [SerializeField] GameObject surfaceSnapOn;
    [SerializeField] GameObject surfaceSnapOff;
    [SerializeField] UnityEngine.UI.Button SingleSelectionButton;
    [SerializeField] UnityEngine.UI.Button MultiSelectionButton;
    [SerializeField] UnityEngine.UI.Button SubSelectionButton;
    [SerializeField] TMPro.TextMeshProUGUI sqftCalc;
    [SerializeField] Material invisibleMaterial;
    public List<string> referenceObjectPaths = new List<string>();
    public JsonSceneManagement.JSONReferences references = new JsonSceneManagement.JSONReferences();

    private Queue<Transform> deletedObjects = new Queue<Transform>();
}

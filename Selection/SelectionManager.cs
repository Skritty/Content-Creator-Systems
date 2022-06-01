using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using System;
using System.Linq;
using HighlightPlus;
using RuntimeInspectorNamespace;
using System.Runtime.InteropServices;

public class SelectionManager : Singleton<SelectionManager>
{
    // Events
    public delegate void _OnComponentChange(Component current, Component previous);
    public _OnComponentChange OnComponentChange;
    public delegate void _OnPropertyChange(MemberInfo property, Component current, Component previous);
    public _OnPropertyChange OnPropertyChange;

    [Header("Selection")]
    public SelectionMode mode;
    public enum SelectionMode { Object, Submesh }

    [SerializeField]
    private List<Selection> _selection = new List<Selection>();

    /// <summary>
    /// An item that can be selected.
    /// </summary>
    [Serializable]
    public class Selection
    {
        public Transform transform;
        public int submeshIndex;
        public HighlightEffect highlight;
        public PlaceOnSurface surfaceSnap;

        public Selection(Selection copy)
        {
            transform = copy.transform;
            submeshIndex = copy.submeshIndex;
            highlight = copy.highlight;
            surfaceSnap = copy.surfaceSnap;
        }

        public Selection(Transform transform, HighlightProfile highlightProfile)
        {
            this.transform = transform;
            this.submeshIndex = -1;
            RefreshVisuals(highlightProfile);
        }

        public Selection(Transform transform, int submeshIndex, HighlightProfile highlightProfile, Material material)
        {
            this.transform = transform;
            this.submeshIndex = submeshIndex;
            RefreshVisuals(highlightProfile, material);
        }

        public void RefreshVisuals(HighlightProfile highlightProfile)
        {
            Deselect();
            surfaceSnap = transform.gameObject.AddComponent<PlaceOnSurface>();
            if (transform.GetComponent<Renderer>())
            {
                highlight = transform.gameObject.AddComponent<HighlightEffect>();
                highlight.profileSync = true;
                highlight.profile = highlightProfile;
                highlight.highlighted = true;
                highlight.enabled = false;
                highlight.enabled = true;
            }
        }

        public void RefreshVisuals(HighlightProfile highlightProfile, Material material)
        {
            Deselect();
            surfaceSnap = transform.gameObject.AddComponent<PlaceOnSurface>();
            MeshFilter mf = transform.GetComponent<MeshFilter>();
            if (mf)
            {
                highlight = new GameObject(transform.gameObject.name).AddComponent<HighlightEffect>();
                highlight.gameObject.AddComponent<MeshFilter>().mesh = MeshSlicer.DisconnectSubmesh(mf.sharedMesh, submeshIndex);
                highlight.transform.position = mf.transform.position;
                highlight.transform.rotation = mf.transform.rotation;
                highlight.transform.localScale = mf.transform.localScale;
                highlight.gameObject.AddComponent<MeshRenderer>().material = material;
                highlight.profileSync = true;
                highlight.profile = highlightProfile;
                highlight.highlighted = true;
                highlight.enabled = false;
                highlight.enabled = true;
            }
        }

        public void Deselect()
        {
            if(surfaceSnap)
                Destroy(surfaceSnap);
            if(highlight)
                if(submeshIndex == -1)
                {
                    Destroy(highlight);
                }
                else
                {
                    Destroy(highlight.gameObject);
                }
        }
    }

    /// <summary>
    /// The currently selected object. 
    /// In the case where there are multiple objects selected, returns the multiselection helper object.
    /// </summary>
    public Transform CurrentSelection
    {
        get
        {
            switch (_selection.Count)
            {
                case 0: return null;
                case 1: return _selection[_selection.Count - 1].transform;
                default: return componentContainer.transform;

            }
        }
    }

    /// <summary>
    /// The list of selected transforms.
    /// </summary>
    public List<Transform> SelectedTransforms
    {
        get
        {
            List<Transform> transforms = new List<Transform>();
            foreach (Selection s in _selection)
                transforms.Add(s.transform);
            return transforms;
        }
    }

    /// <summary>
    /// The raw list of Selections.
    /// </summary>
    public List<Selection> RawSelection
    {
        get
        {
            return _selection;
        }

        private set
        {
            Deselect();

            if (value.Count == 0) return;

            if(value.Count > 1)
                DoMultiselect = true;

            foreach (Selection s in value)
            {
                if (s == null) continue;

                if (value[0].submeshIndex != -1)
                    mode = SelectionMode.Submesh;
                else
                    mode = SelectionMode.Object;
                
                Select(s);
            }
                
        }
    }

    public bool DoMultiselect { get; set; }
    public bool IsMultiselecting => _selection.Count > 1;

    private int _currentSubmeshIndex = -1;
    public int CurrentSubmeshIndex => _currentSubmeshIndex;

    [Header("References")]
    [SerializeField]
    private RuntimeHierarchy hierarchy;
    [SerializeField]
    private GizmoManager gizmos;
    [SerializeField]
    private UnityEngine.UI.Button ObjectSelectionButton;
    [SerializeField]
    private UnityEngine.UI.Button SubSelectionButton;

    [Header("Selection Settings")]
    [SerializeField] 
    private HighlightProfile objectHighlight;
    [SerializeField]
    private HighlightProfile submeshHighlight;
    [SerializeField]
    private Material submeshHighlightMaterial;
    [SerializeField] 
    private bool useCenterAsPivot = true;
    [SerializeField]
    [Tooltip("Components that will be used by multiselect")]
    private List<string> componentWhitelist;
    [SerializeField]
    [Tooltip("Property names that will be ignored when copying components")]
    private List<string> ignoreProperties;
    [SerializeField]
    [Tooltip("Variable names that will not be updated with multiselect")]
    private List<string> doNotUpdate;
    [SerializeField]
    [Tooltip("Variable names that will instead be combined together. Works for lists/arrays, numbers, vectors, and quaternions")]
    private List<string> combineInstead;

    private bool currentlySelecting = false;
    private bool currentlyDeselecting = false;
    private GameObject componentContainer;
    private GameObject comparisonContainer;
    private Vector3 initialPos = Vector3.zero;

    private void OnEnable()
    {
        OnPropertyChange += UpdateSelectedComponentsProperty;
    }

    private void OnDisable()
    {
        OnPropertyChange -= UpdateSelectedComponentsProperty;
    }

    private void Start()
    {
        hierarchy.OnSelectionChanged += Select;
        CreateMultiselectContainers();
    }

    private void Update()
    {
        DoMultiselect = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        if (Input.GetMouseButtonDown(0)) CheckClicked(Input.mousePosition);
        if(IsMultiselecting) UpdateMultiselection();

        foreach (HierarchyField hf in hierarchy.drawers)
            if (hf.Data != null)
                if (_selection.Exists(x => x.transform == hf.Data.BoundTransform))
                    hf.IsSelected = true;
                else
                    hf.IsSelected = false;
    }

    #region Buttons
    public void SetObjectSelectionMode()
    {
        _currentSubmeshIndex = -1;
        mode = SelectionMode.Object;
        Deselect();
        ObjectSelectionButton.interactable = false;
        SubSelectionButton.interactable = true;
        gizmos.EnableLastGizmo();
    }
    public void SetSubSelectionMode()
    {
        mode = SelectionMode.Submesh;
        Deselect();
        ObjectSelectionButton.interactable = true;
        SubSelectionButton.interactable = false;
        gizmos.DisableGizmos();
    }
    #endregion

    #region Basic Selection
    public void CheckClicked(Vector2 mousePos)
    {
        // Check if any UI was hit
        if (RTG.RTGizmosEngine.Get.HoveredGizmo != null) return;

        List<UnityEngine.EventSystems.RaycastResult> result = new List<UnityEngine.EventSystems.RaycastResult>();
        UnityEngine.EventSystems.PointerEventData pointer = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
        pointer.position = mousePos;
        UnityEngine.EventSystems.EventSystem.current.RaycastAll(pointer, result);
        if (result.Count > 0) return;

        // Figure out what was hit
        RaycastHit hit;
        if (Physics.Raycast(Camera.main.ScreenPointToRay(mousePos), out hit))
        {
            Transform obj = hit.transform;

            if (obj == null)
            {
                Select(null as Transform);
                return;
            }

            // Do special logic depending on the selection type
            switch (mode)
            {
                case SelectionMode.Object:
                    {
                        // Select the parent object before the child objects
                        if (!DoMultiselect && (CurrentSelection == null
                            || (CurrentSelection.GetComponent<ObjData>() && obj.GetComponent<ObjData>() 
                            && obj.GetComponent<ObjData>().rootObj != CurrentSelection.GetComponent<ObjData>().rootObj)))
                        {
                            obj = obj.GetComponent<ObjData>().rootObj;
                            if (!obj.GetComponent<Renderer>())
                                objectHighlight.effectGroup = TargetOptions.Children;
                            else
                                objectHighlight.effectGroup = TargetOptions.OnlyThisObject;
                        }
                        break;
                    }
                case SelectionMode.Submesh:
                    {
                        // Select the currently selected submeshIndex
                        if (obj.GetComponent<MeshFilter>())
                        {
                            int submeshIndex = -1;
                            Mesh m = obj.GetComponent<MeshFilter>().sharedMesh;
                            int[] hitTri = new int[]
                            {
                                m.triangles[hit.triangleIndex * 3],
                                m.triangles[hit.triangleIndex * 3 + 1],
                                m.triangles[hit.triangleIndex * 3 + 2]
                            };
                            for (int i = 0; i < m.subMeshCount; i++)
                            {
                                int[] tris = m.GetTriangles(i);
                                for (int j = 0; j < tris.Length; j++)
                                    if (tris[j] == hitTri[0] && tris[j + 1] == hitTri[1] && tris[j + 2] == hitTri[2])
                                    {
                                        submeshIndex = i;
                                        break;
                                    }
                                if (submeshIndex != -1) break;
                            }
                            _currentSubmeshIndex = submeshIndex;
                        }
                        break;
                    }
            }
            Select(obj);
        }
        else
        {
            Select(null as Transform);
        }

        Debug.Log("Clicked");

        // Set selections + expand all selected
        List<HierarchyField> changedFields = new List<HierarchyField>();
        foreach (HierarchyField hf in hierarchy.drawers)
            if (hf.Data != null)
                if (_selection.Exists(x => x.transform == hf.Data.BoundTransform))
                {
                    hf.IsSelected = true;
                    hf.ChainExpand(true, changedFields);
                }
                else
                    hf.IsSelected = false;

        // De-Expand
        foreach (HierarchyField hf in hierarchy.drawers)
            if (hf.Data != null)
            {
                Debug.Log(hf);
                if (!changedFields.Contains(hf))
                {
                    hf.SetExpandedState(false);
                }
            }
    }

    public void Select(Transform t)
    {
        if (currentlySelecting || currentlyDeselecting) return;
        currentlySelecting = true;

        Selection[] oldSelection = _selection.ToArray();

        switch (mode)
        {
            case SelectionMode.Object:
                {
                    if (DoMultiselect)
                    {
                        if (t == null) break;
                        if (_selection.Exists(x => x.transform == t))
                            Deselect(t);
                        else
                        {
                            for (int i = _selection.Count - 1; i >= 0; i--)
                            {
                                if (_selection[i].transform.IsChildOf(t) || t.IsChildOf(_selection[i].transform))
                                    Deselect(_selection[i]);
                            }
                            _selection.Add(new Selection(t, objectHighlight));
                            GenerateSharedObjectData();
                        }
                    }
                    else
                    {
                        Deselect();
                        if (t == null) break;
                        _selection.Add(new Selection(t, objectHighlight));
                    }
                    ActionLog.Log($"{t.name} selected");
                    break;
                }
            case SelectionMode.Submesh:
                {
                    if (DoMultiselect)
                    {
                        if (t == null) break;
                        if (_selection.Exists(x => x.transform == t && x.submeshIndex == _currentSubmeshIndex))
                            Deselect(_selection.Find(x => x.transform == t && x.submeshIndex == _currentSubmeshIndex));
                        else
                        {
                            _selection.Add(new Selection(t, _currentSubmeshIndex, submeshHighlight, submeshHighlightMaterial));
                            GenerateSharedObjectData();
                        }
                    }
                    else
                    {
                        Deselect();
                        if (t == null) break;
                        _selection.Add(new Selection(t, _currentSubmeshIndex, submeshHighlight, submeshHighlightMaterial));
                    }
                    ActionLog.Log($"{t.name}-{_currentSubmeshIndex} selected");
                    break;
                }
        }

        gizmos.SetSelection(CurrentSelection);
        hierarchy.Select(CurrentSelection);
        if (t != null) TutorialManager.Instance.OnObjectSelect.Invoke();
        hierarchy.ConnectedInspector.Refresh();
        hierarchy.Refresh();

        Selection[] newSelection = _selection.ToArray();
        ChangeHistory.LogState(
            () =>
            {
                RawSelection = oldSelection.ToList();
            },
            () =>
            {
                RawSelection = newSelection.ToList();
            }
        );
    
        currentlySelecting = false;
    }

    /// <summary>
    /// Internal selection that is used to help set RawSelection
    /// </summary>
    /// <param name="selection"></param>
    private void Select(Selection selection)
    {
        currentlySelecting = true;

        switch (mode)
        {
            case SelectionMode.Object:
                {
                    if (DoMultiselect)
                    {
                        if (selection == null) break;
                        if (_selection.Exists(x => x == selection))
                            Deselect(selection);
                        else
                        {
                            for (int i = _selection.Count - 1; i >= 0; i--)
                            {
                                if (_selection[i].transform.IsChildOf(selection.transform) || selection.transform.IsChildOf(_selection[i].transform))
                                    Deselect(_selection[i]);
                            }
                            _selection.Add(selection);
                            selection.RefreshVisuals(objectHighlight);
                            GenerateSharedObjectData();
                        }
                    }
                    else
                    {
                        Deselect();
                        if (selection == null) break;
                        _selection.Add(selection);
                        selection.RefreshVisuals(objectHighlight);
                    }
                    break;
                }
            case SelectionMode.Submesh:
                {
                    if (DoMultiselect)
                    {
                        if (selection == null) break;
                        if (_selection.Exists(x => x.transform == selection.transform && x.submeshIndex == selection.submeshIndex))
                            Deselect(_selection.Find(x => x.transform == selection.transform && x.submeshIndex == selection.submeshIndex));
                        else
                        {
                            _selection.Add(selection);
                            selection.RefreshVisuals(submeshHighlight, submeshHighlightMaterial);
                            GenerateSharedObjectData();
                        }
                    }
                    else
                    {
                        Deselect();
                        if (selection == null) break;
                        _selection.Add(selection);
                        selection.RefreshVisuals(submeshHighlight, submeshHighlightMaterial);
                    }
                    break;
                }
        }

        gizmos.SetSelection(CurrentSelection);
        hierarchy.Select(CurrentSelection);
        hierarchy.ConnectedInspector.Refresh();
        hierarchy.Refresh();

        currentlySelecting = false;
    }

    public void Deselect()
    {
        currentlyDeselecting = true;

        foreach (Selection s in _selection.ToArray())
        {
            s.Deselect();
        }
        if (componentContainer)
            Destroy(componentContainer);
        if (comparisonContainer)
            Destroy(comparisonContainer);
        _selection.Clear();
        gizmos.SetSelection(null);
        gizmos.DisableGizmos();
        hierarchy.Deselect();
        hierarchy.ConnectedInspector.StopInspect();

        currentlyDeselecting = false;
    }

    public void Deselect(Transform transform)
    {
        currentlyDeselecting = true;

        if (transform == null) return;
        Selection s = _selection.Find(x => x.transform == transform);
        if (s == null) return;
        s.Deselect();
        _selection.Remove(s);
        if(_selection.Count == 0)
        {
            gizmos.SetSelection(null);
            gizmos.DisableGizmos();
            hierarchy.Deselect();
            hierarchy.ConnectedInspector.StopInspect();
        }
        else
        {
            gizmos.SetSelection(CurrentSelection);
            hierarchy.Select(CurrentSelection);
        }

        currentlyDeselecting = false;
    }

    public void Deselect(Selection selection)
    {
        currentlyDeselecting = true;

        if (selection == null) return;
        selection.Deselect();
        _selection.Remove(selection);
        if (_selection.Count == 0)
        {
            gizmos.SetSelection(null);
            gizmos.DisableGizmos();
            hierarchy.Deselect();
            hierarchy.ConnectedInspector.StopInspect();
        }
        else
        {
            gizmos.SetSelection(CurrentSelection);
            hierarchy.Select(CurrentSelection);
        }

        currentlyDeselecting = false;
    }

    public void ExapndDrawers()
    {
        foreach(HierarchyField hf in hierarchy.drawers)
        {
            if (hf == null) continue;
            if (!FindSelected(hf))
                hf.ExpandToggleActiveState(true);
        }
        bool FindSelected(HierarchyField current)
        {
            bool selected = current.IsSelected;
            if(!selected && current.Data != null && current.Data.children != null)
                foreach (HierarchyDataTransform child in current.Data.children)
                    if (child.field != null)
                    {
                        selected = FindSelected(child.field);
                        if (selected)
                            break;
                    }
            return selected;
        }
    }
    #endregion

    #region Multi Selection
    private void UpdateMultiselection()
    {
        CheckGameObjectChanges();
        foreach (Component previous in GetAllComponents(comparisonContainer))
        {
            if (!componentWhitelist.Contains(previous.GetType().Name)) continue;

            Type componentType = previous.GetType();
            Component current = componentContainer.GetComponent(componentType);

            if (!previous || !current) continue;

            foreach (MemberInfo info in GetChanges(current, previous))
            {
                Debug.Log($"Chang'e in {info.Name}");
                OnPropertyChange.Invoke(info, current, previous);
            }
        }
    }

    private void CheckGameObjectChanges()
    {
        foreach (MemberInfo info in GetChanges(componentContainer, comparisonContainer))
        {
            Debug.Log($"Chang'e in {info.Name}");
            foreach (Selection obj in _selection)
            {
                if (info.CanWrite())
                {
                    var newVal = info.GetValue(componentContainer);
                    if (combineInstead.Contains(info.Name))
                    {
                        // Add combination support if needed
                    }
                    info.SetValue(obj.transform.gameObject, newVal);
                }
            }
            info.SetValue(comparisonContainer, info.GetValue(componentContainer));
        }
    }

    private void CreateMultiselectContainers()
    {
        if(componentContainer)
            Destroy(componentContainer);
        if(comparisonContainer)
            Destroy(comparisonContainer);
        componentContainer = new GameObject();
        componentContainer.name = "Multiselect Component Container";
        comparisonContainer = new GameObject();
        comparisonContainer.name = "Multiselect Comparison Container";
    }

    private void GenerateSharedObjectData()
    {
        CreateMultiselectContainers();

        switch (mode)
        {
            case SelectionMode.Object:
                {
                    foreach (Selection obj in _selection)
                    {
                        foreach (Component c in obj.transform.GetComponents<Component>())
                        {
                            CopyComponentTo(componentContainer, c);
                            CopyComponentTo(comparisonContainer, c);
                        }
                        CopyComponentTo(componentContainer, obj.transform);
                        CopyComponentTo(comparisonContainer, obj.transform);
                    }

                    if (componentContainer.GetComponent<MeshRenderer>())
                    {
                        List<Material> materials = new List<Material>();
                        foreach (Selection obj in _selection)
                        {
                            Renderer rend = obj.transform.GetComponent<Renderer>();
                            if (!rend) continue;
                            foreach (Material m in rend.sharedMaterials)
                            {
                                if (!materials.Contains(m))
                                    materials.Add(m);
                            }
                        }
                        componentContainer.GetComponent<MeshRenderer>().sharedMaterials = materials.ToArray();
                        comparisonContainer.GetComponent<MeshRenderer>().sharedMaterials = materials.ToArray();
                    }
                    break;
                }
            case SelectionMode.Submesh:
                {
                    List<Material> materials = new List<Material>();
                    foreach (Selection obj in _selection)
                    {
                        Renderer rend = obj.transform.GetComponent<Renderer>();
                        if (!rend) continue;

                        Material m = rend.sharedMaterials[obj.submeshIndex];
                        if (!materials.Contains(m))
                            materials.Add(m);
                    }
                    componentContainer.AddComponent<MeshRenderer>().sharedMaterials = materials.ToArray();
                    comparisonContainer.AddComponent<MeshRenderer>().sharedMaterials = materials.ToArray();
                    break;
                }
        }
        
    }

    private List<Component> GetAllComponents(GameObject obj)
    {
        List<Component> components = new List<Component>();
        components.Add(obj.transform);
        components.AddRange(obj.GetComponents<Component>());
        return components;
    }

    private void UpdateSelectedComponentsProperty(MemberInfo info, Component current, Component previous)
    {
        foreach (Selection obj in _selection)
        {
            Component component = obj.transform.GetComponent(current.GetType());
            if (component != null)
            {
                if (info.CanWrite())
                {
                    if (combineInstead.Contains(info.Name))
                    {
                        var newVal = info.GetValue(component);
                        var currentVal = info.GetValue(current);
                        var previousVal = info.GetValue(previous);
                        switch (newVal)
                        {
                            case int x:
                                x += (int)currentVal - (int)previousVal;
                                newVal = x;
                                break;
                            case float x:
                                x += (float)currentVal - (float)previousVal;
                                newVal = x;
                                break;
                            case Vector2 x:
                                x += (Vector2)currentVal - (Vector2)previousVal;
                                newVal = x;
                                break;
                            case Vector3 x:
                                x += (Vector3)currentVal - (Vector3)previousVal;
                                newVal = x;
                                break;
                            case Quaternion x:
                                x *= Quaternion.Inverse((Quaternion)previousVal) * (Quaternion)currentVal;
                                newVal = x;
                                break;
                            case IList x:
                                foreach (object o in (IList)previousVal)
                                    x.Remove(o);
                                foreach (object o in (IList)currentVal)
                                    x.Add(o);
                                newVal = x;
                                break;
                        }
                        info.SetValue(component, newVal);
                    }
                    else
                    {
                        if (info.Name == "sharedMaterials")
                        {
                            Material[] mats = ((MeshRenderer)component).sharedMaterials;
                            Material[] from = previous.transform.GetComponent<MeshRenderer>().sharedMaterials;
                            Material[] to = current.transform.GetComponent<MeshRenderer>().sharedMaterials;

                            for (int i = 0; i < to.Length; i++)
                            {
                                if (from.Length <= i) break;
                                if (!to[i].Equals(from[i]))
                                {
                                    if (obj.submeshIndex == -1)
                                    {
                                        for (int j = 0; j < mats.Length; j++)
                                        {
                                            if (mats[j] == from[i])
                                                mats[j] = to[i];
                                        }
                                    }
                                    else
                                    {
                                        if (mats[obj.submeshIndex] == from[i])
                                            mats[obj.submeshIndex] = to[i];
                                    }
                                }

                            }
                            info.SetValue(component, mats);
                        }
                        else
                            info.SetValue(component, info.GetValue(current));
                    }
                }
            }
        }
        if(info.Name == "sharedMaterials")
        {
            MeshRenderer component = current.transform.GetComponent<MeshRenderer>();
            info.SetValue(previous, component.sharedMaterials);
        }
        else info.SetValue(previous, info.GetValue(current));
    }

    private List<MemberInfo> GetChanges(object current, object old)
    {
        List<MemberInfo> properties = new List<MemberInfo>();
        List<MemberInfo> memberInfo = new List<MemberInfo>();
        memberInfo.AddRange(current.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public));
        memberInfo.AddRange(current.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public));
        foreach (var info in memberInfo)
        {
            if (doNotUpdate.Contains(info.Name) || !info.CanWrite() || info.GetValue(current) == null) continue;
            //Debug.Log($"{current.GetType()}/{info.Name} Change Check: {!info.GetValue(current).Equals(info.GetValue(old))} \n Old: {info.GetValue(current)} | New: {info.GetValue(old)}");
            if (!info.GetValue(current).Equals(info.GetValue(old)))
            {
                properties.Add(info);
            }
        }
        return properties;
    }

    private void CopyComponentTo(GameObject obj, Component component)
    {
        Type componentType = component.GetType();
        if (!componentWhitelist.Contains(componentType.Name)) return;
        Component comparisonComponent = obj.GetComponent(componentType);
        if (comparisonComponent == null)
        {
            comparisonComponent = obj.AddComponent(componentType);
            List<MemberInfo> memberInfo = new List<MemberInfo>();
            memberInfo.AddRange(componentType.GetProperties(BindingFlags.Instance | BindingFlags.Public));
            memberInfo.AddRange(componentType.GetFields(BindingFlags.Instance | BindingFlags.Public));
            foreach (var info in memberInfo)
            {
                //Debug.Log($"{componentType.Name} | {info.Name}: Can Write? {info.CanWrite()}, Ignored? {ignoreProperties.Contains(info.Name)}");
                if (!info.CanWrite() || ignoreProperties.Contains(info.Name)) continue;
                var val = info.GetValue(component);
                //Debug.Log($"{componentType.Name} | {info.Name}:{val}");
                info.SetValue(comparisonComponent, val);
            }
        }
        else
        {
            List<MemberInfo> memberInfo = new List<MemberInfo>();
            memberInfo.AddRange(componentType.GetProperties(BindingFlags.Instance | BindingFlags.Public));
            memberInfo.AddRange(componentType.GetFields(BindingFlags.Instance | BindingFlags.Public));
            foreach (var info in memberInfo)
            {
                if (!info.CanWrite() || ignoreProperties.Contains(info.Name)) continue;
                if (typeof(List<object>).IsAssignableFrom(info.ReflectedType) && !(info.GetValue(component) as IList).IsFixedSize)
                {
                    var list = (info.GetValue(comparisonComponent) as List<object>);
                    foreach (var item in info.GetValue(component) as List<object>)
                    {
                        list.Add(item);
                    }
                    info.SetValue(comparisonComponent, list);
                }
                else
                {
                    info.SetValue(comparisonComponent, info.GetValue(component));
                }
            }
        }
    }
    #endregion
}

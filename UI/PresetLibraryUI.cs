using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using RuntimeInspectorNamespace;

public class PresetLibraryUI : Singleton<PresetLibraryUI>
{
    public Action<Preset> OnPresetSelected;
    public void ClearCallback()
    {
        OnPresetSelected = null;
    }

    [Header("Anchors")]
    [SerializeField]
    private RadioButtonGroup tagsAnchor;
    [SerializeField]
    private RadioButtonGroup tabsAnchor;
    [SerializeField]
    private RadioButtonGroup presetsAnchor;
    [Header("Prefabs")]
    [SerializeField]
    private DynamicUIContainer tagPrefab;
    [SerializeField]
    private DynamicUIContainer tabPrefab;
    [SerializeField]
    private DynamicUIContainer presetPrefab;

    [Header("Other")]
    [SerializeField]
    private Texture defaultLightImage;

    private PresetTags tagSort;
    [SerializeField]
    private TMP_InputField stringSort;
    private Type typeSort;
    private DynamicUIContainer selectedPreset;

    private List<DynamicUIContainer> presets = new List<DynamicUIContainer>();
    private Dictionary<Type, DynamicUIContainer> tabs = new Dictionary<Type, DynamicUIContainer>();
    private Dictionary<PresetTags, DynamicUIContainer> tags = new Dictionary<PresetTags, DynamicUIContainer>();

    private void Start()
    {
        stringSort.onValueChanged.AddListener(x => RefreshSort());
        DrawPresets();
        DrawTabs();
        DrawTags();
        RefreshSort();
        gameObject.SetActive(false);
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }
    public void Show(string search, PresetTags tags, Type type)
    {
        stringSort.text = search;
        tagSort = tags;
        typeSort = type;
        RefreshSort();
        gameObject.SetActive(true);

        foreach (KeyValuePair<Type, DynamicUIContainer> tab in tabs)
        {
            if (tab.Key == typeSort)
            {
                tabsAnchor.SelectWithoutInvoke(tab.Value.graphicElements.Get<Button>("button"));
                break;
            }
        }

        foreach (KeyValuePair<PresetTags, DynamicUIContainer> tag in this.tags)
        {
            tag.Value.graphicElements.Get<Toggle>("toggle").SetIsOnWithoutNotify(tagSort.HasFlag(tag.Key));
        }
    }

    private void DrawPresets()
    {
        foreach (Preset preset in PresetLibrary.GetAllPresets())
        {
            AddPresetUI(preset);
        }
    }

    public void AddPresetUI(Preset preset)
    {
        if (preset == null) return;

        DynamicUIContainer container = CreateUIContainerFromPreset(preset);
        presets.Add(container);

        Button button = container.graphicElements.Get<Button>("button");
        presetsAnchor.Add(button);
        presetsAnchor.AddActionSet(button, () => selectedPreset = container, null);
        presetsAnchor.AddActionSet(button,
            () => container.graphicElements.Get<Button>("editButton").gameObject.SetActive(true),
            () => container.graphicElements.Get<Button>("editButton").gameObject.SetActive(false));
        presetsAnchor.AddActionSet(button,
            () => container.graphicElements.Get<Button>("deleteButton").gameObject.SetActive(true),
            () => container.graphicElements.Get<Button>("deleteButton").gameObject.SetActive(false));
        presetsAnchor.AddActionSet(button,
            () => container.GetComponent<DraggedReferenceSourceUI>().enabled = true,
            () => container.GetComponent<DraggedReferenceSourceUI>().enabled = false);
        presetsAnchor.AddActionSet(button, RefreshSort, null);
        presetsAnchor.AddActionSet(button, () => OnPresetSelected?.Invoke(preset), null);
    }

    private DynamicUIContainer CreateUIContainerFromPreset(Preset preset)
    {
        DynamicUIContainer container = Instantiate(presetPrefab, presetsAnchor.transform);

        container.GetComponent<DraggedReferenceSourceUI>().Reference = preset.GetPresetReference();
        container.GetComponent<DraggedReferenceSourceUI>().enabled = false;

        container.data.Add("type", preset.PresetType());
        container.data.Add("tags", preset.Tags);
        container.data.Add("name", preset.name);

        container.graphicElements.Get<TextMeshProUGUI>("label").text = preset.name;

        // Set thumnail texture
        if (preset.GetType() == typeof(MaterialPreset))
        {
            container.graphicElements.Get<RawImage>("thumbnail").texture =
                container.graphicElements.Get<ThumbnailRenderer>("camera")
                .RenderThumbnail((preset as Preset<Material>).PresetReference.ToPropertyBlock());
        }
        else if (preset.GetType() == typeof(LightPreset))
        {
            container.graphicElements.Get<RawImage>("thumbnail").texture = defaultLightImage;
        }

        return container;
    }

    private void DrawTabs()
    {
        foreach (Type type in PresetLibrary.GetTypes())
        {
            DynamicUIContainer container = Instantiate(tabPrefab, tabsAnchor.transform);
            tabs.Add(type, container);
            container.graphicElements.Get<TextMeshProUGUI>("label").text = type.Name;
            Type temp = type;
            Button button = container.graphicElements.Get<Button>("button");
            tabsAnchor.Add(button);
            tabsAnchor.AddActionSet(button, () => typeSort = temp, null);
            tabsAnchor.AddActionSet(button, RefreshSort, null);
            if (tabsAnchor.DefaultButton == null)
                tabsAnchor.DefaultButton = button;
        }
    }

    private void DrawTags()
    {
        float height = 0;
        foreach (PresetTags tag in Enum.GetValues(typeof(PresetTags)))
        {
            if (tag == PresetTags.None) continue;
            PresetTags temp = tag;
            DynamicUIContainer container = Instantiate(tagPrefab, tagsAnchor.transform);
            tags.Add(tag, container);
            height += container.GetComponent<RectTransform>().rect.height;
            container.graphicElements.Get<TextMeshProUGUI>("label").text = temp.ToString();
            container.graphicElements.Get<Toggle>("toggle").SetIsOnWithoutNotify(false);
            container.graphicElements.Get<Toggle>("toggle").onValueChanged.AddListener(x => 
            {
                if (x)
                    tagSort |= temp;
                else
                    tagSort &= ~temp;
                RefreshSort();
            });
        }
        tagsAnchor.GetComponent<RectTransform>().sizeDelta = new Vector2(tagsAnchor.GetComponent<RectTransform>().sizeDelta.x, height);
    }

    public void RefreshSort()
    {
        float height = 20;
        int total = 0;
        foreach (DynamicUIContainer preset in presets)
        {
            bool valid = true;
            if ((ulong)tagSort != 0 && ((ulong)tagSort & (ulong)(PresetTags)preset.data["tags"]) == 0)
                valid = false;
            if (stringSort.text != "" && !((string)preset.data["name"]).ToLower().Contains(stringSort.text.ToLower()))
                valid = false;
            if (typeSort != (Type)preset.data["type"])
                valid = false;

            if (valid)
            {
                preset.gameObject.SetActive(true);
                if (total++ % 4 == 0)
                    height += preset.GetComponent<RectTransform>().rect.height;
            }
            else
                preset.gameObject.SetActive(false);
        }
        
        presetsAnchor.GetComponent<RectTransform>().sizeDelta = new Vector2(presetsAnchor.GetComponent<RectTransform>().sizeDelta.x, height);
    }
}

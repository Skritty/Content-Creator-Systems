using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Linq;

[CreateAssetMenu(fileName = "Preset Library", menuName = "Presets/Preset Library")]
public class PresetLibrary : Singleton<PresetLibrary>
{
    [System.Serializable]
    public class Presets
    {
        public List<Preset> presets = new List<Preset>();
    }
    [SerializeField]
    private Presets _presets = new Presets();
    public Presets PresetContainer
    {
        get => _presets;
        set => _presets = value;
    }

    private void Add(Preset preset)
    {
        Instance._presets.presets.Add(preset);
    }

    private void Remove(Preset preset)
    {
        Instance._presets.presets.Remove(preset);
    }

    public static List<System.Type> GetTypes()
    {
        List<System.Type> types = new List<System.Type>();
        foreach(Preset p in Instance._presets.presets)
        {
            System.Type t = p.GetType().BaseType.GenericTypeArguments[0];
            if (!types.Contains(t))
                types.Add(t);
        }
        return types;
    }

    public static Preset<T> Get<T>(string name) where T : Object
    {
        return Instance._presets.presets.Find(x => x.name == name) as Preset<T>;
    }

    public static Preset<T> Get<T>(T presetObject) where T : Object
    {
        return Instance._presets.presets.Find(x => (x as Preset<T>).PresetReference == presetObject) as Preset<T>;
    }

    public static dynamic GetPreset<T>(string name) where T : Object
    {
        Preset<T> p = (Preset<T>)Instance._presets.presets.Find(x => x.name == name);
        if (p == null || p.PresetReference == null) return null;
        switch (p.PresetReference)
        {
            case Material material:
                return (p.PresetReference as Material).ToPropertyBlock();
            default:
                return p.PresetReference;
        }
    }

    public static List<Preset<T>> GetPresets<T>() where T : Object
    {
        Instance._presets.presets.RemoveAll(x => x == null);
        return Instance._presets.presets.Select(x => (x as Preset<T>)).ToList();
    }

    public static List<Preset> GetAllPresets()
    {
        Instance._presets.presets.RemoveAll(x => x == null);
        return Instance._presets.presets;
    }

    public static MaterialPreset CreateNewMaterialPreset(Material material, string name, PresetTags tags)
    {
        MaterialPreset preset = ScriptableObject.CreateInstance<MaterialPreset>();
        preset.name = name;
        preset.Initialize(material, tags);
        Instance.Add(preset);
        PresetLibraryUI.Instance.AddPresetUI(preset);
        return preset;
    }

    public static LightPreset CreateNewLightPreset()
    {
        LightPreset preset = ScriptableObject.CreateInstance<LightPreset>();
        Instance.Add(preset);
        return preset;
    }

    public static string ToJson()
    {
        return "";
    }
}

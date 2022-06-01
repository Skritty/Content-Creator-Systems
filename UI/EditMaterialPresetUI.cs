using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class EditMaterialPresetUI : Singleton<EditMaterialPresetUI>
{
    [Header("UI References")]
    [SerializeField]
    private Canvas panel;
    [SerializeField]
    private TMP_InputField nameField;
    [SerializeField]
    private ColorPickerElement colorPicker;
    [SerializeField]
    private TexturePickerElement albedo, normal, height;
    [SerializeField]
    private TMP_InputField scalingX, scalingY, offsetX, offsetY;
    [SerializeField]
    private Slider metallic, smoothness;
    [SerializeField]
    private ThumbnailRenderer render;
    [SerializeField]
    private RawImage renderImg;

    [SerializeField]
    private Material currentMaterial;
    private bool updating = false;

    private void Start()
    {
        colorPicker.OnColorChange += c => UpdateMaterial();
        albedo.OnTextureChange += t => UpdateMaterial();
        normal.OnTextureChange += t => UpdateMaterial();
        height.OnTextureChange += t => UpdateMaterial();
        metallic.onValueChanged.AddListener(v => UpdateMaterial());
        smoothness.onValueChanged.AddListener(v => UpdateMaterial());
        scalingX.onValueChanged.AddListener(t => UpdateMaterial());
        scalingY.onValueChanged.AddListener(t => UpdateMaterial());
        offsetX.onValueChanged.AddListener(t => UpdateMaterial());
        offsetY.onValueChanged.AddListener(t => UpdateMaterial());
        CreateNewMaterialPreset();
        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        Reset();
    }

    private void Reset()
    {
        nameField.text = "";
        colorPicker.Color = Color.white;
        albedo.Texture = null;
        normal.Texture = null;
        height.Texture = null;
        scalingX.text = ""+1;
        scalingY.text = ""+1;
        offsetX.text = ""+0;
        offsetY.text = ""+0;
        metallic.value = 0;
        smoothness.value = 0;
    }

    public Material CreateNewMaterialPreset()
    {
        currentMaterial = new Material(Shader.Find("Standard"));
        UpdateUI();
        return currentMaterial;
    }

    public void EditMaterialPreset(Material editing)
    {
        currentMaterial = editing;
        UpdateUI();
    }

    private void UpdateUI()
    {
        updating = true;
        colorPicker.Color = currentMaterial.GetColor("_Color");
        albedo.Texture = currentMaterial.GetTexture("_MainTex");
        normal.Texture = currentMaterial.GetTexture("_BumpMap");
        height.Texture = currentMaterial.GetTexture("_ParallaxMap");
        metallic.value = currentMaterial.GetFloat("_Metallic");
        smoothness.value = currentMaterial.GetFloat("_Glossiness");
        Vector4 st = currentMaterial.GetVector("_MainTex_ST");
        scalingX.text = $"{st.x}";
        scalingY.text = $"{st.y}";
        offsetX.text = $"{st.z}";
        offsetY.text = $"{st.w}";
        updating = false;
    }

    public void UpdateMaterial()
    {
        if (updating) return;
        updating = true;
        currentMaterial.SetColor("_Color", colorPicker.Color);
        currentMaterial.SetTexture("_MainTex", albedo.Texture);
        currentMaterial.SetTexture("_BumpMap", normal.Texture);
        currentMaterial.SetTexture("_ParallaxMap", height.Texture);
        currentMaterial.SetFloat("_Metallic", metallic.value);
        currentMaterial.SetFloat("_Glossiness", smoothness.value);
        currentMaterial.SetVector("_MainTex_ST", new Vector4(
            Convert.ToInt32(scalingX.text), Convert.ToInt32(scalingY.text),
            Convert.ToInt32(offsetX.text), Convert.ToInt32(offsetY.text)));
        renderImg.texture = render.RenderThumbnail(currentMaterial.ToPropertyBlock());
        updating = false;
    }

    public void EndEditing(bool saveChanges)
    {
        if (saveChanges)
            PresetLibrary.CreateNewMaterialPreset(currentMaterial, nameField.text, ~PresetTags.None);
        currentMaterial = null;
        panel.gameObject.SetActive(false);
    }
}

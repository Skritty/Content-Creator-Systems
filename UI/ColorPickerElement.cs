using RuntimeInspectorNamespace;
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ColorPickerElement : MonoBehaviour
{
    public Action<Color> OnColorChange;
    [SerializeField]
    private PointerEventListener inputColor;
    private Image colorImg => inputColor.GetComponent<Image>();
    private Color _color = Color.white;
    public Color Color
    {
        get => _color;
        set
        {
            _color = value;
            colorImg.color = value;
        }
    }
    void Start()
    {
        inputColor.PointerClick += ShowColorPicker;
    }

    private void ShowColorPicker(PointerEventData eventData)
    {
        ColorPicker.Instance.Show(OnColorChanged, null, Color, ProjectManager.Instance.hierarchy.ConnectedInspector.Canvas);
    }

    private void OnColorChanged(Color32 color)
    {
        Color = color;
        OnColorChange?.Invoke(color);
    }
}

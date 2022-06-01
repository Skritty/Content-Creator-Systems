using RuntimeInspectorNamespace;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TexturePickerElement : MonoBehaviour
{
	public Action<Texture> OnTextureChange;
	[SerializeField]
	private PointerEventListener inputUI;
	private RawImage img;
	private Texture _texture = null;
	public Texture Texture
	{
		get => _texture;
		set
		{
			_texture = value;
			if(img)
				img.texture = value;
		}
	}

	void Start()
	{
		img = inputUI.GetComponent<RawImage>();
		inputUI.PointerClick += ShowReferencePicker;
	}

	private void ShowReferencePicker(PointerEventData eventData)
	{
		Texture[] allReferences = Resources.FindObjectsOfTypeAll<Texture>();
		allReferences = allReferences.Where(a => a.name.Contains("CCAsset-")).ToArray();

		CustomObjectReferencePicker.Instance.Show(
			(reference) => OnReferenceChanged((Texture)reference), null,
			(reference) => (Texture)reference ? ((Texture)reference).name : "None",
			(reference) => reference.GetNameWithType(),
			allReferences, _texture, true, "Texture", ProjectManager.Instance.hierarchy.ConnectedInspector.Canvas);
	}

	protected virtual void OnReferenceChanged(Texture reference)
	{
		Texture = reference;
		OnTextureChange.Invoke(reference);
	}
}

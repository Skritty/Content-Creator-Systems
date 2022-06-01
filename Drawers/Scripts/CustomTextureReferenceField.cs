using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using RuntimeInspectorNamespace;
using System.Collections.Generic;
using Object = UnityEngine.Object;

public class CustomTextureReferenceField : CustomObjectReferenceField
{
#pragma warning disable 0649
	[SerializeField]
	private RawImage referencePreview;
#pragma warning restore 0649

	protected override float HeightMultiplier { get { return 2f; } }

	public override bool SupportsType(Type type)
	{
		return typeof(Texture).IsAssignableFrom(type) || typeof(Sprite).IsAssignableFrom(type);
	}

	protected override void OnReferenceChanged(Object reference)
	{
		base.OnReferenceChanged(reference);

		referenceNameText.gameObject.SetActive(!reference);

		Texture tex = reference.GetTexture();
		referencePreview.enabled = tex != null;
		referencePreview.texture = tex;
	}
}

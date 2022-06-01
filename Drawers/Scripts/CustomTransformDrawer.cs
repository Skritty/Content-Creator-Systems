using System;
using System.Reflection;
using UnityEngine;
using RuntimeInspectorNamespace;

public class CustomTransformDrawer : ExpandableInspectorField
{
	protected override int Length { get { return 3; } } // localPosition, localEulerAngles, localScale

	//private PropertyInfo positionProp, rotationProp, scaleProp;
	private Getter positionGetter, rotationGetter, scaleGetter;
	private Setter positionSetter, rotationSetter, scaleSetter;

	public override void Initialize()
	{
		base.Initialize();

		//positionProp = typeof(Transform).GetProperty("localPosition");
		//rotationProp = typeof(Transform).GetProperty("localEulerAngles");
		//scaleProp = typeof(Transform).GetProperty("localScale");
		positionGetter = () => ((Transform)Value).localPosition;
		positionSetter = (value) =>
		{
			if (((Transform)Value).localPosition == (Vector3)value) return;
			((Transform)Value).LogState("localPosition", () => ProjectManager.Instance.gizmos.RefreshGizmos());
			((Transform)Value).localPosition = (Vector3)value;
		};

		rotationGetter = () => ((Transform)Value).localEulerAngles;
		rotationSetter = (value) =>
		{
			if (((Transform)Value).localEulerAngles == (Vector3)value) return;
			((Transform)Value).LogState("localEulerAngles", () => ProjectManager.Instance.gizmos.RefreshGizmos());
			((Transform)Value).localEulerAngles = (Vector3)value;
		};

		scaleGetter = () => ((Transform)Value).localScale;
		scaleSetter = (value) =>
		{
			if (((Transform)Value).localScale == (Vector3)value) return;
			((Transform)Value).LogState("localScale", () => ProjectManager.Instance.gizmos.RefreshGizmos());
			((Transform)Value).localScale = (Vector3)value;
		};
	}

	public override bool SupportsType(Type type)
	{
		return type == typeof(Transform);
	}

	protected override void GenerateElements()
	{
		HeaderVisibility = RuntimeInspector.HeaderVisibility.Hidden;
		//CreateDrawerForVariable(positionProp, "Position");
		//CreateDrawerForVariable(rotationProp, "Rotation");
		//CreateDrawerForVariable(scaleProp, "Scale");
		CreateDrawer(typeof(Vector3), "Position", positionGetter, positionSetter);
		CreateDrawer(typeof(Vector3), "Rotation", rotationGetter, rotationSetter);
		CreateDrawer(typeof(Vector3), "Scale", scaleGetter, scaleSetter);
	}

    protected override void OnDepthChanged()
    {
        
    }
}

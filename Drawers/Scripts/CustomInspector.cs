using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;
using RuntimeInspectorNamespace;

public class CustomInspector : ExpandableInspectorField
{
	protected override int Length { get {
			switch (SelectionManager.Instance.mode)
			{
				case SelectionManager.SelectionMode.Object:
					return 5
				+ (((GameObject)Value).GetComponent<MeshRenderer>() ? 2 : 0)
				//+ (((GameObject)Value).GetComponent<MeshRenderer>() && !SelectionManager.Instance.IsMultiselecting ? 1 : 0)
				+ (((GameObject)Value).GetComponent<Light>() ? 1 : 0);
				case SelectionManager.SelectionMode.Submesh:
				default:
					return 1;
			}
		} }

	private Getter isActiveGetter, nameGetter, tagGetter, pivotGetter;
	private Setter isActiveSetter, nameSetter, tagSetter, pivotSetter;
	private enum Tags { Untagged, Floor, GarageFloor, Roof, Furniture }

	public override void Initialize()
	{
		base.Initialize();

		isActiveGetter = () => !((GameObject)Value).activeSelf;
		isActiveSetter = (value) =>
		{
			if((bool)value == ((GameObject)Value).activeSelf)
				((GameObject)Value).LogState(
					() => ((GameObject)Value).activeSelf,
					(v) => ((GameObject)Value).SetActive(v));
            ((GameObject)Value).SetActive(!(bool)value);
		};

		nameGetter = () => ((GameObject)Value).name;
		nameSetter = (value) =>
		{
			((GameObject)Value).LogState("name");
			((GameObject)Value).name = (string)value;
			if(SelectionManager.Instance.IsMultiselecting)
				NameRaw = "Multiselect";
			else
				NameRaw = Value.GetNameWithType();

			RuntimeHierarchy hierarchy = Inspector.ConnectedHierarchy;
			if (hierarchy)
				hierarchy.RefreshNameOf(((GameObject)Value).transform);
		};

		tagGetter = () =>
		{
            switch (((GameObject)Value).tag)
            {
				case "Floor":
					return Tags.Floor;
				case "Roof":
					return Tags.Roof;
				case "GarageFloor":
					return Tags.GarageFloor;
				case "Furniture":
					return Tags.Furniture;
				default:
					return Tags.Untagged;
			}
		};
		tagSetter = (value) =>
		{
			if (((GameObject)Value).tag == ((Tags)value).ToString()) return;
			((GameObject)Value).LogState("tag");
			((GameObject)Value).tag = ((Tags)value).ToString();
		};

		pivotGetter = () =>
		{
			return ((GameObject)Value).GetComponent<ObjData>().pivot;
		};
		pivotSetter = (value) =>
		{
			if (((GameObject)Value).GetComponent<ObjData>().pivot == (MeshZeroer.Pivot)value) return;
			((GameObject)Value).GetComponent<ObjData>().LogState(
				() => 
				{
					return ((GameObject)Value).GetComponent<ObjData>().pivot;
				},
				(v) =>
                {
					((GameObject)Value).GetComponent<ObjData>().pivot = (MeshZeroer.Pivot)v;
					MeshZeroer.MovePivotAdvanced(((GameObject)Value), (MeshZeroer.Pivot)v);
					ProjectManager.Instance.gizmos.RefreshGizmos();
				}
			);
			((GameObject)Value).GetComponent<ObjData>().pivot = (MeshZeroer.Pivot)value;
			Debug.Log(((GameObject)Value) + "|" + (MeshZeroer.Pivot)value);
			MeshZeroer.MovePivotAdvanced(((GameObject)Value), (MeshZeroer.Pivot)value);
			ProjectManager.Instance.RefreshInspector();
			ProjectManager.Instance.gizmos.RefreshGizmos();
		};
	}

	public override bool SupportsType(Type type)
	{
		return type == typeof(GameObject);
	}

	protected override void OnBound(MemberInfo variable)
	{
		base.OnBound(variable);
	}

	protected override void OnUnbound()
	{
		base.OnUnbound();
	}

	protected override void GenerateElements()
	{
		HeaderVisibility = RuntimeInspector.HeaderVisibility.Hidden;
		switch (SelectionManager.Instance.mode)
        {
			case SelectionManager.SelectionMode.Object:
				DrawSingleSelectionInspector();
				break;
			case SelectionManager.SelectionMode.Submesh:
				DrawSubSelectionInspector();
				break;
        }
	}

	private void DrawSingleSelectionInspector()
    {
		GameObject obj = ((GameObject)Value);

		((StringField)CreateDrawer(typeof(string), "Name", nameGetter, nameSetter)).SetterMode = StringField.Mode.OnSubmit;

		CreateDrawer(typeof(bool), "Hidden", isActiveGetter, isActiveSetter);

		((ExpandableInspectorField)CreateDrawerForComponent(obj.transform)).IsExpanded = true;

		CreateDrawer(typeof(Tags), "Tracking as", tagGetter, tagSetter);

		CreateDrawer(typeof(MeshZeroer.Pivot), "Pivot", pivotGetter, pivotSetter);

		MeshRenderer r = obj.GetComponent<MeshRenderer>();
		if (r)
		{
			((ExpandableInspectorField)CreateDrawerForComponent(r)).IsExpanded = true;
		}

		OptionalMaterials o = obj.GetComponent<OptionalMaterials>();
		if (o)
		{
			((ExpandableInspectorField)CreateDrawerForComponent(o)).IsExpanded = true;
		}

		Light l = obj.GetComponent<Light>();
		if (l)
		{
			CreateDrawerForComponent(l);
		}
	}

	private void DrawMultiSelectionInspector()
    {
		GameObject obj = ((GameObject)Value);
		NameRaw = "Multiselect";

		((StringField)CreateDrawer(typeof(string), "Name", nameGetter, nameSetter)).SetterMode = StringField.Mode.OnSubmit;

		CreateDrawer(typeof(bool), "Hidden", isActiveGetter, isActiveSetter);

		((ExpandableInspectorField)CreateDrawerForComponent(obj.transform)).IsExpanded = true;

		CreateDrawer(typeof(Tags), "Tracking as", tagGetter, tagSetter);

		MeshRenderer r = obj.GetComponent<MeshRenderer>();
		if (r)
		{
			((ExpandableInspectorField)CreateDrawerForComponent(r)).IsExpanded = true;
		}

		OptionalMaterials o = obj.GetComponent<OptionalMaterials>();
		if (o)
		{
			((ExpandableInspectorField)CreateDrawerForComponent(o)).IsExpanded = true;
		}

		Light l = obj.GetComponent<Light>();
		if (l)
		{
			CreateDrawerForComponent(l);
		}
	}

	private void DrawSubSelectionInspector()
    {
		GameObject obj = ((GameObject)Value);

		MeshRenderer r = obj.GetComponent<MeshRenderer>();
		if (r)
		{
			((ExpandableInspectorField)CreateDrawerForComponent(r)).IsExpanded = true;
		}
	}

    public override void Refresh()
    {
        base.Refresh();
    }

	protected override void OnDepthChanged()
	{

	}
}

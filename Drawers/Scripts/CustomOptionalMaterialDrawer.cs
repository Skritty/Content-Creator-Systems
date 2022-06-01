using System.Collections.Generic;
using UnityEngine;
using RuntimeInspectorNamespace;
using System;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class CustomOptionalMaterialDrawer : ExpandableInspectorField
{
    protected override int Length
    {
        get
        {
            return ((OptionalMaterials)Value).data.optionalMaterials.Count;
        }
    }

    public override bool SupportsType(Type type)
    {
        return type == typeof(OptionalMaterials);
    }

    protected override void GenerateElements()
    {
        //HeaderVisibility = RuntimeInspector.HeaderVisibility.Hidden;

        ObjData data = ((OptionalMaterials)Value).data;
        for (int i = 0; i < data.optionalMaterials.Count; i++)
        {
            int j = i;
            ExpandableInspectorField drawer = (ExpandableInspectorField)CreateDrawer(typeof(Material), $"Material {i + 1}",
                () => {
                    return data.optionalMaterials[j];
                },
                (value) => {
                    data.optionalMaterials[j] = PresetLibrary.Get((Material)value);
                }, false);

            if (j == 0) drawer.IsExpanded = true;
            CustomMaterialDrawer materialDrawer = (CustomMaterialDrawer)drawer;
            if (materialDrawer)
            {
                materialDrawer.index = j;
                materialDrawer.canRemove = true;
                materialDrawer.canUnique = false;
                //materialDrawer.data = data;
                //materialDrawer.obj = ((OptionalMaterials)Value).transform;
                //materialDrawer.UpdateUniqueToggle();
                materialDrawer.OnRemovePressed += () =>
                {
                    data.optionalMaterials.RemoveAt(materialDrawer.index);
                };
            }
        }
    }

    public void AddMaterial()
    {
        ObjData data = ((OptionalMaterials)Value).data;
        data.optionalMaterials.Add(PresetLibrary.Get<Material>("Default"));
        Debug.Log(data.optionalMaterials.Count);
    }

    //protected override void OnDepthChanged()
    //{

    //}
}
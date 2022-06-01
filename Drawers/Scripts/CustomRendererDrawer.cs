using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using RuntimeInspectorNamespace;
using System.Collections.Generic;
using System.Linq;

public class CustomRendererDrawer : ExpandableInspectorField
{
    protected override int Length { get
        {
            if (SelectionManager.Instance.mode == SelectionManager.SelectionMode.Submesh && !SelectionManager.Instance.IsMultiselecting)
                return 1;

            MeshRenderer r = ((MeshRenderer)Value);
            HashSet<Material> exists = new HashSet<Material>();
            for (int i = 0; i < r.sharedMaterials.Length; i++)
            {
                if (exists.Contains(r.sharedMaterials[i])) continue;
                exists.Add(r.sharedMaterials[i]);
            }
            return exists.Count;
        } }

    public override bool SupportsType(Type type)
    {
        return type == typeof(MeshRenderer);
    }

    protected override void GenerateElements()
    {
        HeaderVisibility = RuntimeInspector.HeaderVisibility.Hidden;
        
        if (SelectionManager.Instance.mode == SelectionManager.SelectionMode.Submesh && !SelectionManager.Instance.IsMultiselecting)
        {
            DrawSingleMaterial();
        }
        else
        {
            DrawAllMaterials();
        }
    }

    private void DrawAllMaterials()
    {
        MeshRenderer r = ((MeshRenderer)Value);
        HashSet<Material> exists = new HashSet<Material>();
        for (int i = 0; i < r.sharedMaterials.Length; i++)
        {
            int index = i;
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            r.GetPropertyBlock(block, index);
            Preset<Material> pr = PresetLibrary.GetPresets<Material>().Find(
                        x =>
                        {
                            if (x == null) return false;
                            MaterialPropertyBlock b = new MaterialPropertyBlock();
                            r.GetPropertyBlock(b, index);
                            return x.PresetReference.EqualsPropertyBlock(b);
                        });
            Material p = pr == null ? r.sharedMaterials[i] : pr.PresetReference;
            if (exists.Contains(p)) continue;
            exists.Add(p);

            CreateDrawer(typeof(Material), $"Material {index + 1}",
                () =>
                {
                    return PresetLibrary.GetPresets<Material>().Find(
                        x =>
                        {
                            if (x == null) return false;
                            MaterialPropertyBlock b = new MaterialPropertyBlock();
                            r.GetPropertyBlock(b, index);
                            return x.PresetReference.EqualsPropertyBlock(b);
                        });
                },
                (value) =>
                {
                    r.LogState(() => block, (x) => {
                        
                        Material m2 = PresetLibrary.GetPresets<Material>().Find(
                        y =>
                        {
                            MaterialPropertyBlock b = new MaterialPropertyBlock();
                            r.GetPropertyBlock(b, index);
                            return y.PresetReference.EqualsPropertyBlock(b);
                        }).PresetReference;
                        for (int j = 0; j < r.sharedMaterials.Length; j++)
                        {
                            MaterialPropertyBlock b2 = new MaterialPropertyBlock();
                            r.GetPropertyBlock(b2, j);
                            if (m2.EqualsPropertyBlock(b2))
                                r.SetPropertyBlock(((Material)value).ToPropertyBlock(), j);
                        }
                    });
                    for (int j = 0; j < r.sharedMaterials.Length; j++)
                    {
                        MaterialPropertyBlock b1 = new MaterialPropertyBlock();
                        r.GetPropertyBlock(b1, j);
                        MaterialPropertyBlock b2 = new MaterialPropertyBlock();
                        r.GetPropertyBlock(b2, j);
                        if (b1.Equals(b2, r.sharedMaterials[index]))
                            r.SetPropertyBlock(((Material)value).ToPropertyBlock(), j);
                    }
                        
                });

            //CreateDrawer(typeof(Material), $"Material {index + 1}",
            //    () => {
            //        return r.sharedMaterials[index].PropertyBlock();
            //    },
            //    (value) => {
            //        //r.LogState(() => block, 
            //        //    (x) => 
            //        //    {
            //        //        r.SetPropertyBlock(block);
            //        //    });
            //        Material current = r.sharedMaterials[index];
            //        for (int j = 0; j < r.sharedMaterials.Length; j++)
            //            if (r.sharedMaterials[j] == current)
            //                r.SetPropertyBlock((MaterialPropertyBlock)value);
            //    }, false);

            //drawer.IsExpanded = true;
            //drawer.index = index;
            //drawer.canRemove = false;
            //drawer.canUnique = true;
        }
    }

    private void DrawSingleMaterial()
    {
        MeshRenderer r = ((MeshRenderer)Value);
        int index = SelectionManager.Instance.CurrentSubmeshIndex;
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        r.GetPropertyBlock(block, index);

        CreateDrawer(typeof(Material), $"Material {index + 1}",
            () =>
            {
                return PresetLibrary.GetPresets<Material>().Find(
                    x =>
                    {
                        MaterialPropertyBlock b = new MaterialPropertyBlock();
                        r.GetPropertyBlock(b, index);
                        return x.PresetReference.EqualsPropertyBlock(b);
                    });
            },
            (value) =>
            {
                r.LogState(() => block, (x) => r.SetPropertyBlock(block, index));
                r.SetPropertyBlock(((Material)value).ToPropertyBlock(), index);
            });

        //CustomMaterialDrawer drawer = (CustomMaterialDrawer)CreateDrawer(typeof(MaterialPropertyBlock), $"Material {index + 1}",
        //    () => {
        //        return r.sharedMaterials[index].ToPropertyBlock();
        //    },
        //    (value) => {
        //        r.LogState(() => block, (x) => r.SetPropertyBlock(block));
        //        r.SetPropertyBlock((MaterialPropertyBlock)value);
        //    }, false);

        //drawer.IsExpanded = true;
        //drawer.index = index;
        //drawer.canRemove = false;
        //drawer.canUnique = true;
    }

    protected override void OnDepthChanged()
    {

    }
}

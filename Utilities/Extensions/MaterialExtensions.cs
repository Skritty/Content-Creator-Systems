using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public enum BlendMode { Opaque, Cutout, Fade, Transparent }
public static class MaterialExtensions
{
    public static void SwitchBlendMode(this Material material, BlendMode blendMode)
    {
        switch (blendMode)
        {
            case BlendMode.Opaque:
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                material.SetInt("_ZWrite", 1);
                material.DisableKeyword("_ALPHATEST_ON");
                material.DisableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = -1;
                break;
            case BlendMode.Cutout:
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                material.SetInt("_ZWrite", 1);
                material.EnableKeyword("_ALPHATEST_ON");
                material.DisableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = 2450;
                break;
            case BlendMode.Fade:
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.DisableKeyword("_ALPHATEST_ON");
                material.EnableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = 3000;
                break;
            case BlendMode.Transparent:
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.DisableKeyword("_ALPHATEST_ON");
                material.DisableKeyword("_ALPHABLEND_ON");
                material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = 3000;
                break;
        }
    }

    public static MaterialPropertyBlock ToPropertyBlock(this Material material)
    {
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        for (int i = 0; i < material.GetPropertyCount(); i++)
        {
            var value = material.GetProperty(i);
            string name = material.GetPropertyName(i);
            if (value == null) continue;
            switch (material.GetPropertyType(i))
            {
                case 0:
                    block.SetColor(name, (Color)value);
                    break;
                case 1:
                    block.SetVector(name, (Vector4)value);
                    break;
                case 2:
                    block.SetFloat(name, (float)value);
                    break;
                case 3:
                    block.SetFloat(name, (float)value);
                    break;
                case 4:
                    block.SetTexture(name, (Texture)value);
                    break;
            }
        }
        return block;
    }

    public static bool EqualsPropertyBlock(this Material self, MaterialPropertyBlock other)
    {
        bool equals = true;
        for (int i = 0; i < self.GetPropertyCount(); i++)
        {
            string name = self.GetPropertyName(i);
            switch (self.GetPropertyType(i))
            {
                case 0:
                    if (self.GetColor(name) != other.GetColor(name))
                        equals = false;
                    break;
                case 1:
                    if (self.GetVector(name) != other.GetVector(name))
                        equals = false;
                    break;
                case 2:
                    if (self.GetFloat(name) != other.GetFloat(name))
                        equals = false;
                    break;
                case 3:
                    if (self.GetFloat(name) != other.GetFloat(name))
                        equals = false;
                    break;
                case 4:
                    if (self.GetTexture(name) != other.GetTexture(name))
                        equals = false;
                    break;
            }
        }
        return equals;
    }

    public static bool Equals(this MaterialPropertyBlock self, MaterialPropertyBlock other, Material referenceMaterial)
    {
        bool equals = true;
        for (int i = 0; i < referenceMaterial.GetPropertyCount(); i++)
        {
            string name = referenceMaterial.GetPropertyName(i);
            switch (referenceMaterial.GetPropertyType(i))
            {
                case 0:
                    if (self.GetColor(name) != other.GetColor(name))
                        equals = false;
                    break;
                case 1:
                    if (self.GetVector(name) != other.GetVector(name))
                        equals = false;
                    break;
                case 2:
                    if (self.GetFloat(name) != other.GetFloat(name))
                        equals = false;
                    break;
                case 3:
                    if (self.GetFloat(name) != other.GetFloat(name))
                        equals = false;
                    break;
                case 4:
                    if (self.GetTexture(name) != other.GetTexture(name))
                        equals = false;
                    break;
            }
        }
        return equals;
    }
}

public static class ShaderUtilInterface
{
    public static Dictionary<string, MethodInfo> methods = new Dictionary<string, MethodInfo>();

    static ShaderUtilInterface()
    {
        var asm = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetTypes().Any(t => t.Name == "ShaderUtil"));
        if (asm != null)
        {
            var tp = asm.GetTypes().FirstOrDefault(t => t.Name == "ShaderUtil");
            foreach (var method in tp.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
            {
                methods[method.Name] = method;
            }
        }
    }

    public static List<Texture> GetTextures(this Material shader)
    {
        var list = new List<Texture>();
        var count = shader.GetPropertyCount();
        for (var i = 0; i < count; i++)
        {
            if (shader.GetPropertyType(i) == 4)
            {
                list.Add((Texture)shader.GetProperty(i));
            }
        }
        return list;
    }

    public static int GetPropertyCount(this Material shader)
    {
        return Call<int>("GetPropertyCount", shader.shader);
    }

    public static int GetPropertyType(this Material shader, int index)
    {
        return Call<int>("GetPropertyType", shader.shader, index);
    }

    public static string GetPropertyName(this Material shader, int index)
    {
        return Call<string>("GetPropertyName", shader.shader, index);
    }

    public static void SetProperty(this Material material, int index, object value)
    {
        var name = material.GetPropertyName(index);
        var type = material.GetPropertyType(index);
        switch (type)
        {
            case 0:
                material.SetColor(name, (Color)value);
                break;
            case 1:
                material.SetVector(name, (Vector4)value);
                break;
            case 2:
                material.SetFloat(name, (float)value);
                break;
            case 3:
                material.SetFloat(name, (float)value);
                break;
            case 4:
                material.SetTexture(name, (Texture)value);
                break;
        }
    }

    public static object GetProperty(this Material material, int index)
    {
        var name = material.GetPropertyName(index);
        var type = material.GetPropertyType(index);
        switch (type)
        {
            case 0:
                return material.GetColor(name);

            case 1:
                return material.GetVector(name);


            case 2:
            case 3:
                return material.GetFloat(name);

            case 4:
                return material.GetTexture(name);

        }
        return null;
    }

    public static T Call<T>(string name, params object[] parameters)
    {
        return (T)methods[name].Invoke(null, parameters);
    }
}
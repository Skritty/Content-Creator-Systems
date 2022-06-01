using UnityEngine.UI;
using System;
using System.Reflection;
using Object = UnityEngine.Object;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public static class ComponentExtensions
{
    public static void CopyComponentValues<T>(this T self, T other) where T : Component
    {
        foreach (PropertyInfo property in self.GetType().GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public))
        {
            if (!property.CanWrite || !property.CanRead) continue;

            property.SetValue(self, property.GetValue(other));
        }
    }

    public static void CopyComponentValues<T>(this T self, T other, params string[] exclude) where T : Component
    {
        foreach (PropertyInfo property in self.GetType().GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public))
        {
            if (!property.CanWrite || !property.CanRead || exclude.Contains(property.Name)) continue;

            property.SetValue(self, property.GetValue(other));
        }
    }

    public static void CopyComponentValues<T>(this T self, Dictionary<PropertyInfo, object> properties) where T : Component
    {
        foreach (KeyValuePair<PropertyInfo, object> property in properties)
        {
            if (!property.Key.CanWrite || !property.Key.CanRead) continue;

            property.Key.SetValue(self, property.Value);
        }
    }
}
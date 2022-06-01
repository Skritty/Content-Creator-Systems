using UnityEngine.UI;
using TMPro;
using System.Reflection;

public static class UnityUIExtensions
{
    static UnityUIExtensions()
    {
        toggleSetMethod = typeof(Toggle).GetMethod("Set", BindingFlags.NonPublic | BindingFlags.Instance);
    }

    private static MethodInfo toggleSetMethod;
    public static void Set(this Toggle instance, bool value, bool sendCallback)
    {
        toggleSetMethod.Invoke(instance, new object[] { value, sendCallback });
    }
}

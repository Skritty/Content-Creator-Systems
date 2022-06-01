using UnityEngine;

public static class TransformExtensions
{
    public static void DoFunctionToTree(this Transform root, System.Action<Transform> function)
    {
        function.Invoke(root);
        foreach (Transform child in root)
            DoFunctionToTree(child, function);
    }
}

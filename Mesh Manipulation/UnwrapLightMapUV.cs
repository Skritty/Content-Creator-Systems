using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
#if UNITY_Editor
[ExecuteInEditMode]
public class UnwrapLightMapUV : MonoBehaviour
{
    public Mesh[] toUnwrap;
    [Range(0, 180)]
    public float hardAngle = 88;
    [Range(1, 75)]
    public float angleError = 8;
    [Range(1, 75)]
    public float areaError = 15;
    [Range(0,64)]
    public float packingMargin = 8;
    private UnwrapParam unWrapParams;

    public bool unWrap;
    public void Update()
    {
        if (unWrap == true)
        {
            unWrap = false;
            Unwrap();
        }
    }
    public void Unwrap()
    {
        unWrapParams.areaError = areaError;
        unWrapParams.angleError = angleError;
        unWrapParams.hardAngle = hardAngle;
        unWrapParams.packMargin = packingMargin;
        foreach (Mesh mesh in toUnwrap)
        {
            Unwrapping.GenerateSecondaryUVSet(mesh, unWrapParams);
        }
    }
}
#endif
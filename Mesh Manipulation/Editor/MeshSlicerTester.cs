using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Threading.Tasks;

public class MeshSlicerTester : MonoBehaviour
{
    [ContextMenu("Slice into submeshes")]
    private void GetBoundaries()
    {
        foreach(MeshFilter meshFilter in FindObjectsOfType<MeshFilter>())
        {
            GameObject o = meshFilter.gameObject;//Selection.activeGameObject;
            MeshFilter mf;
            MeshRenderer mr;
            if (o && (mf = o.GetComponent<MeshFilter>()))
            {
                if (o && (mr = o.GetComponent<MeshRenderer>()))
                {

                    MeshSlicer.SliceMesh(mf, mr, o.transform.lossyScale);

                    Material[] newMats = new Material[mf.sharedMesh.subMeshCount];
                    for (int i = 0; i < newMats.Length; i++)
                        newMats[i] = mr.sharedMaterial;
                    mr.sharedMaterials = newMats;
                    for (int i = 0; i < newMats.Length; i++)
                    {
                        MaterialPropertyBlock block = new MaterialPropertyBlock();
                        mr.GetPropertyBlock(block, i);
                        block.SetColor("_Color", new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f)));
                        mr.SetPropertyBlock(block, i);
                    }
                }
            }
        }
    }
}

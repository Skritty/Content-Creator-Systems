using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshZeroer
{
    public enum Pivot { Center, Bottom, Top }

    /// <summary>
    /// Moves all the meshes vertices to where they should be if all gameobjects from the root are zeroed out (rotation and scale, not position)
    /// </summary>
    /// <param name="root">Everything here and down will be affected</param>
    public static void ZeroObjectMeshes(GameObject root, bool position = false, bool rotation = true, bool scale = true)
    {
        MeshFilter[] filters = root.GetComponentsInChildren<MeshFilter>();
        if (filters.Length == 0) return;
        //Debug.Log($"Zeroing {root.name}");

        // De-Warp verts
        for (int i = 0; i < filters.Length; i++)
        {
            ObjData data = filters[i].GetComponent<ObjData>();

            // Find the TRS to warp by
            Matrix4x4 trs = Matrix4x4.TRS(
                position ? filters[i].transform.position : Vector3.zero, 
                rotation ? filters[i].transform.rotation : Quaternion.identity, 
                scale ? filters[i].transform.lossyScale : Vector3.one);
            
            // Transform the verts
            Vector3[] vertices = filters[i].mesh.vertices;
            for (int j = 0; j < vertices.Length; j++)
            {
                vertices[j] = trs * vertices[j];
            }

            // Update the mesh
            filters[i].mesh.vertices = vertices;
            filters[i].mesh.RecalculateBounds();
            filters[i].mesh.RecalculateNormals();
            filters[i].mesh.RecalculateTangents();
            if (filters[i].GetComponent<MeshCollider>())
                filters[i].GetComponent<MeshCollider>().sharedMesh = filters[i].mesh;
        }

        // Set all objects to zero rotation and scale
        root.transform.DoFunctionToTree(o =>
        {
            if(position)
                o.localPosition = Vector3.zero;
            if(rotation)
                o.localRotation = Quaternion.identity;
            if(scale)
                o.localScale = Vector3.one;
        });
    }

    /// <summary>
    /// Changes the apparent pivot of the object passed in by offsetting its children
    /// </summary>
    /// <param name="root"></param>
    /// <param name="position"></param>
    public static void MovePivotSimple(GameObject root, Pivot position)
    {
        MeshRenderer[] renderers = root.GetComponentsInChildren<MeshRenderer>();
        if (renderers.Length == 0) return;
        Debug.Log($"Moving pivot of {root.name} to {position}");

        // Find the encapsulating bounds of the meshes
        Bounds objectBounds = renderers[0].bounds;
        foreach (MeshRenderer mr in renderers)
        {
            objectBounds.Encapsulate(mr.bounds);
        }

        // Find the pivot position
        Vector3 pivot = objectBounds.center;
        switch (position)
        {
            case Pivot.Bottom:
                pivot -= new Vector3(0, objectBounds.extents.y, 0);
                break;
            case Pivot.Top:
                pivot += new Vector3(0, objectBounds.extents.y, 0);
                break;
        }

        // FInd the offset and move everything by it
        Vector3 offset = pivot - root.transform.position;
        Debug.DrawLine(root.transform.position, pivot, Color.blue, 600f);
        root.transform.position = pivot;
        foreach (Transform child in root.transform)
        {
            child.position -= offset;
        }
    }

    /// <summary>
    /// WIP!!
    /// Changes the apparent pivot of the object passed in by offsetting all vertices in meshes under it
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="position"></param>
    public static void MovePivotAdvanced(GameObject root, Pivot position)
    {
        MeshRenderer[] renderers = root.GetComponentsInChildren<MeshRenderer>();
        MeshFilter[] filters = root.GetComponentsInChildren<MeshFilter>();
        if (renderers.Length == 0) return;
        Debug.Log($"Moving pivot of {root.name}");

        // Find the encapsulating bounds of the meshes
        Bounds objectBounds = renderers[0].bounds;
        foreach (MeshRenderer mr in renderers)
        {
            objectBounds.Encapsulate(mr.bounds);
        }

        // Find the pivot position
        Vector3 pivot = objectBounds.center;
        switch (position)
        {
            case Pivot.Bottom:
                pivot -= new Vector3(0, objectBounds.extents.y, 0);
                break;
            case Pivot.Top:
                pivot += new Vector3(0, objectBounds.extents.y, 0);
                break;
        }

        // Find the offset
        Vector3 offset = pivot - root.transform.position;
        root.transform.position = pivot;

        // Offset the verts
        for (int i = 0; i < renderers.Length; i++)
        {
            Vector3[] vertices = filters[i].mesh.vertices;
            for (int j = 0; j < vertices.Length; j++)
            {
                vertices[j] -= offset;
            }

            //renderers[i].transform.position += offset;

            filters[i].mesh.vertices = vertices;
            filters[i].mesh.RecalculateBounds();
            filters[i].mesh.RecalculateNormals();
            filters[i].mesh.RecalculateTangents();
            if (filters[i].GetComponent<MeshCollider>())
                filters[i].GetComponent<MeshCollider>().sharedMesh = filters[i].mesh;
        }
    }
}

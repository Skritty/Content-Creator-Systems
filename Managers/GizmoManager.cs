using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RTG;
using UnityEngine.EventSystems;
using RuntimeInspectorNamespace;
using System;

public class GizmoManager : Singleton<GizmoManager>
{
    public RuntimeHierarchy hierarchy;
    public GameObject gizmoTarget;
    public ObjectTransformGizmo objectMoveGizmo;
    public ObjectTransformGizmo objectRotationGizmo;
    public ObjectTransformGizmo objectScaleGizmo;
    delegate void LastGizmo(); 
    private LastGizmo lastUsedGizmo;

    public void Start()
    {
        //hierarchy.OnSelectionChanged += SetSelection;
        objectMoveGizmo = RTGizmosEngine.Get.CreateObjectMoveGizmo();
        objectRotationGizmo = RTGizmosEngine.Get.CreateObjectRotationGizmo();
        objectScaleGizmo = RTGizmosEngine.Get.CreateObjectScaleGizmo();
        objectMoveGizmo.Gizmo.SetEnabled(false);
        objectRotationGizmo.Gizmo.SetEnabled(false);
        objectScaleGizmo.Gizmo.SetEnabled(false);
        PositionGizmo();
    }

    public void Update()
    {
        if (Input.GetMouseButton(1)) return;
        if (Input.GetKeyDown(KeyCode.Q))
            NoGizmo();

        if (Input.GetKeyDown(KeyCode.W))
            PositionGizmo();

        if (Input.GetKeyDown(KeyCode.E))
            RotationGizmo();

        if (Input.GetKeyDown(KeyCode.R))
            ScaleGizmo();
    }

    public void PositionGizmo()
    {
        DisableGizmos();
        lastUsedGizmo = PositionGizmo;
        if (gizmoTarget == null || SelectionManager.Instance.mode == SelectionManager.SelectionMode.Submesh) return;
        objectMoveGizmo.RefreshPositionAndRotation();
        objectMoveGizmo.Gizmo.SetEnabled(true);
    }

    public void RotationGizmo()
    {
        DisableGizmos();
        lastUsedGizmo = RotationGizmo;
        //objectRotationGizmo.SetTransformPivot();
        if (gizmoTarget == null || SelectionManager.Instance.mode == SelectionManager.SelectionMode.Submesh) return;
        objectRotationGizmo.RefreshPositionAndRotation();
        objectRotationGizmo.Gizmo.SetEnabled(true);
    }

    public void ScaleGizmo()
    {
        DisableGizmos();
        lastUsedGizmo = ScaleGizmo;
        if (gizmoTarget == null || SelectionManager.Instance.mode == SelectionManager.SelectionMode.Submesh) return;
        objectScaleGizmo.RefreshPositionAndRotation();
        objectScaleGizmo.Gizmo.SetEnabled(true);
    }

    public void NoGizmo()
    {
        DisableGizmos();
        lastUsedGizmo = null;
    }

    public void DisableGizmos()
    {
        objectMoveGizmo.Gizmo.SetEnabled(false);
        objectRotationGizmo.Gizmo.SetEnabled(false);
        objectScaleGizmo.Gizmo.SetEnabled(false);
    }

    public void EnableLastGizmo()
    {
        if (lastUsedGizmo != null)
        {
            lastUsedGizmo.Invoke();
        }
    }

    public void SetSelection(Transform t)
    {
        if (t)
        {
            gizmoTarget = t.gameObject;
            objectMoveGizmo.SetTargetObject(t.gameObject);
            objectRotationGizmo.SetTargetObject(t.gameObject);
            objectScaleGizmo.SetTargetObject(t.gameObject);
            if (lastUsedGizmo != null)
            {
                lastUsedGizmo.Invoke();
            }
        }
        else
        {
            gizmoTarget = null;
            //objectMoveGizmo.Gizmo.SetEnabled(false);
            //objectRotationGizmo.Gizmo.SetEnabled(false);
            //objectScaleGizmo.Gizmo.SetEnabled(false);
        }
    }

    public void RefreshGizmos()
    {
        StartCoroutine(RefreshWait());
        IEnumerator RefreshWait()
        {
            yield return new WaitForEndOfFrame();
            Debug.Log("Refreshing Gizmos");
            objectMoveGizmo.SetEnabled(false);
            objectRotationGizmo.SetEnabled(false);
            objectScaleGizmo.SetEnabled(false);
            objectMoveGizmo.RefreshPositionAndRotation();
            objectRotationGizmo.RefreshPositionAndRotation();
            objectScaleGizmo.RefreshPositionAndRotation();
            objectMoveGizmo.SetEnabled(true);
            objectRotationGizmo.SetEnabled(true);
            objectScaleGizmo.SetEnabled(true);
        }
    }
}

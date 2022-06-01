using RTG;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaceOnSurface : MonoBehaviour
{
    private void Update()
    {
        if (ProjectManager.Instance.surfaceSnapping && RTGizmosEngine.Get.DraggedGizmo != null)
            RaycastSurfacePlacement();
    }

    private void RaycastSurfacePlacement()
    {
        if (!ProjectManager.Instance.surfaceSnapping) return;
        RaycastHit hit;
        int currentLayer = gameObject.layer;
        int newLayer = (int)Mathf.Log(LayerMask.GetMask("Selected Object Ignore"), 2);
        transform.DoFunctionToTree((o) => 
        {
            o.gameObject.layer = newLayer;
        });

        if (Physics.Raycast(Camera.main.transform.position, transform.position - Camera.main.transform.position, out hit, 10000f, ~LayerMask.GetMask("Selected Object Ignore")))
        {
            transform.position = hit.point;
        }

        transform.DoFunctionToTree((o) =>
        {
            o.gameObject.layer = currentLayer;
        });

        ProjectManager.Instance.gizmos.RefreshGizmos();
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DragUIElement : MonoBehaviour, IDragHandler, IBeginDragHandler, IPointerDownHandler
{
    private static System.Action RevertSortOrder;
    [SerializeField]
    private Canvas canvas;
    [SerializeField]
    private RectTransform toMove;
    private Vector2 offset;
    private int order = 0;

    private void Awake()
    {
        order = canvas.sortingOrder;
        RevertSortOrder += () => canvas.sortingOrder = order;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        offset = eventData.position - (Vector2)toMove.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        toMove.position = new Vector2(Mathf.Clamp(eventData.position.x, 0, Screen.width), Mathf.Clamp(eventData.position.y, 0, Screen.height)) - offset;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        RevertSortOrder?.Invoke();
        canvas.sortingOrder = 1;
    }
}

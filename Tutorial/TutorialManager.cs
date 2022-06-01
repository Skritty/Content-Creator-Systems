using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;

    public GameObject darkener;
    public RectTransform inverseMask;
    public bool disabledOnStartup;
    public List<TutorialPanel> panels = new List<TutorialPanel>();

    [Space()]
    public UnityEvent OnNewProject;
    public UnityEvent OnObjectStartImport;
    public UnityEvent OnObjectFinishImport;
    public UnityEvent OnTextureImport;
    public UnityEvent OnGizmoChange;
    public UnityEvent OnSelectionTypeChange;
    public UnityEvent OnObjectSelect;

    private void Start()
    {
        if (Instance)
            Destroy(this);
        else Instance = this;

        int i = 0;
        foreach(TutorialPanel t in panels)
        {
            t.id = i;

            t.count.text = $"{t.id + 1}/{panels.Count}";
            if(t.number)
                t.number.text = $"{t.id}";

            if (t.id == panels.Count - 1)
            {
                t.nextButton.gameObject.SetActive(false);
                t.doneButton.gameObject.SetActive(true);
            }
            else t.doneButton.gameObject.SetActive(false);
            if(t.altNextButton != null || t.altContinue)
            {
                t.nextButton.gameObject.SetActive(false);
                t.doneButton.gameObject.SetActive(false);
            }

            if (t.id == 0)
            {
                t.previousButton.gameObject.SetActive(false);
                t.gameObject.SetActive(true);
            }
            else
            {
                if(panels[t.id - 1].noReturn)
                    t.previousButton.gameObject.SetActive(false);
                t.gameObject.SetActive(false);
            }
            i++;
        }
        if (disabledOnStartup)
        {
            panels[0].gameObject.SetActive(false);
            darkener.SetActive(false);
            inverseMask.gameObject.SetActive(false);
        }
    }

    public void PositionMask(RectTransform[] UIelements)
    {
        if (UIelements.Length == 0)
        {
            inverseMask.gameObject.SetActive(false);
            return;
        }
        else inverseMask.gameObject.SetActive(true);
        Vector2 max = Vector2.negativeInfinity;
        Vector2 min = Vector2.positiveInfinity;
        foreach(RectTransform rt in UIelements)
        {
            if (rt.rect.min.x * rt.lossyScale.x + rt.position.x < min.x)
                min.x = rt.rect.min.x * rt.lossyScale.x + rt.position.x;
            if (rt.rect.min.y * rt.lossyScale.y + rt.position.y < min.y)
                min.y = rt.rect.min.y * rt.lossyScale.y + rt.position.y;

            if (rt.rect.max.x * rt.lossyScale.x + rt.position.x > max.x)
                max.x = rt.rect.max.x * rt.lossyScale.x + rt.position.x;
            if (rt.rect.min.y * rt.lossyScale.y + rt.position.y > max.y)
                max.y = rt.rect.max.y * rt.lossyScale.y + rt.position.y;
        }
        inverseMask.sizeDelta = max-min;
        //Vector3 position = new Vector3();
        //RectTransformUtility.ScreenPointToWorldPointInRectangle(inverseMask, (max + min), null, out position);
        Vector2 localPoint;
        Vector2 screenP = RectTransformUtility.WorldToScreenPoint(null, (max + min) / 2);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(inverseMask, (max + min) / 2, null, out localPoint);
        inverseMask.position = (max + min) / 2;
    }
}

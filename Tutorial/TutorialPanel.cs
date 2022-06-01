using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TutorialPanel : MonoBehaviour
{
    public bool noReturn;
    public bool altContinue;
    public RectTransform[] highlightTargets;
    private Transform previousParent;
    public TextMeshProUGUI count;
    public TextMeshProUGUI number;
    public Button altNextButton;
    public Button previousButton;
    public Button nextButton;
    public Button skipButton;
    public Button doneButton;
    [HideInInspector]
    public int id;

    private void OnEnable()
    {
        previousButton.onClick.AddListener(Previous);
        if (altNextButton)
            altNextButton.onClick.AddListener(Next);
        nextButton.onClick.AddListener(Next);
        skipButton.onClick.AddListener(Skip);
        doneButton.onClick.AddListener(Skip);
    }

    public void OnEnter()
    {
        TutorialManager.Instance.PositionMask(highlightTargets);
    }

    public void EnableTutorialPanel()
    {
        gameObject.SetActive(true);
        TutorialManager.Instance.darkener.SetActive(true);
        TutorialManager.Instance.inverseMask.gameObject.SetActive(true);
        OnEnter();
    }

    private void OnDisable()
    {
        previousButton.onClick.RemoveListener(Previous);
        if (altNextButton)
            altNextButton.onClick.RemoveListener(Next);
        nextButton.onClick.RemoveListener(Next);
        skipButton.onClick.RemoveListener(Skip);
        doneButton.onClick.RemoveListener(Skip);
    }

    public void Skip()
    {
        if (!gameObject.activeSelf) return;
        foreach (TutorialPanel p in TutorialManager.Instance.panels)
        {
            p.gameObject.SetActive(false);
        }
        TutorialManager.Instance.darkener.SetActive(false);
        TutorialManager.Instance.inverseMask.gameObject.SetActive(false);
    }

    public void Previous()
    {
        if (!gameObject.activeSelf) return;
        TutorialManager.Instance.panels[id - 1].gameObject.SetActive(true);
        TutorialManager.Instance.panels[id - 1].OnEnter();
        gameObject.SetActive(false);
    }

    public void Next()
    {
        if (!gameObject.activeSelf) return;
        if (id + 1 == TutorialManager.Instance.panels.Count)
        {
            Skip();
            return;
        }
        TutorialManager.Instance.panels[id + 1].gameObject.SetActive(true);
        TutorialManager.Instance.panels[id + 1].OnEnter();
        gameObject.SetActive(false);
    }
}

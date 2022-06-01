using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Tutorial : MonoBehaviour
{

    public GameObject firstTimePanel;
    public GameObject[] tutorialPanels;
    void Start()
    {
        //int hasPlayed = PlayerPrefs.GetInt("HasPlayed");
        //Debug.Log(hasPlayed);

        //if (hasPlayed == 0)
        //{
        //    // First Time
        //    // show First time Panel
        //    firstTimePanel.SetActive(true);
        //    PlayerPrefs.SetInt("HasPlayed", 1);
        //}
        //else
        //{
        //    // Not First Time
        //}
    }

    public void StartTutorial()
    {
        tutorialPanels[0].SetActive(true);
    }

    public void AdvanceTutorial()
    {
        foreach(GameObject currentPanel in tutorialPanels)
        {
            if(currentPanel.activeInHierarchy == true)
            {
                var index = System.Array.IndexOf(tutorialPanels, currentPanel);
                Debug.Log(tutorialPanels.Length);
                Debug.Log(index);
                if(index < tutorialPanels.Length - 1)
                    tutorialPanels[index + 1].SetActive(true);

                tutorialPanels[index].SetActive(false);
                break;
            }
        }
    }


}

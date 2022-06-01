using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActionLog : MonoBehaviour
{
    private static ActionLog Instance;
    [SerializeField] TMPro.TextMeshProUGUI textBox;
    private void Start()
    {
        if (Instance)
            Destroy(Instance);
        Instance = this;
    }

    public static void Log(dynamic message)
    {
        if (Instance && Instance.textBox)
            Instance.textBox.text = message.ToString();
    }
}

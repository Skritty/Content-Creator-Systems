using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


public class DoOnEnable : MonoBehaviour
{
    public UnityEvent toDo = new UnityEvent();

    private void OnEnable()
    {
        toDo.Invoke();
    }
}

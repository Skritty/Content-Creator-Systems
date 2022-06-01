using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    public static T Instance { get; private set; }

    protected void Awake()
    {
        if(Instance == null)
        {
            if(this is T)
            {
                Instance = (T)System.Convert.ChangeType(this, typeof(T));
            }
        }
        else if(Instance != this)
        {
            Destroy(this);
        }
    }

    protected void OnDestroy()
    {
        if (this is T && ((T)System.Convert.ChangeType(this, typeof(T))).Equals(Instance))
        {
            Instance = null;
        }
    }
}

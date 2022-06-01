using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[System.Obsolete("A failed experiment, do not use")]
public abstract class SOSingleton<T> : ScriptableObject, ISerializationCallbackReceiver where T : ScriptableObject
{
    [SerializeField]
    private T savedInstance;
    protected static T Instance { get; private set; }

    private void OnEnable()
    {
        Instance = savedInstance;
    }

    [ExecuteAlways]
    protected void Awake()
    {
        Instance = savedInstance;
        if (Instance == null)
        {
            if (this is T)
            {
                Instance = (T)System.Convert.ChangeType(this, typeof(T));
            }
        }
        else if (Instance != this)
        {

            //Destroy(this);
#if UNITY_EDITOR
            //Debug.Log(UnityEditor.AssetDatabase.GetAssetPath(Instance));
            //UnityEditor.AssetDatabase.DeleteAsset(UnityEditor.AssetDatabase.GetAssetPath(this));
#endif
        }
        savedInstance = Instance;
    }

    [ExecuteAlways]
    protected void OnDestroy()
    {
        if (this is T && ((T)System.Convert.ChangeType(this, typeof(T))).Equals(Instance))
        {
            Instance = null;
        }
    }

    public void OnBeforeSerialize()
    {
        savedInstance = Instance;
    }

    public void OnAfterDeserialize()
    {
        Instance = savedInstance;
    }
}

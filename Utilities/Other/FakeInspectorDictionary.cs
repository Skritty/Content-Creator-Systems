using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Works like a dictionary in code, looks like an array of container classes in the inspector
/// </summary>
/// <typeparam name="Key"></typeparam>
/// <typeparam name="Value"></typeparam>
[System.Serializable]
public class FakeInspectorDictionary<Key, Value> where Value : Object
{
    [SerializeField]
    private Element[] elements;

    [System.Serializable]
    private class Element
    {
        public Key key;
        public Value value;
    }

    private Dictionary<Key, Value> _internalDictionary;
    public Dictionary<Key, Value> Dictionary
    {
        get
        {
            if (_internalDictionary == null)
            {
                _internalDictionary = new Dictionary<Key, Value>();
                foreach (Element e in elements)
                    _internalDictionary.Add(e.key, e.value);
            }
            return _internalDictionary;
        }
    }

    public T Get<T>(Key key) where T : Value
    {
        return Dictionary[key] as T;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RuntimeInspectorNamespace;

public class PseudoSceneManager : MonoBehaviour
{
    public RuntimeHierarchy hierarchy;
    public PseudoSceneSourceTransform transformer;
    public GameObject testObj;
    public string sceneName;

    private void Start()
    {
        hierarchy.CreatePseudoScene(sceneName = "test scene");
        transformer.SceneName = sceneName;
        hierarchy.AddToPseudoScene(sceneName, Instantiate(testObj).transform);
    }
}

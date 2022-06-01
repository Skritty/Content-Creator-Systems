using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class DynamicUIContainer : UIBehaviour
{
    public FakeInspectorDictionary<string, UIBehaviour> graphicElements;
    public Dictionary<string, dynamic> data = new Dictionary<string, dynamic>();
}

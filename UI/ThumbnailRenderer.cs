using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ThumbnailRenderer : UIBehaviour
{
    public MeshRenderer display;
    public Camera cam;
    [SerializeField]
    private bool renderOnStart = false;
    private RenderTexture rt;

    protected override void Awake()
    {
        cam.transform.position += Vector3.up * 10000;
        rt = new RenderTexture(512, 512, 1);
        RawImage image = GetComponent<RawImage>();
        if(renderOnStart && image)
        {
            image.texture = RenderThumbnail();
        }
    }

    public Texture RenderThumbnail(MaterialPropertyBlock block)
    {
        bool makeLight = true;
        Light l = null;
        foreach (Light light in FindObjectsOfType<Light>())
            if (light.gameObject.activeSelf && light.enabled && light.type == LightType.Directional)
                makeLight = false;
        if (makeLight)
        {
            l = new GameObject().AddComponent<Light>();
            l.transform.parent = cam.transform;
            l.transform.rotation = Quaternion.Euler(30, 30, 0);
            l.type = LightType.Directional;
        }
        display.SetPropertyBlock(block);
        cam.targetTexture = rt;
        cam.Render();
        if (l != null)
        {
            l.enabled = false;
            Destroy(l.gameObject);
        }
        return rt;
    }

    public Texture RenderThumbnail()
    {
        bool makeLight = true;
        Light l = null;
        foreach (Light light in FindObjectsOfType<Light>())
            if (light.gameObject.activeSelf && light.enabled && light.type == LightType.Directional)
                makeLight = false;
        if (makeLight)
        {
            l = new GameObject().AddComponent<Light>();
            l.transform.parent = cam.transform;
            l.transform.rotation = Quaternion.Euler(30, 30, 0);
            l.type = LightType.Directional;
        }
        cam.targetTexture = rt;
        cam.Render();
        if (l != null)
        {
            l.enabled = false;
            Destroy(l.gameObject);
        }
        return rt;
    }
}

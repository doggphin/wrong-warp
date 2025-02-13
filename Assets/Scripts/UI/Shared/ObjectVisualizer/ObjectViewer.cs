using System;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class ObjectViewer : MonoBehaviour
{
    [SerializeField] private RenderTexture renderToTexture;
    public RenderTexture RenderToTexture { get => renderToTexture; }
    private Camera cam;

    void Awake() {
        cam = GetComponent<Camera>();
        cam.targetTexture = renderToTexture;
        cam.enabled = false;
        cam.cullingMask = LayerMask.GetMask("ObjectViewer");
    }


    public void TakeRotatedImage(ViewableObject viewableObject, float rotX, float rotY) {
        viewableObject.CacheAndSetLayers(LayerMask.NameToLayer("ObjectViewer"));

        cam.transform.position = viewableObject.transform.position + (Quaternion.Euler(rotY, rotX, 0) * Vector3.forward * viewableObject.ViewDistance);
        cam.transform.LookAt(viewableObject.transform, Vector3.up);
        cam.transform.position += viewableObject.ViewOffset;
        // Error occurs when window is resized -- "Recursive rendering is not supported in SRP"
        // Almost certainly occurs when derivations of ObjectViewer call TakeRotatedImage twice in the same update loop, but who gives a shit, this doesn't matter much
        cam.Render();

        viewableObject.ResetLayers();
    }


    public Vector2 GetScreenSpacePosition(Vector3 worldSpacePosition) {
        return cam.WorldToScreenPoint(worldSpacePosition);
    }
}

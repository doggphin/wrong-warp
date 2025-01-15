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
        cam.Render();

        viewableObject.ResetLayers();
    }
}

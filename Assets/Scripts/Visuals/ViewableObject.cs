using UnityEngine;

public class ViewableObject : MonoBehaviour
{
    [SerializeField] private GameObject[] viewableObjects;
    [SerializeField] private Vector3 viewOffset;
    [SerializeField] private float viewDistance;
    public Vector3 ViewOffset => viewOffset;
    public float ViewDistance => viewDistance;

    private int[] layersCache;

    void Awake() {
        layersCache = new int[viewableObjects.Length];
    }

    bool layersAreSet = false;
    public void ResetLayers() {
        if(!layersAreSet) {
            Debug.LogError("Cannot reset ViewableObject layers without setting them first!");
            return;
        }
        
        for(int i=0; i<layersCache.Length; i++) {
            viewableObjects[i].layer = viewableObjects[i].layer;
        }

        layersAreSet = false;
    }

    public void CacheAndSetLayers(int layer) {
        if(layersAreSet) {
            Debug.LogError("Cannot cache and set ViewableObject layers without resetting first!");
            return;
        }

        layersAreSet = true;

        for(int i=0; i<viewableObjects.Length; i++) {
            layersCache[i] = viewableObjects[i].layer;
            viewableObjects[i].layer = layer;
        }
    }
}

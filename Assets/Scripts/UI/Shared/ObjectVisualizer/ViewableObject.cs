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
        SaveLayers();
    }

    bool layersAreSet = false;
    public void CacheAndSetLayers(int layer) {
        if(layersAreSet) {
            return;
        }

        for(int i=0; i<viewableObjects.Length; i++) {
            layersCache[i] = viewableObjects[i].layer;
            viewableObjects[i].layer = layer;
        }

        layersAreSet = true;
    }

    public void ResetLayers() {
        if(!layersAreSet) {
            return;
        }
        
        SaveLayers();

        layersAreSet = false;
    }

    private void SaveLayers() {
        for(int i=0; i<layersCache.Length; i++) {
            viewableObjects[i].layer = viewableObjects[i].layer;
        }
    }
}

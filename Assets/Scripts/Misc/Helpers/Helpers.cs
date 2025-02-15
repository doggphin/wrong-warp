using UnityEngine;

public static class Helpers {
    public static T InstantiateAndGetComponent<T>(Transform source, GameObject instantiate) where T : MonoBehaviour {
        return Object.Instantiate(instantiate, source).GetComponent<T>();
    }
}
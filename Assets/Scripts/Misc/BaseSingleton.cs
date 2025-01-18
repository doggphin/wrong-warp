using UnityEngine;

public abstract class BaseSingleton<T> : MonoBehaviour where T : MonoBehaviour
{
    public static T Instance { get; private set; }

    protected virtual void Awake() {
        if(Instance != null) {
            Destroy(gameObject);
            return;
        } else {
            Instance = this as T;
            DontDestroyOnLoad(gameObject);
        }
    }

    protected virtual void OnDestroy() {
        Instance = null;
        Destroy(gameObject);
    }
}
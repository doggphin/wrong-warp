using UnityEngine;
using UnityEngine.Analytics;

public abstract class BaseSingleton<T> : MonoBehaviour where T : MonoBehaviour
{
    public static T Instance { get; private set; }

    protected virtual void Awake() {   
        if(Instance != null) {
            Destroy(this);
            return;
        }
        
        Instance = this as T;
    }

    protected virtual void OnDestroy() {
        Instance = null;
        Destroy(gameObject);
    }
}
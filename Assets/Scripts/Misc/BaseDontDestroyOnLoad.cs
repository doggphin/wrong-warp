using UnityEngine;

public class BaseDontDestroyOnLoad : MonoBehaviour
{
    void Awake() {
        DontDestroyOnLoad(gameObject);
    }
}

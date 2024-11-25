using Networking.Shared;
using UnityEngine;

public class SpinnerTest : MonoBehaviour
{

    WEntityBase entityBase = null;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        entityBase = GetComponent<WEntityBase>();
    }

    // Update is called once per frame
    void Update()
    {
        entityBase.currentPosition = new Vector3(Mathf.Cos(Time.time * 10), 0, Mathf.Sin(Time.time * 10));
    }
}

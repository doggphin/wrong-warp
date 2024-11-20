using UnityEngine;

public class SpinnerTest : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = new Vector3(Mathf.Cos(Time.time * 10), 0, Mathf.Sin(Time.time * 10));
    }
}

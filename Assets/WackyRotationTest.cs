using UnityEngine;

public class WackyRotationTest : MonoBehaviour
{
    public Transform thingToLookAt;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.LookAt(thingToLookAt);

        float x = NetDataExtensions.DecompressNormalizedFloat(NetDataExtensions.CompressNormalizedFloat(transform.rotation.x));
        float y = NetDataExtensions.DecompressNormalizedFloat(NetDataExtensions.CompressNormalizedFloat(transform.rotation.y));
        float z = NetDataExtensions.DecompressNormalizedFloat(NetDataExtensions.CompressNormalizedFloat(transform.rotation.z));
        float w = NetDataExtensions.DecompressNormalizedFloat(NetDataExtensions.CompressNormalizedFloat(transform.rotation.w));
        transform.rotation = new Quaternion(x, y, z, w);
    }
}

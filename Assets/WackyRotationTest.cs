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

        float x = WExtensions.DecompressNormalizedFloat(WExtensions.CompressNormalizedFloat(transform.rotation.x));
        float y = WExtensions.DecompressNormalizedFloat(WExtensions.CompressNormalizedFloat(transform.rotation.y));
        float z = WExtensions.DecompressNormalizedFloat(WExtensions.CompressNormalizedFloat(transform.rotation.z));
        float w = WExtensions.DecompressNormalizedFloat(WExtensions.CompressNormalizedFloat(transform.rotation.w));
        transform.rotation = new Quaternion(x, y, z, w);
    }
}

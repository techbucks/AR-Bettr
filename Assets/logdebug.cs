using UnityEngine;
//#if ARCORE_EXTENSIONS_PRESENT
using Google.XR.ARCoreExtensions;
//#endifv
using Google.XR.ARCoreExtensions.GeospatialCreator;

public class DebugRealWorldLocation : MonoBehaviour
{
    private Transform arCamera;

    //#if ARCORE_EXTENSIONS_PRESENT
    public ARGeospatialCreatorAnchor geoAnchor;
    //#endif

    void Start()
    {
        arCamera = Camera.main.transform;

        //#if ARCORE_EXTENSIONS_PRESENT
        //      geoAnchor = GetComponent<ARGeospatialAnchor>();
        if (geoAnchor == null)
        {
            Debug.LogWarning("No ARGeospatialAnchor component found on this object.");
        }
        //#endif
    }

    void Update()
    {
        // Log Unity world position
        Debug.Log($"[{gameObject.name}] World Position: {transform.position}");

        // Log relative distance to camera
        if (arCamera != null)
        {
            float distance = Vector3.Distance(transform.position, arCamera.position);
            Debug.Log($"[{gameObject.name}] Distance to Camera: {distance:F2} units");
        }

        // #if ARCORE_EXTENSIONS_PRESENT
        // Log Geospatial Anchor info if available
        if (geoAnchor != null)
        {
            Debug.Log($"[{gameObject.name}] Geospatial Location: Lat={geoAnchor.Latitude}, Lon={geoAnchor.Longitude}, Alt={geoAnchor.Altitude}");
        }
        // #endif
    }
}

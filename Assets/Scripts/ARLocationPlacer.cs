using UnityEngine;
using Google.XR.ARCoreExtensions;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using TMPro;
using Unity.VisualScripting.Antlr3.Runtime;
using Unity.Mathematics;

public class ARLocationPlacer : MonoBehaviour
{
    public ARAnchorManager anchorManager;
    public AREarthManager earthManager;
    public GameObject arObjectPrefab;
    public TMP_InputField latInput;
    public TMP_InputField longInput;
    public TMP_InputField altitide;

    private ARGeospatialAnchor currentAnchor;

    // Update or place AR object at a specific location
    public void PlaceObjectAt()
    {
        double latitude = float.Parse(latInput.text);
        double longitude = float.Parse(longInput.text);
        double altitude = float.Parse(altitide.text);
        Quaternion rotation = quaternion.identity;

        if (earthManager.EarthTrackingState != TrackingState.Tracking)
        {
            Debug.Log("ab: Earth is not tracking yet.");
            return;
        }

        if (currentAnchor != null)
        {
            Destroy(currentAnchor.gameObject);  // Remove previous anchor if any
        }

        // Create a new geospatial anchor
        currentAnchor = anchorManager.AddAnchor(latitude, longitude, altitude, rotation);

        if (currentAnchor != null && arObjectPrefab != null)
        {
            Instantiate(arObjectPrefab, currentAnchor.transform);  // Attach your AR object to the anchor
            Debug.Log("ab: AR object placed successfully.");
        }
        else
        {
            Debug.LogError("ab: Failed to place AR object.");
        }
    }
}

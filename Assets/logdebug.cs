using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Google.XR.ARCoreExtensions;
using Google.XR.ARCoreExtensions.GeospatialCreator;

public class DebugRealWorldLocation : MonoBehaviour
{
    private Transform arCamera;

    [Header("AR Managers")]
    public AREarthManager earthManager;
    public ARAnchorManager anchorManager;

    [Header("Prefab to Place at Anchor")]
    public GameObject anchorPrefab;

    [Header("Optional Geospatial Anchor (for Logging)")]
    public ARGeospatialCreatorAnchor geoAnchor;

    private bool anchorPlaced = false;

    void Start()
    {
        arCamera = Camera.main?.transform;

        if (anchorManager == null)
            anchorManager = FindFirstObjectByType<ARAnchorManager>();

        if (earthManager == null)
            earthManager = FindFirstObjectByType<AREarthManager>();

        if (anchorManager == null || earthManager == null)
            Debug.LogError("❌ Required AR Managers are not assigned.");

        if (anchorPrefab == null)
            Debug.LogWarning("⚠️ Anchor prefab is not assigned.");
    }

    void Update()
    {
        // Logging position
        Debug.Log($"[{gameObject.name}] Unity World Position: {transform.position}");

        if (arCamera != null)
        {
            float distance = Vector3.Distance(transform.position, arCamera.position);
            Debug.Log($"📏 Distance to AR Camera: {distance:F2} units");
        }

        // Log geospatial anchor info if assigned
        if (geoAnchor != null)
        {
            Debug.Log($"📍 GeoAnchor Position => Lat: {geoAnchor.Latitude}, Lon: {geoAnchor.Longitude}, Alt: {geoAnchor.Altitude}");
        }

        // Try to place the anchor once
        if (!anchorPlaced)
        {
            TryPlaceGeospatialAnchor();
        }
    }

    void TryPlaceGeospatialAnchor()
    {
        if (earthManager == null || anchorManager == null || anchorPrefab == null)
        {
            Debug.LogError("❌ One or more required components are not assigned.");
            return;
        }

        TrackingState tracking = earthManager.EarthTrackingState;
        Debug.Log($"🌍 Earth Tracking State: {tracking}");

        if (tracking != TrackingState.Tracking)
        {
            Debug.Log("⛔ Earth tracking is not active.");
            return;
        }

        GeospatialPose pose = earthManager.CameraGeospatialPose;

        Debug.Log($"📍 Device Geospatial Pose => Lat: {pose.Latitude}, Lon: {pose.Longitude}, Alt: {pose.Altitude}");
        Debug.Log($"📏 Accuracy => Horizontal: {pose.HorizontalAccuracy}m, Vertical: {pose.VerticalAccuracy}m, Yaw: {pose.OrientationYawAccuracy}°");

        // Accuracy thresholds
        const float horizontalAccuracyThreshold = 7.0f;
        const float verticalAccuracyThreshold = 2.0f;
        const float yawAccuracyThreshold = 5.0f;

        if (pose.HorizontalAccuracy <= horizontalAccuracyThreshold &&
            pose.VerticalAccuracy <= verticalAccuracyThreshold &&
            pose.OrientationYawAccuracy <= yawAccuracyThreshold)
        {
            ARGeospatialAnchor anchor = anchorManager.AddAnchor(
                pose.Latitude,
                pose.Longitude,
                pose.Altitude,
                pose.EunRotation);

            if (anchor != null)
            {
                Instantiate(anchorPrefab, anchor.transform);
                Debug.Log($"✅ Anchor placed at Lat: {pose.Latitude}, Lon: {pose.Longitude}, Alt: {pose.Altitude}");
                anchorPlaced = true;
            }
            else
            {
                Debug.LogWarning("⚠️ Failed to place anchor.");
            }
        }
        else
        {
            Debug.Log("❌ Accuracy too low — anchor not placed.");
        }
    }
}

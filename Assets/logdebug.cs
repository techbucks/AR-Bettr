using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Google.XR.ARCoreExtensions;
using Google.XR.ARCoreExtensions.GeospatialCreator;
using System.Collections; // For coroutines

public class DebugRealWorldLocation : MonoBehaviour
{
    private Transform arCamera;

    [Header("AR Managers")]
    public AREarthManager earthManager;
    public ARAnchorManager anchorManager;

    [Header("Prefab to Place at Anchor")]
    public GameObject anchorPrefab;

    [Header("Optional Geospatial Anchor (for Logging)")]
    public ARGeospatialCreatorAnchor geoAnchor; // This defines the target lat/lon/alt

    [Header("Accuracy Thresholds")]
    [Tooltip("Maximum allowed horizontal accuracy (meters) for placing the anchor.")]
    public float horizontalAccuracyThreshold = 5.0f; // Made stricter
    [Tooltip("Maximum allowed vertical accuracy (meters) for placing the anchor.")]
    public float verticalAccuracyThreshold = 1.5f;   // Made stricter
    [Tooltip("Maximum allowed yaw accuracy (degrees) for placing the anchor.")]
    public float yawAccuracyThreshold = 3.0f;      // Made stricter

    [Tooltip("How long to wait (seconds) for accuracy to stabilize before attempting placement.")]
    public float accuracyStabilizationTime = 2.0f; // New: Wait for accuracy to stabilize

    private bool anchorPlaced = false;
    private ARGeospatialAnchor placedGeospatialAnchor; // Reference to the placed anchor
    private Coroutine placementAttemptCoroutine;

    // Store coordinates used to place the anchor
    private double placedLatitude;
    private double placedLongitude;
    private double placedAltitude;

    void Start()
    {
        arCamera = Camera.main?.transform;

        if (anchorManager == null)
            anchorManager = FindFirstObjectByType<ARAnchorManager>();

        if (earthManager == null)
            earthManager = FindFirstObjectByType<AREarthManager>();

        if (anchorManager == null || earthManager == null)
            Debug.LogError("Required AR Managers are not assigned.");

        if (anchorPrefab == null)
            Debug.LogWarning("Anchor prefab is not assigned.");
        
        // Ensure geoAnchor is assigned if you intend to use its coordinates
        if (geoAnchor == null)
        {
            Debug.LogError("ARGeospatialCreatorAnchor (geoAnchor) is not assigned. Please assign the target geospatial anchor from your scene.");
        }
    }

    void Update()
    {
        // Logging current position
        Debug.Log($"[{gameObject.name}] Unity World Position: {transform.position}");

        if (arCamera != null)
        {
            float distance = Vector3.Distance(transform.position, arCamera.position);
            Debug.Log($"📏 Distance to AR Camera: {distance:F2} units");
        }

        // Log geospatial anchor info if assigned and tracking
        if (placedGeospatialAnchor != null)
        {
            Debug.Log($"Placed Anchor Tracking State: {placedGeospatialAnchor.trackingState}");
            Debug.Log($"Placed Anchor Geospatial Pose => Lat: {placedLatitude}, Lon: {placedLongitude}, Alt: {placedAltitude}");

            if (earthManager != null)
            {
                GeospatialPose devicePose = earthManager.CameraGeospatialPose;
                Debug.Log($"Device Geospatial Pose Accuracy => Horizontal: {devicePose.HorizontalAccuracy}m, Vertical: {devicePose.VerticalAccuracy}m, Yaw: {devicePose.OrientationYawAccuracy}°");
            }
        }

        // Only try to place the anchor if it hasn't been placed yet and we have a valid geoAnchor
        if (!anchorPlaced && geoAnchor != null)
        {
            // If we are not currently trying to place, start a new attempt
            if (placementAttemptCoroutine == null)
            {
                placementAttemptCoroutine = StartCoroutine(AttemptPlaceGeospatialAnchorRoutine());
            }
        }
    }

    IEnumerator AttemptPlaceGeospatialAnchorRoutine()
    {
        float timer = 0f;
        bool accuracyMet = false;

        while (timer < accuracyStabilizationTime)
        {
            if (earthManager == null || anchorManager == null || anchorPrefab == null || geoAnchor == null)
            {
                Debug.LogError("One or more required components are not assigned or geoAnchor is null.");
                placementAttemptCoroutine = null; // Reset coroutine
                yield break;
            }

            TrackingState earthTracking = earthManager.EarthTrackingState;
            Debug.Log($"🌍 Earth Tracking State: {earthTracking}");

            if (earthTracking != TrackingState.Tracking)
            {
                Debug.Log("Earth tracking is not active. Resetting timer.");
                timer = 0f; // Reset timer if tracking is lost
                accuracyMet = false;
                yield return null; // Wait for next frame
                continue;
            }

            GeospatialPose cameraPose = earthManager.CameraGeospatialPose;

            Debug.Log($"Device Geospatial Pose => Lat: {cameraPose.Latitude}, Lon: {cameraPose.Longitude}, Alt: {cameraPose.Altitude}");
            Debug.Log($"Accuracy => Horizontal: {cameraPose.HorizontalAccuracy:F2}m, Vertical: {cameraPose.VerticalAccuracy:F2}m, Yaw: {cameraPose.OrientationYawAccuracy:F2}°");

            // Check if current accuracy meets thresholds
            if (cameraPose.HorizontalAccuracy <= horizontalAccuracyThreshold &&
                cameraPose.VerticalAccuracy <= verticalAccuracyThreshold &&
                cameraPose.OrientationYawAccuracy <= yawAccuracyThreshold)
            {
                if (!accuracyMet) // First time accuracy is met
                {
                    Debug.Log($"Accuracy thresholds met. Starting stabilization timer for {accuracyStabilizationTime} seconds.");
                    accuracyMet = true;
                }
                timer += Time.deltaTime;
            }
            else
            {
                if (accuracyMet) // Accuracy was met, but now it's not
                {
                    Debug.Log("Accuracy fell below thresholds. Resetting stabilization timer.");
                }
                accuracyMet = false;
                timer = 0f; // Reset timer if accuracy drops
            }

            yield return null; // Wait for next frame
        }

        // If we reach here, accuracy has been stable for the required time
        if (accuracyMet) // Double-check, though it should be true
        {
            // Store coordinates before placing anchor
            placedLatitude = geoAnchor.Latitude;
            placedLongitude = geoAnchor.Longitude;
            placedAltitude = geoAnchor.Altitude;

            // Place the anchor using the *pre-defined* geoAnchor coordinates
            // and the device's current rotation for orientation.
            placedGeospatialAnchor = anchorManager.AddAnchor(
                placedLatitude,
                placedLongitude,
                placedAltitude,
                earthManager.CameraGeospatialPose.EunRotation); // Use current device orientation

            if (placedGeospatialAnchor != null)
            {
                // Instantiate the prefab as a child of the ARGeospatialAnchor
                Instantiate(anchorPrefab, placedGeospatialAnchor.transform);
                Debug.Log($"✅ Anchor successfully placed at Lat: {placedLatitude}, Lon: {placedLongitude}, Alt: {placedAltitude}");
                anchorPlaced = true;
            }
            else
            {
                Debug.LogWarning("⚠️ Failed to place anchor. ARCore returned null anchor.");
            }
        }
        else
        {
            Debug.Log("❌ Could not achieve stable accuracy within the required time.");
        }

        placementAttemptCoroutine = null; // Allow new placement attempts if needed (though anchorPlaced prevents it)
    }
}

using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Google.XR.ARCoreExtensions;
using Google.XR.ARCoreExtensions.GeospatialCreator;
using System.Collections;

public class GeospatialAnchorPlacer : MonoBehaviour
{
    [Header("AR Managers")]
    public AREarthManager earthManager;
    public ARAnchorManager anchorManager;

    [Header("Prefab to Place at Anchor")]
    public GameObject anchorPrefab;

    [Header("Target Geospatial Anchor")]
    public ARGeospatialCreatorAnchor geoAnchor;

    [Header("Accuracy Settings")]
    public float horizontalAccuracyThreshold = 5.0f;
    public float verticalAccuracyThreshold = 1.5f;
    public float yawAccuracyThreshold = 3.0f;
    public float accuracyStabilizationTime = 2.0f;

    private bool anchorPlaced = false;
    private Coroutine placementCoroutine;

    public ARGeospatialAnchor PlacedAnchor { get; private set; }

    void Start()
    {
        earthManager ??= FindFirstObjectByType<AREarthManager>();
        anchorManager ??= FindFirstObjectByType<ARAnchorManager>();

        if (earthManager == null || anchorManager == null)
            Debug.LogError("Missing AREarthManager or ARAnchorManager.");

        if (geoAnchor == null)
            Debug.LogError("GeoAnchor not assigned.");
    }

    void Update()
    {
        if (anchorPlaced || geoAnchor == null)
            return;

        if (placementCoroutine == null)
            placementCoroutine = StartCoroutine(TryPlaceAnchorWhenAccuracyStable());
    }

    private IEnumerator TryPlaceAnchorWhenAccuracyStable()
    {
        float stableTime = 0f;

        while (stableTime < accuracyStabilizationTime)
        {
            if (earthManager.EarthTrackingState != TrackingState.Tracking)
            {
                Debug.Log("⛔ Earth tracking not active, waiting...");
                stableTime = 0f;
                yield return null;
                continue;
            }

            var pose = earthManager.CameraGeospatialPose;

            bool isAccurate =
                pose.HorizontalAccuracy <= horizontalAccuracyThreshold &&
                pose.VerticalAccuracy <= verticalAccuracyThreshold &&
                pose.OrientationYawAccuracy <= yawAccuracyThreshold;

            if (isAccurate)
            {
                stableTime += Time.deltaTime;
                Debug.Log($"✅ Accuracy OK for {stableTime:F2}s");
            }
            else
            {
                Debug.Log($"❌ Accuracy not stable. H:{pose.HorizontalAccuracy:F2} V:{pose.VerticalAccuracy:F2} Y:{pose.OrientationYawAccuracy:F2}");
                stableTime = 0f;
            }

            yield return null;
        }

        if (!anchorPlaced)
        {
            var devicePose = earthManager.CameraGeospatialPose;

            var anchor = anchorManager.AddAnchor(
                geoAnchor.Latitude,
                geoAnchor.Longitude,
                geoAnchor.Altitude,
                devicePose.EunRotation);

            if (anchor != null)
            {
                Instantiate(anchorPrefab, anchor.transform); // Parent to anchor
                PlacedAnchor = anchor;
                anchorPlaced = true;
                Debug.Log($"📍 Anchor placed at Lat:{geoAnchor.Latitude}, Lon:{geoAnchor.Longitude}, Alt:{geoAnchor.Altitude}");
            }
            else
            {
                Debug.LogWarning("⚠️ Failed to place anchor.");
            }
        }

        placementCoroutine = null;
    }
}

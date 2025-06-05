using UnityEngine;
using Google.XR.ARCoreExtensions;
using UnityEngine.XR.ARFoundation;

public class RealWorldLogger : MonoBehaviour
{
    public GeospatialAnchorPlacer anchorPlacer;
    public AREarthManager earthManager;

    private Transform arCamera;

    void Start()
    {
        arCamera = Camera.main?.transform;
        earthManager ??= FindFirstObjectByType<AREarthManager>();
    }

    void Update()
    {
        if (arCamera != null)
        {
            float dist = Vector3.Distance(transform.position, arCamera.position);
            Debug.Log($"Distance to AR Camera: {dist:F2} units");
        }

        Debug.Log($"[{gameObject.name}] Unity World Position: {transform.position}");

        if (earthManager != null)
        {
            var pose = earthManager.CameraGeospatialPose;
            Debug.Log($"Device Geospatial Pose => Lat: {pose.Latitude}, Lon: {pose.Longitude}, Alt: {pose.Altitude}");
            Debug.Log($"Accuracy => Horizontal: {pose.HorizontalAccuracy}m, Vertical: {pose.VerticalAccuracy}m, Yaw: {pose.OrientationYawAccuracy}°");
        }

        if (anchorPlacer?.PlacedAnchor != null)
        {
            Debug.Log($"Anchor Placed. Tracking State: {anchorPlacer.PlacedAnchor.trackingState}");
        }
    }
}

using UnityEngine;

namespace CosmicZoom
{
    /// <summary>
    /// Smooth camera-facing billboard for stellar corona halos and celestial beacons.
    /// Eliminates rigid polygonal planar intersections.
    /// </summary>
    [ExecuteAlways]
    public class BillboardToCamera : MonoBehaviour
    {
        private void LateUpdate()
        {
            Camera cam = Camera.main;
            if (cam == null) cam = Object.FindAnyObjectByType<Camera>();
            if (cam != null)
            {
                transform.rotation = cam.transform.rotation;
            }
        }
    }
}

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
            if (cam != null)
            {
                transform.LookAt(cam.transform.position, cam.transform.up);
            }
        }
    }
}

using UnityEngine;

namespace CosmicZoom
{
    /// <summary>
    /// Animates celestial bodies with axial rotation, heliocentric orbital revolution,
    /// and orbital inclination tilts for vivid, living astronomical motion.
    /// </summary>
    public class CelestialBody : MonoBehaviour
    {
        [Header("Kinematics")]
        public Transform orbitCenter;
        public float orbitalSpeed = 8.0f;     // Degrees per second around Sun
        public float rotationSpeed = 25.0f;    // Degrees per second around self
        public Vector3 rotationAxis = Vector3.up;
        public float axialTiltDegrees = 0.0f;
        private Vector3 spinAxis;

        private void Start()
        {
            Quaternion tilt = Quaternion.AngleAxis(axialTiltDegrees, Vector3.forward);
            // Preserve the FBX's Z-up to Y-up conversion instead of overwriting it.
            transform.rotation = tilt * transform.rotation;
            spinAxis = tilt * rotationAxis.normalized;
        }

        private void Update()
        {
            // 1. Axial Spin (day/night cycle)
            transform.Rotate(spinAxis, rotationSpeed * Time.deltaTime, Space.World);

            // 2. Orbital Revolution (year cycle around Sun)
            if (orbitCenter != null && orbitalSpeed != 0.0f)
            {
                // Revolution changes position without precessing the spin axis/rings.
                Vector3 offset = transform.position - orbitCenter.position;
                transform.position = orbitCenter.position + Quaternion.AngleAxis(orbitalSpeed * Time.deltaTime, Vector3.up) * offset;
            }
        }
    }
}

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

        private void Start()
        {
            if (axialTiltDegrees != 0.0f)
            {
                transform.rotation = Quaternion.Euler(axialTiltDegrees, 0f, 0f);
            }
        }

        private void Update()
        {
            // 1. Axial Spin (day/night cycle)
            transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime, Space.Self);

            // 2. Orbital Revolution (year cycle around Sun)
            if (orbitCenter != null && orbitalSpeed != 0.0f)
            {
                transform.RotateAround(orbitCenter.position, Vector3.up, orbitalSpeed * Time.deltaTime);
            }
        }
    }
}

using UnityEngine;

namespace CosmicZoom
{
    /// <summary>
    /// Represents a celestial body with orbital kinematics and scientific dossier metadata.
    /// </summary>
    public class CelestialBody : MonoBehaviour
    {
        [Header("Dossier Metadata")]
        public string bodyName = "Celestial Body";
        public string bodyClassification = "Terrestrial Planet";
        public string distanceFromOrigin = "1.000 AU";
        public string scaleDomain = "Solar System (LH)";
        public string orbitalVelocity = "29.78 km/s";
        public string physicalDiameter = "12,742 km";
        [TextArea(3, 6)]
        public string scientificFact = "Detailed astronomical facts and observations.";

        [Header("Kinematics")]
        public Transform orbitCenter;
        public float orbitalSpeed = 5.0f;
        public float rotationSpeed = 20.0f;
        public Vector3 rotationAxis = Vector3.up;

        private void Update()
        {
            // Axial rotation
            transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime, Space.Self);

            // Heliocentric / barycentric revolution
            if (orbitCenter != null && orbitalSpeed != 0.0f)
            {
                transform.RotateAround(orbitCenter.position, Vector3.up, orbitalSpeed * Time.deltaTime);
            }
        }
    }
}

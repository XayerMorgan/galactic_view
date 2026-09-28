using UnityEngine;

namespace CosmicZoom
{
    /// <summary>
    /// Animates galaxies and cosmological structures with smooth differential rotation,
    /// core swirl, and orbital precession to bring the galactic scale to life.
    /// </summary>
    public class GalacticRotator : MonoBehaviour
    {
        [SerializeField] private float rotationSpeedDegrees = 3.5f;
        [SerializeField] private Vector3 rotationAxis = Vector3.up;
        [SerializeField] private bool includeChildren = false;

        private void Update()
        {
            float delta = rotationSpeedDegrees * Time.deltaTime;
            transform.Rotate(rotationAxis, delta, Space.Self);

            if (includeChildren)
            {
                for (int i = 0; i < transform.childCount; i++)
                {
                    Transform child = transform.GetChild(i);
                    // Slower or faster differential rotation for inner vs outer components
                    child.Rotate(rotationAxis, delta * 0.5f, Space.Self);
                }
            }
        }
    }
}

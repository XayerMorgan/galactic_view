using UnityEngine;

namespace CosmicZoom
{
    /// <summary>
    /// Renders an expanding spherical light pulse at speed c to visualize light transit.
    /// </summary>
    public class LightPulseEmitter : MonoBehaviour
    {
        [SerializeField] private GameObject pulseSphere;
        [SerializeField] private float expansionSpeed = 120.0f;
        [SerializeField] private float maxRadius = 400.0f;

        private bool isActive = false;
        private float currentRadius = 1.0f;
        private float elapsedTime = 0.0f;

        private void Start()
        {
            if (pulseSphere == null)
            {
                pulseSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                pulseSphere.transform.SetParent(transform);
                pulseSphere.GetComponent<Collider>().enabled = false;

                // Transparent wireframe/glow material
                var rend = pulseSphere.GetComponent<Renderer>();
                rend.material = new Material(Shader.Find("Standard"));
                rend.material.color = new Color(1.0f, 0.95f, 0.5f, 0.4f);
            }
            pulseSphere.SetActive(false);
        }

        public void FireLightPulse(Vector3 origin)
        {
            transform.position = origin;
            currentRadius = 1.0f;
            elapsedTime = 0.0f;
            isActive = true;
            pulseSphere.SetActive(true);
            pulseSphere.transform.localScale = Vector3.one;
        }

        private void Update()
        {
            if (!isActive) return;

            elapsedTime += Time.deltaTime;
            currentRadius += expansionSpeed * Time.deltaTime;
            pulseSphere.transform.localScale = Vector3.one * currentRadius;

            if (currentRadius >= maxRadius)
            {
                isActive = false;
                pulseSphere.SetActive(false);
            }
        }

        public bool IsPulseActive => isActive;
        public float ElapsedPulseSeconds => elapsedTime;
    }
}

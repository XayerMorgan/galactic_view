using UnityEngine;

namespace CosmicZoom
{
    /// <summary>
    /// Adds a luminous atmospheric rim or solar corona halo around celestial bodies,
    /// providing visual depth, radiance, and photorealistic beauty.
    /// </summary>
    public class AtmosphereGlow : MonoBehaviour
    {
        [SerializeField] private Color glowColor = new Color(0.22f, 0.75f, 1.0f, 0.45f);
        [SerializeField] private float glowScale = 1.06f;
        [SerializeField] private bool pulseGlow = false;

        private Renderer glowRenderer;
        private float baseScale;

        private void Start()
        {
            GameObject glowObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            glowObj.name = gameObject.name + "_AtmosphereGlow";
            glowObj.transform.SetParent(transform);
            glowObj.transform.localPosition = Vector3.zero;
            glowObj.transform.localScale = Vector3.one * glowScale;
            baseScale = glowScale;

            var col = glowObj.GetComponent<Collider>();
            if (col != null) col.enabled = false;

            glowRenderer = glowObj.GetComponent<Renderer>();
            var shader = Shader.Find("Mobile/Particles/Additive") ?? Shader.Find("Unlit/Transparent") ?? Shader.Find("Standard");
            Material mat = new Material(shader);
            if (mat.HasProperty("_TintColor")) mat.SetColor("_TintColor", glowColor);
            else mat.color = glowColor;
            glowRenderer.material = mat;
        }

        private void Update()
        {
            if (pulseGlow && glowRenderer != null)
            {
                float s = baseScale + Mathf.Sin(Time.time * 2.5f) * 0.04f;
                glowRenderer.transform.localScale = Vector3.one * s;
            }
        }
    }
}

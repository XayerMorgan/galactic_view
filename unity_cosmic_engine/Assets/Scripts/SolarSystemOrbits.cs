using UnityEngine;

namespace CosmicZoom
{
    /// <summary>
    /// Renders luminous, high-contrast scientific orbital trajectory tracks for Earth,
    /// Jupiter, Saturn, and Neptune around the Sun.
    /// </summary>
    public class SolarSystemOrbits : MonoBehaviour
    {
        private void Start()
        {
            CreateOrbitTrack("Orbit_Earth_1AU", 18.0f, new Color(0.25f, 0.75f, 1.0f, 0.65f), 0.15f);
            CreateOrbitTrack("Orbit_Jupiter_5AU", 38.0f, new Color(1.0f, 0.85f, 0.40f, 0.65f), 0.22f);
            CreateOrbitTrack("Orbit_Saturn_9AU", 54.0f, new Color(0.95f, 0.80f, 0.60f, 0.60f), 0.25f);
            CreateOrbitTrack("Orbit_Neptune_30AU", 88.0f, new Color(0.20f, 0.50f, 1.0f, 0.70f), 0.30f);
        }

        private void CreateOrbitTrack(string name, float radius, Color color, float width)
        {
            GameObject trackObj = new GameObject(name);
            trackObj.transform.SetParent(transform);
            trackObj.transform.localPosition = Vector3.zero;

            LineRenderer line = trackObj.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = true;
            int segments = 128;
            line.positionCount = segments;

            var shader = Shader.Find("Mobile/Particles/Additive") ?? Shader.Find("Unlit/Transparent") ?? Shader.Find("Standard");
            Material mat = new Material(shader);
            if (mat.HasProperty("_TintColor")) mat.SetColor("_TintColor", color);
            else mat.color = color;
            line.material = mat;

            line.startWidth = width;
            line.endWidth = width;

            Vector3[] positions = new Vector3[segments];
            for (int i = 0; i < segments; i++)
            {
                float angle = (i / (float)segments) * Mathf.PI * 2.0f;
                positions[i] = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            }
            line.SetPositions(positions);
        }
    }
}

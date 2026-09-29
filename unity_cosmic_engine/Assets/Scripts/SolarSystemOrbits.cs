using UnityEngine;

namespace CosmicZoom
{
    /// <summary>
    /// Renders luminous, high-contrast scientific orbital trajectory tracks for Earth,
    /// Jupiter, Saturn, and Neptune around the Sun.
    /// </summary>
    [ExecuteAlways]
    public class SolarSystemOrbits : MonoBehaviour
    {
        private void OnEnable()
        {
            if (transform.childCount == 0)
            {
                BuildAllOrbits();
            }
        }

        private void Start()
        {
            if (transform.childCount == 0)
            {
                BuildAllOrbits();
            }
        }

        private void BuildAllOrbits()
        {
            CreateOrbitTrack("Orbit_Earth_1AU", 18.0f, new Color(0.18f, 0.58f, 0.95f, 0.35f), 0.08f);
            CreateOrbitTrack("Orbit_Jupiter_5AU", 38.0f, new Color(0.95f, 0.80f, 0.35f, 0.35f), 0.10f);
            CreateOrbitTrack("Orbit_Saturn_9AU", 54.0f, new Color(0.90f, 0.75f, 0.50f, 0.35f), 0.12f);
            CreateOrbitTrack("Orbit_Neptune_30AU", 88.0f, new Color(0.20f, 0.55f, 0.95f, 0.35f), 0.14f);
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

            var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
            Material mat = new Material(shader) { color = color };
            line.sharedMaterial = mat;
            line.startColor = color;
            line.endColor = color;

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

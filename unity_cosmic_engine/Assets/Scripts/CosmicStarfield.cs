using UnityEngine;

namespace CosmicZoom
{
    /// <summary>
    /// Generates a breathtaking 3D deep-space starfield with thousands of
    /// spectral star classes (O-B blue giants, G yellow suns, M red dwarfs)
    /// using high-performance procedural mesh geometry.
    /// </summary>
    [ExecuteAlways]
    public class CosmicStarfield : MonoBehaviour
    {
        [SerializeField] private int starCount = 2500;
        [SerializeField] private float starfieldRadius = 25000f;
        [SerializeField] private float minStarSize = 6.0f;
        [SerializeField] private float maxStarSize = 18.0f;

        // Stellar Spectral Classes Colors
        private readonly Color[] spectralColors = new Color[]
        {
            new Color(0.75f, 0.88f, 1.00f, 0.90f), // Class O/B (Blue-White Giants)
            new Color(0.92f, 0.96f, 1.00f, 0.95f), // Class A (White)
            new Color(1.00f, 0.98f, 0.92f, 0.90f), // Class F (Yellow-White)
            new Color(1.00f, 0.92f, 0.65f, 0.85f), // Class G (Solar Yellow)
            new Color(1.00f, 0.75f, 0.45f, 0.85f), // Class K (Orange)
            new Color(1.00f, 0.45f, 0.35f, 0.80f)  // Class M (Red Supergiant)
        };

        private void OnEnable()
        {
            BuildStarfieldMesh();
        }

        private void Start()
        {
            BuildStarfieldMesh();
        }

        private void BuildStarfieldMesh()
        {
            MeshFilter mf = GetComponent<MeshFilter>();
            if (mf == null) mf = gameObject.AddComponent<MeshFilter>();

            MeshRenderer mr = GetComponent<MeshRenderer>();
            if (mr == null) mr = gameObject.AddComponent<MeshRenderer>();

            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;

            Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
            Material mat = new Material(shader);
            mat.mainTexture = MakeStarTexture(64);
            mr.sharedMaterial = mat;

            Mesh mesh = new Mesh
            {
                name = "Procedural_Starfield_Mesh",
                indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
            };

            Vector3[] vertices = new Vector3[starCount * 4];
            Vector2[] uvs = new Vector2[starCount * 4];
            Color[] colors = new Color[starCount * 4];
            int[] triangles = new int[starCount * 6];

            Random.InitState(42);

            for (int i = 0; i < starCount; i++)
            {
                Vector3 dir = Random.onUnitSphere;
                float dist = Random.Range(starfieldRadius * 0.5f, starfieldRadius);
                Vector3 center = dir * dist;

                Color col = spectralColors[Random.Range(0, spectralColors.Length)];
                float size = Random.Range(minStarSize, maxStarSize);
                if (Random.value < 0.04f)
                {
                    size *= 2.5f;
                    col.a = 1.0f;
                }

                // Billboard orientation tangent to sphere
                Vector3 up = Vector3.Cross(dir, Vector3.up).normalized;
                if (up == Vector3.zero) up = Vector3.Cross(dir, Vector3.right).normalized;
                Vector3 right = Vector3.Cross(dir, up).normalized;

                int vi = i * 4;
                vertices[vi + 0] = center - right * size - up * size;
                vertices[vi + 1] = center + right * size - up * size;
                vertices[vi + 2] = center + right * size + up * size;
                vertices[vi + 3] = center - right * size + up * size;

                uvs[vi + 0] = new Vector2(0f, 0f);
                uvs[vi + 1] = new Vector2(1f, 0f);
                uvs[vi + 2] = new Vector2(1f, 1f);
                uvs[vi + 3] = new Vector2(0f, 1f);

                colors[vi + 0] = col;
                colors[vi + 1] = col;
                colors[vi + 2] = col;
                colors[vi + 3] = col;

                int ti = i * 6;
                triangles[ti + 0] = vi + 0;
                triangles[ti + 1] = vi + 2;
                triangles[ti + 2] = vi + 1;
                triangles[ti + 3] = vi + 0;
                triangles[ti + 4] = vi + 3;
                triangles[ti + 5] = vi + 2;
            }

            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.colors = colors;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();

            mf.mesh = mesh;
        }

        private Texture2D MakeStarTexture(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float center = (size - 1) / 2.0f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - center) / center;
                    float dy = (y - center) / center;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);

                    if (r >= 1.0f)
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                    else
                    {
                        float core = Mathf.Pow(1.0f - r, 2.5f);
                        float flareH = Mathf.Pow(Mathf.Clamp01(1.0f - Mathf.Abs(dy) * 6f), 3f) * Mathf.Pow(Mathf.Clamp01(1.0f - Mathf.Abs(dx)), 2f) * 0.5f;
                        float flareV = Mathf.Pow(Mathf.Clamp01(1.0f - Mathf.Abs(dx) * 6f), 3f) * Mathf.Pow(Mathf.Clamp01(1.0f - Mathf.Abs(dy)), 2f) * 0.5f;

                        float alpha = Mathf.Clamp01(core + flareH + flareV);
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                    }
                }
            }
            tex.Apply();
            return tex;
        }

        private void LateUpdate()
        {
            if (Camera.main != null)
            {
                transform.position = Camera.main.transform.position;
            }
        }
    }
}

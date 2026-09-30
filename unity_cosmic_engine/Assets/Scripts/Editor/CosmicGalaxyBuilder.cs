using UnityEditor;
using UnityEngine;

namespace CosmicZoom.Editor
{
    /// <summary>Source-backed galaxy maps. Legacy FBX layouts are retained only in the art workbench.</summary>
    internal static class CosmicGalaxyBuilder
    {
        public static void BuildMilkyWay(Transform parent, Material map, Material core)
        {
            Disk("MilkyWay_Galactic_Disk", parent, 75, Vector3.zero, map, 1);
            Sphere("Sagittarius_A_Horizon", parent, Vector3.zero, 1.2f, core);
            // NASA ssc2008-10b1: Sun circle at (2798, 3874) in the 5600px square.
            // Disk UVs run left-to-right and bottom-to-top; use the SAME mapping as the art.
            Vector3 sun = new Vector3((2798f / 5600 - .5f) * 150, .55f, (.5f - 3874f / 5600) * 150);
            var beaconMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_SolarBeacon.mat");
            if (beaconMaterial == null)
            {
                beaconMaterial = new Material(Shader.Find("Unlit/Color"));
                AssetDatabase.CreateAsset(beaconMaterial, "Assets/Materials/Mat_SolarBeacon.mat");
            }
            beaconMaterial.color = new Color(1, .72f, .28f);
            EditorUtility.SetDirty(beaconMaterial);
            Sphere("Our_Position_Sun_Beacon", parent, sun, 1.1f, beaconMaterial);
        }

        public static void BuildLocalGroup(Transform parent, Material milkyWay, Material andromeda, Material triangulum)
        {
            // Readable schematic, NOT scaled intergalactic distances or measured orientations.
            Disk("LocalGroup_MilkyWay", parent, 21, new Vector3(-38, 0, -8), milkyWay, 1);
            Disk("LocalGroup_Andromeda_M31", parent, 32, new Vector3(29, 0, 24), andromeda, 1);
            Disk("LocalGroup_Triangulum_M33", parent, 15, new Vector3(35, 0, -40), triangulum, .38f);
        }

        private static void Sphere(string name, Transform parent, Vector3 position, float diameter, Material material)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position;
            obj.transform.localScale = Vector3.one * diameter;
            Object.DestroyImmediate(obj.GetComponent<Collider>());
            obj.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static void Disk(string name, Transform parent, float radius, Vector3 position, Material material, float uvSpan)
        {
            const int segments = 192;
            const int rings = 12;
            int stride = segments + 1;
            var vertices = new Vector3[stride * (rings + 1)];
            var uv = new Vector2[vertices.Length];
            var colors = new Color[vertices.Length];
            var triangles = new int[rings * segments * 6];
            for (int r = 0; r <= rings; r++)
            for (int i = 0; i <= segments; i++)
            {
                int index = r * stride + i;
                float distance = (float)r / rings;
                float angle = i * Mathf.PI * 2 / segments;
                Vector2 p = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
                vertices[index] = new Vector3(p.x * radius, 0, p.y * radius);
                uv[index] = Vector2.one * .5f + p * (.5f * uvSpan);
                colors[index] = new Color(1, 1, 1, 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.82f, 1, distance)));
                if (r == rings || i == segments) continue;
                int t = (r * segments + i) * 6;
                triangles[t] = index; triangles[t + 1] = index + stride; triangles[t + 2] = index + 1;
                triangles[t + 3] = index + 1; triangles[t + 4] = index + stride; triangles[t + 5] = index + stride + 1;
            }
            string path = "Assets/Models/" + name + ".asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null) { mesh = new Mesh(); AssetDatabase.CreateAsset(mesh, path); }
            mesh.Clear(); mesh.name = name;
            mesh.vertices = vertices; mesh.uv = uv; mesh.colors = colors; mesh.triangles = triangles;
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); EditorUtility.SetDirty(mesh);
            var disk = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            disk.transform.SetParent(parent, false);
            disk.transform.localPosition = position;
            disk.GetComponent<MeshFilter>().sharedMesh = mesh;
            disk.GetComponent<Renderer>().sharedMaterial = material;
        }
    }
}

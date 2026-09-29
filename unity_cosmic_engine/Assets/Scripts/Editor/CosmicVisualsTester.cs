using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CosmicZoom.Editor
{
    public static class CosmicVisualsTester
    {
        [MenuItem("Cosmic Zoom/Capture Visuals Test Screenshots")]
        public static void CaptureTestScreenshots()
        {
            // First re-assemble scene to make sure latest materials, lighting, and FBX are bound
            CosmicSceneBuilder.AssembleScene();

            string scenePath = "Assets/Scenes/CosmicZoomScene.unity";
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            Camera cam = Camera.main;
            if (cam == null)
            {
                cam = Object.FindAnyObjectByType<Camera>();
            }

            if (cam == null)
            {
                Debug.LogError("[VisualsTester] No camera found in scene!");
                return;
            }

            CosmicZoomEngine engine = Object.FindAnyObjectByType<CosmicZoomEngine>();
            CelestialMessierCatalog messier = Object.FindAnyObjectByType<CelestialMessierCatalog>();

            string outDir = @"d:\Vibe Code Repo\galactic_view\visual_tests";
            Directory.CreateDirectory(outDir);

            int width = 1920;
            int height = 1080;
            RenderTexture rt = new RenderTexture(width, height, 24);
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);

            cam.targetTexture = rt;

            // Ensure Cockpit Canopy Overlay is readable for compositing
            string canopyPath = "Assets/Textures/cockpit_canopy_overlay.png";
            TextureImporter ti = AssetImporter.GetAtPath(canopyPath) as TextureImporter;
            if (ti != null && !ti.isReadable)
            {
                ti.isReadable = true;
                ti.SaveAndReimport();
            }
            Texture2D canopyOverlay = AssetDatabase.LoadAssetAtPath<Texture2D>(canopyPath);

            // 1. Capture Stage 1 (Solar System & Starships)
            if (engine != null)
            {
                engine.ApplyZoom(1.0f);
            }
            if (messier != null) messier.isStarryNightActive = false;

            cam.transform.position = new Vector3(0, 25f, 70f);
            cam.transform.LookAt(Vector3.zero);
            RenderAndSave(cam, rt, tex, canopyOverlay, Path.Combine(outDir, "stage1_solar_system.png"));

            // 1b. Close-up on Sun (Photosphere & Soft Radial Corona Halo)
            cam.transform.position = new Vector3(0, 3f, 18f);
            cam.transform.LookAt(Vector3.zero);
            RenderAndSave(cam, rt, tex, null, Path.Combine(outDir, "sun_closeup.png"));

            // 1c. Close-up on Deep-Space Survey Flagship (PBR Metallic Hull, Gold Wings, Cyan Thrusters)
            GameObject flagshipObj = GameObject.Find("Survey_Flagship");
            if (flagshipObj != null)
            {
                Vector3 shipPos = flagshipObj.transform.position;
                cam.transform.position = shipPos + new Vector3(5f, 3.5f, 7.5f);
                cam.transform.LookAt(shipPos);
                RenderAndSave(cam, rt, tex, null, Path.Combine(outDir, "flagship_closeup.png"));
            }

            // 2. Capture Stage 2 (Milky Way Galaxy)
            if (engine != null)
            {
                engine.ApplyZoom(2.0f);
            }
            cam.transform.position = new Vector3(0, 70f, 100f);
            cam.transform.LookAt(Vector3.zero);
            RenderAndSave(cam, rt, tex, canopyOverlay, Path.Combine(outDir, "stage2_milky_way.png"));

            // 3. Capture Stage 3 (Local Group Cluster)
            if (engine != null)
            {
                engine.ApplyZoom(3.0f);
            }
            cam.transform.position = new Vector3(0, 35f, 65f);
            cam.transform.LookAt(Vector3.zero);
            RenderAndSave(cam, rt, tex, canopyOverlay, Path.Combine(outDir, "stage3_local_group.png"));

            // 4. Capture Stage 4 (Cosmic Web & CMB Horizon)
            if (engine != null)
            {
                engine.ApplyZoom(4.0f);
            }
            cam.transform.position = new Vector3(0, 140f, 190f);
            cam.transform.LookAt(Vector3.zero);
            RenderAndSave(cam, rt, tex, canopyOverlay, Path.Combine(outDir, "stage4_cosmic_web.png"));

            // 5. Capture Stage 5 (Starry Night Celestial Vault & Deep-Sky Reticle)
            if (engine != null)
            {
                engine.ApplyZoom(1.0f);
            }
            if (messier != null)
            {
                messier.EnsureDatabaseBuilt();
                messier.isStarryNightActive = true;
                if (messier.vault3DRoot != null) messier.vault3DRoot.SetActive(true);
                var target = System.Linq.Enumerable.FirstOrDefault(messier.Catalog, c => c.id == "M42") ?? (messier.Catalog.Count > 0 ? messier.Catalog[0] : null);
                if (target != null)
                {
                    messier.currentTarget = target;
                    Vector3 targetDir = target.GetUnitSpherePosition();
                    cam.transform.position = Vector3.zero;
                    cam.transform.rotation = Quaternion.LookRotation(targetDir, Vector3.up);
                }
            }
            if (engine != null && engine.stage1SolarSystem != null)
            {
                engine.stage1SolarSystem.SetActive(false);
            }
            RenderAndSave(cam, rt, tex, canopyOverlay, Path.Combine(outDir, "stage5_starry_night_mode.png"));
            if (engine != null && engine.stage1SolarSystem != null)
            {
                engine.stage1SolarSystem.SetActive(true);
            }

            // 6. Reset back to Stage 1 Flight Deck
            if (messier != null)
            {
                messier.isStarryNightActive = false;
            }
            if (engine != null)
            {
                engine.ApplyZoom(1.0f);
            }

            cam.targetTexture = null;
            RenderTexture.active = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);

            Debug.Log("[VisualsTester] All test screenshots captured with cockpit overlay to: " + outDir);
        }

        private static void RenderAndSave(Camera cam, RenderTexture rt, Texture2D tex, Texture2D canopy, string filePath)
        {
            cam.Render();
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);

            // Composite Cockpit Canopy Overlay with bilinear coordinates (eliminates any stride mismatch)
            if (canopy != null)
            {
                int w = tex.width;
                int h = tex.height;
                Color[] sceneColors = tex.GetPixels();

                for (int y = 0; y < h; y++)
                {
                    float v = (float)y / (h - 1);
                    int row = y * w;
                    for (int x = 0; x < w; x++)
                    {
                        float u = (float)x / (w - 1);
                        Color c = canopy.GetPixelBilinear(u, v);
                        if (c.a > 0.01f)
                        {
                            Color s = sceneColors[row + x];
                            sceneColors[row + x] = new Color(
                                s.r * (1f - c.a) + c.r * c.a,
                                s.g * (1f - c.a) + c.g * c.a,
                                s.b * (1f - c.a) + c.b * c.a,
                                1.0f
                            );
                        }
                    }
                }
                tex.SetPixels(sceneColors);
            }

            tex.Apply();
            byte[] bytes = tex.EncodeToPNG();
            File.WriteAllBytes(filePath, bytes);
            Debug.Log("[VisualsTester] Saved: " + filePath + " (" + bytes.Length + " bytes)");
        }
    }
}

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

namespace CosmicZoom.Editor
{
    /// <summary>
    /// Editor automation tool to assemble the complete 3D Cosmic Zoom Engine scene with
    /// circular disk geometry, transparent alpha-masked materials (ZERO black square artifacts),
    /// acoustic audio suite, and build native Windows 64-bit standalone player.
    /// </summary>
    public static class CosmicSceneBuilder
    {
        [MenuItem("Cosmic Zoom/Assemble Complete 3D Cosmic Scene")]
        public static void AssembleScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Create Materials Directory
            string matDir = "Assets/Materials";
            if (!AssetDatabase.IsValidFolder(matDir))
            {
                AssetDatabase.CreateFolder("Assets", "Materials");
            }

            // Configure Texture Importers for alpha transparency
            ConfigureTextureImporter("Assets/Textures/milky_way_disk.png", true);
            ConfigureTextureImporter("Assets/Textures/andromeda_galaxy_disk.png", true);
            ConfigureTextureImporter("Assets/Textures/cosmic_web_simulation.png", true);
            ConfigureTextureImporter("Assets/Textures/sun_corona_glow.png", true);

            // Create PBR & Transparent Additive Materials
            Material matSun = CreateMaterial("Mat_Sun", "Assets/Textures/sun_photosphere.jpg", new Color(1f, 0.95f, 0.8f), 3.0f);
            Material matSunCorona = CreateMaterial("Mat_SunCorona", "Assets/Textures/sun_corona_glow.png", new Color(1f, 0.9f, 0.5f), 3.5f, 0.5f, isAdditive: true);
            Material matEarth = CreateMaterial("Mat_Earth", "Assets/Textures/earth_photosphere.jpg", Color.white, 0f, 0.2f);
            Material matJupiter = CreateMaterial("Mat_Jupiter", "Assets/Textures/jupiter_photosphere.jpg", Color.white, 0f, 0.5f);
            Material matSaturn = CreateMaterial("Mat_Saturn", "Assets/Textures/jupiter_photosphere.jpg", new Color(0.95f, 0.9f, 0.75f), 0.2f, 0.5f);
            Material matSaturnRings = CreateMaterial("Mat_SaturnRings", "Assets/Textures/jupiter_photosphere.jpg", new Color(0.9f, 0.85f, 0.7f, 0.8f), 0.5f, 0.5f, isAdditive: false, isTransparent: true);
            Material matNeptune = CreateMaterial("Mat_Neptune", "Assets/Textures/earth_photosphere.jpg", new Color(0.2f, 0.5f, 1.0f), 0.3f, 0.4f);
            Material matOrbit = CreateMaterial("Mat_OrbitBoundary", "", new Color(0.25f, 0.75f, 1.0f, 0.8f), 2.0f, 0.5f, isAdditive: true);

            // Stage 2 Milky Way (Additive: Black space adds 0 light, zero square boundaries)
            Material matMilkyWay = CreateMaterial("Mat_MilkyWay", "Assets/Textures/milky_way_disk.png", Color.white, 2.5f, 0.5f, isAdditive: true);
            Material matBeacon = CreateMaterial("Mat_Beacon", "Assets/Textures/sun_corona_glow.png", new Color(1.0f, 0.85f, 0.2f), 5.0f, 0.5f, isAdditive: true);
            Material matSgrA = CreateMaterial("Mat_SgrA", "Assets/Textures/sun_photosphere.jpg", new Color(1.0f, 0.95f, 0.7f), 5.0f, 0.5f);

            // Stage 3 Local Group
            Material matAndromeda = CreateMaterial("Mat_Andromeda", "Assets/Textures/andromeda_galaxy_disk.png", new Color(0.95f, 0.95f, 1.0f), 2.5f, 0.5f, isAdditive: true);

            // Stage 4 Cosmic Web & CMB
            Material matCosmicWeb = CreateMaterial("Mat_CosmicWeb", "Assets/Textures/cosmic_web_simulation.png", new Color(1f, 0.85f, 0.4f), 2.2f, 0.5f, isAdditive: true);
            Material matCMB = CreateMaterial("Mat_CMBHorizon", "Assets/Textures/cmb_horizon_sky.jpg", new Color(0.3f, 0.7f, 1f), 1.8f);

            // 2. Root Manager GameObject
            GameObject managerObj = new GameObject("[Cosmic_Zoom_Engine]");
            CosmicZoomEngine engine = managerObj.AddComponent<CosmicZoomEngine>();
            CosmicAudioController audioController = managerObj.AddComponent<CosmicAudioController>();
            CosmicHUD hud = managerObj.AddComponent<CosmicHUD>();
            LightPulseEmitter pulseEmitter = managerObj.AddComponent<LightPulseEmitter>();

            // 3. Camera Setup
            GameObject camObj = new GameObject("Main Camera");
            Camera cam = camObj.AddComponent<Camera>();
            camObj.tag = "MainCamera";
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.005f, 0.008f, 0.015f, 1.0f);
            cam.fieldOfView = 45f;
            cam.nearClipPlane = 0.5f;
            cam.farClipPlane = 150000f; // Wide cosmic clipping depth
            camObj.transform.position = new Vector3(0, 60f, 130f);
            camObj.transform.LookAt(Vector3.zero);

            // 4. Ambient & Directional Lighting
            RenderSettings.ambientLight = new Color(0.20f, 0.28f, 0.42f, 1.0f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;

            GameObject dirLightObj = new GameObject("Sun_PointLight");
            Light sunLight = dirLightObj.AddComponent<Light>();
            sunLight.type = LightType.Point;
            sunLight.color = new Color(1.0f, 0.98f, 0.92f);
            sunLight.intensity = 5.0f;
            sunLight.range = 5000f;
            sunLight.shadows = LightShadows.None; // Prevent shadow acne artifacts
            dirLightObj.transform.position = Vector3.zero;

            // 5. Stage Hierarchies
            GameObject stage1 = new GameObject("Stage1_Solar_System");
            GameObject stage2 = new GameObject("Stage2_Milky_Way");
            GameObject stage3 = new GameObject("Stage3_Local_Group");
            GameObject stage4 = new GameObject("Stage4_Cosmic_Web");

            // Instantiate Blender FBX Models
            GameObject s1Obj = LoadAndInstantiateModel("Assets/Models/solar_system_bodies.fbx", stage1.transform);
            GameObject s2Obj = LoadAndInstantiateModel("Assets/Models/milky_way_spiral.fbx", stage2.transform);
            GameObject s3Obj = LoadAndInstantiateModel("Assets/Models/local_group_galaxies.fbx", stage3.transform);
            GameObject s4Obj = LoadAndInstantiateModel("Assets/Models/observable_universe_boundary.fbx", stage4.transform);

            // Assign Textures & Materials to Renderers
            AssignMaterialsToStage1(s1Obj, matSun, matSunCorona, matEarth, matJupiter, matSaturn, matSaturnRings, matNeptune, matOrbit);
            AssignMaterialsToStage2(s2Obj, matMilkyWay, matBeacon, matSgrA);
            AssignMaterialsToStage3(s3Obj, matMilkyWay, matAndromeda);
            AssignMaterialsToStage4(s4Obj, matCMB, matCosmicWeb);

            // Stage initial visibility: Stage 1 active, others inactive until zoom threshold
            stage1.SetActive(true);
            stage2.SetActive(false);
            stage3.SetActive(false);
            stage4.SetActive(false);

            // Wire up Engine References
            SerializedObject soEngine = new SerializedObject(engine);
            soEngine.FindProperty("stage1SolarSystem").objectReferenceValue = stage1;
            soEngine.FindProperty("stage2MilkyWay").objectReferenceValue = stage2;
            soEngine.FindProperty("stage3LocalGroup").objectReferenceValue = stage3;
            soEngine.FindProperty("stage4CosmicWeb").objectReferenceValue = stage4;
            soEngine.FindProperty("mainCamera").objectReferenceValue = cam;
            soEngine.FindProperty("cameraFocusTarget").objectReferenceValue = managerObj.transform;
            soEngine.FindProperty("audioController").objectReferenceValue = audioController;
            soEngine.FindProperty("lightPulseEmitter").objectReferenceValue = pulseEmitter;
            soEngine.ApplyModifiedProperties();

            // Wire up Audio Controller Clips
            SerializedObject soAudio = new SerializedObject(audioController);
            soAudio.FindProperty("narrationStage1").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/narration_stage1.mp3");
            soAudio.FindProperty("narrationStage2").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/narration_stage2.mp3");
            soAudio.FindProperty("narrationStage3").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/narration_stage3.mp3");
            soAudio.FindProperty("narrationStage4").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/narration_stage4.mp3");

            soAudio.FindProperty("acousticMovement1").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/acoustic_space_movement1.mp3");
            soAudio.FindProperty("acousticMovement2").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/acoustic_space_movement2.mp3");
            soAudio.FindProperty("acousticMovement3").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/acoustic_space_movement3.mp3");
            soAudio.FindProperty("soothingAcoustic").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/soothing_acoustic_space.mp3");

            soAudio.FindProperty("gentleChimeSFX").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/gentle_acoustic_chime.mp3");
            soAudio.FindProperty("lightPulseVoice").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/light_pulse_voice.mp3");
            soAudio.ApplyModifiedProperties();

            // Wire up HUD
            SerializedObject soHud = new SerializedObject(hud);
            soHud.FindProperty("engine").objectReferenceValue = engine;
            soHud.FindProperty("audioController").objectReferenceValue = audioController;
            soHud.ApplyModifiedProperties();

            // Save Scene
            string scenePath = "Assets/Scenes/CosmicZoomScene.unity";
            EditorSceneManager.SaveScene(scene, scenePath);

            // Ensure scene is registered in Build Settings
            EditorBuildSettingsScene[] buildScenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene(scenePath, true)
            };
            EditorBuildSettings.scenes = buildScenes;

            AssetDatabase.SaveAssets();
            Debug.Log("<color=#38bdf8><b>[Cosmic Scene Builder]</b> Scene assembled with seamless circular geometry and transparent materials: " + scenePath + "</color>");
        }

        public static void BuildStandalonePlayer()
        {
            AssembleScene();

            string appDir = @"d:\Vibe Code Repo\galactic_view\CosmicZoomEngine_App";
            Directory.CreateDirectory(appDir);
            string exePath = Path.Combine(appDir, "CosmicZoomEngine.exe");

            BuildPlayerOptions opts = new BuildPlayerOptions
            {
                scenes = new string[] { "Assets/Scenes/CosmicZoomScene.unity" },
                locationPathName = exePath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            Debug.Log("[Cosmic Scene Builder] Starting Unity 6 Standalone Windows 64-bit build to: " + exePath);
            var report = BuildPipeline.BuildPlayer(opts);
            if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                Debug.Log("<color=#34d399><b>[Cosmic Scene Builder] SUCCESS!</b> Standalone native player built (" + report.summary.totalSize + " bytes): " + exePath + "</color>");
            }
            else
            {
                Debug.LogError("[Cosmic Scene Builder] Build failed with result: " + report.summary.result);
            }
        }

        private static void ConfigureTextureImporter(string texturePath, bool isTransparent)
        {
            TextureImporter ti = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (ti != null)
            {
                ti.alphaIsTransparency = isTransparent;
                ti.mipmapEnabled = true;
                ti.wrapMode = TextureWrapMode.Clamp;
                ti.SaveAndReimport();
            }
        }

        private static Material CreateMaterial(string name, string texturePath, Color color, float emission, float roughness = 0.5f, bool isAdditive = false, bool isTransparent = false)
        {
            string matPath = "Assets/Materials/" + name + ".mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);

            Shader shader;
            if (isAdditive)
            {
                shader = Shader.Find("Mobile/Particles/Additive") ?? Shader.Find("Particles/Standard Unlit") ?? Shader.Find("Unlit/Transparent");
            }
            else if (isTransparent)
            {
                shader = Shader.Find("Unlit/Transparent") ?? Shader.Find("Mobile/Particles/Alpha Blended") ?? Shader.Find("Standard");
            }
            else
            {
                shader = Shader.Find("Standard") ?? Shader.Find("Unlit/Texture");
            }

            if (mat == null || mat.shader != shader)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, matPath);
            }

            mat.color = color;
            if (mat.HasProperty("_TintColor")) mat.SetColor("_TintColor", color);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 1.0f - roughness);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 1.0f - roughness);

            if (!string.IsNullOrEmpty(texturePath))
            {
                Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                if (tex != null)
                {
                    mat.mainTexture = tex;
                }
            }

            if (emission > 0)
            {
                mat.EnableKeyword("_EMISSION");
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.SetColor("_EmissionColor", color * emission);
                }
            }

            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static GameObject LoadAndInstantiateModel(string assetPath, Transform parent)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab != null)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                instance.transform.localPosition = Vector3.zero;
                return instance;
            }
            return null;
        }

        private static void AssignMaterialsToStage1(GameObject root, Material sun, Material sunCorona, Material earth, Material jupiter, Material saturn, Material saturnRings, Material neptune, Material orbit)
        {
            if (root == null) return;
            foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
            {
                string n = r.gameObject.name.ToLower();
                if (n.Contains("corona")) r.sharedMaterial = sunCorona;
                else if (n.Contains("sun")) r.sharedMaterial = sun;
                else if (n.Contains("earth")) r.sharedMaterial = earth;
                else if (n.Contains("jupiter")) r.sharedMaterial = jupiter;
                else if (n.Contains("saturn_rings") || n.Contains("rings")) r.sharedMaterial = saturnRings;
                else if (n.Contains("saturn")) r.sharedMaterial = saturn;
                else if (n.Contains("neptune_orbit") || n.Contains("boundary")) r.sharedMaterial = orbit;
                else if (n.Contains("neptune")) r.sharedMaterial = neptune;
            }
        }

        private static void AssignMaterialsToStage2(GameObject root, Material milkyWay, Material beacon, Material sgrA)
        {
            if (root == null) return;
            foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
            {
                string n = r.gameObject.name.ToLower();
                if (n.Contains("beacon") || n.Contains("sun_ring")) r.sharedMaterial = beacon;
                else if (n.Contains("sagittarius") || n.Contains("sgra")) r.sharedMaterial = sgrA;
                else r.sharedMaterial = milkyWay;
            }
        }

        private static void AssignMaterialsToStage3(GameObject root, Material milkyWay, Material andromeda)
        {
            if (root == null) return;
            foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
            {
                string n = r.gameObject.name.ToLower();
                if (n.Contains("andromeda") || n.Contains("m31")) r.sharedMaterial = andromeda;
                else r.sharedMaterial = milkyWay;
            }
        }

        private static void AssignMaterialsToStage4(GameObject root, Material cmb, Material cosmicWeb)
        {
            if (root == null) return;
            foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
            {
                string n = r.gameObject.name.ToLower();
                if (n.Contains("horizon") || n.Contains("cmb")) r.sharedMaterial = cmb;
                else r.sharedMaterial = cosmicWeb;
            }
        }
    }
}

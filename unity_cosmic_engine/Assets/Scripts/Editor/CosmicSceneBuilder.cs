using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

namespace CosmicZoom.Editor
{
    /// <summary>
    /// Editor automation tool to assemble the complete 3D Cosmic Zoom Engine scene with
    /// PBR textured materials, acoustic audio suite, and build native Windows 64-bit standalone player.
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

            // Create PBR Materials with Textures
            Material matSun = CreateMaterial("Mat_Sun", "Assets/Textures/sun_photosphere.jpg", new Color(1f, 0.9f, 0.5f), 2.5f);
            Material matEarth = CreateMaterial("Mat_Earth", "Assets/Textures/earth_photosphere.jpg", Color.white, 0f, 0.2f);
            Material matJupiter = CreateMaterial("Mat_Jupiter", "Assets/Textures/jupiter_photosphere.jpg", Color.white, 0f, 0.5f);
            Material matMilkyWay = CreateMaterial("Mat_MilkyWay", "Assets/Textures/milky_way_disk.jpg", Color.white, 2.0f);
            Material matAndromeda = CreateMaterial("Mat_Andromeda", "Assets/Textures/andromeda_galaxy_disk.jpg", Color.white, 2.0f);
            Material matCosmicWeb = CreateMaterial("Mat_CosmicWeb", "Assets/Textures/cosmic_web_simulation.jpg", new Color(1f, 0.85f, 0.4f), 2.5f);
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
            AssignMaterialsToStage1(s1Obj, matSun, matEarth, matJupiter);
            AssignMaterialsToStage2(s2Obj, matMilkyWay);
            AssignMaterialsToStage3(s3Obj, matMilkyWay, matAndromeda);
            AssignMaterialsToStage4(s4Obj, matCMB, matCosmicWeb);

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
            Debug.Log("<color=#38bdf8><b>[Cosmic Scene Builder]</b> Scene assembled with PBR textures and acoustic score: " + scenePath + "</color>");
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

        private static Material CreateMaterial(string name, string texturePath, Color color, float emission, float roughness = 0.5f)
        {
            string matPath = "Assets/Materials/" + name + ".mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                Shader shader = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Unlit/Texture");
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, matPath);
            }

            mat.color = color;
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

        private static void AssignMaterialsToStage1(GameObject root, Material sun, Material earth, Material jupiter)
        {
            if (root == null) return;
            foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
            {
                string n = r.gameObject.name.ToLower();
                if (n.Contains("sun")) r.sharedMaterial = sun;
                else if (n.Contains("earth")) r.sharedMaterial = earth;
                else if (n.Contains("jupiter")) r.sharedMaterial = jupiter;
            }
        }

        private static void AssignMaterialsToStage2(GameObject root, Material milkyWay)
        {
            if (root == null) return;
            foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
            {
                r.sharedMaterial = milkyWay;
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

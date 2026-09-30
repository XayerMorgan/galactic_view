using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

namespace CosmicZoom.Editor
{
    /// <summary>
    /// Editor automation tool to assemble the complete 3D Cosmic Zoom Engine scene with:
    /// - AudioListener (full sound output for acoustic score, narrations, and sound effects)
    /// - CosmicStarfield (3,500 twinkling spectral background stars)
    /// - Orbital kinematics & axial rotation on all planets and Sun
    /// - Glowing orbital trajectory lines (Earth 1 AU, Jupiter 5.2 AU, Saturn 9 AU, Neptune 30 AU)
    /// - Source-backed Milky Way and Local Group maps with stable labels
    /// - Atmospheric radiance and solar corona halos
    /// - Circular disk geometry with transparent alpha-masked materials (ZERO black boxes)
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
            ConfigureTextureImporter("Assets/Textures/milky_way_nasa.jpg", false);
            ConfigureTextureImporter("Assets/Textures/triangulum_dss.jpg", false);
            ConfigureTextureImporter("Assets/Textures/andromeda_galaxy_disk.png", true);
            ConfigureTextureImporter("Assets/Textures/cosmic_web_simulation.png", true);
            ConfigureTextureImporter("Assets/Textures/sun_corona_glow.png", true);
            ConfigureTextureImporter("Assets/Textures/cockpit_canopy_overlay.png", true);
            ConfigureTextureImporter("Assets/Resources/cockpit_canopy_overlay.png", true);
            foreach (string audioPath in Directory.GetFiles("Assets/Audio", "*.mp3"))
            {
                string name = Path.GetFileName(audioPath);
                if (!name.StartsWith("space_") && !name.StartsWith("acoustic_space_") && name != "soothing_acoustic_space.mp3") continue;
                var importer = AssetImporter.GetAtPath(audioPath.Replace('\\', '/')) as AudioImporter;
                if (importer == null) continue;
                var sample = importer.defaultSampleSettings;
                if (sample.loadType == AudioClipLoadType.Streaming) continue;
                sample.loadType = AudioClipLoadType.Streaming;
                importer.defaultSampleSettings = sample;
                importer.SaveAndReimport();
            }

            // Create PBR, Unlit, and Additive Materials
            Material matSun = CreateMaterial("Mat_Sun", "Assets/Textures/sun_photosphere.jpg", new Color(1.0f, 0.85f, 0.5f), 0.65f, roughness: 1.0f);
            Material matSunCorona = CreateMaterial("Mat_SunCorona", "Assets/Textures/sun_corona_glow.png", new Color(1.0f, 0.75f, 0.22f, 0.85f), 1.0f, isTransparent: true);
            Material matEarth = CreateMaterial("Mat_Earth", "Assets/Textures/earth_photosphere.jpg", Color.white, 0f, 0.2f);
            Material matJupiter = CreateMaterial("Mat_Jupiter", "Assets/Textures/jupiter_photosphere.jpg", Color.white, 0f, 0.5f);
            Material matSaturn = CreateMaterial("Mat_Saturn", "Assets/Textures/jupiter_photosphere.jpg", new Color(0.95f, 0.9f, 0.75f), 0.1f, 0.5f);
            Material matSaturnRings = CreateMaterial("Mat_SaturnRings", "Assets/Textures/jupiter_photosphere.jpg", new Color(0.9f, 0.85f, 0.7f, 0.85f), 0.5f, 0.5f, isAdditive: false, isTransparent: true);
            Material matNeptune = CreateMaterial("Mat_Neptune", "Assets/Textures/earth_photosphere.jpg", new Color(0.2f, 0.5f, 1.0f), 0.1f, 0.4f);
            Material matOrbit = CreateMaterial("Mat_OrbitBoundary", "", new Color(0.12f, 0.45f, 0.85f, 0.25f), 1.0f, 0.5f, isAdditive: true);

            // Stage 2 Milky Way (Additive: Black space adds 0 light, zero square boundaries)
            Material matMilkyWay = CreateMaterial("Mat_MilkyWay", "Assets/Textures/milky_way_nasa.jpg", Color.white, 3.0f, 0.5f, isAdditive: true);
            Material matSgrA = CreateMaterial("Mat_SgrA", "Assets/Textures/sun_photosphere.jpg", new Color(1.0f, 0.95f, 0.7f), 5.0f, 0.5f, isUnlit: true);

            // Stage 3 Local Group
            Material matAndromeda = CreateMaterial("Mat_Andromeda", "Assets/Textures/andromeda_galaxy_disk.png", new Color(0.95f, 0.95f, 1.0f), 3.0f, 0.5f, isAdditive: true);

            Material matTriangulum = CreateMaterial("Mat_Triangulum", "Assets/Textures/triangulum_dss.jpg", Color.white, 1, isAdditive: true);

            // Stage 4 Cosmic Web & CMB
            Material matCosmicWeb = CreateMaterial("Mat_CosmicWeb", "Assets/Textures/cosmic_web_simulation.png", new Color(1f, 0.85f, 0.4f), 2.5f, 0.5f, isAdditive: true);
            Material matCMB = CreateMaterial("Mat_CMBHorizon", "Assets/Textures/cmb_horizon_sky.jpg", new Color(0.35f, 0.75f, 1f), 1.0f, isUnlit: true);

            // 2. Root Manager GameObject
            GameObject managerObj = new GameObject("[Cosmic_Zoom_Engine]");
            CosmicZoomEngine engine = managerObj.AddComponent<CosmicZoomEngine>();
            CosmicAudioController audioController = managerObj.AddComponent<CosmicAudioController>();
            CosmicHUD hud = managerObj.AddComponent<CosmicHUD>();
            LightPulseEmitter pulseEmitter = managerObj.AddComponent<LightPulseEmitter>();
            CelestialMessierCatalog messierCatalog = managerObj.AddComponent<CelestialMessierCatalog>();
            messierCatalog.Build3DVault();
            if (messierCatalog.vault3DRoot != null)
            {
                messierCatalog.vault3DRoot.SetActive(false);
            }

            // 3. Camera Setup WITH AudioListener
            GameObject camObj = new GameObject("Main Camera");
            Camera cam = camObj.AddComponent<Camera>();
            camObj.tag = "MainCamera";
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.002f, 0.004f, 0.008f, 1.0f);
            cam.fieldOfView = 45f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 150000f; // Wide cosmic clipping depth
            camObj.transform.position = new Vector3(0, 45f, 95f);
            camObj.transform.LookAt(Vector3.zero);

            // AUDIO LISTENER: CRITICAL FOR SOUND TO BE AUDIBLE!
            camObj.AddComponent<AudioListener>();

            // 3D PROCEDURAL STARFIELD: Dedicated Starfield Object (Tracks Main Camera)
            GameObject starfieldObj = new GameObject("[Cosmic_Starfield]");
            starfieldObj.AddComponent<CosmicStarfield>();

            // 4. Ambient & Directional Lighting
            RenderSettings.ambientLight = new Color(0.22f, 0.28f, 0.42f, 1.0f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;

            GameObject dirLightObj = new GameObject("Sun_PointLight");
            Light sunLight = dirLightObj.AddComponent<Light>();
            sunLight.type = LightType.Point;
            sunLight.color = new Color(1.0f, 0.90f, 0.75f);
            sunLight.intensity = 1.2f;
            sunLight.range = 50000f;
            sunLight.shadows = LightShadows.None; // Prevent shadow acne artifacts
            dirLightObj.transform.position = Vector3.zero;

            GameObject fillLightObj = new GameObject("Galactic_Fill_Light");
            Light fillLight = fillLightObj.AddComponent<Light>();
            fillLight.type = LightType.Directional;
            fillLight.color = new Color(0.35f, 0.45f, 0.62f);
            fillLight.intensity = 0.35f;
            fillLightObj.transform.rotation = Quaternion.Euler(35f, 25f, 0f);

            // 5. Stage Hierarchies
            GameObject stage1 = new GameObject("Stage1_Solar_System");
            GameObject stage2 = new GameObject("Stage2_Milky_Way");
            GameObject stage3 = new GameObject("Stage3_Local_Group");
            GameObject stage4 = new GameObject("Stage4_Cosmic_Web");

            // Add Glowing Orbital Trajectory Lines to Stage 1
            stage1.AddComponent<SolarSystemOrbits>();

            // Rotate the cosmic web; keep annotated galaxy maps steady.
            // Annotated maps keep a stable orientation; users can still orbit the camera.
            stage4.AddComponent<GalacticRotator>();

            // Instantiate Blender FBX Models
            GameObject s1Obj = LoadAndInstantiateModel("Assets/Models/solar_system_bodies.fbx", stage1.transform);
            CosmicGalaxyBuilder.BuildMilkyWay(stage2.transform, matMilkyWay, matSgrA);
            CosmicGalaxyBuilder.BuildLocalGroup(stage3.transform, matMilkyWay, matAndromeda, matTriangulum);
            GameObject s4Obj = LoadAndInstantiateModel("Assets/Models/observable_universe_boundary.fbx", stage4.transform);

            // Assign Textures, Materials, Kinematics & Rotation to Renderers
            AssignStage1Bodies(s1Obj, matSun, matSunCorona, matEarth, matJupiter, matSaturn, matSaturnRings, matNeptune, matOrbit);
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

            // Wire up Audio Controller Sources & Clips
            AudioSource srcNarration = managerObj.AddComponent<AudioSource>();
            srcNarration.playOnAwake = false;
            srcNarration.volume = 1.0f;

            AudioSource srcMusicA = managerObj.AddComponent<AudioSource>();
            srcMusicA.playOnAwake = false;
            srcMusicA.volume = 0.75f;

            AudioSource srcMusicB = managerObj.AddComponent<AudioSource>();
            srcMusicB.playOnAwake = false;
            srcMusicB.volume = 0f;

            AudioSource srcSfx = managerObj.AddComponent<AudioSource>();
            srcSfx.playOnAwake = false;
            srcSfx.volume = 0.8f;

            SerializedObject soAudio = new SerializedObject(audioController);
            soAudio.FindProperty("narrationSource").objectReferenceValue = srcNarration;
            soAudio.FindProperty("musicSourceA").objectReferenceValue = srcMusicA;
            soAudio.FindProperty("musicSourceB").objectReferenceValue = srcMusicB;
            soAudio.FindProperty("sfxSource").objectReferenceValue = srcSfx;

            soAudio.FindProperty("narrationStage1").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/narration_stage1.mp3");
            soAudio.FindProperty("narrationStage2").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/narration_stage2.mp3");
            soAudio.FindProperty("narrationStage3").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/narration_stage3.mp3");
            soAudio.FindProperty("narrationStage4").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/narration_stage4.mp3");

            soAudio.FindProperty("acousticMovement1").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/acoustic_space_movement1.mp3");
            soAudio.FindProperty("acousticMovement2").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/acoustic_space_movement2.mp3");
            soAudio.FindProperty("acousticMovement3").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/acoustic_space_movement3.mp3");
            soAudio.FindProperty("soothingAcoustic").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/soothing_acoustic_space.mp3");

            string[] tracks = { "asterism", "drift_beyond_orion", "ion_trails", "pale_blue_horizon", "the_long_return" };
            string[] titles = { "Asterism", "Drift Beyond Orion", "Ion Trails", "Pale Blue Horizon", "The Long Return" };
            soAudio.FindProperty("additionalMusic").arraySize = tracks.Length;
            soAudio.FindProperty("additionalMusicTitles").arraySize = titles.Length;
            for (int i = 0; i < tracks.Length; i++)
            {
                soAudio.FindProperty("additionalMusic").GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/space_" + tracks[i] + ".mp3");
                soAudio.FindProperty("additionalMusicTitles").GetArrayElementAtIndex(i).stringValue = titles[i];
            }

            soAudio.FindProperty("gentleChimeSFX").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/gentle_acoustic_chime.mp3");
            soAudio.FindProperty("lightPulseVoice").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/light_pulse_voice.mp3");
            soAudio.ApplyModifiedProperties();

            // Wire up HUD
            SerializedObject soHud = new SerializedObject(hud);
            soHud.FindProperty("engine").objectReferenceValue = engine;
            soHud.FindProperty("audioController").objectReferenceValue = audioController;
            soHud.FindProperty("messierCatalog").objectReferenceValue = messierCatalog;
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
            Debug.Log("<color=#38bdf8><b>[Cosmic Scene Builder]</b> Scene assembled with AudioListener, Starfield, and living kinematics: " + scenePath + "</color>");
        }

        public static void BuildStandalonePlayer()
        {
            AssembleScene();

            string appDir = Path.GetFullPath(Path.Combine(Application.dataPath, "../../CosmicZoomEngine_App"));
            PlayerSettings.productName = "Astronautica - Cosmic Explorer";
            PlayerSettings.bundleVersion = File.ReadAllText(Path.Combine(Application.dataPath, "../../VERSION")).Trim();
            PlayerSettings.defaultIsNativeResolution = false;
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
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
                File.WriteAllText(Path.Combine(appDir, "VERSION"), PlayerSettings.bundleVersion + "\n");
                Debug.Log("<color=#34d399><b>[Cosmic Scene Builder] SUCCESS!</b> Standalone native player built (" + report.summary.totalSize + " bytes): " + exePath + "</color>");
            }
            else
            {
                throw new System.InvalidOperationException("Build failed: " + report.summary.result);
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

        private static Material CreateMaterial(string name, string texturePath, Color color, float emission, float roughness = 0.5f, bool isAdditive = false, bool isTransparent = false, bool isUnlit = false, float metallic = 0.0f)
        {
            string matPath = "Assets/Materials/" + name + ".mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);

            Shader shader;
            if (name == "Mat_Sun")
            {
                shader = Shader.Find("Standard");
            }
            else if (name == "Mat_SunCorona")
            {
                shader = Shader.Find("Cosmic/Additive");
            }
            else if (isAdditive)
            {
                shader = Shader.Find("Cosmic/Additive");
            }
            else if (isTransparent)
            {
                shader = Shader.Find("Cosmic/Transparent");
            }
            else if (isUnlit || name.Contains("CMB") || name.Contains("SgrA"))
            {
                shader = Shader.Find("Unlit/Texture") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
            }
            else
            {
                shader = Shader.Find("Standard") ?? Shader.Find("Unlit/Texture");
            }

            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, matPath);
            }
            else mat.shader = shader;

            mat.color = color;
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            if (mat.HasProperty("_TintColor")) mat.SetColor("_TintColor", color);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 1.0f - roughness);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 1.0f - roughness);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);

            Texture2D tex = null;
            if (!string.IsNullOrEmpty(texturePath))
            {
                tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                if (tex != null)
                {
                    mat.mainTexture = tex;
                    if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
                }
            }

            if (name == "Mat_Sun")
            {
                mat.EnableKeyword("_EMISSION");
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                if (tex != null && mat.HasProperty("_EmissionMap"))
                {
                    mat.SetTexture("_EmissionMap", tex);
                }
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.SetColor("_EmissionColor", new Color(1.0f, 0.58f, 0.12f, 1.0f) * emission);
                }
            }
            else if (emission > 0 && mat.shader.name == "Standard")
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
                PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                return instance;
            }
            return null;
        }

        private static void AssignStage1Bodies(GameObject root, Material sun, Material sunCorona, Material earth, Material jupiter, Material saturn, Material saturnRings, Material neptune, Material orbit)
        {
            if (root == null) return;

            Transform sunTrans = null;

            // First pass: Find Sun transform for orbit center
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name.ToLower().Contains("sun") && !t.name.ToLower().Contains("corona"))
                {
                    sunTrans = t;
                    break;
                }
            }

            // Create or update dedicated soft radial Sun Corona Halo billboard quad
            if (sunTrans != null)
            {
                Transform parentStage = root.transform.parent != null ? root.transform.parent : root.transform;
                Transform existingCorona = parentStage.Find("Sun_Corona_Halo");
                GameObject coronaQuad;
                if (existingCorona != null)
                {
                    coronaQuad = existingCorona.gameObject;
                }
                else
                {
                    coronaQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    coronaQuad.name = "Sun_Corona_Halo";
                    coronaQuad.transform.SetParent(parentStage, false);
                    Object.DestroyImmediate(coronaQuad.GetComponent<Collider>());
                }
                coronaQuad.transform.position = sunTrans.position;
                coronaQuad.transform.localScale = Vector3.one * 14.5f;
                var quadRenderer = coronaQuad.GetComponent<Renderer>();
                if (quadRenderer != null)
                {
                    quadRenderer.sharedMaterial = sunCorona;
                }
                if (coronaQuad.GetComponent<BillboardToCamera>() == null)
                {
                    coronaQuad.AddComponent<BillboardToCamera>();
                }
            }

            // Second pass: Assign materials and attach living rotation/revolution kinematics
            foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
            {
                GameObject go = r.gameObject;
                string n = go.name.ToLower();

                if (n == "earth" || n == "jupiter" || n == "saturn" || n == "neptune" || n == "sun_photosphere")
                    SmoothSphere(go.GetComponent<MeshFilter>());

                if (n.Contains("corona"))
                {
                    r.sharedMaterial = sunCorona;
                }
                else if (n.Contains("saturn_rings") || (n.Contains("rings") && n.Contains("saturn")))
                {
                    r.sharedMaterial = saturnRings;
                    // Blender exported the parent's position a second time into the ring.
                    go.transform.localPosition = Vector3.zero;
                    go.transform.localRotation = Quaternion.identity;
                }
                else if (n.Contains("orbit") || n.Contains("boundary") || (n.Contains("ring") && !n.Contains("saturn")))
                {
                    r.gameObject.SetActive(false); // Clean: smooth glowing vector LineRenderers handled by SolarSystemOrbits
                }
                else if (n.Contains("sun"))
                {
                    r.sharedMaterial = sun;
                    var body = go.GetComponent<CelestialBody>() ?? go.AddComponent<CelestialBody>();
                    body.rotationSpeed = 2.0f;
                    body.orbitalSpeed = 0f;
                }
                else if (n.Contains("earth"))
                {
                    r.sharedMaterial = earth;
                    var body = go.AddComponent<CelestialBody>();
                    body.orbitCenter = sunTrans;
                    body.orbitalSpeed = 10.0f;
                    body.rotationSpeed = 35.0f;
                    body.axialTiltDegrees = 23.4f;

                    // Atmosphere glow shell around Earth
                    go.AddComponent<AtmosphereGlow>();
                }
                else if (n.Contains("jupiter"))
                {
                    r.sharedMaterial = jupiter;
                    var body = go.AddComponent<CelestialBody>();
                    body.orbitCenter = sunTrans;
                    body.orbitalSpeed = 5.0f;
                    body.rotationSpeed = 45.0f;
                    body.axialTiltDegrees = 3.1f;
                }
                else if (n.Contains("saturn"))
                {
                    r.sharedMaterial = saturn;
                    var body = go.AddComponent<CelestialBody>();
                    body.orbitCenter = sunTrans;
                    body.orbitalSpeed = 3.5f;
                    body.rotationSpeed = 30.0f;
                    body.axialTiltDegrees = 26.7f;
                }
                else if (n.Contains("neptune"))
                {
                    r.sharedMaterial = neptune;
                    var body = go.AddComponent<CelestialBody>();
                    body.orbitCenter = sunTrans;
                    body.orbitalSpeed = 1.8f;
                    body.rotationSpeed = 25.0f;
                    body.axialTiltDegrees = 28.3f;
                }
            }
        }

        private static void AssignMaterialsToStage4(GameObject root, Material cmb, Material cosmicWeb)
        {
            if (root == null) return;
            foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
            {
                string n = r.gameObject.name.ToLower();
                if (n.Contains("horizon") || n.Contains("cmb"))
                {
                    // A luminous boundary, not an opaque shell hiding the cosmic web.
                    cmb.shader = Shader.Find("Cosmic/Rim");
                    cmb.color = new Color(0.32f, 0.58f, 0.75f, 0.28f);
                    cmb.SetFloat("_Power", 7f);
                    r.sharedMaterial = cmb;
                    SmoothSphere(r.GetComponent<MeshFilter>());
                    EditorUtility.SetDirty(cmb);
                }
                else r.sharedMaterial = cosmicWeb;
            }
        }

        private static void SmoothSphere(MeshFilter filter)
        {
            if (filter == null || filter.sharedMesh == null) return;
            // Blender's flat-shaded faces retain split normals, including at UV seams.
            // Analytic radial normals smooth the sphere without changing its UV layout.
            Mesh mesh = Object.Instantiate(filter.sharedMesh);
            mesh.name = filter.sharedMesh.name + "_Smooth";
            Vector3 center = mesh.bounds.center;
            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = new Vector3[vertices.Length];
            for (int i = 0; i < vertices.Length; i++) normals[i] = (vertices[i] - center).normalized;
            mesh.normals = normals;
            filter.sharedMesh = mesh;
        }
    }
}

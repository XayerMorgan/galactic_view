using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Reflection;

namespace CosmicZoom.Editor
{
    /// <summary>
    /// Editor automation tool to assemble the 3D Cosmic Zoom Engine scene with one click.
    /// Loads the Blender-generated FBX assets and ElevenLabs audio clips.
    /// </summary>
    public static class CosmicSceneBuilder
    {
        [MenuItem("Cosmic Zoom/Assemble Complete 3D Cosmic Scene")]
        public static void AssembleScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Root Manager GameObject
            GameObject managerObj = new GameObject("[Cosmic_Zoom_Engine]");
            CosmicZoomEngine engine = managerObj.AddComponent<CosmicZoomEngine>();
            CosmicAudioController audioController = managerObj.AddComponent<CosmicAudioController>();
            CosmicHUD hud = managerObj.AddComponent<CosmicHUD>();
            LightPulseEmitter pulseEmitter = managerObj.AddComponent<LightPulseEmitter>();

            // 2. Camera Setup
            GameObject camObj = new GameObject("Main Camera");
            Camera cam = camObj.AddComponent<Camera>();
            camObj.tag = "MainCamera";
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.015f, 0.02f, 0.04f, 1.0f);
            cam.fieldOfView = 45f;
            cam.nearClipPlane = 0.5f;
            cam.farClipPlane = 100000f; // Wide cosmic clipping depth
            camObj.transform.position = new Vector3(0, 75f, 150f);
            camObj.transform.LookAt(Vector3.zero);

            // 3. Ambient Lighting
            RenderSettings.ambientLight = new Color(0.15f, 0.22f, 0.35f, 1.0f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;

            // 4. Sun Dynamic Point Light
            GameObject sunLightObj = new GameObject("Sun_PointLight");
            Light sunLight = sunLightObj.AddComponent<Light>();
            sunLight.type = LightType.Point;
            sunLight.color = new Color(1.0f, 0.98f, 0.9f);
            sunLight.intensity = 3.5f;
            sunLight.range = 2500f;
            sunLightObj.transform.position = Vector3.zero;

            // 5. Stage Hierarchies
            GameObject stage1 = new GameObject("Stage1_Solar_System");
            GameObject stage2 = new GameObject("Stage2_Milky_Way");
            GameObject stage3 = new GameObject("Stage3_Local_Group");
            GameObject stage4 = new GameObject("Stage4_Cosmic_Web");

            // Load Blender FBX Prefabs
            LoadAndInstantiateModel("Assets/Models/solar_system_bodies.fbx", stage1.transform);
            LoadAndInstantiateModel("Assets/Models/milky_way_spiral.fbx", stage2.transform);
            LoadAndInstantiateModel("Assets/Models/local_group_galaxies.fbx", stage3.transform);
            LoadAndInstantiateModel("Assets/Models/observable_universe_boundary.fbx", stage4.transform);

            // Wire up Engine References via Reflection/SerializedObject
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
            soAudio.FindProperty("warpWhooshSFX").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/warp_whoosh.mp3");
            soAudio.FindProperty("ambientSpaceDrone").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/ambient_space.mp3");
            soAudio.FindProperty("uiPingSFX").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/ui_ping.mp3");
            soAudio.ApplyModifiedProperties();

            // Save Scene
            string scenePath = "Assets/Scenes/CosmicZoomScene.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            AssetDatabase.SaveAssets();

            Debug.Log("<color=#38bdf8><b>[Cosmic Zoom Engine]</b> Scene successfully assembled and saved to " + scenePath + "!</color>");
        }

        private static void LoadAndInstantiateModel(string assetPath, Transform parent)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab != null)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;
                instance.transform.localScale = Vector3.one;
            }
            else
            {
                Debug.LogWarning("Model not found at: " + assetPath);
            }
        }
    }
}

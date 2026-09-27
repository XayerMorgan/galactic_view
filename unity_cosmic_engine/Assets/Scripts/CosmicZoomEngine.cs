using System.Collections;
using UnityEngine;

namespace CosmicZoom
{
    /// <summary>
    /// Master engine coordinating the 4 Universal Scales in Unity:
    /// Stage 1: The Solar System Scale (~8.33 Light-Hours)
    /// Stage 2: The Milky Way Galaxy Scale (~100,000 Light-Years)
    /// Stage 3: The Local Group Scale (~10 Million Light-Years)
    /// Stage 4: The Macro Cosmic Web & Boundary (~93 Billion Light-Years)
    /// </summary>
    public class CosmicZoomEngine : MonoBehaviour
    {
        [Header("Stage Root Hierarchies")]
        [SerializeField] private GameObject stage1SolarSystem;
        [SerializeField] private GameObject stage2MilkyWay;
        [SerializeField] private GameObject stage3LocalGroup;
        [SerializeField] private GameObject stage4CosmicWeb;

        [Header("Camera & Viewport")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private Transform cameraFocusTarget;

        [Header("Audio Controller")]
        [SerializeField] private CosmicAudioController audioController;

        [Header("Pulse Emitter")]
        [SerializeField] private LightPulseEmitter lightPulseEmitter;

        [Header("Continuous Zoom State")]
        [Range(1.0f, 4.0f)]
        public float currentZoom = 1.0f;
        private float targetZoom = 1.0f;
        public int activeStageIndex { get; private set; } = 1;

        // Stage camera distance limits
        private readonly float[] stageDistances = { 0f, 150f, 500f, 1200f, 2800f };

        private void Start()
        {
            if (mainCamera == null) mainCamera = Camera.main;
            if (audioController == null) audioController = FindObjectOfType<CosmicAudioController>();
            if (lightPulseEmitter == null) lightPulseEmitter = FindObjectOfType<LightPulseEmitter>();

            ApplyZoom(1.0f);
        }

        private void Update()
        {
            // Smooth zoom interpolation
            if (Mathf.Abs(currentZoom - targetZoom) > 0.001f)
            {
                currentZoom = Mathf.Lerp(currentZoom, targetZoom, Time.deltaTime * 3.5f);
                ApplyZoom(currentZoom);
            }

            // Keyboard Shortcuts: Keys 1, 2, 3, 4
            if (Input.GetKeyDown(KeyCode.Alpha1)) JumpToStage(1);
            else if (Input.GetKeyDown(KeyCode.Alpha2)) JumpToStage(2);
            else if (Input.GetKeyDown(KeyCode.Alpha3)) JumpToStage(3);
            else if (Input.GetKeyDown(KeyCode.Alpha4)) JumpToStage(4);

            // Light pulse trigger: Spacebar
            if (Input.GetKeyDown(KeyCode.Space))
            {
                FirePulse();
            }
        }

        public void JumpToStage(int stageNumber)
        {
            stageNumber = Mathf.Clamp(stageNumber, 1, 4);
            targetZoom = stageNumber;

            if (audioController != null)
            {
                audioController.PlayWarpTransition();
                audioController.PlayStageNarration(stageNumber);
            }
        }

        public void SetZoomDirect(float zoomVal)
        {
            targetZoom = Mathf.Clamp(zoomVal, 1.0f, 4.0f);
        }

        public void FirePulse()
        {
            if (lightPulseEmitter != null)
            {
                Vector3 origin = cameraFocusTarget != null ? cameraFocusTarget.position : Vector3.zero;
                lightPulseEmitter.FireLightPulse(origin);
                if (audioController != null) audioController.PlayUIPing();
            }
        }

        private void ApplyZoom(float z)
        {
            // Visibility blending
            if (stage1SolarSystem != null) stage1SolarSystem.SetActive(z < 1.8f);
            if (stage2MilkyWay != null) stage2MilkyWay.SetActive(z >= 1.2f && z <= 2.8f);
            if (stage3LocalGroup != null) stage3LocalGroup.SetActive(z >= 2.2f && z <= 3.8f);
            if (stage4CosmicWeb != null) stage4CosmicWeb.SetActive(z >= 3.2f);

            // Determine nominal stage integer
            int newStage = 1;
            if (z < 1.5f) newStage = 1;
            else if (z < 2.5f) newStage = 2;
            else if (z < 3.5f) newStage = 3;
            else newStage = 4;

            if (newStage != activeStageIndex)
            {
                activeStageIndex = newStage;
                if (audioController != null) audioController.PlayStageNarration(activeStageIndex);
            }

            // Adjust camera position logarithmically
            if (mainCamera != null)
            {
                float t = (z - 1.0f) / 3.0f; // 0 to 1
                float targetDist = Mathf.Lerp(stageDistances[1], stageDistances[4], t);
                Vector3 focusPoint = cameraFocusTarget != null ? cameraFocusTarget.position : Vector3.zero;
                Vector3 direction = (mainCamera.transform.position - focusPoint).normalized;
                if (direction == Vector3.zero) direction = new Vector3(0, 0.45f, 1f).normalized;

                mainCamera.transform.position = focusPoint + direction * targetDist;
                mainCamera.transform.LookAt(focusPoint);
            }
        }
    }
}

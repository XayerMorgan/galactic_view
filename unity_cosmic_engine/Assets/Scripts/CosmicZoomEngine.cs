using System.Collections;
using UnityEngine;

namespace CosmicZoom
{
    /// <summary>
    /// Master engine coordinating the 4 Universal Scales in native Unity:
    /// Stage 1: The Solar System Scale (~8.33 Light-Hours)
    /// Stage 2: The Milky Way Galaxy Scale (~100,000 Light-Years)
    /// Stage 3: The Local Group Scale (~10 Million Light-Years)
    /// Stage 4: The Macro Cosmic Web & Boundary (~93 Billion Light-Years)
    /// </summary>
    public class CosmicZoomEngine : MonoBehaviour
    {
        [Header("Stage Root Hierarchies")]
        public GameObject stage1SolarSystem;
        public GameObject stage2MilkyWay;
        public GameObject stage3LocalGroup;
        public GameObject stage4CosmicWeb;

        [Header("Camera & Viewport")]
        public Camera mainCamera;
        public Transform cameraFocusTarget;

        [Header("Audio Controller")]
        public CosmicAudioController audioController;

        [Header("Pulse Emitter")]
        public LightPulseEmitter lightPulseEmitter;

        [Header("Continuous Zoom State")]
        [Range(1.0f, 4.0f)]
        public float currentZoom = 1.0f;
        private float targetZoom = 1.0f;
        public int activeStageIndex { get; private set; } = 1;

        // Stage camera distance limits
        private readonly float[] stageDistances = { 0f, 120f, 450f, 1100f, 2500f };

        private void Start()
        {
            if (mainCamera == null) mainCamera = Camera.main;
            if (audioController == null) audioController = FindAnyObjectByType<CosmicAudioController>();
            if (lightPulseEmitter == null) lightPulseEmitter = FindAnyObjectByType<LightPulseEmitter>();

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

            // Continuous scroll wheel zoom
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                SetZoomDirect(Mathf.Clamp(targetZoom + scroll * 1.5f, 1.0f, 4.0f));
            }
        }

        public void JumpToStage(int stageNumber)
        {
            stageNumber = Mathf.Clamp(stageNumber, 1, 4);
            targetZoom = stageNumber;

            if (audioController != null)
            {
                audioController.PlaySoftChime();
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
                if (audioController != null) audioController.PlayLightPulseVoice();
            }
        }

        public void ResetCamera()
        {
            if (cameraFocusTarget != null) cameraFocusTarget.position = Vector3.zero;
            JumpToStage(activeStageIndex);
        }

        private void ApplyZoom(float z)
        {
            // Visibility blending
            if (stage1SolarSystem != null) stage1SolarSystem.SetActive(z < 2.0f);
            if (stage2MilkyWay != null) stage2MilkyWay.SetActive(z >= 1.4f && z < 3.0f);
            if (stage3LocalGroup != null) stage3LocalGroup.SetActive(z >= 2.4f && z < 3.8f);
            if (stage4CosmicWeb != null) stage4CosmicWeb.SetActive(z >= 3.2f);

            // Nominal stage index
            int newStage = 1;
            if (z < 1.5f) newStage = 1;
            else if (z < 2.5f) newStage = 2;
            else if (z < 3.5f) newStage = 3;
            else newStage = 4;

            if (newStage != activeStageIndex)
            {
                activeStageIndex = newStage;
            }

            // Camera distance positioning
            if (mainCamera != null)
            {
                float normalizedT = (z - 1.0f) / 3.0f; // 0 to 1
                float targetDist = Mathf.Lerp(120f, 2500f, normalizedT);

                Vector3 focusPoint = cameraFocusTarget != null ? cameraFocusTarget.position : Vector3.zero;
                Vector3 dir = (mainCamera.transform.position - focusPoint).normalized;
                if (dir == Vector3.zero) dir = new Vector3(0, 0.45f, 1f).normalized;

                mainCamera.transform.position = focusPoint + dir * targetDist;
                mainCamera.transform.LookAt(focusPoint);
            }
        }
    }
}

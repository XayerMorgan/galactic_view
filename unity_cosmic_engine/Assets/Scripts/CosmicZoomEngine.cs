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
    /// Features continuous inspection zoom from close-up features out to full overview,
    /// target lock-on (Sun, Earth, Jupiter, Saturn, Flagship, Sgr A*), 360-degree orbital view,
    /// and universal escape/quit handling.
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

        [Header("Scale & Stage State")]
        [Range(1.0f, 4.0f)]
        public float currentZoom = 1.0f;
        public int activeStageIndex { get; private set; } = 1;

        [Header("Continuous Inspection Zoom & Orbit")]
        public string currentTargetName = "Sun";
        public Vector3 currentTargetPosition = Vector3.zero;
        public Transform currentTargetTransform = null;
        public float cameraDistance = 35.0f;
        public float targetDistance = 35.0f;
        public float minCameraDistance = 3.5f;
        public float maxCameraDistance = 250.0f;
        public float cameraPitch = 24.0f;
        public float cameraYaw = 0.0f;
        private float targetPitch = 24.0f;
        private float targetYaw = 0.0f;

        [Header("Automated Guided Tour")]
        public bool isTourActive { get; private set; } = false;
        public int tourCurrentStage { get; private set; } = 1;
        private Coroutine tourCoroutine = null;
        private bool isProgrammaticStageJump = false;

        private void Start()
        {
            if (mainCamera == null) mainCamera = Camera.main;
            if (audioController == null) audioController = FindAnyObjectByType<CosmicAudioController>();
            if (lightPulseEmitter == null) lightPulseEmitter = FindAnyObjectByType<LightPulseEmitter>();

            JumpToStage(1);
        }

        private void Update()
        {
            // 1. Universal Escape Key: Exit Starry Night or Quit Game
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                var messier = CelestialMessierCatalog.Instance ?? FindAnyObjectByType<CelestialMessierCatalog>();
                if (messier != null && messier.isStarryNightActive)
                {
                    messier.ExitStarryNight();
                }
                else
                {
                    QuitApplication();
                }
            }

            // Keyboard Shortcuts: Keys 1, 2, 3, 4 for Universal Scales
            if (Input.GetKeyDown(KeyCode.Alpha1)) JumpToStage(1);
            else if (Input.GetKeyDown(KeyCode.Alpha2)) JumpToStage(2);
            else if (Input.GetKeyDown(KeyCode.Alpha3)) JumpToStage(3);
            else if (Input.GetKeyDown(KeyCode.Alpha4)) JumpToStage(4);

            // Guided Tour toggle: Key T
            if (Input.GetKeyDown(KeyCode.T))
            {
                ToggleTour();
            }

            // Light pulse trigger: Spacebar
            if (Input.GetKeyDown(KeyCode.Space))
            {
                FirePulse();
            }

            // Zero Gyro / Reset Camera: Key R
            if (Input.GetKeyDown(KeyCode.R))
            {
                ResetCamera();
            }

            // Starry Night & Messier observation mode toggle: Key S
            if (Input.GetKeyDown(KeyCode.S))
            {
                var hud = FindAnyObjectByType<CosmicHUD>();
                if (hud != null) hud.ToggleStarryNight();
            }

            // Unit system toggle: Key U
            if (Input.GetKeyDown(KeyCode.U))
            {
                var hud = FindAnyObjectByType<CosmicHUD>();
                if (hud != null) hud.CycleUnitSystem();
            }

            // Starry Night decouples flight camera
            var messierCat = CelestialMessierCatalog.Instance;
            if (messierCat != null && messierCat.isStarryNightActive)
            {
                return;
            }

            // 2. Continuous Inspection Mouse Wheel Zooming
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.001f)
            {
                if (isTourActive) StopTour();
                // Proportional scroll step: smooth close-up control without abrupt stage jumps
                float zoomStep = (targetDistance * 0.28f + 1.2f) * (scroll > 0 ? 1f : -1f);
                targetDistance = Mathf.Clamp(targetDistance - zoomStep, minCameraDistance, maxCameraDistance);
            }

            // 3. Smooth Orbit Controls (Right Mouse Button or Left Drag outside UI)
            if (Input.GetMouseButton(1) || (Input.GetMouseButton(0) && !IsPointerOverConsoleUI()))
            {
                float mx = Input.GetAxis("Mouse X");
                float my = Input.GetAxis("Mouse Y");
                if (Mathf.Abs(mx) > 0.01f || Mathf.Abs(my) > 0.01f)
                {
                    targetYaw += mx * 2.8f;
                    targetPitch = Mathf.Clamp(targetPitch - my * 2.4f, -85f, 85f);
                }
            }

            // 4. Smooth Camera Positioning
            if (currentTargetTransform != null)
            {
                currentTargetPosition = currentTargetTransform.position;
            }

            cameraDistance = Mathf.Lerp(cameraDistance, targetDistance, Time.deltaTime * 6.5f);
            cameraPitch = Mathf.Lerp(cameraPitch, targetPitch, Time.deltaTime * 8.0f);
            cameraYaw = Mathf.Lerp(cameraYaw, targetYaw, Time.deltaTime * 8.0f);

            Quaternion rot = Quaternion.Euler(cameraPitch, cameraYaw, 0f);
            Vector3 camPos = currentTargetPosition + rot * new Vector3(0, 0, -cameraDistance);

            if (mainCamera != null)
            {
                mainCamera.transform.position = camPos;
                mainCamera.transform.LookAt(currentTargetPosition);
            }
        }

        private bool IsPointerOverConsoleUI()
        {
            Vector3 mousePos = Input.mousePosition;
            // Protect HUD margins: top bar (y > Screen.height - 70) and bottom console (y < 120) and left/right docks
            if (mousePos.y > Screen.height - 70 || mousePos.y < 120) return true;
            if (mousePos.x < 320 || mousePos.x > Screen.width - 340) return true;
            return false;
        }

        public void QuitApplication()
        {
            Debug.Log("[CosmicZoomEngine] Exiting application...");
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        public void FocusOnTarget(string name, Vector3 pos, float defaultDist, float minDist, float maxDist, Transform t = null)
        {
            currentTargetName = name;
            currentTargetPosition = pos;
            currentTargetTransform = t;
            minCameraDistance = minDist;
            maxCameraDistance = maxDist;
            targetDistance = defaultDist;
            targetPitch = 24.0f;
            targetYaw = 0.0f;

            if (audioController != null) audioController.PlaySoftChime();
        }

        // --- Quick Focus Targets for Features & Ships ---

        public void FocusOnSun()
        {
            Transform t = stage1SolarSystem != null ? stage1SolarSystem.transform.Find("Sun_Photosphere") : null;
            FocusOnTarget("Sun (Solar Photosphere)", Vector3.zero, defaultDist: 20.0f, minDist: 6.0f, maxDist: 150.0f, t);
        }

        public void FocusOnEarth()
        {
            Transform t = FindChildRecursive(stage1SolarSystem, "earth");
            Vector3 p = t != null ? t.position : new Vector3(12.7f, 0.3f, 12.7f);
            FocusOnTarget("Earth (1.0 AU)", p, defaultDist: 5.0f, minDist: 2.0f, maxDist: 50.0f, t);
        }

        public void FocusOnJupiter()
        {
            Transform t = FindChildRecursive(stage1SolarSystem, "jupiter");
            Vector3 p = t != null ? t.position : new Vector3(-31.1f, -0.6f, 21.8f);
            FocusOnTarget("Jupiter (5.2 AU)", p, defaultDist: 12.0f, minDist: 4.0f, maxDist: 90.0f, t);
        }

        public void FocusOnSaturn()
        {
            Transform t = FindChildRecursive(stage1SolarSystem, "saturn");
            Vector3 p = t != null ? t.position : new Vector3(-18.5f, 0.8f, -50.7f);
            FocusOnTarget("Saturn (9.5 AU)", p, defaultDist: 14.0f, minDist: 4.5f, maxDist: 100.0f, t);
        }

        public void FocusOnFlagship()
        {
            Transform t = FindChildRecursive(stage1SolarSystem, "Survey_Flagship");
            Vector3 p = t != null ? t.position : new Vector3(14.0f, 4.5f, 18.0f);
            FocusOnTarget("Deep-Space Flagship", p, defaultDist: 6.5f, minDist: 2.0f, maxDist: 45.0f, t);
        }

        public void FocusOnScout()
        {
            Transform t = FindChildRecursive(stage1SolarSystem, "Sol_Scout_Interceptor");
            Vector3 p = t != null ? t.position : new Vector3(20.0f, 7.5f, 14.0f);
            FocusOnTarget("Sol Scout Ship", p, defaultDist: 5.0f, minDist: 1.8f, maxDist: 35.0f, t);
        }

        public void FocusOnOverview()
        {
            FocusOnTarget("Solar System Overview", Vector3.zero, defaultDist: 110.0f, minDist: 30.0f, maxDist: 250.0f, null);
        }

        public void FocusOnSgrA()
        {
            FocusOnTarget("Sagittarius A* (Supermassive Core)", Vector3.zero, defaultDist: 16.0f, minDist: 5.0f, maxDist: 80.0f, null);
        }

        public void FocusOnOrionSpur()
        {
            FocusOnTarget("Orion Spur (Solar Beacon)", new Vector3(0, 0, -32f), defaultDist: 10.0f, minDist: 3.0f, maxDist: 50.0f, null);
        }

        public void FocusOnMilkyWay()
        {
            FocusOnTarget("Milky Way Disk", Vector3.zero, defaultDist: 95.0f, minDist: 25.0f, maxDist: 220.0f, null);
        }

        public void FocusOnAndromeda()
        {
            FocusOnTarget("Andromeda Galaxy (M31)", new Vector3(15f, 0, 0), defaultDist: 30.0f, minDist: 10.0f, maxDist: 90.0f, null);
        }

        public void FocusOnLocalGroup()
        {
            FocusOnTarget("Local Group Cluster", Vector3.zero, defaultDist: 90.0f, minDist: 30.0f, maxDist: 220.0f, null);
        }

        public void FocusOnCosmicFilaments()
        {
            FocusOnTarget("Cosmic Web Filaments", Vector3.zero, defaultDist: 60.0f, minDist: 20.0f, maxDist: 150.0f, null);
        }

        public void FocusOnCMB()
        {
            FocusOnTarget("CMB Horizon Sphere", Vector3.zero, defaultDist: 200.0f, minDist: 80.0f, maxDist: 350.0f, null);
        }

        private Transform FindChildRecursive(GameObject parent, string partialName)
        {
            if (parent == null) return null;
            string lower = partialName.ToLower();
            foreach (Transform t in parent.GetComponentsInChildren<Transform>(true))
            {
                if (t.name.ToLower().Contains(lower)) return t;
            }
            return null;
        }

        public void JumpToStage(int stageNumber, bool fromTour = false)
        {
            if (!fromTour && isTourActive)
            {
                StopTour();
            }

            stageNumber = Mathf.Clamp(stageNumber, 1, 4);
            activeStageIndex = stageNumber;
            currentZoom = stageNumber;
            isProgrammaticStageJump = true;

            // Activate current stage root, deactivate others
            if (stage1SolarSystem != null) stage1SolarSystem.SetActive(stageNumber == 1);
            if (stage2MilkyWay != null) stage2MilkyWay.SetActive(stageNumber == 2);
            if (stage3LocalGroup != null) stage3LocalGroup.SetActive(stageNumber == 3);
            if (stage4CosmicWeb != null) stage4CosmicWeb.SetActive(stageNumber == 4);

            // Default focus and distance for stage
            switch (stageNumber)
            {
                case 1:
                    FocusOnSun();
                    break;
                case 2:
                    FocusOnMilkyWay();
                    break;
                case 3:
                    FocusOnLocalGroup();
                    break;
                case 4:
                    FocusOnCosmicFilaments();
                    break;
            }

            if (audioController != null)
            {
                audioController.PlaySoftChime();
                audioController.PlayStageNarration(stageNumber);
            }

            StartCoroutine(ClearProgrammaticFlag());
        }

        private IEnumerator ClearProgrammaticFlag()
        {
            yield return new WaitForSeconds(1.8f);
            isProgrammaticStageJump = false;
        }

        public void SetZoomDirect(float zoomVal)
        {
            if (isTourActive) StopTour();
            int newStage = Mathf.RoundToInt(zoomVal);
            if (newStage != activeStageIndex)
            {
                JumpToStage(newStage);
            }
        }

        public void FirePulse()
        {
            if (lightPulseEmitter != null)
            {
                Vector3 origin = currentTargetPosition;
                double spanKm = TravelTimeCalculator.GetSpanKmFromZoom(activeStageIndex);
                float radius = activeStageIndex switch
                {
                    1 => 90f,
                    2 => 140f,
                    3 => 100f,
                    4 => 200f,
                    _ => 150f
                };
                lightPulseEmitter.FireLightPulse(origin, radius, spanKm);
                if (audioController != null) audioController.PlayLightPulseVoice();
            }
        }

        public void ResetCamera()
        {
            targetPitch = 24.0f;
            targetYaw = 0.0f;
            JumpToStage(activeStageIndex);
        }

        public void ToggleTour()
        {
            if (isTourActive) StopTour();
            else StartTour();
        }

        public void StartTour()
        {
            if (isTourActive) return;
            isTourActive = true;
            tourCurrentStage = activeStageIndex;
            if (tourCoroutine != null) StopCoroutine(tourCoroutine);
            tourCoroutine = StartCoroutine(TourSequenceRoutine());
        }

        public void StopTour()
        {
            isTourActive = false;
            if (tourCoroutine != null)
            {
                StopCoroutine(tourCoroutine);
                tourCoroutine = null;
            }
        }

        private IEnumerator TourSequenceRoutine()
        {
            while (isTourActive)
            {
                int stage = tourCurrentStage;
                JumpToStage(stage, true);

                yield return new WaitForSeconds(1.0f);

                if (audioController != null)
                {
                    while (audioController.IsNarrationPlaying)
                    {
                        if (!isTourActive) yield break;
                        yield return null;
                    }
                }
                else
                {
                    yield return new WaitForSeconds(8.0f);
                }

                yield return new WaitForSeconds(2.5f);
                if (!isTourActive) yield break;

                tourCurrentStage = (tourCurrentStage % 4) + 1;
            }
        }

        public void ApplyZoom(float z)
        {
            SetZoomDirect(z);
        }
    }
}

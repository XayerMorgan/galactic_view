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
    /// target lock-on (Sun, Earth, Jupiter, Saturn, Sgr A*), 360-degree orbital view,
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

        private void Start()
        {
            if (mainCamera == null) mainCamera = Camera.main;
            if (audioController == null) audioController = FindAnyObjectByType<CosmicAudioController>();
            if (lightPulseEmitter == null) lightPulseEmitter = FindAnyObjectByType<LightPulseEmitter>();

            JumpToStage(1);
            cameraDistance = targetDistance;
            cameraPitch = targetPitch;
            cameraYaw = targetYaw;
        }

        private void Update()
        {
            // Help remains available while a gallery, search field or settings overlay is open.
            if (Input.GetKeyDown(KeyCode.F1)) { CosmicHUD.Instance?.OpenHelp(); return; }
            // 1. Universal Escape Key: Exit Starry Night or Quit Game
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (CosmicHUD.Instance != null && CosmicHUD.Instance.HandledOverlayEscape) return;
                if (CosmicHUD.Instance != null && CosmicHUD.Instance.CloseOverlay()) return;
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

            if (CosmicHUD.Instance != null && CosmicHUD.Instance.BlocksSceneInput) return;
            if (Input.GetKeyDown(KeyCode.G)) { CosmicHUD.Instance?.OpenGallery(); return; }

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
                var hud = CosmicHUD.Instance ?? FindAnyObjectByType<CosmicHUD>();
                if (hud != null) hud.ToggleStarryNight();
            }

            // Unit system toggle: Key U
            if (Input.GetKeyDown(KeyCode.U))
            {
                var hud = CosmicHUD.Instance ?? FindAnyObjectByType<CosmicHUD>();
                if (hud != null) hud.CycleUnitSystem();
            }

            // HUD View Mode Toggle: Key H
            if (Input.GetKeyDown(KeyCode.H))
            {
                var hud = CosmicHUD.Instance ?? FindAnyObjectByType<CosmicHUD>();
                if (hud != null) hud.CycleHUDMode();
            }

            // Starry Night decouples flight camera
            var messierCat = CelestialMessierCatalog.Instance;
            if (messierCat != null && messierCat.isStarryNightActive)
            {
                return;
            }

            // 2. Continuous Inspection Mouse Wheel Zooming
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.001f && !IsPointerOverConsoleUI())
            {
                if (isTourActive) StopTour();
                // Proportional scroll step: smooth close-up control without abrupt stage jumps
                float zoomStep = (targetDistance * 0.22f + 0.8f) * (scroll > 0 ? 1f : -1f);
                targetDistance = Mathf.Clamp(targetDistance - zoomStep, minCameraDistance, maxCameraDistance);
            }

            // 3. Smooth Orbit Controls (Right Mouse Button ALWAYS, or Left Drag outside active HUD)
            if ((Input.GetMouseButton(1) || Input.GetMouseButton(0)) && !IsPointerOverConsoleUI())
            {
                float mx = Input.GetAxis("Mouse X");
                float my = Input.GetAxis("Mouse Y");
                if (Mathf.Abs(mx) > 0.01f || Mathf.Abs(my) > 0.01f)
                {
                    if (isTourActive) StopTour();
                    targetYaw += mx * 2.8f;
                    targetPitch = Mathf.Clamp(targetPitch - my * 2.4f, -85f, 85f);
                }
            }

            // Keyboard Camera Orbiting / Panning: Arrow keys or WASD
            float kh = (Input.GetKey(KeyCode.RightArrow) ? 1 : 0) - (Input.GetKey(KeyCode.LeftArrow) ? 1 : 0);
            float kv = (Input.GetKey(KeyCode.UpArrow) ? 1 : 0) - (Input.GetKey(KeyCode.DownArrow) ? 1 : 0);
            if (Mathf.Abs(kh) > 0.05f || Mathf.Abs(kv) > 0.05f)
            {
                targetYaw += kh * 60f * Time.deltaTime;
                targetPitch = Mathf.Clamp(targetPitch - kv * 50f * Time.deltaTime, -85f, 85f);
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
            return CosmicHUD.Instance != null && CosmicHUD.Instance.IsPointerOverInterface();
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
            if (t != null)
            {
                Renderer[] renderers = t.GetComponentsInChildren<Renderer>();
                if (renderers.Length > 0)
                {
                    Bounds bounds = renderers[0].bounds;
                    foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                    float radius = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z);
                    defaultDist = Mathf.Max(defaultDist, radius * 5f);
                    maxDist = Mathf.Max(maxDist, defaultDist * 2);
                }
            }
            currentTargetName = name;
            currentTargetPosition = pos;
            currentTargetTransform = t;
            minCameraDistance = minDist;
            maxCameraDistance = maxDist;
            targetDistance = defaultDist;
            targetPitch = 32.0f;
            targetYaw = 0.0f;

            if (audioController != null) audioController.PlaySoftChime();
        }

        // --- Quick Focus Targets for Features & Ships ---

        public void FocusOnSun()
        {
            Transform t = FindChildRecursive(stage1SolarSystem, "Sun_Photosphere");
            FocusOnTarget("Sun (Solar Photosphere)", Vector3.zero, defaultDist: 20.0f, minDist: 6.0f, maxDist: 150.0f, t);
        }

        public void FocusOnEarth()
        {
            Transform t = FindChildRecursive(stage1SolarSystem, "earth");
            Vector3 p = t != null ? t.position : new Vector3(12.7f, 0.3f, 12.7f);
            FocusOnTarget("Earth (1.0 AU)", p, defaultDist: 4.5f, minDist: 1.5f, maxDist: 50.0f, t);
        }

        public void FocusOnJupiter()
        {
            Transform t = FindChildRecursive(stage1SolarSystem, "jupiter");
            Vector3 p = t != null ? t.position : new Vector3(-31.1f, -0.6f, 21.8f);
            FocusOnTarget("Jupiter (5.2 AU)", p, defaultDist: 10.0f, minDist: 3.5f, maxDist: 90.0f, t);
        }

        public void FocusOnSaturn()
        {
            Transform t = FindChildRecursive(stage1SolarSystem, "saturn");
            Vector3 p = t != null ? t.position : new Vector3(-18.5f, 0.8f, -50.7f);
            FocusOnTarget("Saturn (9.5 AU)", p, defaultDist: 12.0f, minDist: 3.5f, maxDist: 100.0f, t);
        }

        public void FocusOnOverview()
        {
            FocusOnTarget("Solar System Overview", Vector3.zero, defaultDist: 220.0f, minDist: 6.0f, maxDist: 420.0f, null);
        }

        public void FocusOnSgrA()
        {
            FocusOnTarget("Sagittarius A* (Supermassive Core)", Vector3.zero, defaultDist: 14.0f, minDist: 4.0f, maxDist: 80.0f, null);
        }

        public void FocusOnOrionSpur()
        {
            Transform t = FindChildRecursive(stage2MilkyWay, "Our_Position_Sun_Beacon");
            FocusOnTarget("Orion Spur (Solar Beacon)", t != null ? t.position : Vector3.zero, 14f, 3f, 220f, t);
        }

        public void FocusOnMilkyWay()
        {
            FocusOnTarget("Milky Way Disk", Vector3.zero, defaultDist: 205.0f, minDist: 6.0f, maxDist: 400.0f, null);
        }

        public void FocusOnAndromeda()
        {
            Transform t = FindChildRecursive(stage3LocalGroup, "Andromeda");
            FocusOnTarget("Andromeda Galaxy (M31)", t != null ? t.position : Vector3.zero, 95f, 7f, 250f, t);
        }

        public void FocusOnLocalGroup()
        {
            FocusOnTarget("Local Group Cluster", Vector3.zero, defaultDist: 220.0f, minDist: 7.0f, maxDist: 400.0f, null);
        }

        public void FocusOnCosmicFilaments()
        {
            FocusOnTarget("Cosmic Web Filaments", Vector3.zero, defaultDist: 240.0f, minDist: 8.0f, maxDist: 500.0f, null);
        }

        public void FocusOnCMB()
        {
            FocusOnTarget("CMB Horizon Sphere", Vector3.zero, defaultDist: 360.0f, minDist: 140.0f, maxDist: 600.0f, null);
        }

        private Transform FindChildRecursive(GameObject parent, string partialName)
        {
            if (parent == null) return null;
            string lower = partialName.ToLower();
            Transform[] children = parent.GetComponentsInChildren<Transform>(true);
            // Orbit_Earth_1AU precedes Earth in the hierarchy. Resolve actual bodies
            // first instead of locking to the first substring match at the origin.
            foreach (Transform t in children)
                if (string.Equals(t.name, partialName, System.StringComparison.OrdinalIgnoreCase)) return t;
            foreach (Transform t in children)
            {
                string name = t.name.ToLower();
                if (name.Contains(lower) && !name.Contains("orbit") && !name.Contains("ring") && !name.Contains("glow")) return t;
            }
            return null;
        }

        public void JumpToStage(int stageNumber, bool fromTour = false)
        {
            if (!fromTour && isTourActive)
            {
                StopTour();
            }

            CelestialMessierCatalog.Instance?.LeaveObservatoryImmediately();
            lightPulseEmitter?.CancelPulse();
            stageNumber = Mathf.Clamp(stageNumber, 1, 4);
            activeStageIndex = stageNumber;
            currentZoom = stageNumber;

            // Activate current stage root, deactivate others
            if (stage1SolarSystem != null) stage1SolarSystem.SetActive(stageNumber == 1);
            if (stage2MilkyWay != null) stage2MilkyWay.SetActive(stageNumber == 2);
            if (stage3LocalGroup != null) stage3LocalGroup.SetActive(stageNumber == 3);
            if (stage4CosmicWeb != null) stage4CosmicWeb.SetActive(stageNumber == 4);

            // Default focus and distance for stage
            switch (stageNumber)
            {
                case 1:
                    FocusOnOverview();
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
            if (CelestialMessierCatalog.Instance != null && CelestialMessierCatalog.Instance.isStarryNightActive) return;
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
            if (CelestialMessierCatalog.Instance != null && CelestialMessierCatalog.Instance.isStarryNightActive)
            {
                CelestialMessierCatalog.Instance.ResetSkyView();
                return;
            }
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

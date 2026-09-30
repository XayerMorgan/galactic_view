using UnityEngine;

namespace CosmicZoom
{
    /// <summary>
    /// Renders an expanding spherical light pulse at universal constant c (299,792.458 km/s).
    /// Provides readable pacing, pause/resume, speed regulation (0.25x slow motion, 0.5x, 1x),
    /// and transit telemetry for low-vision accessibility.
    /// </summary>
    public class LightPulseEmitter : MonoBehaviour
    {
        [SerializeField] private GameObject pulseSphere;
        [SerializeField] private float baseExpansionDuration = 10.0f; // 10 seconds for comfortable readability

        private bool isActive = false;
        private bool isPaused = false;
        private float currentRadius = 0.5f;
        private float targetMaxRadius = 150.0f;
        private float elapsedTime = 0.0f;
        private float pulseSpeedMultiplier = 1.0f;
        private double totalDistanceKm = 0;
        private double currentDistanceKm = 0;
        private string completedSummary = "";

        private void Start()
        {
            if (pulseSphere == null)
            {
                pulseSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                pulseSphere.transform.SetParent(transform);
                var col = pulseSphere.GetComponent<Collider>();
                if (col != null) col.enabled = false;

                var rend = pulseSphere.GetComponent<Renderer>();
                var shader = Resources.Load<Shader>("CosmicRim");
                rend.material = new Material(shader) { color = new Color(0.22f, 0.75f, 1.0f, 0.4f) };
                if (rend.material.HasProperty("_TintColor"))
                {
                    rend.material.SetColor("_TintColor", new Color(0.22f, 0.75f, 1.0f, 0.6f));
                }
            }
            pulseSphere.SetActive(false);
        }

        public void FireLightPulse(Vector3 origin, float maxRadius = 150.0f, double spanKm = 8.996e9)
        {
            // This component shares the engine root: moving it also moved the entire sky vault.
            if (pulseSphere != null) pulseSphere.transform.position = origin;
            targetMaxRadius = Mathf.Max(50.0f, maxRadius);
            totalDistanceKm = spanKm;
            currentRadius = 0.5f;
            currentDistanceKm = 0;
            elapsedTime = 0.0f;
            isActive = true;
            isPaused = false;
            completedSummary = "";

            if (pulseSphere != null)
            {
                pulseSphere.SetActive(true);
                pulseSphere.transform.localScale = Vector3.one * (currentRadius * 2f);
            }
        }

        public void CancelPulse()
        {
            isActive = false;
            isPaused = false;
            elapsedTime = 0;
            currentDistanceKm = 0;
            completedSummary = "";
            if (pulseSphere != null) pulseSphere.SetActive(false);
        }

        public void TogglePause()
        {
            if (!isActive) return;
            isPaused = !isPaused;
        }

        public void SetSpeedMultiplier(float mult)
        {
            pulseSpeedMultiplier = Mathf.Clamp(mult, 0.1f, 2.0f);
        }

        private void Update()
        {
            if (!isActive || isPaused) return;

            float dt = Time.deltaTime * pulseSpeedMultiplier;
            elapsedTime += dt;

            // Normalized progress 0 to 1 over baseExpansionDuration
            float progress = Mathf.Clamp01(elapsedTime / baseExpansionDuration);
            currentRadius = Mathf.Lerp(0.5f, targetMaxRadius, progress);
            currentDistanceKm = totalDistanceKm * progress;

            if (pulseSphere != null)
            {
                pulseSphere.transform.localScale = Vector3.one * (currentRadius * 2.0f);
            }

            if (progress >= 1.0f)
            {
                isActive = false;
                completedSummary = $"TRANSIT COMPLETE: {TravelTimeCalculator.FormatSpan(totalDistanceKm)} in {TravelTimeCalculator.FormatDuration(TravelTimeCalculator.GetLightTransitSeconds(totalDistanceKm))}";
                if (pulseSphere != null)
                {
                    pulseSphere.SetActive(false);
                }
            }
        }

        public bool IsPulseActive => isActive;
        public bool IsPaused => isPaused;
        public float ProgressNormalized => Mathf.Clamp01(elapsedTime / baseExpansionDuration);
        public float ElapsedSimSeconds => elapsedTime;
        public float SpeedMultiplier => pulseSpeedMultiplier;
        public double CurrentDistanceKm => currentDistanceKm;
        public double TotalDistanceKm => totalDistanceKm;
        public string CompletedSummary => completedSummary;
    }
}

using UnityEngine;

namespace CosmicZoom
{
    /// <summary>
    /// Renders the Cosmic Zoom Engine telemetry HUD, speed of light travel calculations,
    /// stage selector buttons, and comparative velocity analytics.
    /// Operates via OnGUI for immediate plug-and-play visual feedback without complex Canvas wiring.
    /// </summary>
    public class CosmicHUD : MonoBehaviour
    {
        [SerializeField] private CosmicZoomEngine engine;
        [SerializeField] private CosmicAudioController audioController;

        private GUIStyle panelStyle;
        private GUIStyle headerStyle;
        private GUIStyle subHeaderStyle;
        private GUIStyle bigValueStyle;
        private GUIStyle labelStyle;
        private GUIStyle buttonStyle;
        private GUIStyle activeButtonStyle;

        private bool stylesInitialized = false;

        private void Start()
        {
            if (engine == null) engine = FindObjectOfType<CosmicZoomEngine>();
            if (audioController == null) audioController = FindObjectOfType<CosmicAudioController>();
        }

        private void InitStyles()
        {
            if (stylesInitialized) return;

            panelStyle = new GUIStyle(GUI.skin.box);
            panelStyle.normal.background = MakeTex(2, 2, new Color(0.03f, 0.05f, 0.1f, 0.88f));

            headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.22f, 0.75f, 1.0f) }
            };

            subHeaderStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Normal,
                normal = { textColor = new Color(0.6f, 0.7f, 0.8f) }
            };

            bigValueStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1.0f, 0.85f, 0.3f) }
            };

            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                normal = { textColor = Color.white }
            };

            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            activeButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.22f, 0.75f, 1.0f) }
            };

            stylesInitialized = true;
        }

        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; ++i) pix[i] = col;
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        private void OnGUI()
        {
            InitStyles();

            float zoom = engine != null ? engine.currentZoom : 1.0f;
            double spanKm = TravelTimeCalculator.GetSpanKmFromZoom(zoom);
            double lightTransitSecs = TravelTimeCalculator.GetLightTransitSeconds(spanKm);

            // 1. Top Header Banner
            GUILayout.BeginArea(new Rect(20, 15, Screen.width - 40, 50), panelStyle);
            GUILayout.BeginHorizontal();
            GUILayout.Label("COSMIC ZOOM ENGINE", headerStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Label($"c = 299,792.458 km/s   |   Active Scale: STAGE {engine?.activeStageIndex}", subHeaderStyle);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button(audioController != null && audioController.IsAudioMuted ? "Audio: MUTED" : "Audio: ON", GUILayout.Width(110)))
            {
                audioController?.ToggleMute();
            }
            GUILayout.EndHorizontal();
            GUILayout.EndArea();

            // 2. Left Stage Selection Dock
            GUILayout.BeginArea(new Rect(20, 80, 240, 320), panelStyle);
            GUILayout.Label("COSMIC SCALES [1 - 4]", headerStyle);
            GUILayout.Space(5);

            DrawStageButton(1, "1. Solar System", "Neptune: ~8.33 LH");
            DrawStageButton(2, "2. Milky Way", "Disk: ~100,000 LY");
            DrawStageButton(3, "3. Local Group", "Cluster: ~10 MLY");
            DrawStageButton(4, "4. Cosmic Web", "Horizon: ~93 GLY");

            GUILayout.Space(10);
            if (GUILayout.Button("⚡ Fire Light Pulse (Space)", GUILayout.Height(35)))
            {
                engine?.FirePulse();
            }
            GUILayout.EndArea();

            // 3. Right Travel Time Calculation HUD
            GUILayout.BeginArea(new Rect(Screen.width - 340, 80, 320, 320), panelStyle);
            GUILayout.Label("⚡ LIGHT TRAVEL DURATION", headerStyle);
            GUILayout.Label($"Field of View: {TravelTimeCalculator.FormatDistanceSpan(spanKm)}", subHeaderStyle);
            GUILayout.Space(8);

            GUILayout.Label(TravelTimeCalculator.FormatDuration(lightTransitSecs), bigValueStyle);
            GUILayout.Label($"Distance: {spanKm:E3} km", subHeaderStyle);

            GUILayout.Space(12);
            GUILayout.Label("Comparative Velocities Across FOV:", subHeaderStyle);
            GUILayout.Space(4);

            double tRel = TravelTimeCalculator.GetTransitSeconds(spanKm, TravelTimeCalculator.SPEED_RELATIVISTIC);
            double tParker = TravelTimeCalculator.GetTransitSeconds(spanKm, TravelTimeCalculator.SPEED_PARKER_SOLAR_PROBE);
            double tVoyager = TravelTimeCalculator.GetTransitSeconds(spanKm, TravelTimeCalculator.SPEED_VOYAGER_1);
            double tJet = TravelTimeCalculator.GetTransitSeconds(spanKm, TravelTimeCalculator.SPEED_JETLINER);

            GUILayout.Label($"• Photon (1.0c): {TravelTimeCalculator.FormatDuration(lightTransitSecs)}", labelStyle);
            GUILayout.Label($"• Starship (0.1c): {TravelTimeCalculator.FormatDuration(tRel)}", labelStyle);
            GUILayout.Label($"• Parker Probe (192 km/s): {TravelTimeCalculator.FormatDuration(tParker)}", labelStyle);
            GUILayout.Label($"• Voyager 1 (17 km/s): {TravelTimeCalculator.FormatDuration(tVoyager)}", labelStyle);
            GUILayout.Label($"• Jetliner (900 km/h): {TravelTimeCalculator.FormatDuration(tJet)}", labelStyle);
            GUILayout.EndArea();

            // 4. Bottom Continuous Zoom Slider
            GUILayout.BeginArea(new Rect(Screen.width * 0.2f, Screen.height - 75, Screen.width * 0.6f, 60), panelStyle);
            GUILayout.BeginHorizontal();
            GUILayout.Label("ZOOM SCALE:", headerStyle, GUILayout.Width(120));
            float newZoom = GUILayout.HorizontalSlider(zoom, 1.0f, 4.0f);
            if (Mathf.Abs(newZoom - zoom) > 0.001f && engine != null)
            {
                engine.SetZoomDirect(newZoom);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Stage 1 (LH)", subHeaderStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Label("Stage 2 (LY)", subHeaderStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Label("Stage 3 (MLY)", subHeaderStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Label("Stage 4 (GLY)", subHeaderStyle);
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void DrawStageButton(int stageNum, string title, string subtitle)
        {
            bool isActive = engine != null && engine.activeStageIndex == stageNum;
            GUIStyle st = isActive ? activeButtonStyle : buttonStyle;

            if (GUILayout.Button($"{title}\n<size=10>{subtitle}</size>", st, GUILayout.Height(48)))
            {
                engine?.JumpToStage(stageNum);
            }
            GUILayout.Space(4);
        }
    }
}

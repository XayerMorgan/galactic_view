using UnityEngine;

namespace CosmicZoom
{
    /// <summary>
    /// Professional Native Unity HUD with high-DPI GUI Matrix scaling,
    /// high-contrast accessibility controls, large scalable typography,
    /// relativistic speed-of-light telemetry, and acoustic soundtrack controls.
    /// </summary>
    public class CosmicHUD : MonoBehaviour
    {
        [SerializeField] private CosmicZoomEngine engine;
        [SerializeField] private CosmicAudioController audioController;

        // Accessibility settings
        public float userScale = 1.25f; // Default to 125% for high readability
        public bool isHighContrast = false;

        private GUIStyle panelStyle;
        private GUIStyle headerTitleStyle;
        private GUIStyle headerSubStyle;
        private GUIStyle statLabelStyle;
        private GUIStyle statValueStyle;
        private GUIStyle statValueGoldStyle;
        private GUIStyle statValueCyanStyle;
        private GUIStyle bigValueStyle;
        private GUIStyle bodyStyle;
        private GUIStyle compLabelStyle;
        private GUIStyle compValueStyle;
        private GUIStyle buttonStyle;
        private GUIStyle activeButtonStyle;
        private GUIStyle a11yBtnStyle;
        private GUIStyle subtitleStyle;

        private Texture2D panelTex;
        private Texture2D highContrastTex;
        private Texture2D btnNormalTex;
        private Texture2D btnActiveTex;
        private Texture2D pillTex;

        private bool stylesInitialized = false;

        private void Start()
        {
            if (engine == null) engine = FindAnyObjectByType<CosmicZoomEngine>();
            if (audioController == null) audioController = FindAnyObjectByType<CosmicAudioController>();

            // Load saved accessibility scale from PlayerPrefs if available
            if (PlayerPrefs.HasKey("Cosmic_A11y_Scale"))
            {
                userScale = PlayerPrefs.GetFloat("Cosmic_A11y_Scale", 1.25f);
            }
            if (PlayerPrefs.HasKey("Cosmic_A11y_Contrast"))
            {
                isHighContrast = PlayerPrefs.GetInt("Cosmic_A11y_Contrast", 0) == 1;
            }
        }

        private Texture2D MakeColorTexture(int w, int h, Color col)
        {
            Color[] pix = new Color[w * h];
            for (int i = 0; i < pix.Length; ++i) pix[i] = col;
            Texture2D result = new Texture2D(w, h);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        private void InitStyles()
        {
            if (stylesInitialized) return;

            panelTex = MakeColorTexture(2, 2, new Color(0.02f, 0.05f, 0.10f, 0.90f));
            highContrastTex = MakeColorTexture(2, 2, new Color(0.0f, 0.0f, 0.0f, 0.98f));
            btnNormalTex = MakeColorTexture(2, 2, new Color(0.06f, 0.12f, 0.22f, 0.85f));
            btnActiveTex = MakeColorTexture(2, 2, new Color(0.18f, 0.65f, 0.92f, 1.0f));
            pillTex = MakeColorTexture(2, 2, new Color(0.03f, 0.08f, 0.16f, 0.80f));

            headerTitleStyle = new GUIStyle
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            headerSubStyle = new GUIStyle
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.35f, 0.82f, 1.0f) }
            };

            statLabelStyle = new GUIStyle
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.80f, 0.86f, 0.94f) }
            };

            statValueStyle = new GUIStyle
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            statValueGoldStyle = new GUIStyle
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1.0f, 0.82f, 0.25f) }
            };

            statValueCyanStyle = new GUIStyle
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.22f, 0.75f, 1.0f) }
            };

            bigValueStyle = new GUIStyle
            {
                fontSize = 30,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            bodyStyle = new GUIStyle
            {
                fontSize = 14,
                wordWrap = true,
                normal = { textColor = new Color(0.85f, 0.90f, 0.96f) }
            };

            compLabelStyle = new GUIStyle
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.75f, 0.82f, 0.90f) }
            };

            compValueStyle = new GUIStyle
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleRight,
                normal = { textColor = Color.white }
            };

            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { background = btnNormalTex, textColor = Color.white },
                hover = { background = btnActiveTex, textColor = Color.black }
            };

            activeButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { background = btnActiveTex, textColor = Color.black }
            };

            a11yBtnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { background = btnNormalTex, textColor = new Color(1.0f, 0.85f, 0.3f) }
            };

            subtitleStyle = new GUIStyle
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                normal = { textColor = new Color(0.35f, 0.85f, 1.0f) }
            };

            stylesInitialized = true;
        }

        private void OnGUI()
        {
            InitStyles();

            // Native High-DPI Virtual Canvas Scaling Matrix (Reference 1920x1080)
            float baseW = 1920f;
            float baseH = 1080f;
            float aspectScale = Mathf.Min(Screen.width / baseW, Screen.height / baseH);
            float finalScale = aspectScale * userScale;

            Matrix4x4 oldMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(finalScale, finalScale, 1.0f));

            float virtualW = Screen.width / finalScale;
            float virtualH = Screen.height / finalScale;

            Texture2D activePanelBg = isHighContrast ? highContrastTex : panelTex;
            panelStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = activePanelBg }
            };

            float zoom = engine != null ? engine.currentZoom : 1.0f;
            double spanKm = TravelTimeCalculator.GetSpanKmFromZoom(zoom);
            double lightTransitSecs = TravelTimeCalculator.GetLightTransitSeconds(spanKm);

            // =========================================================================
            // 1. TOP HEADER & TELEMETRY STRIP
            // =========================================================================
            GUI.Box(new Rect(15, 12, virtualW - 30, 75), "", panelStyle);

            // Logo & Title
            GUI.Label(new Rect(28, 18, 380, 30), "COSMIC ZOOM ENGINE", headerTitleStyle);
            GUI.Label(new Rect(28, 48, 450, 20), "UNIVERSAL SCALE & RELATIVISTIC TRAVEL COMPASS", headerSubStyle);

            // Telemetry Pills
            float pillX = 460;
            // Scale Domain Pill
            GUI.Box(new Rect(pillX, 18, 190, 60), "", panelStyle);
            GUI.Label(new Rect(pillX + 10, 22, 170, 18), "CURRENT DOMAIN", statLabelStyle);
            string domainName = zoom < 1.5f ? "SOLAR SYSTEM" : (zoom < 2.5f ? "MILKY WAY" : (zoom < 3.5f ? "LOCAL GROUP" : "COSMIC WEB"));
            GUI.Label(new Rect(pillX + 10, 42, 170, 26), domainName, statValueCyanStyle);
            pillX += 200;

            // Physical FOV Span Pill
            GUI.Box(new Rect(pillX, 18, 230, 60), "", panelStyle);
            GUI.Label(new Rect(pillX + 10, 22, 210, 18), "PHYSICAL FOV SPAN", statLabelStyle);
            GUI.Label(new Rect(pillX + 10, 42, 210, 26), TravelTimeCalculator.FormatSpan(spanKm), statValueGoldStyle);
            pillX += 240;

            // Acoustic Score Track Pill
            string trackTitle = audioController != null ? audioController.GetCurrentTrackTitle() : "Acoustic Score";
            GUI.Box(new Rect(pillX, 18, 300, 60), "", panelStyle);
            GUI.Label(new Rect(pillX + 10, 22, 280, 18), "ACOUSTIC SCORE (CLICK TO CYCLE)", statLabelStyle);
            if (GUI.Button(new Rect(pillX + 5, 40, 290, 30), "♫ " + trackTitle, a11yBtnStyle))
            {
                if (audioController != null) audioController.NextTrack();
            }
            pillX += 310;

            // Top Action & Accessibility Buttons
            float actionX = virtualW - 470;
            // Font Scale Steppers: [ A- ] [ A+ ]
            if (GUI.Button(new Rect(actionX, 22, 45, 45), "A−", a11yBtnStyle))
            {
                SetScale(userScale - 0.15f);
            }
            if (GUI.Button(new Rect(actionX + 50, 22, 45, 45), "A+", a11yBtnStyle))
            {
                SetScale(userScale + 0.15f);
            }
            if (GUI.Button(new Rect(actionX + 100, 22, 75, 45), Mathf.RoundToInt(userScale * 100) + "%", a11yBtnStyle))
            {
                // Cycle scale presets
                float nextScale = userScale >= 1.75f ? 1.0f : (userScale < 1.25f ? 1.25f : (userScale < 1.5f ? 1.5f : 1.75f));
                SetScale(nextScale);
            }

            // High Contrast Toggle
            string contrastText = isHighContrast ? "Contrast: ON" : "Contrast: OFF";
            if (GUI.Button(new Rect(actionX + 180, 22, 110, 45), contrastText, isHighContrast ? activeButtonStyle : buttonStyle))
            {
                isHighContrast = !isHighContrast;
                PlayerPrefs.SetInt("Cosmic_A11y_Contrast", isHighContrast ? 1 : 0);
            }

            // Audio Toggle
            bool isMuted = audioController != null && audioController.IsAudioMuted;
            if (GUI.Button(new Rect(actionX + 295, 22, 85, 45), isMuted ? "Audio: OFF" : "Audio: ON", isMuted ? buttonStyle : activeButtonStyle))
            {
                if (audioController != null) audioController.ToggleAudioMute();
            }

            // Narrator Toggle
            bool isNarrOn = audioController != null && audioController.IsNarratorAutoPlay;
            if (GUI.Button(new Rect(actionX + 385, 22, 70, 45), isNarrOn ? "Voice: ON" : "Voice: OFF", isNarrOn ? activeButtonStyle : buttonStyle))
            {
                if (audioController != null) audioController.ToggleNarrator();
            }

            // =========================================================================
            // 2. LEFT DOCK: COSMIC SCALE STAGES
            // =========================================================================
            float leftW = 320;
            float leftY = 100;
            GUI.Box(new Rect(15, leftY, leftW, 460), "", panelStyle);

            GUI.Label(new Rect(28, leftY + 12, 280, 24), "COSMIC SCALE STAGES", headerTitleStyle);
            GUI.Label(new Rect(28, leftY + 36, 280, 18), "SELECT DOMAIN [KEYS 1 - 4]", statLabelStyle);

            float stageBtnY = leftY + 65;
            for (int s = 1; s <= 4; s++)
            {
                bool isActive = (engine != null && engine.activeStageIndex == s);
                GUIStyle sStyle = isActive ? activeButtonStyle : buttonStyle;

                string sName = s switch
                {
                    1 => "1. Solar System (~8.33 LH)\nSun to Neptune Orbit",
                    2 => "2. Milky Way Galaxy (~100,000 LY)\nBarred Spiral & Sgr A*",
                    3 => "3. Local Group Cluster (~10 MLY)\nMilky Way & Andromeda",
                    4 => "4. Cosmic Web & Boundary (~93 GLY)\nObservable CMB Horizon",
                    _ => ""
                };

                if (GUI.Button(new Rect(25, stageBtnY, leftW - 20, 85), sName, sStyle))
                {
                    if (engine != null) engine.JumpToStage(s);
                }
                stageBtnY += 95;
            }

            // =========================================================================
            // 3. RIGHT DOCK: RELATIVISTIC SPEED OF LIGHT TELEMETRY
            // =========================================================================
            float rightW = 420;
            float rightX = virtualW - rightW - 15;
            GUI.Box(new Rect(rightX, leftY, rightW, 460), "", panelStyle);

            GUI.Label(new Rect(rightX + 15, leftY + 12, 380, 24), "SPEED OF LIGHT TRANSIT (c)", headerTitleStyle);
            GUI.Label(new Rect(rightX + 15, leftY + 36, 380, 18), "c = 299,792.458 km/s (UNIVERSAL CONSTANT)", statLabelStyle);

            // Big Travel Time
            GUI.Box(new Rect(rightX + 15, leftY + 65, rightW - 30, 85), "", panelStyle);
            GUI.Label(new Rect(rightX + 25, leftY + 72, 360, 40), TravelTimeCalculator.FormatTime(lightTransitSecs), bigValueStyle);
            GUI.Label(new Rect(rightX + 25, leftY + 115, 360, 25), "Diameter: " + spanKm.ToString("E3") + " km (" + TravelTimeCalculator.FormatSpan(spanKm) + ")", statLabelStyle);

            // Comparative Benchmarks
            GUI.Label(new Rect(rightX + 15, leftY + 165, 380, 22), "COMPARATIVE TRAVEL DURATIONS:", headerSubStyle);

            double tRelativistic = spanKm / (0.1 * 299792.458);
            double tParker = spanKm / 192.0;
            double tVoyager = spanKm / 17.0;
            double tJetliner = spanKm / 0.25;

            float compY = leftY + 195;
            DrawCompRow(rightX + 15, compY, rightW - 30, "Photon (Speed of Light, c)", TravelTimeCalculator.FormatTime(lightTransitSecs), new Color(0.22f, 0.75f, 1.0f)); compY += 36;
            DrawCompRow(rightX + 15, compY, rightW - 30, "Relativistic Probe (0.10 c)", TravelTimeCalculator.FormatTime(tRelativistic), Color.white); compY += 36;
            DrawCompRow(rightX + 15, compY, rightW - 30, "Parker Solar Probe (192 km/s)", TravelTimeCalculator.FormatTime(tParker), Color.white); compY += 36;
            DrawCompRow(rightX + 15, compY, rightW - 30, "Voyager 1 (17 km/s)", TravelTimeCalculator.FormatTime(tVoyager), Color.white); compY += 36;
            DrawCompRow(rightX + 15, compY, rightW - 30, "Commercial Jet (900 km/h)", TravelTimeCalculator.FormatTime(tJetliner), Color.white);

            // =========================================================================
            // 4. BOTTOM COCKPIT & NARRATION SUBTITLE
            // =========================================================================
            float bottomH = 110;
            float bottomY = virtualH - bottomH - 15;

            // Narration Subtitle Banner
            string subText = zoom < 1.5f ? "Stage 1: The Solar System. Spanning ~8.33 light-hours across Neptune's orbit." :
                (zoom < 2.5f ? "Stage 2: The Milky Way Galaxy. ~100,000 light-years across with Orion Spur and Sagittarius A*." :
                (zoom < 3.5f ? "Stage 3: The Local Group Cluster. ~10 million light-years encompassing Andromeda and Milky Way." :
                "Stage 4: Cosmic Web & Particle Horizon. ~93 billion light-years to the Cosmic Microwave Background."));

            GUI.Box(new Rect(virtualW * 0.2f, bottomY - 45, virtualW * 0.6f, 36), "", panelStyle);
            GUI.Label(new Rect(virtualW * 0.2f + 10, bottomY - 43, virtualW * 0.6f - 20, 32), "🎙️ GEORGE: " + subText, subtitleStyle);

            // Cockpit Bar
            GUI.Box(new Rect(15, bottomY, virtualW - 30, bottomH), "", panelStyle);

            // Continuous Zoom Slider
            GUI.Label(new Rect(30, bottomY + 15, 180, 22), "SCALE ZOOM SLIDER", statLabelStyle);
            GUI.Label(new Rect(210, bottomY + 15, virtualW * 0.45f, 20), "1.0 [Solar System] ---- 2.0 [Milky Way] ---- 3.0 [Local Group] ---- 4.0 [Cosmic Web]", statLabelStyle);
            float newZoom = GUI.HorizontalSlider(new Rect(30, bottomY + 45, virtualW * 0.55f, 30), zoom, 1.0f, 4.0f);
            if (Mathf.Abs(newZoom - zoom) > 0.005f && engine != null)
            {
                engine.SetZoomDirect(newZoom);
            }

            // Cockpit Action Buttons
            float btnX = virtualW - 480;
            if (GUI.Button(new Rect(btnX, bottomY + 25, 220, 60), "⚡ Fire Light Pulse (c)", activeButtonStyle))
            {
                if (engine != null) engine.FirePulse();
            }
            if (GUI.Button(new Rect(btnX + 230, bottomY + 25, 200, 60), "↺ Reset Camera", buttonStyle))
            {
                if (engine != null) engine.ResetCamera();
            }

            GUI.matrix = oldMatrix;
        }

        private void DrawCompRow(float x, float y, float w, string name, string time, Color col)
        {
            GUI.Box(new Rect(x, y, w, 30), "", panelStyle);
            GUI.Label(new Rect(x + 10, y + 5, w * 0.6f, 20), name, compLabelStyle);
            compValueStyle.normal.textColor = col;
            GUI.Label(new Rect(x + w * 0.6f, y + 5, w * 0.38f, 20), time, compValueStyle);
        }

        private void SetScale(float newScale)
        {
            userScale = Mathf.Clamp(newScale, 0.9f, 2.0f);
            PlayerPrefs.SetFloat("Cosmic_A11y_Scale", userScale);
            if (audioController != null) audioController.PlaySoftChime();
        }
    }
}

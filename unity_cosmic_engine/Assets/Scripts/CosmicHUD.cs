using UnityEngine;

namespace CosmicZoom
{
    /// <summary>
    /// Professional Native Unity HUD with high-DPI GUI Matrix scaling,
    /// dedicated readable Light Transit Pulse monitor (with pause/speed controls & persistent readouts),
    /// uncluttered tabbed telemetry (separating transit pulse from spacecraft benchmarks),
    /// and low-vision accessibility suite.
    /// </summary>
    public class CosmicHUD : MonoBehaviour
    {
        [SerializeField] private CosmicZoomEngine engine;
        [SerializeField] private CosmicAudioController audioController;

        // Accessibility settings
        public float userScale = 1.25f; // Default to 125% for high readability
        public bool isHighContrast = false;

        // Right Dock Tab: 0 = Light Transit Pulse (c), 1 = Spacecraft Benchmarks
        private int activeRightTab = 0;

        private GUIStyle panelStyle;
        private GUIStyle cardStyle;
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
        private GUIStyle tabButtonStyle;
        private GUIStyle activeTabStyle;
        private GUIStyle a11yBtnStyle;
        private GUIStyle subtitleStyle;
        private GUIStyle pulseStatusStyle;

        private Texture2D panelTex;
        private Texture2D cardTex;
        private Texture2D highContrastTex;
        private Texture2D btnNormalTex;
        private Texture2D btnActiveTex;
        private Texture2D progressBgTex;
        private Texture2D progressFillTex;

        private bool stylesInitialized = false;

        private void Start()
        {
            if (engine == null) engine = FindAnyObjectByType<CosmicZoomEngine>();
            if (audioController == null) audioController = FindAnyObjectByType<CosmicAudioController>();

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

            panelTex = MakeColorTexture(2, 2, new Color(0.02f, 0.05f, 0.10f, 0.92f));
            cardTex = MakeColorTexture(2, 2, new Color(0.04f, 0.08f, 0.16f, 0.95f));
            highContrastTex = MakeColorTexture(2, 2, new Color(0.0f, 0.0f, 0.0f, 0.98f));
            btnNormalTex = MakeColorTexture(2, 2, new Color(0.06f, 0.12f, 0.22f, 0.88f));
            btnActiveTex = MakeColorTexture(2, 2, new Color(0.18f, 0.65f, 0.92f, 1.0f));
            progressBgTex = MakeColorTexture(2, 2, new Color(0.08f, 0.12f, 0.20f, 1.0f));
            progressFillTex = MakeColorTexture(2, 2, new Color(0.22f, 0.80f, 1.0f, 1.0f));

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
                fontSize = 28,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.25f, 0.85f, 1.0f) }
            };

            pulseStatusStyle = new GUIStyle
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                wordWrap = true,
                normal = { textColor = new Color(1.0f, 0.90f, 0.35f) }
            };

            bodyStyle = new GUIStyle
            {
                fontSize = 14,
                wordWrap = true,
                normal = { textColor = new Color(0.88f, 0.92f, 0.98f) }
            };

            compLabelStyle = new GUIStyle
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.80f, 0.86f, 0.94f) }
            };

            compValueStyle = new GUIStyle
            {
                fontSize = 14,
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

            tabButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { background = btnNormalTex, textColor = new Color(0.70f, 0.80f, 0.92f) }
            };

            activeTabStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 13,
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
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                normal = { textColor = new Color(1.0f, 0.92f, 0.45f) }
            };

            stylesInitialized = true;
        }

        private void OnGUI()
        {
            InitStyles();

            float dpiScale = Screen.dpi > 0 ? (Screen.dpi / 96.0f) : 1.0f;
            dpiScale = Mathf.Clamp(dpiScale, 1.0f, 2.0f);
            float finalScale = dpiScale * userScale;

            Matrix4x4 oldMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(finalScale, finalScale, 1.0f));

            float virtualW = Screen.width / finalScale;
            float virtualH = Screen.height / finalScale;

            Texture2D activePanelBg = isHighContrast ? highContrastTex : panelTex;
            Texture2D activeCardBg = isHighContrast ? highContrastTex : cardTex;
            panelStyle = new GUIStyle(GUI.skin.box) { normal = { background = activePanelBg } };
            cardStyle = new GUIStyle(GUI.skin.box) { normal = { background = activeCardBg } };

            float zoom = engine != null ? engine.currentZoom : 1.0f;
            double spanKm = TravelTimeCalculator.GetSpanKmFromZoom(zoom);
            double lightTransitSecs = TravelTimeCalculator.GetLightTransitSeconds(spanKm);

            // =========================================================================
            // 1. TOP HEADER & TELEMETRY STRIP (RESPONSIVE NON-COLLIDING)
            // =========================================================================
            GUI.Box(new Rect(15, 12, virtualW - 30, 75), "", panelStyle);

            // Logo & Title
            GUI.Label(new Rect(28, 18, 320, 28), "COSMIC ZOOM ENGINE", headerTitleStyle);
            GUI.Label(new Rect(28, 48, 340, 20), "UNIVERSAL SCALE & RELATIVISTIC COMPASS", headerSubStyle);

            // Top Action & Accessibility Buttons (Anchored to Right)
            float actionX = virtualW - 475;
            if (GUI.Button(new Rect(actionX, 22, 45, 45), "A−", a11yBtnStyle)) SetScale(userScale - 0.15f);
            if (GUI.Button(new Rect(actionX + 50, 22, 45, 45), "A+", a11yBtnStyle)) SetScale(userScale + 0.15f);
            if (GUI.Button(new Rect(actionX + 100, 22, 75, 45), Mathf.RoundToInt(userScale * 100) + "%", a11yBtnStyle))
            {
                float nextScale = userScale >= 1.75f ? 1.0f : (userScale < 1.25f ? 1.25f : (userScale < 1.5f ? 1.5f : 1.75f));
                SetScale(nextScale);
            }
            string contrastText = isHighContrast ? "Contrast: ON" : "Contrast: OFF";
            if (GUI.Button(new Rect(actionX + 180, 22, 110, 45), contrastText, isHighContrast ? activeButtonStyle : buttonStyle))
            {
                isHighContrast = !isHighContrast;
                PlayerPrefs.SetInt("Cosmic_A11y_Contrast", isHighContrast ? 1 : 0);
            }
            bool isMuted = audioController != null && audioController.IsAudioMuted;
            if (GUI.Button(new Rect(actionX + 295, 22, 85, 45), isMuted ? "Audio: OFF" : "Audio: ON", isMuted ? buttonStyle : activeButtonStyle))
            {
                if (audioController != null) audioController.ToggleAudioMute();
            }
            bool isNarrOn = audioController != null && audioController.IsNarratorAutoPlay;
            if (GUI.Button(new Rect(actionX + 385, 22, 75, 45), isNarrOn ? "Voice: ON" : "Voice: OFF", isNarrOn ? activeButtonStyle : buttonStyle))
            {
                if (audioController != null) audioController.ToggleNarrator();
            }

            // Middle Telemetry Pills (Fits between title and action buttons)
            float middleSpace = actionX - 350;
            if (middleSpace > 380)
            {
                float pillX = 350;
                // Scale Domain Pill
                GUI.Box(new Rect(pillX, 18, 175, 60), "", cardStyle);
                GUI.Label(new Rect(pillX + 10, 22, 155, 18), "CURRENT DOMAIN", statLabelStyle);
                string domainName = zoom < 1.75f ? "SOLAR SYSTEM" : (zoom < 2.75f ? "MILKY WAY" : (zoom < 3.75f ? "LOCAL GROUP" : "COSMIC WEB"));
                GUI.Label(new Rect(pillX + 10, 42, 155, 26), domainName, statValueCyanStyle);
                pillX += 185;

                // Physical FOV Span Pill
                GUI.Box(new Rect(pillX, 18, 200, 60), "", cardStyle);
                GUI.Label(new Rect(pillX + 10, 22, 180, 18), "PHYSICAL FOV SPAN", statLabelStyle);
                GUI.Label(new Rect(pillX + 10, 42, 180, 26), TravelTimeCalculator.FormatSpan(spanKm), statValueGoldStyle);
            }

            // =========================================================================
            // 2. LEFT DOCK: COSMIC SCALE STAGES
            // =========================================================================
            float leftW = 310;
            float leftY = 98;
            GUI.Box(new Rect(15, leftY, leftW, 475), "", panelStyle);

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

                if (GUI.Button(new Rect(25, stageBtnY, leftW - 20, 88), sName, sStyle))
                {
                    if (engine != null) engine.JumpToStage(s);
                }
                stageBtnY += 98;
            }

            // =========================================================================
            // 3. RIGHT DOCK: UNCLUTTERED TABBED TELEMETRY & DEDICATED TRANSIT MONITOR
            // =========================================================================
            float rightW = 440;
            float rightX = virtualW - rightW - 15;
            GUI.Box(new Rect(rightX, leftY, rightW, 475), "", panelStyle);

            // Tab Buttons: [ ⚡ Light Transit Pulse ] vs [ 🚀 Spacecraft Benchmarks ]
            float tabW = (rightW - 30) / 2.0f;
            if (GUI.Button(new Rect(rightX + 15, leftY + 12, tabW, 36), "⚡ Light Transit Pulse", activeRightTab == 0 ? activeTabStyle : tabButtonStyle))
            {
                activeRightTab = 0;
            }
            if (GUI.Button(new Rect(rightX + 15 + tabW, leftY + 12, tabW, 36), "🚀 Spacecraft Benchmarks", activeRightTab == 1 ? activeTabStyle : tabButtonStyle))
            {
                activeRightTab = 1;
            }

            var pulseEmitter = engine != null ? engine.lightPulseEmitter : null;
            bool isPulseActive = pulseEmitter != null && pulseEmitter.IsPulseActive;
            bool isPulsePaused = pulseEmitter != null && pulseEmitter.IsPaused;
            float pulseProgress = pulseEmitter != null ? pulseEmitter.ProgressNormalized : 0f;
            string completedText = pulseEmitter != null ? pulseEmitter.CompletedSummary : "";

            if (activeRightTab == 0)
            {
                // -------------------------------------------------------------
                // TAB 0: DEDICATED, UNCLUTTERED TRANSIT PULSE MONITOR
                // -------------------------------------------------------------
                float ry = leftY + 58;

                // Section 1: Universal Light Transit Duration Card
                GUI.Box(new Rect(rightX + 15, ry, rightW - 30, 110), "", cardStyle);
                GUI.Label(new Rect(rightX + 25, ry + 8, rightW - 50, 18), "TIME FOR LIGHT TO TRANSIT ACROSS THIS SCALE (c):", statLabelStyle);
                GUI.Label(new Rect(rightX + 25, ry + 28, rightW - 50, 42), TravelTimeCalculator.FormatTime(lightTransitSecs), bigValueStyle);
                GUI.Label(new Rect(rightX + 25, ry + 75, rightW - 50, 24), "Scale Diameter: " + TravelTimeCalculator.FormatSpan(spanKm) + " (" + spanKm.ToString("E2") + " km)", statLabelStyle);

                ry += 122;

                // Section 2: Active Transit Pulse Interactive Card
                GUI.Box(new Rect(rightX + 15, ry, rightW - 30, 275), "", cardStyle);
                GUI.Label(new Rect(rightX + 25, ry + 10, rightW - 50, 22), "⚡ TRANSIT SIMULATOR (SPEED c = 299,792 km/s)", headerTitleStyle);

                // Status Banner
                string statusMsg;
                if (isPulseActive)
                {
                    double currentDist = pulseEmitter.CurrentDistanceKm;
                    statusMsg = isPulsePaused
                        ? $"⏸ PAUSED: Traversed {TravelTimeCalculator.FormatSpan(currentDist)} ({Mathf.RoundToInt(pulseProgress * 100)}%)"
                        : $"▶ TRAVELING: {TravelTimeCalculator.FormatSpan(currentDist)} ({Mathf.RoundToInt(pulseProgress * 100)}%)";
                }
                else if (!string.IsNullOrEmpty(completedText))
                {
                    statusMsg = "✓ " + completedText;
                }
                else
                {
                    statusMsg = "READY: Press 'Fire Pulse' to visualize transit across this scale.";
                }
                GUI.Label(new Rect(rightX + 25, ry + 36, rightW - 50, 48), statusMsg, pulseStatusStyle);

                // Visual Progress Bar
                float pBarX = rightX + 25;
                float pBarY = ry + 88;
                float pBarW = rightW - 50;
                float pBarH = 22;
                GUI.DrawTexture(new Rect(pBarX, pBarY, pBarW, pBarH), progressBgTex);
                if (pulseProgress > 0)
                {
                    GUI.DrawTexture(new Rect(pBarX, pBarY, pBarW * pulseProgress, pBarH), progressFillTex);
                }
                GUI.Label(new Rect(pBarX, pBarY, pBarW, pBarH), $"{Mathf.RoundToInt(pulseProgress * 100)}% Traversed", compValueStyle);

                // Simulation Speed Regulation Buttons
                float speedY = ry + 120;
                GUI.Label(new Rect(rightX + 25, speedY, 150, 20), "SIMULATION SPEED:", statLabelStyle);
                float curSpeed = pulseEmitter != null ? pulseEmitter.SpeedMultiplier : 1.0f;

                float spdW = (rightW - 50) / 3.0f;
                if (GUI.Button(new Rect(rightX + 25, speedY + 24, spdW - 5, 34), "0.25x Slow-Mo", Mathf.Approximately(curSpeed, 0.25f) ? activeButtonStyle : buttonStyle))
                {
                    if (pulseEmitter != null) pulseEmitter.SetSpeedMultiplier(0.25f);
                }
                if (GUI.Button(new Rect(rightX + 25 + spdW, speedY + 24, spdW - 5, 34), "0.50x Steady", Mathf.Approximately(curSpeed, 0.50f) ? activeButtonStyle : buttonStyle))
                {
                    if (pulseEmitter != null) pulseEmitter.SetSpeedMultiplier(0.50f);
                }
                if (GUI.Button(new Rect(rightX + 25 + spdW * 2, speedY + 24, spdW - 5, 34), "1.0x Normal", Mathf.Approximately(curSpeed, 1.0f) ? activeButtonStyle : buttonStyle))
                {
                    if (pulseEmitter != null) pulseEmitter.SetSpeedMultiplier(1.0f);
                }

                // Playback Action Controls
                float ctrlY = ry + 195;
                float ctrlW = (rightW - 55) / 2.0f;
                string pauseBtnText = isPulsePaused ? "▶ Resume Transit" : "⏸ Pause Transit";
                if (GUI.Button(new Rect(rightX + 25, ctrlY, ctrlW, 55), pauseBtnText, isPulseActive ? activeButtonStyle : buttonStyle))
                {
                    if (pulseEmitter != null) pulseEmitter.TogglePause();
                }
                if (GUI.Button(new Rect(rightX + 30 + ctrlW, ctrlY, ctrlW, 55), isPulseActive ? "⚡ Replay Pulse" : "⚡ Fire Pulse", activeButtonStyle))
                {
                    if (engine != null) engine.FirePulse();
                }
            }
            else
            {
                // -------------------------------------------------------------
                // TAB 1: SPACECRAFT & RELATIVISTIC COMPARISONS (UNCLUTTERED)
                // -------------------------------------------------------------
                float ry = leftY + 58;
                GUI.Label(new Rect(rightX + 20, ry, rightW - 40, 22), "TRAVEL TIME COMPARISONS AT THIS SCALE:", headerTitleStyle);
                GUI.Label(new Rect(rightX + 20, ry + 24, rightW - 40, 18), "Distance: " + TravelTimeCalculator.FormatSpan(spanKm), statLabelStyle);

                double tRelativistic = spanKm / (0.1 * 299792.458);
                double tParker = spanKm / 192.0;
                double tVoyager = spanKm / 17.0;
                double tJetliner = spanKm / 0.25;

                float compY = ry + 50;
                float rowH = 46;
                DrawCompRow(rightX + 15, compY, rightW - 30, rowH, "Photon (Speed of Light, 1.0c)", TravelTimeCalculator.FormatTime(lightTransitSecs), new Color(0.22f, 0.80f, 1.0f)); compY += rowH + 8;
                DrawCompRow(rightX + 15, compY, rightW - 30, rowH, "Relativistic Craft (0.10c)", TravelTimeCalculator.FormatTime(tRelativistic), new Color(0.95f, 0.90f, 0.40f)); compY += rowH + 8;
                DrawCompRow(rightX + 15, compY, rightW - 30, rowH, "Parker Solar Probe (192 km/s)", TravelTimeCalculator.FormatTime(tParker), Color.white); compY += rowH + 8;
                DrawCompRow(rightX + 15, compY, rightW - 30, rowH, "Voyager 1 (17 km/s)", TravelTimeCalculator.FormatTime(tVoyager), Color.white); compY += rowH + 8;
                DrawCompRow(rightX + 15, compY, rightW - 30, rowH, "Commercial Airliner (900 km/h)", TravelTimeCalculator.FormatTime(tJetliner), new Color(0.85f, 0.85f, 0.85f));
            }

            // =========================================================================
            // 4. BOTTOM COCKPIT & NARRATION SUBTITLE
            // =========================================================================
            float bottomH = 110;
            float bottomY = virtualH - bottomH - 15;

            // Narration Subtitle Banner
            string subText = zoom < 1.75f ? "Stage 1: The Solar System. Spanning ~8.33 light-hours across Neptune's orbit." :
                (zoom < 2.75f ? "Stage 2: The Milky Way Galaxy. ~100,000 light-years across with Orion Spur and Sagittarius A*." :
                (zoom < 3.75f ? "Stage 3: The Local Group Cluster. ~10 million light-years encompassing Andromeda and Milky Way." :
                "Stage 4: Cosmic Web & Particle Horizon. ~93 billion light-years to the Cosmic Microwave Background."));

            GUI.Box(new Rect(virtualW * 0.15f, bottomY - 45, virtualW * 0.7f, 36), "", panelStyle);
            GUI.Label(new Rect(virtualW * 0.15f + 10, bottomY - 43, virtualW * 0.7f - 20, 32), "🎙️ GEORGE: " + subText, subtitleStyle);

            // Cockpit Bar
            GUI.Box(new Rect(15, bottomY, virtualW - 30, bottomH), "", panelStyle);

            // Continuous Zoom Slider
            GUI.Label(new Rect(30, bottomY + 14, 180, 22), "SCALE ZOOM SLIDER", statLabelStyle);
            GUI.Label(new Rect(210, bottomY + 14, virtualW * 0.45f, 20), "1.0 [Solar System] ---- 2.0 [Milky Way] ---- 3.0 [Local Group] ---- 4.0 [Cosmic Web]", statLabelStyle);
            float newZoom = GUI.HorizontalSlider(new Rect(30, bottomY + 44, virtualW * 0.52f, 30), zoom, 1.0f, 4.0f);
            if (Mathf.Abs(newZoom - zoom) > 0.005f && engine != null)
            {
                engine.SetZoomDirect(newZoom);
            }

            // Cockpit Action Buttons
            float btnX = virtualW - 460;
            string pulseBtnText = isPulseActive ? (isPulsePaused ? "▶ Resume Pulse" : "⏸ Pause Pulse") : "⚡ Fire Light Pulse (c)";
            if (GUI.Button(new Rect(btnX, bottomY + 22, 230, 65), pulseBtnText, activeButtonStyle))
            {
                if (engine != null)
                {
                    if (isPulseActive)
                    {
                        if (pulseEmitter != null) pulseEmitter.TogglePause();
                    }
                    else
                    {
                        engine.FirePulse();
                    }
                }
            }
            if (GUI.Button(new Rect(btnX + 240, bottomY + 22, 180, 65), "↺ Reset Camera", buttonStyle))
            {
                if (engine != null) engine.ResetCamera();
            }

            GUI.matrix = oldMatrix;
        }

        private void DrawCompRow(float x, float y, float w, float h, string name, string time, Color col)
        {
            GUI.Box(new Rect(x, y, w, h), "", cardStyle);
            GUI.Label(new Rect(x + 12, y + (h - 22) / 2.0f, w * 0.55f, 24), name, compLabelStyle);
            compValueStyle.normal.textColor = col;
            GUI.Label(new Rect(x + w * 0.55f, y + (h - 22) / 2.0f, w * 0.42f, 24), time, compValueStyle);
        }

        private void SetScale(float newScale)
        {
            userScale = Mathf.Clamp(newScale, 0.9f, 2.0f);
            PlayerPrefs.SetFloat("Cosmic_A11y_Scale", userScale);
            if (audioController != null) audioController.PlaySoftChime();
        }
    }
}

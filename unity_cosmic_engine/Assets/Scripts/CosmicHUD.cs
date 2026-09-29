using UnityEngine;

namespace CosmicZoom
{
    /// <summary>
    /// Professional Starship Flight Deck Avionics HUD (Diegetic Spaceship Canopy Interface).
    /// Features:
    /// - Comprehensive Unit System: Metric (KM), Imperial (Miles), and Dual / Both Systems (Hotkey: U).
    /// - Canopy Glass Flight Reticle: Boresight crosshairs, targeting brackets, pitch ladder, and Lorentz vector telemetry.
    /// - Tactical Spaceship Avionics Styling: Chamfered tech bezels, corner brackets, glowing neon status indicators.
    /// - Dedicated Light Transit Pulse Engine & Speed-of-Light Simulator.
    /// - Dynamic Craft Propulsion Benchmark comparisons formatted in KM/s, MPH, or Dual speeds.
    /// - High-DPI Matrix scaling with low-vision accessibility suite.
    /// </summary>
    public class CosmicHUD : MonoBehaviour
    {
        [SerializeField] private CosmicZoomEngine engine;
        [SerializeField] private CosmicAudioController audioController;

        // Measurement Unit System: 0 = KM, 1 = Miles, 2 = Dual (Both)
        public UnitSystem activeUnitSystem = UnitSystem.Kilometers;

        // Accessibility settings
        public float userScale = 1.25f; // Default to 125% for high readability
        public bool isHighContrast = false;

        // Right Dock Tab: 0 = Light Transit Pulse (c), 1 = Spacecraft Benchmarks
        private int activeRightTab = 0;

        // GUI Styles
        private GUIStyle panelStyle;
        private GUIStyle cardStyle;
        private GUIStyle headerTitleStyle;
        private GUIStyle headerSubStyle;
        private GUIStyle shipIdStyle;
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
        private GUIStyle reticleHeadingStyle;
        private GUIStyle reticleSubStyle;
        private GUIStyle hudMicroStyle;

        // Textures
        private Texture2D panelTex;
        private Texture2D cardTex;
        private Texture2D highContrastTex;
        private Texture2D btnNormalTex;
        private Texture2D btnActiveTex;
        private Texture2D progressBgTex;
        private Texture2D progressFillTex;
        private Texture2D cyanAccentTex;
        private Texture2D cyanDimTex;
        private Texture2D amberAccentTex;
        private Texture2D strutTex;

        private bool stylesInitialized = false;

        private void Start()
        {
            if (engine == null) engine = FindAnyObjectByType<CosmicZoomEngine>();
            if (audioController == null) audioController = FindAnyObjectByType<CosmicAudioController>();

            if (PlayerPrefs.HasKey("Cosmic_Unit_System"))
            {
                activeUnitSystem = (UnitSystem)PlayerPrefs.GetInt("Cosmic_Unit_System", 0);
            }
            if (PlayerPrefs.HasKey("Cosmic_A11y_Scale"))
            {
                userScale = PlayerPrefs.GetFloat("Cosmic_A11y_Scale", 1.25f);
            }
            if (PlayerPrefs.HasKey("Cosmic_A11y_Contrast"))
            {
                isHighContrast = PlayerPrefs.GetInt("Cosmic_A11y_Contrast", 0) == 1;
            }
        }

        public void CycleUnitSystem()
        {
            activeUnitSystem = (UnitSystem)(((int)activeUnitSystem + 1) % 3);
            PlayerPrefs.SetInt("Cosmic_Unit_System", (int)activeUnitSystem);
            if (audioController != null) audioController.PlaySoftChime();
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

            // Spacecraft Titanium / Carbon Hull Bezel Textures
            panelTex = MakeColorTexture(2, 2, new Color(0.015f, 0.035f, 0.07f, 0.94f));
            cardTex = MakeColorTexture(2, 2, new Color(0.025f, 0.055f, 0.11f, 0.96f));
            highContrastTex = MakeColorTexture(2, 2, new Color(0.0f, 0.0f, 0.0f, 0.98f));
            btnNormalTex = MakeColorTexture(2, 2, new Color(0.04f, 0.09f, 0.17f, 0.90f));
            btnActiveTex = MakeColorTexture(2, 2, new Color(0.0f, 0.82f, 1.0f, 0.95f));
            progressBgTex = MakeColorTexture(2, 2, new Color(0.05f, 0.09f, 0.15f, 1.0f));
            progressFillTex = MakeColorTexture(2, 2, new Color(0.0f, 0.90f, 1.0f, 1.0f));

            // HUD Neon Accents & Canopy Framework
            cyanAccentTex = MakeColorTexture(2, 2, new Color(0.0f, 0.90f, 1.0f, 0.95f));
            cyanDimTex = MakeColorTexture(2, 2, new Color(0.0f, 0.90f, 1.0f, 0.30f));
            amberAccentTex = MakeColorTexture(2, 2, new Color(1.0f, 0.75f, 0.20f, 0.95f));
            strutTex = MakeColorTexture(2, 2, new Color(0.03f, 0.06f, 0.12f, 0.88f));

            shipIdStyle = new GUIStyle
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.0f, 0.92f, 1.0f) }
            };

            headerTitleStyle = new GUIStyle
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            headerSubStyle = new GUIStyle
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1.0f, 0.78f, 0.28f) }
            };

            statLabelStyle = new GUIStyle
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.70f, 0.82f, 0.95f) }
            };

            statValueStyle = new GUIStyle
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            statValueGoldStyle = new GUIStyle
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1.0f, 0.82f, 0.25f) }
            };

            statValueCyanStyle = new GUIStyle
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.0f, 0.90f, 1.0f) }
            };

            bigValueStyle = new GUIStyle
            {
                fontSize = 26,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.0f, 0.92f, 1.0f) }
            };

            pulseStatusStyle = new GUIStyle
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                wordWrap = true,
                normal = { textColor = new Color(1.0f, 0.88f, 0.30f) }
            };

            bodyStyle = new GUIStyle
            {
                fontSize = 13,
                wordWrap = true,
                normal = { textColor = new Color(0.88f, 0.92f, 0.98f) }
            };

            compLabelStyle = new GUIStyle
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.75f, 0.86f, 0.96f) }
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
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { background = btnNormalTex, textColor = new Color(0.9f, 0.95f, 1.0f) },
                hover = { background = btnActiveTex, textColor = Color.black }
            };

            activeButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { background = btnActiveTex, textColor = Color.black }
            };

            tabButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { background = btnNormalTex, textColor = new Color(0.70f, 0.80f, 0.92f) }
            };

            activeTabStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { background = btnActiveTex, textColor = Color.black }
            };

            a11yBtnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { background = btnNormalTex, textColor = new Color(1.0f, 0.85f, 0.3f) }
            };

            subtitleStyle = new GUIStyle
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                normal = { textColor = new Color(1.0f, 0.92f, 0.45f) }
            };

            reticleHeadingStyle = new GUIStyle
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.0f, 0.85f, 1.0f, 0.85f) }
            };

            reticleSubStyle = new GUIStyle
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1.0f, 0.80f, 0.25f, 0.80f) }
            };

            hudMicroStyle = new GUIStyle
            {
                fontSize = 9,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.LowerRight,
                normal = { textColor = new Color(0.0f, 0.80f, 1.0f, 0.55f) }
            };

            stylesInitialized = true;
        }

        private void DrawSciFiBezel(Rect r, string tag, Color accentCol)
        {
            // Panel Body
            Texture2D activeBg = isHighContrast ? highContrastTex : panelTex;
            GUI.DrawTexture(r, activeBg);

            // Top Glowing Accent Strip
            Texture2D topTex = accentCol == Color.cyan ? cyanAccentTex : (accentCol == Color.yellow ? amberAccentTex : cyanAccentTex);
            GUI.DrawTexture(new Rect(r.x, r.y, r.width, 3), topTex);

            // Corner Brackets ┌ ┐ └ ┘
            float bLen = 14f;
            float bThick = 2f;
            // Top-left
            GUI.DrawTexture(new Rect(r.x, r.y, bLen, bThick), topTex);
            GUI.DrawTexture(new Rect(r.x, r.y, bThick, bLen), topTex);
            // Top-right
            GUI.DrawTexture(new Rect(r.x + r.width - bLen, r.y, bLen, bThick), topTex);
            GUI.DrawTexture(new Rect(r.x + r.width - bThick, r.y, bThick, bLen), topTex);
            // Bottom-left
            GUI.DrawTexture(new Rect(r.x, r.y + r.height - bThick, bLen, bThick), topTex);
            GUI.DrawTexture(new Rect(r.x, r.y + r.height - bLen, bThick, bLen), topTex);
            // Bottom-right
            GUI.DrawTexture(new Rect(r.x + r.width - bLen, r.y + r.height - bThick, bLen, bThick), topTex);
            GUI.DrawTexture(new Rect(r.x + r.width - bThick, r.y + r.height - bLen, bThick, bLen), topTex);

            // Tactical Corner Tag
            if (!string.IsNullOrEmpty(tag))
            {
                GUI.Label(new Rect(r.x + r.width - 130, r.y + r.height - 18, 120, 16), tag, hudMicroStyle);
            }
        }

        private void DrawCenterFlightHUD(float virtualW, float virtualH)
        {
            float cx = virtualW * 0.5f;
            float cy = virtualH * 0.46f;

            // Center Boresight Crosshair
            float crossSize = 16f;
            GUI.DrawTexture(new Rect(cx - crossSize * 0.5f, cy - 1f, crossSize, 2f), cyanAccentTex);
            GUI.DrawTexture(new Rect(cx - 1f, cy - crossSize * 0.5f, 2f, crossSize), cyanAccentTex);

            // Tactical Targeting Brackets [   ]
            float boxR = 40f;
            float tick = 10f;
            // Top-left
            GUI.DrawTexture(new Rect(cx - boxR, cy - boxR, tick, 2f), cyanAccentTex);
            GUI.DrawTexture(new Rect(cx - boxR, cy - boxR, 2f, tick), cyanAccentTex);
            // Top-right
            GUI.DrawTexture(new Rect(cx + boxR - tick, cy - boxR, tick, 2f), cyanAccentTex);
            GUI.DrawTexture(new Rect(cx + boxR - 2f, cy - boxR, 2f, tick), cyanAccentTex);
            // Bottom-left
            GUI.DrawTexture(new Rect(cx - boxR, cy + boxR - 2f, tick, 2f), cyanAccentTex);
            GUI.DrawTexture(new Rect(cx - boxR, cy + boxR - tick, 2f, tick), cyanAccentTex);
            // Bottom-right
            GUI.DrawTexture(new Rect(cx + boxR - tick, cy + boxR - 2f, tick, 2f), cyanAccentTex);
            GUI.DrawTexture(new Rect(cx + boxR - 2f, cy + boxR - tick, 2f, tick), cyanAccentTex);

            // Horizon Pitch Ladder
            float pitchW = 60f;
            GUI.DrawTexture(new Rect(cx - pitchW * 0.5f, cy - 55f, pitchW, 1.5f), cyanDimTex);
            GUI.DrawTexture(new Rect(cx - pitchW * 0.5f, cy + 55f, pitchW, 1.5f), cyanDimTex);

            // Starship Flight Vector & Relativistic Lorentz Telemetry
            GUI.Label(new Rect(cx - 240, cy - 80, 480, 20), "◈ SENSOR TRACK: NOMINAL // VECTOR: [0.00, +0.45, +1.00] ◈", reticleHeadingStyle);
            GUI.Label(new Rect(cx - 240, cy + 62, 480, 20), "RELATIVISTIC LORENTZ FACTOR: γ = 1.0000 // WARP FIELD: STABLE", reticleSubStyle);

            // Cockpit Canopy Corner Struts (Simulates looking through starship canopy glass)
            GUI.DrawTexture(new Rect(0, 0, 180, 10), strutTex);
            GUI.DrawTexture(new Rect(0, 0, 10, 120), strutTex);

            GUI.DrawTexture(new Rect(virtualW - 180, 0, 180, 10), strutTex);
            GUI.DrawTexture(new Rect(virtualW - 10, 0, 10, 120), strutTex);

            GUI.DrawTexture(new Rect(0, virtualH - 10, 180, 10), strutTex);
            GUI.DrawTexture(new Rect(0, virtualH - 120, 10, 120), strutTex);

            GUI.DrawTexture(new Rect(virtualW - 180, virtualH - 10, 180, 10), strutTex);
            GUI.DrawTexture(new Rect(virtualW - 10, virtualH - 120, 10, 120), strutTex);
        }

        private void OnGUI()
        {
            InitStyles();

            // Keyboard shortcut: Key U toggles units
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.U)
            {
                CycleUnitSystem();
                Event.current.Use();
            }

            float dpiScale = Screen.dpi > 0 ? (Screen.dpi / 96.0f) : 1.0f;
            dpiScale = Mathf.Clamp(dpiScale, 1.0f, 2.0f);
            float finalScale = dpiScale * userScale;

            Matrix4x4 oldMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(finalScale, finalScale, 1.0f));

            float virtualW = Screen.width / finalScale;
            float virtualH = Screen.height / finalScale;

            Texture2D activeCardBg = isHighContrast ? highContrastTex : cardTex;
            cardStyle = new GUIStyle(GUI.skin.box) { normal = { background = activeCardBg } };

            float zoom = engine != null ? engine.currentZoom : 1.0f;
            double spanKm = TravelTimeCalculator.GetSpanKmFromZoom(zoom);
            double lightTransitSecs = TravelTimeCalculator.GetLightTransitSeconds(spanKm);

            // =========================================================================
            // 0. CENTER FLIGHT DECK CANOPY HUD (RETICLE & VIEWPORT STRUTS)
            // =========================================================================
            DrawCenterFlightHUD(virtualW, virtualH);

            // =========================================================================
            // 1. TOP AVIONICS HELM CONSOLE (RESPONSIVE SCI-FI STRIP)
            // =========================================================================
            Rect headerRect = new Rect(15, 12, virtualW - 30, 80);
            DrawSciFiBezel(headerRect, "[SYS.HELM // MK-VII]", Color.cyan);

            // Starship Helm Designation
            GUI.Label(new Rect(28, 18, 360, 26), "USS ASTRONAUTICA // HELM 01", shipIdStyle);
            GUI.Label(new Rect(28, 46, 360, 20), "RELATIVISTIC COMPASS & QUANTUM HORIZON SUITE", headerSubStyle);

            // Top Action & Avionics Switches (Anchored to Right)
            float actionX = virtualW - 575;

            // Unit Switcher: KM / MILES / DUAL (Both)
            string unitBtnText = activeUnitSystem switch
            {
                UnitSystem.Miles => "UNITS: [MILES] (U)",
                UnitSystem.Dual => "UNITS: [DUAL KM+MI] (U)",
                _ => "UNITS: [KM] (U)"
            };
            if (GUI.Button(new Rect(actionX, 24, 155, 44), unitBtnText, activeUnitSystem == UnitSystem.Dual ? activeButtonStyle : buttonStyle))
            {
                CycleUnitSystem();
            }

            if (GUI.Button(new Rect(actionX + 162, 24, 38, 44), "A−", a11yBtnStyle)) SetScale(userScale - 0.15f);
            if (GUI.Button(new Rect(actionX + 204, 24, 38, 44), "A+", a11yBtnStyle)) SetScale(userScale + 0.15f);
            if (GUI.Button(new Rect(actionX + 246, 24, 58, 44), Mathf.RoundToInt(userScale * 100) + "%", a11yBtnStyle))
            {
                float nextScale = userScale >= 1.75f ? 1.0f : (userScale < 1.25f ? 1.25f : (userScale < 1.5f ? 1.5f : 1.75f));
                SetScale(nextScale);
            }

            string contrastText = isHighContrast ? "HUD: NIGHT" : "HUD: COLOR";
            if (GUI.Button(new Rect(actionX + 310, 24, 95, 44), contrastText, isHighContrast ? activeButtonStyle : buttonStyle))
            {
                isHighContrast = !isHighContrast;
                PlayerPrefs.SetInt("Cosmic_A11y_Contrast", isHighContrast ? 1 : 0);
            }

            bool isMuted = audioController != null && audioController.IsAudioMuted;
            if (GUI.Button(new Rect(actionX + 412, 24, 78, 44), isMuted ? "SND: OFF" : "SND: ON", isMuted ? buttonStyle : activeButtonStyle))
            {
                if (audioController != null) audioController.ToggleAudioMute();
            }

            bool isNarrOn = audioController != null && audioController.IsNarratorAutoPlay;
            if (GUI.Button(new Rect(actionX + 496, 24, 68, 44), isNarrOn ? "VOX: ON" : "VOX: OFF", isNarrOn ? activeButtonStyle : buttonStyle))
            {
                if (audioController != null) audioController.ToggleNarrator();
            }

            // Middle Telemetry Data Computers (Fits between title and switches)
            float middleSpace = actionX - 380;
            if (middleSpace > 320)
            {
                float pillX = 375;
                // Scale Domain Data Cell
                GUI.Box(new Rect(pillX, 18, 160, 60), "", cardStyle);
                GUI.Label(new Rect(pillX + 8, 22, 144, 18), "ACTIVE DOMAIN", statLabelStyle);
                string domainName = zoom < 1.75f ? "SOLAR SYSTEM" : (zoom < 2.75f ? "MILKY WAY" : (zoom < 3.75f ? "LOCAL GROUP" : "COSMIC WEB"));
                GUI.Label(new Rect(pillX + 8, 42, 144, 26), domainName, statValueCyanStyle);
                pillX += 168;

                // Physical FOV Span Data Cell (Responsive to Unit System)
                float spanCellW = Mathf.Min(middleSpace - 175, 230);
                GUI.Box(new Rect(pillX, 18, spanCellW, 60), "", cardStyle);
                GUI.Label(new Rect(pillX + 8, 22, spanCellW - 16, 18), "PHYSICAL FOV SPAN", statLabelStyle);
                GUI.Label(new Rect(pillX + 8, 42, spanCellW - 16, 26), TravelTimeCalculator.FormatSpan(spanKm, activeUnitSystem), statValueGoldStyle);
            }

            // =========================================================================
            // 2. LEFT WING CONSOLE: SECTOR SCALE SELECTOR
            // =========================================================================
            float leftW = 320;
            float leftY = 100;
            Rect leftRect = new Rect(15, leftY, leftW, 475);
            DrawSciFiBezel(leftRect, "[NAV.SECTOR // 1-4]", Color.cyan);

            GUI.Label(new Rect(28, leftY + 12, 280, 22), "// SECTOR SCALE SELECTOR //", headerTitleStyle);
            GUI.Label(new Rect(28, leftY + 36, 280, 18), "DESTINATION LOCK [KEYS 1 - 4]", statLabelStyle);

            float stageBtnY = leftY + 62;
            for (int s = 1; s <= 4; s++)
            {
                bool isActive = (engine != null && engine.activeStageIndex == s);
                GUIStyle sStyle = isActive ? activeButtonStyle : buttonStyle;

                string sName = s switch
                {
                    1 => activeUnitSystem switch
                    {
                        UnitSystem.Miles => "1. Solar System (~8.33 LH)\nSun to Neptune (5.59B mi)",
                        UnitSystem.Dual => "1. Solar System (~8.33 LH)\n9.0B km [5.59B mi] Span",
                        _ => "1. Solar System (~8.33 LH)\nSun to Neptune (8.996B km)"
                    },
                    2 => activeUnitSystem switch
                    {
                        UnitSystem.Miles => "2. Milky Way Galaxy (~100k LY)\nSpiral Arms (5.88e17 mi)",
                        UnitSystem.Dual => "2. Milky Way Galaxy (~100k LY)\n9.46e17 km [5.88e17 mi]",
                        _ => "2. Milky Way Galaxy (~100k LY)\nSpiral Arms (9.461e17 km)"
                    },
                    3 => activeUnitSystem switch
                    {
                        UnitSystem.Miles => "3. Local Group (~10 MLY)\nAndromeda (5.88e19 mi)",
                        UnitSystem.Dual => "3. Local Group (~10 MLY)\n9.46e19 km [5.88e19 mi]",
                        _ => "3. Local Group (~10 MLY)\nAndromeda (9.461e19 km)"
                    },
                    4 => activeUnitSystem switch
                    {
                        UnitSystem.Miles => "4. Cosmic Web & CMB (~93 GLY)\nCMB Horizon (5.47e23 mi)",
                        UnitSystem.Dual => "4. Cosmic Web & CMB (~93 GLY)\n8.80e23 km [5.47e23 mi]",
                        _ => "4. Cosmic Web & CMB (~93 GLY)\nCMB Horizon (8.798e23 km)"
                    },
                    _ => ""
                };

                if (GUI.Button(new Rect(25, stageBtnY, leftW - 20, 88), sName, sStyle))
                {
                    if (engine != null) engine.JumpToStage(s);
                }
                stageBtnY += 98;
            }

            // =========================================================================
            // 3. RIGHT WING CONSOLE: RELATIVISTIC PHOTON & CRAFT BENCHMARK TELEMETRY
            // =========================================================================
            float rightW = 450;
            float rightX = virtualW - rightW - 15;
            Rect rightRect = new Rect(rightX, leftY, rightW, 475);
            DrawSciFiBezel(rightRect, "[QUANTUM.FDC // 01]", Color.cyan);

            // Tab Buttons: [ ⚡ Photon Transit (c) ] vs [ 🚀 Craft Benchmarks ]
            float tabW = (rightW - 30) / 2.0f;
            if (GUI.Button(new Rect(rightX + 15, leftY + 12, tabW, 36), "⚡ Photon Transit (c)", activeRightTab == 0 ? activeTabStyle : tabButtonStyle))
            {
                activeRightTab = 0;
            }
            if (GUI.Button(new Rect(rightX + 15 + tabW, leftY + 12, tabW, 36), "🚀 Craft Benchmarks", activeRightTab == 1 ? activeTabStyle : tabButtonStyle))
            {
                activeRightTab = 1;
            }

            var pulseEmitter = engine != null ? engine.lightPulseEmitter : null;
            bool isPulseActive = pulseEmitter != null && pulseEmitter.IsPulseActive;
            bool isPulsePaused = pulseEmitter != null && pulseEmitter.IsPaused;
            float pulseProgress = pulseEmitter != null ? pulseEmitter.ProgressNormalized : 0f;

            if (activeRightTab == 0)
            {
                // -------------------------------------------------------------
                // TAB 0: PHOTON TRANSIT SIMULATOR (SPEED OF LIGHT c)
                // -------------------------------------------------------------
                float ry = leftY + 58;

                // Section 1: Universal Light Transit Duration Card
                GUI.Box(new Rect(rightX + 15, ry, rightW - 30, 110), "", cardStyle);
                GUI.Label(new Rect(rightX + 25, ry + 8, rightW - 50, 18), "TIME FOR PHOTON (1.0c) TO TRANSIT ACROSS THIS SCALE:", statLabelStyle);
                GUI.Label(new Rect(rightX + 25, ry + 28, rightW - 50, 40), TravelTimeCalculator.FormatTime(lightTransitSecs), bigValueStyle);
                GUI.Label(new Rect(rightX + 25, ry + 74, rightW - 50, 24), "Scale Span: " + TravelTimeCalculator.FormatSpan(spanKm, activeUnitSystem), statLabelStyle);

                ry += 122;

                // Section 2: Active Transit Pulse Interactive Card
                GUI.Box(new Rect(rightX + 15, ry, rightW - 30, 275), "", cardStyle);
                string speedLabel = activeUnitSystem switch
                {
                    UnitSystem.Miles => "SPEED c = 186,282 mi/s",
                    UnitSystem.Dual => "SPEED c = 299,792 km/s [186,282 mi/s]",
                    _ => "SPEED c = 299,792 km/s"
                };
                GUI.Label(new Rect(rightX + 25, ry + 10, rightW - 50, 22), $"⚡ PHOTON TRANSIT COMPUTER ({speedLabel})", headerTitleStyle);

                // Status Banner
                string statusMsg;
                if (isPulseActive && pulseEmitter != null)
                {
                    double currentDist = pulseEmitter.CurrentDistanceKm;
                    statusMsg = isPulsePaused
                        ? $"⏸ PAUSED: Traversed {TravelTimeCalculator.FormatSpan(currentDist, activeUnitSystem)} ({Mathf.RoundToInt(pulseProgress * 100)}%)"
                        : $"▶ TRANSIT IN FLIGHT: {TravelTimeCalculator.FormatSpan(currentDist, activeUnitSystem)} ({Mathf.RoundToInt(pulseProgress * 100)}%)";
                }
                else if (pulseEmitter != null && !string.IsNullOrEmpty(pulseEmitter.CompletedSummary))
                {
                    statusMsg = "✓ TRANSIT COMPLETE: " + TravelTimeCalculator.FormatSpan(pulseEmitter.TotalDistanceKm, activeUnitSystem);
                }
                else
                {
                    statusMsg = "READY: Engage 'Fire Light Pulse' to launch relativistic wave.";
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

                // Simulation Speed Regulation Switches
                float speedY = ry + 120;
                GUI.Label(new Rect(rightX + 25, speedY, 200, 20), "SIMULATION TIME FACTOR:", statLabelStyle);
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
                if (GUI.Button(new Rect(rightX + 30 + ctrlW, ctrlY, ctrlW, 55), isPulseActive ? "⚡ Replay Pulse" : "⚡ Discharge Pulse", activeButtonStyle))
                {
                    if (engine != null) engine.FirePulse();
                }
            }
            else
            {
                // -------------------------------------------------------------
                // TAB 1: CRAFT PROPULSION BENCHMARKS (METRIC / IMPERIAL / DUAL)
                // -------------------------------------------------------------
                float ry = leftY + 58;
                GUI.Label(new Rect(rightX + 20, ry, rightW - 40, 22), "CRAFT TRAVEL DURATION ACROSS THIS SCALE:", headerTitleStyle);
                GUI.Label(new Rect(rightX + 20, ry + 24, rightW - 40, 18), "Distance: " + TravelTimeCalculator.FormatSpan(spanKm, activeUnitSystem), statLabelStyle);

                double tRelativistic = spanKm / (0.1 * 299792.458);
                double tParker = spanKm / 192.0;
                double tVoyager = spanKm / 17.0;
                double tJetliner = spanKm / 0.25;

                float compY = ry + 50;
                float rowH = 46;
                DrawCompRow(rightX + 15, compY, rightW - 30, rowH, TravelTimeCalculator.FormatSpeed(TravelTimeCalculator.SPEED_PHOTON, activeUnitSystem, "photon"), TravelTimeCalculator.FormatTime(lightTransitSecs), new Color(0.22f, 0.85f, 1.0f)); compY += rowH + 8;
                DrawCompRow(rightX + 15, compY, rightW - 30, rowH, TravelTimeCalculator.FormatSpeed(TravelTimeCalculator.SPEED_RELATIVISTIC, activeUnitSystem, "relativistic"), TravelTimeCalculator.FormatTime(tRelativistic), new Color(0.95f, 0.90f, 0.40f)); compY += rowH + 8;
                DrawCompRow(rightX + 15, compY, rightW - 30, rowH, TravelTimeCalculator.FormatSpeed(TravelTimeCalculator.SPEED_PARKER_SOLAR_PROBE, activeUnitSystem, "parker"), TravelTimeCalculator.FormatTime(tParker), Color.white); compY += rowH + 8;
                DrawCompRow(rightX + 15, compY, rightW - 30, rowH, TravelTimeCalculator.FormatSpeed(TravelTimeCalculator.SPEED_VOYAGER_1, activeUnitSystem, "voyager"), TravelTimeCalculator.FormatTime(tVoyager), Color.white); compY += rowH + 8;
                DrawCompRow(rightX + 15, compY, rightW - 30, rowH, TravelTimeCalculator.FormatSpeed(TravelTimeCalculator.SPEED_JETLINER, activeUnitSystem, "jetliner"), TravelTimeCalculator.FormatTime(tJetliner), new Color(0.85f, 0.85f, 0.85f));
            }

            // =========================================================================
            // 4. BOTTOM MASTER HELM COCKPIT & NARRATOR COMM FEED
            // =========================================================================
            float bottomH = 110;
            float bottomY = virtualH - bottomH - 15;

            // Comm Feed Narration Subtitle Banner
            bool isTour = engine != null && engine.isTourActive;
            string tourTag = isTour ? $"[🚀 GUIDED TOUR — STAGE {engine.tourCurrentStage}/4] " : "";
            string subText = zoom < 1.75f ? "Stage 1: The Solar System. Spanning ~8.33 light-hours across Neptune's orbit." :
                (zoom < 2.75f ? "Stage 2: The Milky Way Galaxy. ~100,000 light-years across with Orion Spur and Sagittarius A*." :
                (zoom < 3.75f ? "Stage 3: The Local Group Cluster. ~10 million light-years encompassing Andromeda and Milky Way." :
                "Stage 4: Cosmic Web & Particle Horizon. ~93 billion light-years to the Cosmic Microwave Background."));

            Rect commRect = new Rect(virtualW * 0.08f, bottomY - 45, virtualW * 0.84f, 36);
            DrawSciFiBezel(commRect, "[SUB-VOCAL COMM // 432 Hz]", Color.yellow);
            GUI.Label(new Rect(commRect.x + 10, commRect.y + 2, commRect.width - 20, 32), $"🎙️ GEORGE [COMM-LINK]: {tourTag}{subText}", subtitleStyle);

            // Cockpit Helm Bar
            Rect cockpitRect = new Rect(15, bottomY, virtualW - 30, bottomH);
            DrawSciFiBezel(cockpitRect, "[WARP THROTTLE & FLIGHT CONSOLE]", Color.cyan);

            // Continuous Scale Zoom Warp Slider
            GUI.Label(new Rect(30, bottomY + 14, 180, 22), "SCALE WARP THROTTLE", statLabelStyle);
            GUI.Label(new Rect(210, bottomY + 14, virtualW * 0.38f, 20), "1.0 [Solar System] ════ 2.0 [Milky Way] ════ 3.0 [Local Group] ════ 4.0 [Cosmic Web]", statLabelStyle);
            float newZoom = GUI.HorizontalSlider(new Rect(30, bottomY + 44, virtualW * 0.42f, 30), zoom, 1.0f, 4.0f);
            if (Mathf.Abs(newZoom - zoom) > 0.005f && engine != null)
            {
                engine.SetZoomDirect(newZoom);
            }

            // Cockpit Action Buttons: Guided Tour, Light Pulse, Reset Camera
            float btnX = virtualW - 680;
            string tourBtnText = isTour ? $"🚀 Tour: ACTIVE [S{engine.tourCurrentStage}]" : "🚀 Guided Tour [T]";
            if (GUI.Button(new Rect(btnX, bottomY + 22, 210, 65), tourBtnText, isTour ? activeButtonStyle : buttonStyle))
            {
                if (engine != null) engine.ToggleTour();
            }

            string pulseBtnText = isPulseActive ? (isPulsePaused ? "▶ Resume Pulse" : "⏸ Pause Pulse") : "⚡ Discharge Pulse [SPACE]";
            if (GUI.Button(new Rect(btnX + 225, bottomY + 22, 220, 65), pulseBtnText, activeButtonStyle))
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

            if (GUI.Button(new Rect(btnX + 460, bottomY + 22, 160, 65), "↺ Zero Gyro [R]", buttonStyle))
            {
                if (engine != null) engine.ResetCamera();
            }

            GUI.matrix = oldMatrix;
        }

        private void DrawCompRow(float x, float y, float w, float h, string name, string time, Color col)
        {
            GUI.Box(new Rect(x, y, w, h), "", cardStyle);
            GUI.Label(new Rect(x + 12, y + (h - 22) / 2.0f, w * 0.60f, 24), name, compLabelStyle);
            compValueStyle.normal.textColor = col;
            GUI.Label(new Rect(x + w * 0.60f, y + (h - 22) / 2.0f, w * 0.38f, 24), time, compValueStyle);
        }

        private void SetScale(float newScale)
        {
            userScale = Mathf.Clamp(newScale, 0.9f, 2.0f);
            PlayerPrefs.SetFloat("Cosmic_A11y_Scale", userScale);
            if (audioController != null) audioController.PlaySoftChime();
        }
    }
}

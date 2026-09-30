using System;
using UnityEngine;

namespace CosmicZoom
{
    /// <summary>
    /// AAA Holographic Sci-Fi Astro-Avionics Console HUD.
    /// Features:
    /// - Sleek Smoked Sapphire Glass Architecture: Translucent dark glass, 1px precision
    ///   technical borders, and delicate cyan corner brackets (No garish thick orange frames).
    /// - Polar Cyan & Electric Mint Palette: Inspired by high-end FUI (The Expanse, Star Citizen, Homeworld).
    /// - Comprehensive Unit System: Metric (KM), Imperial (Miles), and Dual / Both Systems (Hotkey: U).
    /// - Delicate Optical Viewport Reticle: Boresight crosshairs, targeting brackets, pitch ladder.
    /// - Dedicated Light Transit Pulse Engine & Speed-of-Light Simulator with smooth glowing progress bar.
    /// - Dynamic Craft Propulsion Benchmark comparisons formatted in KM/s, MPH, or Dual speeds.
    /// - Charles Messier Deep-Sky Observatory & 100% Privacy-Preserving Local Ephemeris Calculator.
    /// - Smooth Automated Telescope Camera Slew when engaging Starry Night or locking targets (Hotkey: S).
    /// - Sub-Vocal Mission Audio Comm Ribbon (No personal narrator names, authentic aerospace flight-comms).
    /// - High-DPI Matrix scaling with low-vision accessibility suite.
    /// </summary>
    public class CosmicHUD : MonoBehaviour
    {
        [SerializeField] private CosmicZoomEngine engine;
        [SerializeField] private CosmicAudioController audioController;
        [SerializeField] private CelestialMessierCatalog messierCatalog;
        private Camera mainCam;

        // Measurement Unit System: 0 = KM, 1 = Miles, 2 = Dual (Both)
        public UnitSystem activeUnitSystem = UnitSystem.Kilometers;

        // Accessibility settings
        public float userScale = 1.15f;
        public bool isHighContrast = false;

        // Right Dock Tab: 0 = Light Transit Pulse (c), 1 = Spacecraft Benchmarks, 2 = Messier & Starry Sky
        private int activeRightTab = 0;
        private bool showLocationPicker = false;

        // GUI Styles
        private GUIStyle panelStyle;
        private GUIStyle cardStyle;
        private GUIStyle headerTitleStyle;
        private GUIStyle headerSubStyle;
        private GUIStyle statLabelStyle;
        private GUIStyle statValueStyle;
        private GUIStyle statValueGoldStyle;
        private GUIStyle statValueCyanStyle;
        private GUIStyle statValueMintStyle;
        private GUIStyle bigValueStyle;
        private GUIStyle bodyStyle;
        private GUIStyle compLabelStyle;
        private GUIStyle compValueStyle;
        private GUIStyle buttonNormalStyle;
        private GUIStyle buttonActiveStyle;
        private GUIStyle tabNormalStyle;
        private GUIStyle tabActiveStyle;
        private GUIStyle subtitleStyle;
        private GUIStyle pulseStatusStyle;
        private GUIStyle reticleHeadingStyle;
        private GUIStyle reticleSubStyle;
        private GUIStyle hudMicroStyle;

        // Sleek Holographic Smoked Glass Textures
        private Texture2D texGlass;
        private Texture2D texGlassCard;
        private Texture2D texGlassActive;
        private Texture2D texCyan;
        private Texture2D texCyanDim;
        private Texture2D texMint;
        private Texture2D texGold;
        private Texture2D texSlate;
        private Texture2D texBorder;
        private Texture2D texWhite;
        private Texture2D strutTex;
        private Texture2D texCockpitCanopy;

        private bool stylesInitialized = false;

        private void Start()
        {
            if (engine == null) engine = FindAnyObjectByType<CosmicZoomEngine>();
            if (audioController == null) audioController = FindAnyObjectByType<CosmicAudioController>();
            if (messierCatalog == null) messierCatalog = CelestialMessierCatalog.Instance ?? FindAnyObjectByType<CelestialMessierCatalog>();
            if (mainCam == null) mainCam = Camera.main;

            if (PlayerPrefs.HasKey("Cosmic_Unit_System"))
            {
                activeUnitSystem = (UnitSystem)PlayerPrefs.GetInt("Cosmic_Unit_System", 0);
            }
            if (PlayerPrefs.HasKey("Cosmic_A11y_Scale"))
            {
                userScale = PlayerPrefs.GetFloat("Cosmic_A11y_Scale", 1.15f);
            }
            if (PlayerPrefs.HasKey("Cosmic_A11y_Contrast"))
            {
                isHighContrast = PlayerPrefs.GetInt("Cosmic_A11y_Contrast", 0) == 1;
            }
        }

        private void Update()
        {
            // Keyboard shortcut: Key S toggles Starry Night observation mode
            if (Input.GetKeyDown(KeyCode.S))
            {
                ToggleStarryNight();
            }

            // Keyboard shortcut: Key U toggles units (KM / Miles / Dual)
            if (Input.GetKeyDown(KeyCode.U))
            {
                CycleUnitSystem();
            }
        }

        public void ToggleStarryNight()
        {
            if (messierCatalog == null) messierCatalog = CelestialMessierCatalog.Instance ?? FindAnyObjectByType<CelestialMessierCatalog>();
            if (messierCatalog != null)
            {
                if (messierCatalog.isStarryNightActive)
                {
                    messierCatalog.ExitStarryNight();
                }
                else
                {
                    activeRightTab = 2; // Switch directly to Messier tab
                    if (messierCatalog.currentTarget == null && messierCatalog.Catalog.Count > 0)
                    {
                        messierCatalog.currentTarget = messierCatalog.Catalog[0];
                    }
                    if (messierCatalog.currentTarget != null)
                    {
                        messierCatalog.LockTelescopeOnTarget(messierCatalog.currentTarget);
                    }
                }

                if (audioController != null) audioController.PlaySoftChime();
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

            // Sleek Smoked Glass & Holographic FUI Palette
            texGlass = MakeColorTexture(2, 2, new Color(0.015f, 0.035f, 0.07f, 0.86f));       // Translucent smoked obsidian
            texGlassCard = MakeColorTexture(2, 2, new Color(0.03f, 0.065f, 0.12f, 0.88f));    // Subtle card plate
            texGlassActive = MakeColorTexture(2, 2, new Color(0.04f, 0.18f, 0.28f, 0.90f));  // Active cyan glow glass
            texCyan = MakeColorTexture(2, 2, new Color(0.22f, 0.74f, 0.97f, 1.0f));          // Polar Cyan (#38BDF8)
            texCyanDim = MakeColorTexture(2, 2, new Color(0.12f, 0.42f, 0.58f, 0.70f));
            texMint = MakeColorTexture(2, 2, new Color(0.18f, 0.83f, 0.75f, 1.0f));          // Electric Mint (#2DD4BF)
            texGold = MakeColorTexture(2, 2, new Color(0.98f, 0.75f, 0.14f, 1.0f));          // Stellar Gold (#FBBF24)
            texSlate = MakeColorTexture(2, 2, new Color(0.20f, 0.28f, 0.40f, 0.85f));
            texBorder = MakeColorTexture(2, 2, new Color(0.18f, 0.35f, 0.52f, 0.65f));       // 1px tech border
            texWhite = MakeColorTexture(2, 2, Color.white);
            strutTex = MakeColorTexture(2, 2, new Color(0.02f, 0.04f, 0.09f, 0.75f));

            texCockpitCanopy = Resources.Load<Texture2D>("cockpit_canopy_overlay");

            headerTitleStyle = new GUIStyle
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            headerSubStyle = new GUIStyle
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.22f, 0.74f, 0.97f) }
            };

            statLabelStyle = new GUIStyle
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.58f, 0.68f, 0.82f) }
            };

            statValueStyle = new GUIStyle
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            statValueGoldStyle = new GUIStyle
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.98f, 0.75f, 0.14f) }
            };

            statValueCyanStyle = new GUIStyle
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.22f, 0.74f, 0.97f) }
            };

            statValueMintStyle = new GUIStyle
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.18f, 0.83f, 0.75f) }
            };

            bigValueStyle = new GUIStyle
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.22f, 0.74f, 0.97f) }
            };

            pulseStatusStyle = new GUIStyle
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                wordWrap = true,
                normal = { textColor = new Color(0.98f, 0.75f, 0.14f) }
            };

            bodyStyle = new GUIStyle
            {
                fontSize = 12,
                wordWrap = true,
                normal = { textColor = new Color(0.85f, 0.90f, 0.96f) }
            };

            compLabelStyle = new GUIStyle
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.70f, 0.80f, 0.92f) }
            };

            compValueStyle = new GUIStyle
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleRight,
                normal = { textColor = Color.white }
            };

            buttonNormalStyle = new GUIStyle
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                normal = { textColor = new Color(0.82f, 0.88f, 0.96f) }
            };

            buttonActiveStyle = new GUIStyle
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                normal = { textColor = new Color(0.22f, 0.74f, 0.97f) }
            };

            tabNormalStyle = new GUIStyle
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.60f, 0.72f, 0.85f) }
            };

            tabActiveStyle = new GUIStyle
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            subtitleStyle = new GUIStyle
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                normal = { textColor = new Color(0.95f, 0.98f, 1.0f) }
            };

            reticleHeadingStyle = new GUIStyle
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.22f, 0.74f, 0.97f, 0.85f) }
            };

            reticleSubStyle = new GUIStyle
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.58f, 0.68f, 0.82f, 0.80f) }
            };

            hudMicroStyle = new GUIStyle
            {
                fontSize = 9,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.LowerRight,
                normal = { textColor = new Color(0.22f, 0.74f, 0.97f, 0.65f) }
            };

            stylesInitialized = true;
        }

        /// <summary>
        /// Renders a sleek, translucent smoked-glass console panel with 1px precision borders.
        /// </summary>
        private void DrawSciFiPanel(Rect r, string tag = "")
        {
            // Translucent glass body
            GUI.DrawTexture(r, texGlass);

            // 1px Technical border
            GUI.DrawTexture(new Rect(r.x, r.y, r.width, 1), texBorder);
            GUI.DrawTexture(new Rect(r.x, r.y + r.height - 1, r.width, 1), texBorder);
            GUI.DrawTexture(new Rect(r.x, r.y, 1, r.height), texBorder);
            GUI.DrawTexture(new Rect(r.x + r.width - 1, r.y, 1, r.height), texBorder);

            // Subtle 2px Polar Cyan Accent Bar on Top
            float accentW = Mathf.Min(80f, r.width * 0.35f);
            GUI.DrawTexture(new Rect(r.x + 8, r.y, accentW, 2), texCyan);

            // Delicate 4px Corner Ticks
            float tick = 4f;
            GUI.DrawTexture(new Rect(r.x, r.y, tick, 1), texCyan);
            GUI.DrawTexture(new Rect(r.x, r.y, 1, tick), texCyan);
            GUI.DrawTexture(new Rect(r.x + r.width - tick, r.y, tick, 1), texCyan);
            GUI.DrawTexture(new Rect(r.x + r.width - 1, r.y, 1, tick), texCyan);
            GUI.DrawTexture(new Rect(r.x, r.y + r.height - 1, tick, 1), texCyan);
            GUI.DrawTexture(new Rect(r.x, r.y + r.height - tick, 1, tick), texCyan);
            GUI.DrawTexture(new Rect(r.x + r.width - tick, r.y + r.height - 1, tick, 1), texCyan);
            GUI.DrawTexture(new Rect(r.x + r.width - 1, r.y + r.height - tick, 1, tick), texCyan);

            if (!string.IsNullOrEmpty(tag))
            {
                GUI.Label(new Rect(r.x + r.width - 130, r.y + 3, 120, 16), tag, hudMicroStyle);
            }
        }

        /// <summary>
        /// Renders a sleek, responsive sci-fi button with 1px border and glowing active indicator.
        /// </summary>
        private bool DrawSleekButton(Rect r, string label, bool isActive, Color accentCol, int fontSize = 11)
        {
            Texture2D bg = isActive ? texGlassActive : texGlassCard;
            GUI.DrawTexture(r, bg);

            // 1px Border
            Texture2D border = isActive ? (accentCol == Color.green ? texMint : texCyan) : texBorder;
            GUI.DrawTexture(new Rect(r.x, r.y, r.width, 1), border);
            GUI.DrawTexture(new Rect(r.x, r.y + r.height - 1, r.width, 1), border);
            GUI.DrawTexture(new Rect(r.x, r.y, 1, r.height), border);
            GUI.DrawTexture(new Rect(r.x + r.width - 1, r.y, 1, r.height), border);

            if (isActive)
            {
                // Left glowing indicator pip
                GUI.DrawTexture(new Rect(r.x, r.y, 3, r.height), accentCol == Color.green ? texMint : texCyan);
            }

            GUIStyle st = isActive ? buttonActiveStyle : buttonNormalStyle;
            st.fontSize = fontSize;
            GUI.Label(r, label, st);

            bool clicked = GUI.Button(r, GUIContent.none, GUIStyle.none);
            if (clicked && audioController != null) audioController.PlaySoftChime();
            return clicked;
        }

        private void DrawCenterFlightHUD(float virtualW, float virtualH)
        {
            float cx = virtualW * 0.5f;
            float cy = virtualH * 0.46f;

            // Center Boresight Crosshair (Subtle & Precision)
            float crossSize = 14f;
            GUI.DrawTexture(new Rect(cx - crossSize * 0.5f, cy - 1f, crossSize, 1.5f), texCyanDim);
            GUI.DrawTexture(new Rect(cx - 1f, cy - crossSize * 0.5f, 1.5f, crossSize), texCyanDim);

            // Tactical Targeting Brackets [   ]
            float boxR = 36f;
            float tick = 10f;
            GUI.DrawTexture(new Rect(cx - boxR, cy - boxR, tick, 1.5f), texCyanDim);
            GUI.DrawTexture(new Rect(cx - boxR, cy - boxR, 1.5f, tick), texCyanDim);
            GUI.DrawTexture(new Rect(cx + boxR - tick, cy - boxR, tick, 1.5f), texCyanDim);
            GUI.DrawTexture(new Rect(cx + boxR - 1.5f, cy - boxR, 1.5f, tick), texCyanDim);
            GUI.DrawTexture(new Rect(cx - boxR, cy + boxR - 1.5f, tick, 1.5f), texCyanDim);
            GUI.DrawTexture(new Rect(cx - boxR, cy + boxR - tick, 1.5f, tick), texCyanDim);
            GUI.DrawTexture(new Rect(cx + boxR - tick, cy + boxR - 1.5f, tick, 1.5f), texCyanDim);
            GUI.DrawTexture(new Rect(cx + boxR - 1.5f, cy + boxR - tick, 1.5f, tick), texCyanDim);
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

            float zoom = engine != null ? engine.currentZoom : 1.0f;
            double spanKm = TravelTimeCalculator.GetSpanKmFromZoom(zoom);
            double lightTransitSecs = TravelTimeCalculator.GetLightTransitSeconds(spanKm);

            // Widescreen Clear Viewport (No obstructive canopy pillars)

            // 2. High-Visibility Return to Flight Deck Banner Button (If in Starry Night mode)
            bool isStarryMode = messierCatalog != null && messierCatalog.isStarryNightActive;
            if (isStarryMode)
            {
                float returnBtnW = 340f;
                float returnBtnX = (virtualW - returnBtnW) / 2.0f;
                if (DrawSleekButton(new Rect(returnBtnX, 74, returnBtnW, 42), "🚀 RETURN TO FLIGHT DECK [S]", true, Color.cyan, 12))
                {
                    ToggleStarryNight();
                }
            }

            // Center Viewport HUD Reticle
            DrawCenterFlightHUD(virtualW, virtualH);

            // =========================================================================
            // 1. TOP AVIONICS HELM CONSOLE (CLEAN, SLEEK, NON-OVERLAPPING)
            // =========================================================================
            float headerH = 58f;
            Rect headerRect = new Rect(14, 10, virtualW - 28, headerH);
            DrawSciFiPanel(headerRect, "// AVIONICS.01 //");

            // Left Brand & Console Telemetry (Single unified block to prevent collision)
            string domainName = zoom < 1.75f ? "SOLAR SYSTEM" : (zoom < 2.75f ? "MILKY WAY GALAXY" : (zoom < 3.75f ? "LOCAL GROUP CLUSTER" : "OBSERVABLE COSMIC WEB"));
            GUI.Label(new Rect(26, 16, 440, 22), "ASTRONAUTICA DEEP-SURVEY PLATFORM", headerTitleStyle);
            GUI.Label(new Rect(26, 36, 480, 18), $"[ SYS.NAV-01 ]  DOMAIN: {domainName}  •  SPAN: {TravelTimeCalculator.FormatSpan(spanKm, activeUnitSystem)}", headerSubStyle);

            // Right Action Switches
            float rx = virtualW - 24;

            // Universal Quit Button (Esc / ⏻ QUIT)
            rx -= 78f;
            if (DrawSleekButton(new Rect(rx, 18, 72, 36), "⏻ QUIT", false, new Color(1f, 0.4f, 0.4f), 11))
            {
                if (engine != null) engine.QuitApplication();
                else Application.Quit();
            }

            // Mission Audio Narrator Toggle
            rx -= 78f;
            bool isNarrOn = audioController != null && audioController.IsNarratorAutoPlay;
            if (DrawSleekButton(new Rect(rx, 18, 72, 36), isNarrOn ? "🎙️ COMM" : "🔇 COMM", isNarrOn, Color.green))
            {
                if (audioController != null) audioController.ToggleNarrator();
            }

            // Audio Mute Toggle
            rx -= 78f;
            bool isMuted = audioController != null && audioController.IsAudioMuted;
            if (DrawSleekButton(new Rect(rx, 18, 72, 36), isMuted ? "🔇 SND" : "🔊 SND", !isMuted, Color.cyan))
            {
                if (audioController != null) audioController.ToggleAudioMute();
            }

            // HUD Color Mode
            rx -= 94f;
            if (DrawSleekButton(new Rect(rx, 18, 88, 36), isHighContrast ? "🌙 NIGHT" : "✨ COLOR", isHighContrast, Color.cyan))
            {
                isHighContrast = !isHighContrast;
                PlayerPrefs.SetInt("Cosmic_A11y_Contrast", isHighContrast ? 1 : 0);
            }

            // Font Scaling
            rx -= 36f;
            if (DrawSleekButton(new Rect(rx, 18, 32, 36), "A+", false, Color.cyan)) SetScale(userScale + 0.15f);
            rx -= 36f;
            if (DrawSleekButton(new Rect(rx, 18, 32, 36), "A−", false, Color.cyan)) SetScale(userScale - 0.15f);

            // Unit Switcher (KM / Miles / Dual)
            rx -= 128f;
            string unitBtnText = activeUnitSystem switch
            {
                UnitSystem.Miles => "📐 UNITS: MI (U)",
                UnitSystem.Dual => "📐 DUAL KM+MI (U)",
                _ => "📐 UNITS: KM (U)"
            };
            if (DrawSleekButton(new Rect(rx, 18, 122, 36), unitBtnText, activeUnitSystem == UnitSystem.Dual, Color.yellow))
            {
                CycleUnitSystem();
            }

            // Starry Night & Messier Sky Observation Mode Toggle (Hotkey: S)
            rx -= 145f;
            bool isStarry = messierCatalog != null && messierCatalog.isStarryNightActive;
            string starryLabel = isStarry ? "🔭 SKY: ON (S)" : "🔭 STARRY SKY (S)";
            if (DrawSleekButton(new Rect(rx, 18, 138, 36), starryLabel, isStarry, Color.cyan))
            {
                ToggleStarryNight();
            }

            // =========================================================================
            // 2. LEFT COMMAND WING: SECTOR SCALE SELECTOR
            // =========================================================================
            float leftW = 280f;
            float leftY = 76f;
            Rect leftRect = new Rect(14, leftY, leftW, 435f);
            DrawSciFiPanel(leftRect, "// SECTOR.NAV //");

            GUI.Label(new Rect(26, leftY + 14, 250, 20), "SECTOR SCALE SELECTOR", headerTitleStyle);
            GUI.Label(new Rect(26, leftY + 32, 250, 16), "DESTINATION LOCK [KEYS 1 - 4]", statLabelStyle);

            float stageBtnY = leftY + 54f;
            for (int s = 1; s <= 4; s++)
            {
                bool isActive = (engine != null && engine.activeStageIndex == s);
                Rect sRect = new Rect(24, stageBtnY, leftW - 20, 80f);

                string sTitle = s switch
                {
                    1 => "01  SOLAR SYSTEM",
                    2 => "02  MILKY WAY GALAXY",
                    3 => "03  LOCAL GROUP",
                    4 => "04  COSMIC WEB & CMB",
                    _ => ""
                };

                string sSpan = s switch
                {
                    1 => activeUnitSystem switch
                    {
                        UnitSystem.Miles => "Sun to Neptune  •  5.59B mi (8.33 LH)",
                        UnitSystem.Dual => "8.996B km [5.59B mi]  •  8.33 LH",
                        _ => "Sun to Neptune  •  8.996B km (8.33 LH)"
                    },
                    2 => activeUnitSystem switch
                    {
                        UnitSystem.Miles => "Spiral Arms & Sgr A*  •  5.88e17 mi",
                        UnitSystem.Dual => "9.46e17 km [5.88e17 mi]  •  100k LY",
                        _ => "Spiral Arms & Sgr A*  •  9.461e17 km"
                    },
                    3 => activeUnitSystem switch
                    {
                        UnitSystem.Miles => "Andromeda M31 & Satellites  •  10 MLY",
                        UnitSystem.Dual => "9.46e19 km [5.88e19 mi]  •  10 MLY",
                        _ => "Andromeda M31 & Satellites  •  10 MLY"
                    },
                    4 => activeUnitSystem switch
                    {
                        UnitSystem.Miles => "CMB Horizon & Filaments  •  93 GLY",
                        UnitSystem.Dual => "8.80e23 km [5.47e23 mi]  •  93 GLY",
                        _ => "CMB Horizon & Filaments  •  93 GLY"
                    },
                    _ => ""
                };

                // Draw Stage Button with clean 2-line layout
                Texture2D sBg = isActive ? texGlassActive : texGlassCard;
                GUI.DrawTexture(sRect, sBg);

                Texture2D sBorder = isActive ? texCyan : texBorder;
                GUI.DrawTexture(new Rect(sRect.x, sRect.y, sRect.width, 1), sBorder);
                GUI.DrawTexture(new Rect(sRect.x, sRect.y + sRect.height - 1, sRect.width, 1), sBorder);
                GUI.DrawTexture(new Rect(sRect.x, sRect.y, 1, sRect.height), sBorder);
                GUI.DrawTexture(new Rect(sRect.x + sRect.width - 1, sRect.y, 1, sRect.height), sBorder);

                if (isActive)
                {
                    GUI.DrawTexture(new Rect(sRect.x, sRect.y, 3, sRect.height), texCyan);
                }

                GUI.Label(new Rect(sRect.x + 12, sRect.y + 12, sRect.width - 24, 20), sTitle, isActive ? statValueCyanStyle : headerTitleStyle);
                GUI.Label(new Rect(sRect.x + 12, sRect.y + 36, sRect.width - 24, 32), sSpan, statLabelStyle);

                if (GUI.Button(sRect, GUIContent.none, GUIStyle.none))
                {
                    if (engine != null) engine.JumpToStage(s);
                    if (audioController != null) audioController.PlaySoftChime();
                }

                stageBtnY += 86f;
            }

            // Diagnostic Telemetry Strip at Bottom of Left Panel
            GUI.Label(new Rect(26, leftY + 404, leftW - 32, 16), "INERTIAL: NOMINAL • GYRO: LOCKED • DRIFT: 0.000", hudMicroStyle);

            // =========================================================================
            // 3. RIGHT TACTICAL WING: TELEMETRY & CELESTIAL EYEPIECE
            // =========================================================================
            float rightW = 460f;
            float rightX = virtualW - rightW - 14f;
            Rect rightRect = new Rect(rightX, leftY, rightW, 460f);
            DrawSciFiPanel(rightRect, "// TELEMETRY.SYS //");

            // Center Quick-Focus Targets Bar (Close-up inspection of planets, stars & starships)
            if (!isStarryMode)
            {
                DrawQuickFocusBar(virtualW, leftW, rightX);
            }

            // Sleek Tab Switchers (3 wide tabs)
            float tabW = (rightW - 32f) / 3.0f;
            float tabY = leftY + 12f;

            if (DrawSleekButton(new Rect(rightX + 16, tabY, tabW - 4, 32), "⚡ Transit (c)", activeRightTab == 0, Color.cyan))
            {
                activeRightTab = 0;
            }
            if (DrawSleekButton(new Rect(rightX + 16 + tabW, tabY, tabW - 4, 32), "🚀 Benchmarks", activeRightTab == 1, Color.cyan))
            {
                activeRightTab = 1;
            }
            if (DrawSleekButton(new Rect(rightX + 16 + tabW * 2, tabY, tabW - 4, 32), "🔭 Messier Sky", activeRightTab == 2, Color.cyan))
            {
                activeRightTab = 2;
                if (messierCatalog != null) messierCatalog.isStarryNightActive = true;
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
                float ry = leftY + 52f;

                // Duration Card
                GUI.DrawTexture(new Rect(rightX + 16, ry, rightW - 32, 92), texGlassCard);
                GUI.Label(new Rect(rightX + 28, ry + 10, rightW - 56, 16), "TIME FOR PHOTON (1.0c) TO TRANSIT ACROSS THIS SCALE:", statLabelStyle);
                GUI.Label(new Rect(rightX + 28, ry + 28, rightW - 56, 36), TravelTimeCalculator.FormatTime(lightTransitSecs), bigValueStyle);
                GUI.Label(new Rect(rightX + 28, ry + 66, rightW - 56, 18), $"Scale Distance: {TravelTimeCalculator.FormatSpan(spanKm, activeUnitSystem)}", statValueGoldStyle);

                ry += 102f;

                // Active Transit Computer Card
                GUI.DrawTexture(new Rect(rightX + 16, ry, rightW - 32, 276), texGlassCard);

                string speedLabel = activeUnitSystem switch
                {
                    UnitSystem.Miles => "SPEED c = 186,282 mi/s",
                    UnitSystem.Dual => "SPEED c = 299,792 km/s [186,282 mi/s]",
                    _ => "SPEED c = 299,792 km/s"
                };
                GUI.Label(new Rect(rightX + 28, ry + 12, rightW - 56, 20), $"⚡ RELATIVISTIC PHOTON COMPUTER ({speedLabel})", headerTitleStyle);

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
                    statusMsg = "READY: Engage 'Discharge Pulse' to launch wave.";
                }
                GUI.Label(new Rect(rightX + 28, ry + 36, rightW - 56, 38), statusMsg, pulseStatusStyle);

                // Progress Bar
                float pBarX = rightX + 28;
                float pBarY = ry + 80;
                float pBarW = rightW - 56;
                float pBarH = 18;
                GUI.DrawTexture(new Rect(pBarX, pBarY, pBarW, pBarH), texGlass);
                if (pulseProgress > 0)
                {
                    GUI.DrawTexture(new Rect(pBarX, pBarY, pBarW * pulseProgress, pBarH), texCyan);
                }
                GUI.DrawTexture(new Rect(pBarX, pBarY, pBarW, 1), texBorder);
                GUI.DrawTexture(new Rect(pBarX, pBarY + pBarH - 1, pBarW, 1), texBorder);
                GUI.Label(new Rect(pBarX, pBarY, pBarW, pBarH), $"{Mathf.RoundToInt(pulseProgress * 100)}% Traversed", compValueStyle);

                // Time Factor
                float spdY = ry + 114;
                GUI.Label(new Rect(rightX + 28, spdY, 200, 16), "SIMULATION TIME FACTOR:", statLabelStyle);
                float curSpeed = pulseEmitter != null ? pulseEmitter.SpeedMultiplier : 1.0f;

                float spdW = (rightW - 60) / 3.0f;
                if (DrawSleekButton(new Rect(rightX + 28, spdY + 20, spdW - 4, 32), "0.25x Slow", Mathf.Approximately(curSpeed, 0.25f), Color.cyan))
                {
                    if (pulseEmitter != null) pulseEmitter.SetSpeedMultiplier(0.25f);
                }
                if (DrawSleekButton(new Rect(rightX + 28 + spdW, spdY + 20, spdW - 4, 32), "0.50x Steady", Mathf.Approximately(curSpeed, 0.50f), Color.cyan))
                {
                    if (pulseEmitter != null) pulseEmitter.SetSpeedMultiplier(0.50f);
                }
                if (DrawSleekButton(new Rect(rightX + 28 + spdW * 2, spdY + 20, spdW - 4, 32), "1.00x Normal", Mathf.Approximately(curSpeed, 1.0f), Color.cyan))
                {
                    if (pulseEmitter != null) pulseEmitter.SetSpeedMultiplier(1.0f);
                }

                // Playback Action Buttons
                float ctrlY = ry + 188;
                float ctrlW = (rightW - 64) / 2.0f;
                string pauseText = isPulsePaused ? "▶ Resume Transit" : "⏸ Pause Transit";
                if (DrawSleekButton(new Rect(rightX + 28, ctrlY, ctrlW, 48), pauseText, isPulseActive, Color.cyan, 12))
                {
                    if (pulseEmitter != null) pulseEmitter.TogglePause();
                }
                string dischargeText = isPulseActive ? "⚡ Replay Pulse" : "⚡ Discharge Pulse";
                if (DrawSleekButton(new Rect(rightX + 36 + ctrlW, ctrlY, ctrlW, 48), dischargeText, isPulseActive, Color.green, 12))
                {
                    if (engine != null) engine.FirePulse();
                }
            }
            else if (activeRightTab == 1)
            {
                // -------------------------------------------------------------
                // TAB 1: CRAFT PROPULSION BENCHMARKS
                // -------------------------------------------------------------
                float ry = leftY + 54f;
                GUI.Label(new Rect(rightX + 20, ry, rightW - 40, 20), "CRAFT TRAVEL DURATION ACROSS THIS SCALE:", headerTitleStyle);
                GUI.Label(new Rect(rightX + 20, ry + 22, rightW - 40, 16), "Distance: " + TravelTimeCalculator.FormatSpan(spanKm, activeUnitSystem), statLabelStyle);

                double tRelativistic = spanKm / (0.1 * 299792.458);
                double tParker = spanKm / 192.0;
                double tVoyager = spanKm / 17.0;
                double tJetliner = spanKm / 0.25;

                float compY = ry + 44f;
                float rowH = 46f;
                DrawCompRow(rightX + 16, compY, rightW - 32, rowH, TravelTimeCalculator.FormatSpeed(TravelTimeCalculator.SPEED_PHOTON, activeUnitSystem, "photon"), TravelTimeCalculator.FormatTime(lightTransitSecs), new Color(0.22f, 0.74f, 0.97f)); compY += rowH + 8;
                DrawCompRow(rightX + 16, compY, rightW - 32, rowH, TravelTimeCalculator.FormatSpeed(TravelTimeCalculator.SPEED_RELATIVISTIC, activeUnitSystem, "relativistic"), TravelTimeCalculator.FormatTime(tRelativistic), new Color(0.98f, 0.75f, 0.14f)); compY += rowH + 8;
                DrawCompRow(rightX + 16, compY, rightW - 32, rowH, TravelTimeCalculator.FormatSpeed(TravelTimeCalculator.SPEED_PARKER_SOLAR_PROBE, activeUnitSystem, "parker"), TravelTimeCalculator.FormatTime(tParker), Color.white); compY += rowH + 8;
                DrawCompRow(rightX + 16, compY, rightW - 32, rowH, TravelTimeCalculator.FormatSpeed(TravelTimeCalculator.SPEED_VOYAGER_1, activeUnitSystem, "voyager"), TravelTimeCalculator.FormatTime(tVoyager), Color.white); compY += rowH + 8;
                DrawCompRow(rightX + 16, compY, rightW - 32, rowH, TravelTimeCalculator.FormatSpeed(TravelTimeCalculator.SPEED_JETLINER, activeUnitSystem, "jetliner"), TravelTimeCalculator.FormatTime(tJetliner), new Color(0.70f, 0.80f, 0.92f));
            }
            else
            {
                // -------------------------------------------------------------
                // TAB 2: STARRY NIGHT & CHARLES MESSIER DEEP-SKY OBSERVATORY
                // -------------------------------------------------------------
                float ry = leftY + 50f;

                if (messierCatalog == null) messierCatalog = CelestialMessierCatalog.Instance ?? FindAnyObjectByType<CelestialMessierCatalog>();

                // Observer Location Banner (100% Offline Privacy Guarantee)
                GUI.DrawTexture(new Rect(rightX + 16, ry, rightW - 32, 42), texGlassCard);
                string obsName = messierCatalog != null ? messierCatalog.currentObserver.locationName : "Mauna Kea Observatory";
                float obsLat = messierCatalog != null ? messierCatalog.currentObserver.latitude : 19.82f;
                float obsLon = messierCatalog != null ? messierCatalog.currentObserver.longitude : -155.47f;
                string latSign = obsLat >= 0 ? $"{obsLat:0.0}°N" : $"{-obsLat:0.0}°S";
                string lonSign = obsLon >= 0 ? $"{obsLon:0.0}°E" : $"{-obsLon:0.0}°W";

                GUI.Label(new Rect(rightX + 26, ry + 4, rightW - 160, 18), $"📍 OBSERVER: {obsName.ToUpper()}", statValueGoldStyle);
                GUI.Label(new Rect(rightX + 26, ry + 22, rightW - 160, 16), $"🔒 {latSign}, {lonSign} • 100% LOCAL PRIVACY (ZERO NETWORK)", hudMicroStyle);

                if (DrawSleekButton(new Rect(rightX + rightW - 138, ry + 6, 116, 30), showLocationPicker ? "✓ Close" : "📍 Location", showLocationPicker, Color.yellow, 10))
                {
                    showLocationPicker = !showLocationPicker;
                }
                ry += 46f;

                if (showLocationPicker)
                {
                    // Location Preset Drawer
                    GUI.DrawTexture(new Rect(rightX + 16, ry, rightW - 32, 126), texGlassCard);
                    GUI.Label(new Rect(rightX + 24, ry + 6, rightW - 48, 16), "SELECT DARK-SKY OBSERVATORY OR CITY PRESET (OFFLINE):", statLabelStyle);

                    float locBtnW = (rightW - 54) / 3.0f;
                    float locBtnH = 24f;
                    for (int bi = 0; bi < CelestialMessierCatalog.BuiltinLocations.Length; bi++)
                    {
                        var loc = CelestialMessierCatalog.BuiltinLocations[bi];
                        int lRow = bi / 3;
                        int lCol = bi % 3;
                        Rect lRect = new Rect(rightX + 20 + lCol * (locBtnW + 5), ry + 24 + lRow * (locBtnH + 4), locBtnW, locBtnH);
                        bool isCurLoc = messierCatalog != null && messierCatalog.currentObserver.locationName == loc.locationName;
                        if (DrawSleekButton(lRect, loc.locationName, isCurLoc, Color.cyan, 10))
                        {
                            if (messierCatalog != null)
                            {
                                messierCatalog.SetObserverLocation(loc.locationName, loc.latitude, loc.longitude, loc.regionDesc);
                            }
                        }
                    }
                    ry += 132f;
                }

                // 4 Clean Filter Toggles (Single line, no text wrapping)
                float fltW = (rightW - 44f) / 4.0f;
                bool linesOn = messierCatalog != null && messierCatalog.showConstellationLines;
                if (DrawSleekButton(new Rect(rightX + 16, ry, fltW, 26), linesOn ? "✨ Lines: ON" : "✨ Lines: OFF", linesOn, Color.cyan, 10))
                {
                    if (messierCatalog != null) messierCatalog.showConstellationLines = !messierCatalog.showConstellationLines;
                }

                bool messierOn = messierCatalog != null && messierCatalog.showMessierMarkers;
                if (DrawSleekButton(new Rect(rightX + 20 + fltW, ry, fltW, 26), messierOn ? "🌀 Messier: ON" : "🌀 Messier: OFF", messierOn, Color.cyan, 10))
                {
                    if (messierCatalog != null) messierCatalog.showMessierMarkers = !messierCatalog.showMessierMarkers;
                }

                bool starsOn = messierCatalog != null && messierCatalog.showStarLabels;
                if (DrawSleekButton(new Rect(rightX + 24 + fltW * 2, ry, fltW, 26), starsOn ? "⭐ Stars: ON" : "⭐ Stars: OFF", starsOn, Color.cyan, 10))
                {
                    if (messierCatalog != null) messierCatalog.showStarLabels = !messierCatalog.showStarLabels;
                }

                bool horizOn = messierCatalog != null && messierCatalog.showLocalHorizonPlane;
                if (DrawSleekButton(new Rect(rightX + 28 + fltW * 3, ry, fltW, 26), horizOn ? "🌍 Horizon: ON" : "🌍 Horizon: OFF", horizOn, Color.green, 10))
                {
                    if (messierCatalog != null) messierCatalog.showLocalHorizonPlane = !messierCatalog.showLocalHorizonPlane;
                }

                ry += 32f;

                // Quick Target Selector (3 Clean Columns, Readable Font)
                GUI.Label(new Rect(rightX + 20, ry, rightW - 40, 16), "CELESTIAL TARGETS // MESSIER DEEP SKY:", statLabelStyle);
                ry += 18f;

                string[] quickIds = new string[] { "M31", "M42", "M45", "M13", "M1", "M16", "M27", "M51", "M57", "M87", "M104", "STAR-SIRIUS" };
                string[] quickLabels = new string[] { "M31 Andromeda", "M42 Orion", "M45 Pleiades", "M13 Hercules", "M1 Crab Nebula", "M16 Pillars", "M27 Dumbbell", "M51 Whirlpool", "M57 Ring", "M87 Virgo A", "M104 Sombrero", "Sirius A" };

                float qW = (rightW - 42f) / 3.0f;
                float qH = 22f;
                for (int qi = 0; qi < quickIds.Length; qi++)
                {
                    int row = qi / 3;
                    int col = qi % 3;
                    Rect qRect = new Rect(rightX + 16 + col * (qW + 5), ry + row * (qH + 3), qW, qH);
                    bool isCur = messierCatalog != null && messierCatalog.currentTarget != null && messierCatalog.currentTarget.id == quickIds[qi];
                    if (DrawSleekButton(qRect, quickLabels[qi], isCur, Color.cyan, 10))
                    {
                        if (messierCatalog != null) messierCatalog.SelectTargetById(quickIds[qi]);
                    }
                }
                ry += (qH + 3) * 4 + 4f;

                // Deep-Sky Eyepiece Telemetry Card
                var target = messierCatalog != null ? messierCatalog.currentTarget : null;
                if (target != null)
                {
                    GUI.DrawTexture(new Rect(rightX + 16, ry, rightW - 32, 192), texGlassCard);

                    float eyeX = rightX + 26;
                    float eyeY = ry + 8;

                    GUI.Label(new Rect(eyeX, eyeY, rightW - 52, 20), $"{target.iconGlyph}  {target.id}: {target.commonName.ToUpper()}", statValueGoldStyle);
                    GUI.Label(new Rect(eyeX, eyeY + 18, rightW - 52, 16), $"{target.objectType}  •  Constellation: {target.constellation.ToUpper()} ({target.ngcOrAlt})", statLabelStyle);

                    float gridY = eyeY + 38;
                    GUI.Label(new Rect(eyeX, gridY, rightW - 52, 18), $"COORDINATES: {target.GetFormattedCoordinates()}", statValueStyle);

                    // Alt/Az & Horizon Visibility
                    var (alt, az, isAbove) = messierCatalog.CalculateAltAz(target.raHours, target.decDegrees);
                    string compass = GetCompassDirection(az);
                    string visText = isAbove
                        ? $"🟢 ALT: {alt:+0.0;-0.0}° (High in {compass}) • AZ: {az:000}° [VISIBLE TONIGHT]"
                        : $"🔴 ALT: {alt:+0.0;-0.0}° • AZ: {az:000}° [BELOW HORIZON]";

                    GUI.Label(new Rect(eyeX, gridY + 20, rightW - 52, 18), visText, isAbove ? statValueMintStyle : statLabelStyle);

                    double distKm = target.distanceLy * TravelTimeCalculator.LY_KM;
                    string distStr = TravelTimeCalculator.FormatDistanceSpan(distKm, activeUnitSystem);
                    GUI.Label(new Rect(eyeX, gridY + 38, rightW - 52, 18), $"Dist: {target.distanceLy:N0} LY • {distStr}  •  Mag: m={target.apparentMag:+0.00;-0.00;0.00}", statValueCyanStyle);

                    GUI.Label(new Rect(eyeX, gridY + 58, rightW - 52, 34), target.description, bodyStyle);

                    if (DrawSleekButton(new Rect(eyeX, gridY + 98, rightW - 52, 38), $"🎯 LOCK TELESCOPE ON [{target.id}] (AIM CAMERA)", true, Color.cyan, 11))
                    {
                        if (messierCatalog != null)
                        {
                            messierCatalog.LockTelescopeOnTarget(target);
                        }
                    }
                }
            }

            // =========================================================================
            // 4. BOTTOM MASTER FLIGHT CONSOLE & MISSION COMM RIBBON
            // =========================================================================
            float bottomH = 78f;
            float bottomY = virtualH - bottomH - 12f;

            // Mission Audio Comm Feed Banner (Subtle floating ribbon)
            bool isTour = engine != null && engine.isTourActive;
            string tourTag = isTour ? $"[🚀 GUIDED TOUR — STAGE {engine.tourCurrentStage}/4] " : "";
            string subText = zoom < 1.75f ? "Stage 1: The Solar System. Spanning ~8.33 light-hours across Neptune's orbit." :
                (zoom < 2.75f ? "Stage 2: The Milky Way Galaxy. ~100,000 light-years across with Orion Spur and Sagittarius A*." :
                (zoom < 3.75f ? "Stage 3: The Local Group Cluster. ~10 million light-years encompassing Andromeda and Milky Way." :
                "Stage 4: Cosmic Web & Particle Horizon. ~93 billion light-years to the Cosmic Microwave Background."));

            Rect commRect = new Rect(virtualW * 0.12f, bottomY - 32f, virtualW * 0.76f, 26f);
            GUI.DrawTexture(commRect, texGlass);
            GUI.DrawTexture(new Rect(commRect.x, commRect.y, commRect.width, 1), texBorder);
            GUI.DrawTexture(new Rect(commRect.x, commRect.y + commRect.height - 1, commRect.width, 1), texBorder);
            GUI.Label(new Rect(commRect.x + 8, commRect.y + 2, commRect.width - 16, 22), $"🎙️ FLIGHT AUDIO TELEMETRY: {tourTag}{subText}", subtitleStyle);

            // Master Flight Deck Panel
            Rect cockpitRect = new Rect(14, bottomY, virtualW - 28, bottomH);
            DrawSciFiPanel(cockpitRect, "// FLIGHT.DECK //");

            // Continuous Scale Relativistic Accelerator Slider
            GUI.Label(new Rect(26, bottomY + 12, 220, 16), "COSMIC SCALE ACCELERATOR", statLabelStyle);
            GUI.Label(new Rect(250, bottomY + 12, virtualW * 0.35f, 16), "1.0 [Solar System] ── 2.0 [Milky Way] ── 3.0 [Local Group] ── 4.0 [Cosmic Web]", statLabelStyle);
            float newZoom = GUI.HorizontalSlider(new Rect(26, bottomY + 36, virtualW * 0.38f, 26), zoom, 1.0f, 4.0f);
            if (Mathf.Abs(newZoom - zoom) > 0.005f && engine != null)
            {
                engine.SetZoomDirect(newZoom);
            }

            // Flight Action Buttons
            float fBtnX = virtualW - 680f;
            float fBtnW = 155f;

            string tourBtnText = isTour ? $"🚀 Tour: S{engine.tourCurrentStage}" : "🚀 Guided Tour [T]";
            if (DrawSleekButton(new Rect(fBtnX, bottomY + 18, fBtnW, 46), tourBtnText, isTour, Color.cyan, 11))
            {
                if (engine != null) engine.ToggleTour();
            }

            string pulseBtnText = isPulseActive ? (isPulsePaused ? "▶ Resume Wave" : "⏸ Pause Wave") : "⚡ Light Pulse [SPC]";
            if (DrawSleekButton(new Rect(fBtnX + fBtnW + 8, bottomY + 18, fBtnW + 10, 46), pulseBtnText, isPulseActive, Color.yellow, 11))
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

            string starryBtnBottom = (messierCatalog != null && messierCatalog.isStarryNightActive) ? "🔭 Sky: ON [S]" : "🔭 Starry Sky [S]";
            if (DrawSleekButton(new Rect(fBtnX + (fBtnW + 8) * 2 + 10, bottomY + 18, fBtnW - 10, 46), starryBtnBottom, (messierCatalog != null && messierCatalog.isStarryNightActive), Color.cyan, 11))
            {
                ToggleStarryNight();
            }

            if (DrawSleekButton(new Rect(fBtnX + (fBtnW + 8) * 3, bottomY + 18, 140, 46), "↺ Reset Gyro [R]", false, Color.cyan, 11))
            {
                if (engine != null) engine.ResetCamera();
            }

            // Tactical screen-space markers for Messier objects & major stars when Starry Night is enabled
            if (messierCatalog != null && messierCatalog.isStarryNightActive && mainCam != null)
            {
                foreach (var obj in messierCatalog.Catalog)
                {
                    Vector3 worldPos = messierCatalog.GetWorldPositionOfObject(obj);
                    Vector3 screenPos = mainCam.WorldToScreenPoint(worldPos);

                    if (screenPos.z > 0 && screenPos.x > 30 && screenPos.x < Screen.width - 30 && screenPos.y > 30 && screenPos.y < Screen.height - 30)
                    {
                        float gx = screenPos.x / finalScale;
                        float gy = (Screen.height - screenPos.y) / finalScale;

                        bool isOverLeftPanel = gx < leftW + 30 && gy > leftY && gy < leftY + 450;
                        bool isOverRightPanel = gx > rightX - 20 && gy > leftY && gy < leftY + 480;
                        bool isOverTopBar = gy < 75;
                        bool isOverBottomBar = gy > bottomY - 35;

                        if (!isOverLeftPanel && !isOverRightPanel && !isOverTopBar && !isOverBottomBar)
                        {
                            bool isTarget = (messierCatalog.currentTarget == obj);
                            GUI.DrawTexture(new Rect(gx - 3, gy - 3, 6, 6), isTarget ? texGold : texCyan);
                            GUI.Label(new Rect(gx + 6, gy - 8, 140, 18), $"{obj.iconGlyph} {obj.id}", isTarget ? statValueGoldStyle : hudMicroStyle);
                        }
                    }
                }
            }

            GUI.matrix = oldMatrix;
        }

        private void DrawQuickFocusBar(float virtualW, float leftW, float rightX)
        {
            if (engine == null) return;

            float barX = leftW + 28f;
            float barW = rightX - leftW - 28f;
            if (barW < 360f) return; // Screen too narrow

            float barY = 74f;
            float barH = 50f;
            Rect barRect = new Rect(barX, barY, barW, barH);
            DrawSciFiPanel(barRect, "// TARGET.LOCK //");

            // Subtitle status: Active target name + zoom hint
            string statusStr = $"LOCK: {engine.currentTargetName.ToUpper()}  •  RANGE: {engine.targetDistance:0.0}m  (MOUSE SCROLL: ZOOM IN/OUT  •  R-DRAG: ORBIT 360°)";
            GUI.Label(new Rect(barX + 12, barY + 4, barW - 24, 16), statusStr, reticleHeadingStyle);

            int stage = engine.activeStageIndex;
            float btnY = barY + 22f;
            float btnH = 24f;

            if (stage == 1)
            {
                string[] btnLabels = new string[] { "☀️ SUN", "🌍 EARTH", "🪐 JUPITER", "🪐 SATURN", "🚀 FLAGSHIP", "🛸 SCOUT", "🌌 OVERVIEW" };
                float btnWidth = (barW - 20f) / btnLabels.Length;

                for (int i = 0; i < btnLabels.Length; i++)
                {
                    Rect bRect = new Rect(barX + 10f + i * btnWidth, btnY, btnWidth - 4f, btnH);
                    bool isTarget = (i == 0 && engine.currentTargetName.Contains("Sun")) ||
                                   (i == 1 && engine.currentTargetName.Contains("Earth")) ||
                                   (i == 2 && engine.currentTargetName.Contains("Jupiter")) ||
                                   (i == 3 && engine.currentTargetName.Contains("Saturn")) ||
                                   (i == 4 && engine.currentTargetName.Contains("Flagship")) ||
                                   (i == 5 && engine.currentTargetName.Contains("Scout")) ||
                                   (i == 6 && engine.currentTargetName.Contains("Overview"));

                    if (DrawSleekButton(bRect, btnLabels[i], isTarget, Color.cyan, 10))
                    {
                        switch (i)
                        {
                            case 0: engine.FocusOnSun(); break;
                            case 1: engine.FocusOnEarth(); break;
                            case 2: engine.FocusOnJupiter(); break;
                            case 3: engine.FocusOnSaturn(); break;
                            case 4: engine.FocusOnFlagship(); break;
                            case 5: engine.FocusOnScout(); break;
                            case 6: engine.FocusOnOverview(); break;
                        }
                    }
                }
            }
            else if (stage == 2)
            {
                string[] btnLabels = new string[] { "🌀 SAGITTARIUS A*", "☀️ ORION SPUR (BEACON)", "🌌 MILKY WAY DISK" };
                float btnWidth = (barW - 20f) / btnLabels.Length;

                for (int i = 0; i < btnLabels.Length; i++)
                {
                    Rect bRect = new Rect(barX + 10f + i * btnWidth, btnY, btnWidth - 4f, btnH);
                    bool isTarget = (i == 0 && engine.currentTargetName.Contains("Sagittarius")) ||
                                   (i == 1 && engine.currentTargetName.Contains("Orion")) ||
                                   (i == 2 && engine.currentTargetName.Contains("Milky Way"));

                    if (DrawSleekButton(bRect, btnLabels[i], isTarget, Color.cyan, 10))
                    {
                        switch (i)
                        {
                            case 0: engine.FocusOnSgrA(); break;
                            case 1: engine.FocusOnOrionSpur(); break;
                            case 2: engine.FocusOnMilkyWay(); break;
                        }
                    }
                }
            }
            else if (stage == 3)
            {
                string[] btnLabels = new string[] { "🌀 ANDROMEDA GALAXY (M31)", "🌌 LOCAL GROUP OVERVIEW" };
                float btnWidth = (barW - 20f) / btnLabels.Length;

                for (int i = 0; i < btnLabels.Length; i++)
                {
                    Rect bRect = new Rect(barX + 10f + i * btnWidth, btnY, btnWidth - 4f, btnH);
                    bool isTarget = (i == 0 && engine.currentTargetName.Contains("Andromeda")) ||
                                   (i == 1 && engine.currentTargetName.Contains("Local Group"));

                    if (DrawSleekButton(bRect, btnLabels[i], isTarget, Color.cyan, 10))
                    {
                        switch (i)
                        {
                            case 0: engine.FocusOnAndromeda(); break;
                            case 1: engine.FocusOnLocalGroup(); break;
                        }
                    }
                }
            }
            else if (stage == 4)
            {
                string[] btnLabels = new string[] { "🕸️ COSMIC WEB FILAMENTS", "🌐 CMB HORIZON SPHERE" };
                float btnWidth = (barW - 20f) / btnLabels.Length;

                for (int i = 0; i < btnLabels.Length; i++)
                {
                    Rect bRect = new Rect(barX + 10f + i * btnWidth, btnY, btnWidth - 4f, btnH);
                    bool isTarget = (i == 0 && engine.currentTargetName.Contains("Filaments")) ||
                                   (i == 1 && engine.currentTargetName.Contains("CMB"));

                    if (DrawSleekButton(bRect, btnLabels[i], isTarget, Color.cyan, 10))
                    {
                        switch (i)
                        {
                            case 0: engine.FocusOnCosmicFilaments(); break;
                            case 1: engine.FocusOnCMB(); break;
                        }
                    }
                }
            }
        }

        private void DrawCompRow(float x, float y, float w, float h, string name, string time, Color col)
        {
            GUI.DrawTexture(new Rect(x, y, w, h), texGlassCard);
            GUI.DrawTexture(new Rect(x, y, 3, h), col == Color.white ? texBorder : (col == new Color(0.22f, 0.74f, 0.97f) ? texCyan : texGold));
            GUI.Label(new Rect(x + 12, y + (h - 20) / 2.0f, w * 0.60f, 20), name, compLabelStyle);
            compValueStyle.normal.textColor = col;
            GUI.Label(new Rect(x + w * 0.60f, y + (h - 20) / 2.0f, w * 0.38f, 20), time, compValueStyle);
        }

        private void SetScale(float newScale)
        {
            userScale = Mathf.Clamp(newScale, 0.9f, 1.8f);
            PlayerPrefs.SetFloat("Cosmic_A11y_Scale", userScale);
            if (audioController != null) audioController.PlaySoftChime();
        }

        private string GetCompassDirection(float azDeg)
        {
            azDeg = (azDeg % 360f + 360f) % 360f;
            if (azDeg >= 337.5f || azDeg < 22.5f) return "N";
            if (azDeg >= 22.5f && azDeg < 67.5f) return "NE";
            if (azDeg >= 67.5f && azDeg < 112.5f) return "E";
            if (azDeg >= 112.5f && azDeg < 157.5f) return "SE";
            if (azDeg >= 157.5f && azDeg < 202.5f) return "S";
            if (azDeg >= 202.5f && azDeg < 247.5f) return "SW";
            if (azDeg >= 247.5f && azDeg < 292.5f) return "W";
            return "NW";
        }
    }
}

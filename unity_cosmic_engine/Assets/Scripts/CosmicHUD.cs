using System;
using UnityEngine;

namespace CosmicZoom
{
    /// <summary>
    /// Tactile Astro-Avionics Kinetic Terminal System (Cockpit Flight Deck Interface).
    /// Features:
    /// - Monolithic Aerospace Chassis Architecture: Sweeping lateral structural spine,
    ///   chamfered corner caps, and interconnected telemetry bays (Zero website-like floating cards).
    /// - Tactile Segmented Chiclets: Precision two-tone avionics touchpads with alphanumeric index badges.
    /// - Comprehensive Unit System: Metric (KM), Imperial (Miles), and Dual / Both Systems (Hotkey: U).
    /// - Canopy Glass Flight Reticle: Boresight crosshairs, targeting brackets, pitch ladder, and relativistic vector telemetry.
    /// - Dedicated Light Transit Pulse Engine & Speed-of-Light Simulator with segmented bus pip meter.
    /// - Dynamic Craft Propulsion Benchmark comparisons formatted in KM/s, MPH, or Dual speeds.
    /// - Charles Messier Deep-Sky Observatory & 100% Privacy-Preserving Local Ephemeris Calculator.
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
        public float userScale = 1.25f; // Default to 125% for high readability
        public bool isHighContrast = false;

        // Right Dock Tab: 0 = Light Transit Pulse (c), 1 = Spacecraft Benchmarks, 2 = Messier & Starry Sky
        private int activeRightTab = 0;
        private bool showLocationPicker = false;

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

        // Specialized Astro-Avionics Terminal Styles
        private GUIStyle bayBannerStyle;
        private GUIStyle chicletCodeStyle;
        private GUIStyle chicletLabelNormalStyle;
        private GUIStyle chicletLabelActiveStyle;

        // Solid Monolithic Terminal Textures
        private Texture2D texObsidian;
        private Texture2D texPlateDark;
        private Texture2D texPlateMid;
        private Texture2D texAmber;
        private Texture2D texAmberDim;
        private Texture2D texCyan;
        private Texture2D texCyanDim;
        private Texture2D texTeal;
        private Texture2D texTealDim;
        private Texture2D texPurple;
        private Texture2D texCrimson;
        private Texture2D texWhite;
        private Texture2D strutTex;

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
                userScale = PlayerPrefs.GetFloat("Cosmic_A11y_Scale", 1.25f);
            }
            if (PlayerPrefs.HasKey("Cosmic_A11y_Contrast"))
            {
                isHighContrast = PlayerPrefs.GetInt("Cosmic_A11y_Contrast", 0) == 1;
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.S))
            {
                ToggleStarryNight();
            }
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
                messierCatalog.isStarryNightActive = !messierCatalog.isStarryNightActive;
                if (messierCatalog.isStarryNightActive)
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
                else
                {
                    // Reset camera orientation back to flight deck stage view
                    if (engine != null)
                    {
                        engine.JumpToStage(engine.activeStageIndex);
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

            // Solid Monolithic Terminal Textures (Original Astro-Tactical Palette)
            texObsidian = MakeColorTexture(2, 2, new Color(0.025f, 0.04f, 0.07f, 0.96f)); // Deep obsidian carbon chassis
            texPlateDark = MakeColorTexture(2, 2, new Color(0.045f, 0.075f, 0.13f, 0.94f)); // Chassis sub-plate
            texPlateMid = MakeColorTexture(2, 2, new Color(0.08f, 0.13f, 0.22f, 0.92f)); // Mid-slate tactile plate
            texAmber = MakeColorTexture(2, 2, new Color(0.98f, 0.62f, 0.08f, 1.0f)); // Cadmium solar amber
            texAmberDim = MakeColorTexture(2, 2, new Color(0.48f, 0.25f, 0.04f, 0.85f));
            texCyan = MakeColorTexture(2, 2, new Color(0.04f, 0.74f, 0.88f, 1.0f)); // Electric plasma cyan
            texCyanDim = MakeColorTexture(2, 2, new Color(0.03f, 0.35f, 0.45f, 0.85f));
            texTeal = MakeColorTexture(2, 2, new Color(0.06f, 0.76f, 0.52f, 1.0f)); // Hyper emerald teal
            texTealDim = MakeColorTexture(2, 2, new Color(0.02f, 0.35f, 0.24f, 0.85f));
            texPurple = MakeColorTexture(2, 2, new Color(0.66f, 0.35f, 0.96f, 1.0f)); // Deep iris violet
            texCrimson = MakeColorTexture(2, 2, new Color(0.94f, 0.26f, 0.26f, 1.0f)); // Tactical alert coral
            texWhite = MakeColorTexture(2, 2, Color.white);
            strutTex = MakeColorTexture(2, 2, new Color(0.03f, 0.06f, 0.12f, 0.88f));

            shipIdStyle = new GUIStyle
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.98f, 0.62f, 0.08f) }
            };

            headerTitleStyle = new GUIStyle
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            headerSubStyle = new GUIStyle
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.04f, 0.74f, 0.88f) }
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
                normal = { textColor = new Color(0.98f, 0.62f, 0.08f) }
            };

            statValueCyanStyle = new GUIStyle
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.04f, 0.74f, 0.88f) }
            };

            bigValueStyle = new GUIStyle
            {
                fontSize = 26,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.04f, 0.74f, 0.88f) }
            };

            pulseStatusStyle = new GUIStyle
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                wordWrap = true,
                normal = { textColor = new Color(0.98f, 0.62f, 0.08f) }
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
                normal = { background = texPlateMid, textColor = new Color(0.9f, 0.95f, 1.0f) },
                hover = { background = texCyan, textColor = Color.black }
            };

            activeButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { background = texCyan, textColor = Color.black }
            };

            tabButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { background = texPlateDark, textColor = new Color(0.70f, 0.80f, 0.92f) }
            };

            activeTabStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { background = texAmber, textColor = Color.black }
            };

            a11yBtnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { background = texPlateMid, textColor = new Color(0.98f, 0.62f, 0.08f) }
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
                normal = { textColor = new Color(0.04f, 0.74f, 0.88f, 0.90f) }
            };

            reticleSubStyle = new GUIStyle
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.98f, 0.62f, 0.08f, 0.85f) }
            };

            hudMicroStyle = new GUIStyle
            {
                fontSize = 9,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.LowerRight,
                normal = { textColor = new Color(0.04f, 0.74f, 0.88f, 0.65f) }
            };

            bayBannerStyle = new GUIStyle
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.03f, 0.05f, 0.08f) }
            };

            chicletCodeStyle = new GUIStyle
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.03f, 0.05f, 0.08f) }
            };

            chicletLabelNormalStyle = new GUIStyle
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                normal = { textColor = new Color(0.92f, 0.96f, 1.0f) }
            };

            chicletLabelActiveStyle = new GUIStyle
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                normal = { textColor = new Color(0.03f, 0.05f, 0.08f) }
            };

            stylesInitialized = true;
        }

        /// <summary>
        /// Renders an architectural avionics chassis bay with heavy structural header and spine bar.
        /// </summary>
        private void DrawChassisBay(Rect r, string bayCode, string bayTitle, Color accentCol)
        {
            Texture2D accentTex = accentCol == Color.cyan ? texCyan : (accentCol == Color.yellow ? texAmber : texPurple);

            // 1. Monolithic obsidian plate body
            GUI.DrawTexture(r, isHighContrast ? texObsidian : texPlateDark);

            // 2. Thick Top Header Accent Banner (24px solid bar with dark cutout text)
            float bannerH = 24f;
            Rect bannerRect = new Rect(r.x, r.y, r.width, bannerH);
            GUI.DrawTexture(bannerRect, accentTex);
            GUI.Label(new Rect(bannerRect.x + 10, bannerRect.y + 2, bannerRect.width - 20, 20), $"[ {bayCode} ]  {bayTitle.ToUpper()}", bayBannerStyle);

            // 3. Thick lateral spine on left edge (8px solid accent bar)
            GUI.DrawTexture(new Rect(r.x, r.y, 8f, r.height), accentTex);

            // 4. Subtle bottom edge accent rule
            GUI.DrawTexture(new Rect(r.x, r.y + r.height - 3f, r.width, 3f), accentTex);

            // 5. Corner index tick
            GUI.DrawTexture(new Rect(r.x + r.width - 16f, r.y + r.height - 8f, 16f, 8f), accentTex);
        }

        /// <summary>
        /// Renders an authentic two-segment tactile chiclet button (code badge + label surface).
        /// When active, the entire surface lights up in solid radiant saturation with dark crisp text.
        /// </summary>
        private bool DrawTactileChiclet(Rect r, string code, string label, bool isActive, Color col, int fontSize = 12)
        {
            Texture2D accentTex = col == Color.cyan ? texCyan : (col == Color.yellow ? texAmber : (col == Color.green ? texTeal : texPurple));
            Texture2D bodyTex = isActive ? accentTex : texPlateMid;

            // Outer surface
            GUI.DrawTexture(r, bodyTex);

            // Left Code Badge (solid saturated accent block)
            float badgeW = Mathf.Clamp(r.width * 0.28f, 32f, 58f);
            Rect badgeRect = new Rect(r.x, r.y, badgeW, r.height);
            GUI.DrawTexture(badgeRect, isActive ? texWhite : accentTex);

            chicletCodeStyle.fontSize = Mathf.Clamp(fontSize - 1, 9, 13);
            GUI.Label(badgeRect, code, chicletCodeStyle);

            GUIStyle labelStyle = isActive ? chicletLabelActiveStyle : chicletLabelNormalStyle;
            labelStyle.fontSize = fontSize;
            Rect textRect = new Rect(r.x + badgeW + 4, r.y, r.width - badgeW - 8, r.height);
            GUI.Label(textRect, label, labelStyle);

            // Click detection via invisible GUI button
            bool clicked = GUI.Button(r, GUIContent.none, GUIStyle.none);
            if (clicked && audioController != null)
            {
                audioController.PlaySoftChime();
            }
            return clicked;
        }

        /// <summary>
        /// Renders a segmented diagnostic energy/progress bus meter.
        /// </summary>
        private void DrawSegmentedBusMeter(Rect r, float progress, Color col, int segments = 16)
        {
            GUI.DrawTexture(r, texPlateDark);
            float gap = 2f;
            float pipW = (r.width - gap * (segments - 1)) / segments;
            int filled = Mathf.RoundToInt(Mathf.Clamp01(progress) * segments);

            Texture2D fillTex = col == Color.cyan ? texCyan : (col == Color.yellow ? texAmber : texTeal);

            for (int i = 0; i < segments; i++)
            {
                Rect pipRect = new Rect(r.x + i * (pipW + gap), r.y, pipW, r.height);
                GUI.DrawTexture(pipRect, (i < filled) ? fillTex : texPlateMid);
            }
        }

        /// <summary>
        /// Master Left Structural Spine: Connects Top Header and Bottom Deck into an integrated terminal chassis.
        /// </summary>
        private void DrawLeftStructuralSpine(float virtualW, float virtualH)
        {
            float spineX = 6f;
            float spineW = 14f;

            // Solid continuous vertical column
            GUI.DrawTexture(new Rect(spineX, 10f, spineW, virtualH - 20f), texAmber);

            // Top Sweeping Elbow Cap
            GUI.DrawTexture(new Rect(spineX, 10f, 95f, 24f), texAmber);
            GUI.Label(new Rect(spineX + 16f, 12f, 75f, 20f), "SYS-704", chicletCodeStyle);

            // Bottom Sweeping Elbow Cap
            GUI.DrawTexture(new Rect(spineX, virtualH - 34f, 95f, 24f), texAmber);
            GUI.Label(new Rect(spineX + 16f, virtualH - 32f, 75f, 20f), "DECK-01", chicletCodeStyle);

            // Segmented diagnostic pips running down the left spine
            for (int i = 0; i < 18; i++)
            {
                float pipY = 110f + i * 26f;
                if (pipY < virtualH - 140f)
                {
                    Texture2D pTex = (i % 3 == 0) ? texCyan : ((i % 3 == 1) ? texTeal : texAmberDim);
                    GUI.DrawTexture(new Rect(spineX + 2f, pipY, 10f, 6f), pTex);
                }
            }
        }

        private void DrawCenterFlightHUD(float virtualW, float virtualH)
        {
            float cx = virtualW * 0.5f;
            float cy = virtualH * 0.46f;

            // Center Boresight Crosshair
            float crossSize = 18f;
            GUI.DrawTexture(new Rect(cx - crossSize * 0.5f, cy - 1f, crossSize, 2f), texCyan);
            GUI.DrawTexture(new Rect(cx - 1f, cy - crossSize * 0.5f, 2f, crossSize), texCyan);

            // Tactical Targeting Brackets [   ]
            float boxR = 42f;
            float tick = 12f;
            // Top-left
            GUI.DrawTexture(new Rect(cx - boxR, cy - boxR, tick, 2f), texCyan);
            GUI.DrawTexture(new Rect(cx - boxR, cy - boxR, 2f, tick), texCyan);
            // Top-right
            GUI.DrawTexture(new Rect(cx + boxR - tick, cy - boxR, tick, 2f), texCyan);
            GUI.DrawTexture(new Rect(cx + boxR - 2f, cy - boxR, 2f, tick), texCyan);
            // Bottom-left
            GUI.DrawTexture(new Rect(cx - boxR, cy + boxR - 2f, tick, 2f), texCyan);
            GUI.DrawTexture(new Rect(cx - boxR, cy + boxR - tick, 2f, tick), texCyan);
            // Bottom-right
            GUI.DrawTexture(new Rect(cx + boxR - tick, cy + boxR - 2f, tick, 2f), texCyan);
            GUI.DrawTexture(new Rect(cx + boxR - 2f, cy + boxR - tick, 2f, tick), texCyan);

            // Horizon Pitch Ladder
            float pitchW = 64f;
            GUI.DrawTexture(new Rect(cx - pitchW * 0.5f, cy - 55f, pitchW, 1.5f), texCyanDim);
            GUI.DrawTexture(new Rect(cx - pitchW * 0.5f, cy + 55f, pitchW, 1.5f), texCyanDim);

            // Flight Vector & Relativistic Lorentz Telemetry (No warp references)
            GUI.Label(new Rect(cx - 250, cy - 82, 500, 20), "◈ SENSOR TRACK: NOMINAL // VECTOR: [0.00, +0.45, +1.00] ◈", reticleHeadingStyle);
            GUI.Label(new Rect(cx - 250, cy + 62, 500, 20), "RELATIVISTIC LORENTZ FACTOR: γ = 1.0000 // INERTIAL MATRIX: NOMINAL", reticleSubStyle);

            // Cockpit Canopy Corner Struts
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

            // Keyboard shortcut: Key S toggles Starry Night observation mode
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.S)
            {
                ToggleStarryNight();
                Event.current.Use();
            }

            float dpiScale = Screen.dpi > 0 ? (Screen.dpi / 96.0f) : 1.0f;
            dpiScale = Mathf.Clamp(dpiScale, 1.0f, 2.0f);
            float finalScale = dpiScale * userScale;

            Matrix4x4 oldMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(finalScale, finalScale, 1.0f));

            float virtualW = Screen.width / finalScale;
            float virtualH = Screen.height / finalScale;

            cardStyle = new GUIStyle(GUI.skin.box) { normal = { background = isHighContrast ? texObsidian : texPlateDark } };

            float zoom = engine != null ? engine.currentZoom : 1.0f;
            double spanKm = TravelTimeCalculator.GetSpanKmFromZoom(zoom);
            double lightTransitSecs = TravelTimeCalculator.GetLightTransitSeconds(spanKm);

            // =========================================================================
            // 0. STRUCTURAL CHASSIS ARCHITECTURE & FLIGHT RETICLE
            // =========================================================================
            DrawLeftStructuralSpine(virtualW, virtualH);
            DrawCenterFlightHUD(virtualW, virtualH);

            // =========================================================================
            // 1. TOP AVIONICS HELM CONSOLE (THE TELEMETRY ARCH)
            // =========================================================================
            float headerX = 26f;
            float headerW = virtualW - 40f;
            Rect headerRect = new Rect(headerX, 10f, headerW, 82f);
            DrawChassisBay(headerRect, "AVN-01", "ASTRO-AVIONICS KINETIC MATRIX // FLIGHT TELEMETRY", Color.yellow);

            // Starship Designation (No "USS")
            GUI.Label(new Rect(headerX + 16, 36, 360, 24), "ASTRONAUTICA DEEP SURVEY PLATFORM // CONSOLE 01", shipIdStyle);
            GUI.Label(new Rect(headerX + 16, 58, 360, 20), "RELATIVISTIC COMPASS & QUANTUM HORIZON SUITE", headerSubStyle);

            // Top Action Chiclets (Anchored to Right)
            float actionX = virtualW - 745;

            // Starry Night & Messier Catalog Mode Toggle (Hotkey: S)
            bool isStarry = messierCatalog != null && messierCatalog.isStarryNightActive;
            if (DrawTactileChiclet(new Rect(actionX, 32, 155, 48), "SKY", isStarry ? "STARRY SKY [ON]" : "STARRY SKY (S)", isStarry, Color.cyan))
            {
                ToggleStarryNight();
            }

            // Unit Switcher: KM / MILES / DUAL (Hotkey: U)
            string unitLabel = activeUnitSystem switch
            {
                UnitSystem.Miles => "UNITS: [MI] (U)",
                UnitSystem.Dual => "UNITS: [DUAL] (U)",
                _ => "UNITS: [KM] (U)"
            };
            if (DrawTactileChiclet(new Rect(actionX + 162, 32, 150, 48), "UNIT", unitLabel, activeUnitSystem == UnitSystem.Dual, Color.yellow))
            {
                CycleUnitSystem();
            }

            // Low-Vision Scale Controls
            if (DrawTactileChiclet(new Rect(actionX + 318, 32, 42, 48), "A−", "", false, Color.cyan)) SetScale(userScale - 0.15f);
            if (DrawTactileChiclet(new Rect(actionX + 364, 32, 42, 48), "A+", "", false, Color.cyan)) SetScale(userScale + 0.15f);
            if (DrawTactileChiclet(new Rect(actionX + 410, 32, 60, 48), "%", Mathf.RoundToInt(userScale * 100) + "%", false, Color.cyan))
            {
                float nextScale = userScale >= 1.75f ? 1.0f : (userScale < 1.25f ? 1.25f : (userScale < 1.5f ? 1.5f : 1.75f));
                SetScale(nextScale);
            }

            // High-Contrast / Color HUD Toggle
            string contrastText = isHighContrast ? "NIGHT" : "COLOR";
            if (DrawTactileChiclet(new Rect(actionX + 476, 32, 98, 48), "DISP", contrastText, isHighContrast, Color.yellow))
            {
                isHighContrast = !isHighContrast;
                PlayerPrefs.SetInt("Cosmic_A11y_Contrast", isHighContrast ? 1 : 0);
            }

            // Audio Mute Toggle
            bool isMuted = audioController != null && audioController.IsAudioMuted;
            if (DrawTactileChiclet(new Rect(actionX + 580, 32, 78, 48), "SND", isMuted ? "OFF" : "ON", !isMuted, Color.cyan))
            {
                if (audioController != null) audioController.ToggleAudioMute();
            }

            // Mission Audio Narrator Toggle
            bool isNarrOn = audioController != null && audioController.IsNarratorAutoPlay;
            if (DrawTactileChiclet(new Rect(actionX + 664, 32, 70, 48), "VOX", isNarrOn ? "ON" : "OFF", isNarrOn, Color.yellow))
            {
                if (audioController != null) audioController.ToggleNarrator();
            }

            // Middle Telemetry Data Blocks (Fits between title and switchpad)
            float middleSpace = actionX - (headerX + 380);
            if (middleSpace > 240)
            {
                float pillX = headerX + 375;
                // Active Domain Block
                GUI.DrawTexture(new Rect(pillX, 32, 160, 48), texPlateMid);
                GUI.DrawTexture(new Rect(pillX, 32, 5, 48), texCyan);
                GUI.Label(new Rect(pillX + 12, 34, 140, 16), "ACTIVE DOMAIN", statLabelStyle);
                string domainName = zoom < 1.75f ? "SOLAR SYSTEM" : (zoom < 2.75f ? "MILKY WAY" : (zoom < 3.75f ? "LOCAL GROUP" : "COSMIC WEB"));
                GUI.Label(new Rect(pillX + 12, 52, 140, 24), domainName, statValueCyanStyle);
                pillX += 168;

                // Physical FOV Span Block
                float spanCellW = Mathf.Min(middleSpace - 175, 230);
                GUI.DrawTexture(new Rect(pillX, 32, spanCellW, 48), texPlateMid);
                GUI.DrawTexture(new Rect(pillX, 32, 5, 48), texAmber);
                GUI.Label(new Rect(pillX + 12, 34, spanCellW - 20, 16), "PHYSICAL FOV SPAN", statLabelStyle);
                GUI.Label(new Rect(pillX + 12, 52, spanCellW - 20, 24), TravelTimeCalculator.FormatSpan(spanKm, activeUnitSystem), statValueGoldStyle);
            }

            // =========================================================================
            // 2. LEFT COMMAND BAY: SECTOR SCALE KINETICS (BAY-01)
            // =========================================================================
            float leftW = 325;
            float leftY = 100;
            Rect leftRect = new Rect(headerX, leftY, leftW, 475);
            DrawChassisBay(leftRect, "BAY-01", "SECTOR SCALE KINETICS", Color.yellow);

            GUI.Label(new Rect(headerX + 16, leftY + 28, 280, 18), "DESTINATION LOCK [KEYS 1 - 4]", statLabelStyle);

            float stageBtnY = leftY + 50;
            for (int s = 1; s <= 4; s++)
            {
                bool isActive = (engine != null && engine.activeStageIndex == s);
                string sCode = $"SEC-0{s}";
                string sName = s switch
                {
                    1 => activeUnitSystem switch
                    {
                        UnitSystem.Miles => "Solar System (~8.33 LH)\nSun to Neptune (5.59B mi)",
                        UnitSystem.Dual => "Solar System (~8.33 LH)\n9.0B km [5.59B mi] Span",
                        _ => "Solar System (~8.33 LH)\nSun to Neptune (8.996B km)"
                    },
                    2 => activeUnitSystem switch
                    {
                        UnitSystem.Miles => "Milky Way Galaxy (~100k LY)\nSpiral Arms (5.88e17 mi)",
                        UnitSystem.Dual => "Milky Way Galaxy (~100k LY)\n9.46e17 km [5.88e17 mi]",
                        _ => "Milky Way Galaxy (~100k LY)\nSpiral Arms (9.461e17 km)"
                    },
                    3 => activeUnitSystem switch
                    {
                        UnitSystem.Miles => "Local Group (~10 MLY)\nAndromeda (5.88e19 mi)",
                        UnitSystem.Dual => "Local Group (~10 MLY)\n9.46e19 km [5.88e19 mi]",
                        _ => "Local Group (~10 MLY)\nAndromeda (9.461e19 km)"
                    },
                    4 => activeUnitSystem switch
                    {
                        UnitSystem.Miles => "Cosmic Web & CMB (~93 GLY)\nCMB Horizon (5.47e23 mi)",
                        UnitSystem.Dual => "Cosmic Web & CMB (~93 GLY)\n8.80e23 km [5.47e23 mi]",
                        _ => "Cosmic Web & CMB (~93 GLY)\nCMB Horizon (8.798e23 km)"
                    },
                    _ => ""
                };

                if (DrawTactileChiclet(new Rect(headerX + 14, stageBtnY, leftW - 24, 88), sCode, sName, isActive, Color.yellow, 12))
                {
                    if (engine != null) engine.JumpToStage(s);
                }
                stageBtnY += 94;
            }

            // Diagnostic Telemetry Strip at Bottom of Bay 01
            float diagY = leftY + 434;
            GUI.Label(new Rect(headerX + 16, diagY, leftW - 32, 16), "BUS-A: 99.8% // GYRO: LOCKED // DRIFT: 0.000", hudMicroStyle);
            DrawSegmentedBusMeter(new Rect(headerX + 14, diagY + 18, leftW - 28, 12), 0.94f, Color.cyan, 18);

            // =========================================================================
            // 3. RIGHT TACTICAL STATION: RELATIVISTIC TELEMETRY & MESSIER SKY (BAY-02)
            // =========================================================================
            float rightW = 455;
            float rightX = virtualW - rightW - 14;
            Rect rightRect = new Rect(rightX, leftY, rightW, 475);
            DrawChassisBay(rightRect, "BAY-02", "SENSOR TELEMETRY & CELESTIAL TARGETING", Color.cyan);

            // Tactical Tab Selector Chiclets
            float tabW = (rightW - 30) / 3.0f;
            float tabY = leftY + 28;
            if (DrawTactileChiclet(new Rect(rightX + 12, tabY, tabW - 3, 34), "01", "Transit (c)", activeRightTab == 0, Color.cyan))
            {
                activeRightTab = 0;
            }
            if (DrawTactileChiclet(new Rect(rightX + 12 + tabW, tabY, tabW - 3, 34), "02", "Benchmarks", activeRightTab == 1, Color.cyan))
            {
                activeRightTab = 1;
            }
            if (DrawTactileChiclet(new Rect(rightX + 12 + tabW * 2, tabY, tabW - 3, 34), "03", "Messier Sky", activeRightTab == 2, Color.cyan))
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
                float ry = leftY + 68;

                // Section 1: Universal Light Transit Duration Card
                GUI.DrawTexture(new Rect(rightX + 12, ry, rightW - 24, 102), texPlateMid);
                GUI.DrawTexture(new Rect(rightX + 12, ry, 6, 102), texCyan);
                GUI.Label(new Rect(rightX + 26, ry + 6, rightW - 48, 18), "TIME FOR PHOTON (1.0c) TO TRANSIT ACROSS THIS SCALE:", statLabelStyle);
                GUI.Label(new Rect(rightX + 26, ry + 24, rightW - 48, 38), TravelTimeCalculator.FormatTime(lightTransitSecs), bigValueStyle);
                GUI.Label(new Rect(rightX + 26, ry + 68, rightW - 48, 22), "Scale Span: " + TravelTimeCalculator.FormatSpan(spanKm, activeUnitSystem), statLabelStyle);

                ry += 112;

                // Section 2: Active Transit Pulse Interactive Card
                GUI.DrawTexture(new Rect(rightX + 12, ry, rightW - 24, 280), texPlateMid);
                GUI.DrawTexture(new Rect(rightX + 12, ry, 6, 280), texAmber);

                string speedLabel = activeUnitSystem switch
                {
                    UnitSystem.Miles => "SPEED c = 186,282 mi/s",
                    UnitSystem.Dual => "SPEED c = 299,792 km/s [186,282 mi/s]",
                    _ => "SPEED c = 299,792 km/s"
                };
                GUI.Label(new Rect(rightX + 26, ry + 8, rightW - 48, 22), $"⚡ PHOTON TRANSIT COMPUTER ({speedLabel})", headerTitleStyle);

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
                    statusMsg = "READY: Engage 'Discharge Pulse' to launch relativistic wave.";
                }
                GUI.Label(new Rect(rightX + 26, ry + 32, rightW - 48, 44), statusMsg, pulseStatusStyle);

                // Segmented Progress Bar
                DrawSegmentedBusMeter(new Rect(rightX + 26, ry + 82, rightW - 52, 22), pulseProgress, Color.cyan, 24);
                GUI.Label(new Rect(rightX + 26, ry + 108, rightW - 52, 18), $"PROGRESS: {Mathf.RoundToInt(pulseProgress * 100)}% TRAVERSED", statLabelStyle);

                // Simulation Speed Regulation Chiclets
                float speedY = ry + 134;
                GUI.Label(new Rect(rightX + 26, speedY, 200, 20), "SIMULATION TIME FACTOR:", statLabelStyle);
                float curSpeed = pulseEmitter != null ? pulseEmitter.SpeedMultiplier : 1.0f;

                float spdW = (rightW - 56) / 3.0f;
                if (DrawTactileChiclet(new Rect(rightX + 26, speedY + 22, spdW - 4, 34), "0.25x", "Slow-Mo", Mathf.Approximately(curSpeed, 0.25f), Color.yellow))
                {
                    if (pulseEmitter != null) pulseEmitter.SetSpeedMultiplier(0.25f);
                }
                if (DrawTactileChiclet(new Rect(rightX + 26 + spdW, speedY + 22, spdW - 4, 34), "0.50x", "Steady", Mathf.Approximately(curSpeed, 0.50f), Color.yellow))
                {
                    if (pulseEmitter != null) pulseEmitter.SetSpeedMultiplier(0.50f);
                }
                if (DrawTactileChiclet(new Rect(rightX + 26 + spdW * 2, speedY + 22, spdW - 4, 34), "1.00x", "Normal", Mathf.Approximately(curSpeed, 1.0f), Color.yellow))
                {
                    if (pulseEmitter != null) pulseEmitter.SetSpeedMultiplier(1.0f);
                }

                // Playback Action Chiclets
                float ctrlY = ry + 204;
                float ctrlW = (rightW - 58) / 2.0f;
                string pauseText = isPulsePaused ? "Resume Transit" : "Pause Transit";
                if (DrawTactileChiclet(new Rect(rightX + 26, ctrlY, ctrlW, 58), isPulsePaused ? "▶" : "⏸", pauseText, isPulseActive, Color.cyan, 13))
                {
                    if (pulseEmitter != null) pulseEmitter.TogglePause();
                }
                string dischargeText = isPulseActive ? "Replay Pulse" : "Discharge Pulse";
                if (DrawTactileChiclet(new Rect(rightX + 32 + ctrlW, ctrlY, ctrlW, 58), "⚡", dischargeText, isPulseActive, Color.yellow, 13))
                {
                    if (engine != null) engine.FirePulse();
                }
            }
            else if (activeRightTab == 1)
            {
                // -------------------------------------------------------------
                // TAB 1: CRAFT PROPULSION BENCHMARKS (METRIC / IMPERIAL / DUAL)
                // -------------------------------------------------------------
                float ry = leftY + 68;
                GUI.Label(new Rect(rightX + 20, ry, rightW - 40, 22), "CRAFT TRAVEL DURATION ACROSS THIS SCALE:", headerTitleStyle);
                GUI.Label(new Rect(rightX + 20, ry + 24, rightW - 40, 18), "Distance: " + TravelTimeCalculator.FormatSpan(spanKm, activeUnitSystem), statLabelStyle);

                double tRelativistic = spanKm / (0.1 * 299792.458);
                double tParker = spanKm / 192.0;
                double tVoyager = spanKm / 17.0;
                double tJetliner = spanKm / 0.25;

                float compY = ry + 48;
                float rowH = 46;
                DrawCompRow(rightX + 14, compY, rightW - 28, rowH, TravelTimeCalculator.FormatSpeed(TravelTimeCalculator.SPEED_PHOTON, activeUnitSystem, "photon"), TravelTimeCalculator.FormatTime(lightTransitSecs), new Color(0.22f, 0.85f, 1.0f)); compY += rowH + 8;
                DrawCompRow(rightX + 14, compY, rightW - 28, rowH, TravelTimeCalculator.FormatSpeed(TravelTimeCalculator.SPEED_RELATIVISTIC, activeUnitSystem, "relativistic"), TravelTimeCalculator.FormatTime(tRelativistic), new Color(0.95f, 0.90f, 0.40f)); compY += rowH + 8;
                DrawCompRow(rightX + 14, compY, rightW - 28, rowH, TravelTimeCalculator.FormatSpeed(TravelTimeCalculator.SPEED_PARKER_SOLAR_PROBE, activeUnitSystem, "parker"), TravelTimeCalculator.FormatTime(tParker), Color.white); compY += rowH + 8;
                DrawCompRow(rightX + 14, compY, rightW - 28, rowH, TravelTimeCalculator.FormatSpeed(TravelTimeCalculator.SPEED_VOYAGER_1, activeUnitSystem, "voyager"), TravelTimeCalculator.FormatTime(tVoyager), Color.white); compY += rowH + 8;
                DrawCompRow(rightX + 14, compY, rightW - 28, rowH, TravelTimeCalculator.FormatSpeed(TravelTimeCalculator.SPEED_JETLINER, activeUnitSystem, "jetliner"), TravelTimeCalculator.FormatTime(tJetliner), new Color(0.85f, 0.85f, 0.85f));
            }
            else
            {
                // -------------------------------------------------------------
                // TAB 2: STARRY NIGHT & CHARLES MESSIER DEEP-SKY OBSERVATORY
                // -------------------------------------------------------------
                float ry = leftY + 68;

                if (messierCatalog == null) messierCatalog = CelestialMessierCatalog.Instance ?? FindAnyObjectByType<CelestialMessierCatalog>();

                // Observer Location Banner with 100% Offline Privacy Guarantee
                GUI.DrawTexture(new Rect(rightX + 12, ry, rightW - 24, 46), texPlateMid);
                GUI.DrawTexture(new Rect(rightX + 12, ry, 6, 46), texAmber);

                string obsName = messierCatalog != null ? messierCatalog.currentObserver.locationName : "Mauna Kea Observatory";
                float obsLat = messierCatalog != null ? messierCatalog.currentObserver.latitude : 19.82f;
                float obsLon = messierCatalog != null ? messierCatalog.currentObserver.longitude : -155.47f;
                string latSign = obsLat >= 0 ? $"{obsLat:0.0}°N" : $"{-obsLat:0.0}°S";
                string lonSign = obsLon >= 0 ? $"{obsLon:0.0}°E" : $"{-obsLon:0.0}°W";

                GUI.Label(new Rect(rightX + 24, ry + 4, rightW - 170, 20), $"📍 OBSERVER: {obsName.ToUpper()}", statValueGoldStyle);
                GUI.Label(new Rect(rightX + 24, ry + 24, rightW - 170, 18), $"🔒 {latSign}, {lonSign} • 100% LOCAL PRIVACY (ZERO NETWORK)", hudMicroStyle);

                if (DrawTactileChiclet(new Rect(rightX + rightW - 142, ry + 6, 126, 34), "LOC", showLocationPicker ? "Done [Close]" : "Set Location", showLocationPicker, Color.yellow, 11))
                {
                    showLocationPicker = !showLocationPicker;
                }
                ry += 50;

                if (showLocationPicker)
                {
                    // Location Preset Drawer (No GPS / No Remote API needed)
                    GUI.DrawTexture(new Rect(rightX + 12, ry, rightW - 24, 138), texPlateDark);
                    GUI.Label(new Rect(rightX + 20, ry + 6, rightW - 40, 18), "SELECT DARK-SKY OBSERVATORY OR CITY PRESET (OFFLINE):", statLabelStyle);

                    float locBtnW = (rightW - 46) / 3.0f;
                    float locBtnH = 24f;
                    for (int bi = 0; bi < CelestialMessierCatalog.BuiltinLocations.Length; bi++)
                    {
                        var loc = CelestialMessierCatalog.BuiltinLocations[bi];
                        int lRow = bi / 3;
                        int lCol = bi % 3;
                        Rect lRect = new Rect(rightX + 16 + lCol * (locBtnW + 4), ry + 26 + lRow * (locBtnH + 4), locBtnW, locBtnH);
                        bool isCurLoc = messierCatalog != null && messierCatalog.currentObserver.locationName == loc.locationName;
                        if (DrawTactileChiclet(lRect, $"{bi + 1:D2}", loc.locationName, isCurLoc, Color.cyan, 10))
                        {
                            if (messierCatalog != null)
                            {
                                messierCatalog.SetObserverLocation(loc.locationName, loc.latitude, loc.longitude, loc.regionDesc);
                            }
                        }
                    }
                    ry += 144;
                }

                // 5 Filter Toggles: Lines, Messier, Star Labels, Horizon Ring, RA/Dec Grid
                float subBtnW = (rightW - 44) / 5.0f;
                bool linesOn = messierCatalog != null && messierCatalog.showConstellationLines;
                if (DrawTactileChiclet(new Rect(rightX + 12, ry, subBtnW, 28), "✨", linesOn ? "Lines" : "Off", linesOn, Color.cyan, 11))
                {
                    if (messierCatalog != null) messierCatalog.showConstellationLines = !messierCatalog.showConstellationLines;
                }

                bool messierOn = messierCatalog != null && messierCatalog.showMessierMarkers;
                if (DrawTactileChiclet(new Rect(rightX + 15 + subBtnW, ry, subBtnW, 28), "🌀", messierOn ? "Messier" : "Off", messierOn, Color.cyan, 11))
                {
                    if (messierCatalog != null) messierCatalog.showMessierMarkers = !messierCatalog.showMessierMarkers;
                }

                bool starsOn = messierCatalog != null && messierCatalog.showStarLabels;
                if (DrawTactileChiclet(new Rect(rightX + 18 + subBtnW * 2, ry, subBtnW, 28), "⭐", starsOn ? "Stars" : "Off", starsOn, Color.cyan, 11))
                {
                    if (messierCatalog != null) messierCatalog.showStarLabels = !messierCatalog.showStarLabels;
                }

                bool horizOn = messierCatalog != null && messierCatalog.showLocalHorizonPlane;
                if (DrawTactileChiclet(new Rect(rightX + 21 + subBtnW * 3, ry, subBtnW, 28), "🌍", horizOn ? "Horizon" : "Off", horizOn, Color.green, 11))
                {
                    if (messierCatalog != null) messierCatalog.showLocalHorizonPlane = !messierCatalog.showLocalHorizonPlane;
                }

                bool gridOn = messierCatalog != null && messierCatalog.showRaDecGrid;
                if (DrawTactileChiclet(new Rect(rightX + 24 + subBtnW * 4, ry, subBtnW, 28), "🌐", gridOn ? "Grid" : "Off", gridOn, Color.yellow, 11))
                {
                    if (messierCatalog != null) messierCatalog.showRaDecGrid = !messierCatalog.showRaDecGrid;
                }

                ry += 32;

                // Quick Targets Selector Grid
                GUI.Label(new Rect(rightX + 16, ry, rightW - 32, 18), "QUICK TARGET SELECTOR // MESSIER DEEP SKY:", statLabelStyle);
                ry += 18;

                string[] quickIds = new string[] { "M31", "M42", "M45", "M13", "M1", "M16", "M27", "M51", "M57", "M87", "M104", "STAR-SIRIUS" };
                string[] quickLabels = new string[] { "M31 Andromeda", "M42 Orion", "M45 Pleiades", "M13 Hercules", "M1 Crab", "M16 Pillars", "M27 Dumbbell", "M51 Whirlpool", "M57 Ring", "M87 Virgo A", "M104 Sombrero", "Sirius" };

                float qW = (rightW - 42) / 4.0f;
                float qH = 24f;
                for (int qi = 0; qi < quickIds.Length; qi++)
                {
                    int row = qi / 4;
                    int col = qi % 4;
                    Rect qRect = new Rect(rightX + 14 + col * (qW + 4), ry + row * (qH + 3), qW, qH);
                    bool isCur = messierCatalog != null && messierCatalog.currentTarget != null && messierCatalog.currentTarget.id == quickIds[qi];
                    if (DrawTactileChiclet(qRect, quickIds[qi], quickLabels[qi], isCur, Color.purple, 10))
                    {
                        if (messierCatalog != null) messierCatalog.SelectTargetById(quickIds[qi]);
                    }
                }
                ry += (qH + 3) * 3 + 4;

                // Deep-Sky Observation Eyepiece Card
                var target = messierCatalog != null ? messierCatalog.currentTarget : null;
                if (target != null)
                {
                    GUI.DrawTexture(new Rect(rightX + 12, ry, rightW - 24, 245), texPlateDark);
                    GUI.DrawTexture(new Rect(rightX + 12, ry, 6, 245), texPurple);

                    float eyeX = rightX + 24;
                    float eyeY = ry + 8;
                    // Reticle Box
                    GUI.DrawTexture(new Rect(eyeX, eyeY, 40, 40), texPlateMid);
                    GUI.DrawTexture(new Rect(eyeX + 19, eyeY, 2, 40), texAmber);
                    GUI.DrawTexture(new Rect(eyeX, eyeY + 19, 40, 2), texAmber);
                    GUI.Label(new Rect(eyeX + 6, eyeY + 6, 28, 28), target.iconGlyph, bigValueStyle);

                    GUI.Label(new Rect(eyeX + 48, eyeY - 2, rightW - 110, 22), $"{target.id}: {target.commonName}", statValueGoldStyle);
                    GUI.Label(new Rect(eyeX + 48, eyeY + 18, rightW - 110, 18), $"{target.objectType} • {target.constellation.ToUpper()} ({target.ngcOrAlt})", statLabelStyle);

                    float gridY = eyeY + 44;
                    GUI.Label(new Rect(eyeX, gridY, 180, 16), "COORDINATES (RA / DEC):", statLabelStyle);
                    GUI.Label(new Rect(eyeX, gridY + 14, 210, 20), target.GetFormattedCoordinates(), compValueStyle);

                    // Real-Time Alt / Az & Horizon Visibility (Calculated 100% locally from observer lat/lon + sidereal time)
                    var (alt, az, isAbove) = messierCatalog.CalculateAltAz(target.raHours, target.decDegrees);
                    string compass = GetCompassDirection(az);
                    string visText = isAbove
                        ? $"🟢 ALT: {alt:+0.0;-0.0}° (High in {compass}) • AZ: {az:000}° [VISIBLE TONIGHT]"
                        : $"🔴 ALT: {alt:+0.0;-0.0}° • AZ: {az:000}° [BELOW HORIZON]";

                    GUI.Label(new Rect(eyeX + 220, gridY, 180, 16), "MAGNITUDE & DISTANCE:", statLabelStyle);
                    double distKm = target.distanceLy * TravelTimeCalculator.LY_KM;
                    string distStr = TravelTimeCalculator.FormatDistanceSpan(distKm, activeUnitSystem);
                    GUI.Label(new Rect(eyeX + 220, gridY + 14, 180, 20), $"m = {target.apparentMag:+0.00;-0.00;0.00}", compValueStyle);

                    GUI.Label(new Rect(eyeX, gridY + 36, rightW - 50, 20), visText, isAbove ? statValueCyanStyle : statLabelStyle);
                    GUI.Label(new Rect(eyeX, gridY + 54, rightW - 50, 20), $"Dist: {target.distanceLy:N0} LY • {distStr}", statValueGoldStyle);

                    GUI.Label(new Rect(eyeX, gridY + 74, rightW - 50, 38), target.description, bodyStyle);

                    if (DrawTactileChiclet(new Rect(eyeX, gridY + 114, rightW - 50, 42), "🎯", $"LOCK TELESCOPE ON [{target.id}] (AIM CAMERA)", true, Color.cyan, 12))
                    {
                        if (messierCatalog != null)
                        {
                            messierCatalog.LockTelescopeOnTarget(target);
                        }
                    }
                }
            }

            // =========================================================================
            // 4. BOTTOM MASTER PROPULSION CONSOLE & MISSION COMM (BAY-03)
            // =========================================================================
            float bottomH = 110;
            float bottomY = virtualH - bottomH - 12;

            // Mission Audio Comm Feed Banner (No Azazel name; pure aerospace telemetry audio)
            bool isTour = engine != null && engine.isTourActive;
            string tourTag = isTour ? $"[🚀 GUIDED TOUR — STAGE {engine.tourCurrentStage}/4] " : "";
            string subText = zoom < 1.75f ? "Stage 1: The Solar System. Spanning ~8.33 light-hours across Neptune's orbit." :
                (zoom < 2.75f ? "Stage 2: The Milky Way Galaxy. ~100,000 light-years across with Orion Spur and Sagittarius A*." :
                (zoom < 3.75f ? "Stage 3: The Local Group Cluster. ~10 million light-years encompassing Andromeda and Milky Way." :
                "Stage 4: Cosmic Web & Particle Horizon. ~93 billion light-years to the Cosmic Microwave Background."));

            Rect commRect = new Rect(virtualW * 0.08f, bottomY - 42, virtualW * 0.84f, 34);
            GUI.DrawTexture(commRect, texPlateDark);
            GUI.DrawTexture(new Rect(commRect.x, commRect.y, 6f, commRect.height), texAmber);
            GUI.DrawTexture(new Rect(commRect.x + commRect.width - 6f, commRect.y, 6f, commRect.height), texAmber);
            GUI.Label(new Rect(commRect.x + 12, commRect.y + 2, commRect.width - 24, 30), $"🎙️ FLIGHT AUDIO TELEMETRY: {tourTag}{subText}", subtitleStyle);

            // Master Propulsion Deck Bay
            Rect cockpitRect = new Rect(headerX, bottomY, headerW, bottomH);
            DrawChassisBay(cockpitRect, "BAY-03", "PROPULSION VECTOR & FLIGHT DECK", Color.cyan);

            // Continuous Scale Relativistic Accelerator Slider (No warp throttle)
            GUI.Label(new Rect(headerX + 16, bottomY + 28, 260, 20), "COSMIC SCALE ACCELERATOR", statLabelStyle);
            GUI.Label(new Rect(headerX + 240, bottomY + 28, virtualW * 0.36f, 20), "1.0 [Solar System] ════ 2.0 [Milky Way] ════ 3.0 [Local Group] ════ 4.0 [Cosmic Web]", statLabelStyle);
            float newZoom = GUI.HorizontalSlider(new Rect(headerX + 16, bottomY + 54, virtualW * 0.34f, 30), zoom, 1.0f, 4.0f);
            if (Mathf.Abs(newZoom - zoom) > 0.005f && engine != null)
            {
                engine.SetZoomDirect(newZoom);
            }

            // Flight Action Chiclets: Guided Tour, Light Pulse, Starry Sky, Reset Camera
            float btnX = virtualW - 835;
            string tourBtnText = isTour ? $"Tour: S{engine.tourCurrentStage}" : "Guided Tour (T)";
            if (DrawTactileChiclet(new Rect(btnX, bottomY + 34, 180, 62), "🚀", tourBtnText, isTour, Color.cyan, 13))
            {
                if (engine != null) engine.ToggleTour();
            }

            string pulseBtnText = isPulseActive ? (isPulsePaused ? "Resume Wave" : "Pause Wave") : "Light Pulse [SPC]";
            if (DrawTactileChiclet(new Rect(btnX + 188, bottomY + 34, 195, 62), "⚡", pulseBtnText, isPulseActive, Color.yellow, 13))
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

            string starryBtnBottom = (messierCatalog != null && messierCatalog.isStarryNightActive) ? "Sky: ON [S]" : "Starry Sky [S]";
            if (DrawTactileChiclet(new Rect(btnX + 391, bottomY + 34, 165, 62), "🔭", starryBtnBottom, (messierCatalog != null && messierCatalog.isStarryNightActive), Color.cyan, 13))
            {
                ToggleStarryNight();
            }

            if (DrawTactileChiclet(new Rect(btnX + 564, bottomY + 34, 155, 62), "↺", "Reset Vector (R)", false, Color.purple, 13))
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

                        bool isOverLeftPanel = gx < leftW + 40 && gy > leftY && gy < leftY + 500;
                        bool isOverRightPanel = gx > rightX - 20 && gy > leftY && gy < leftY + 500;
                        bool isOverTopBar = gy < 105;
                        bool isOverBottomBar = gy > bottomY - 50;

                        if (!isOverLeftPanel && !isOverRightPanel && !isOverTopBar && !isOverBottomBar)
                        {
                            bool isTarget = (messierCatalog.currentTarget == obj);
                            GUI.DrawTexture(new Rect(gx - 4, gy - 4, 8, 8), isTarget ? texAmber : texCyan);
                            GUI.Label(new Rect(gx + 8, gy - 8, 120, 18), $"{obj.iconGlyph} {obj.id}", isTarget ? statValueGoldStyle : hudMicroStyle);
                        }
                    }
                }
            }

            GUI.matrix = oldMatrix;
        }

        private void DrawCompRow(float x, float y, float w, float h, string name, string time, Color col)
        {
            GUI.DrawTexture(new Rect(x, y, w, h), texPlateMid);
            GUI.DrawTexture(new Rect(x, y, 4, h), col == Color.white ? texPlateDark : (col == new Color(0.22f, 0.85f, 1.0f) ? texCyan : texAmber));
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

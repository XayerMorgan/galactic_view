using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace CosmicZoom
{
    /// <summary>An original survey console: navigation, observation and light-travel instruments.</summary>
    public class CosmicHUD : MonoBehaviour
    {
        public static CosmicHUD Instance { get; private set; }
        public enum HUDViewMode { Full, Minimal, Hidden }
        public HUDViewMode currentHUDMode = HUDViewMode.Full;
        public bool isLeftPanelCollapsed;
        public bool isRightPanelCollapsed;
        public UnitSystem activeUnitSystem = UnitSystem.Kilometers;
        public float userScale = 1.15f;
        public bool isHighContrast;
        [SerializeField] private CosmicZoomEngine engine;
        [SerializeField] private CosmicAudioController audioController;
        [SerializeField] private CelestialMessierCatalog messierCatalog;

        private readonly CosmicConsoleDrawing art = new CosmicConsoleDrawing();
        public CosmicGallery Gallery { get; } = new CosmicGallery();
        public bool BlocksSceneInput => Gallery.IsOpen || showSettings || showLocationPicker;
        private int overlayEscapeFrame = -1;
        public bool HandledOverlayEscape => overlayEscapeFrame == Time.frameCount;
        private string latitudeText = "", longitudeText = "";
        private readonly List<Rect> skyLabelBounds = new List<Rect>();
        private Vector2 navigationScroll, instrumentScroll;
        private int activeRightTab;
        private bool showLocationPicker;
        private bool showSettings;
        private bool aboveHorizonOnly = true;
        private Rect header, navigation, instrument, footer, settings;
        private float width, height;
        private bool SkyActive => messierCatalog != null && messierCatalog.isStarryNightActive;
        private int Stage => engine != null ? engine.activeStageIndex : 1;
        private LightPulseEmitter Pulse => engine != null ? engine.lightPulseEmitter : null;
        private static readonly string[] StageNames = { "Solar system", "Milky Way", "Local group", "Cosmic web" };
        private static readonly string[] StageSpans = { "60.14 AU / orbital diameter", "100,000 light-years", "10 million light-years", "93 billion light-years" };
        private static readonly string[] StageNotes = {
            "Across Neptune's orbit. Planet sizes and orbit spacing are illustrative.",
            "Our galactic home. Explore the disk, central core and the solar beacon.",
            "Two-galaxy schematic: Milky Way and Andromeda. Triangulum and dwarf members are not yet included.",
            "The observable universe. An illustrative web within the particle horizon."
        };

        private void Awake() { Instance = this; }
        private void Start()
        {
            if (engine == null) engine = FindAnyObjectByType<CosmicZoomEngine>();
            if (audioController == null) audioController = FindAnyObjectByType<CosmicAudioController>();
            if (messierCatalog == null) messierCatalog = FindAnyObjectByType<CelestialMessierCatalog>();
            activeUnitSystem = (UnitSystem)Mathf.Clamp(PlayerPrefs.GetInt("Cosmic_Unit_System", 0), 0, 2);
            userScale = Mathf.Clamp(PlayerPrefs.GetFloat("Cosmic_A11y_Scale", 1.15f), 0.9f, 1.8f);
            isHighContrast = PlayerPrefs.GetInt("Cosmic_A11y_Contrast", 0) == 1;
        }
        private void OnDestroy() { Gallery.Dispose(); art.Dispose(); if (Instance == this) Instance = null; }
        // The engine owns keyboard dispatch so each shortcut runs exactly once.
        public void CycleHUDMode() { currentHUDMode = (HUDViewMode)(((int)currentHUDMode + 1) % 3); showSettings = false; }
        public void CycleUnitSystem()
        {
            activeUnitSystem = (UnitSystem)(((int)activeUnitSystem + 1) % 3);
            PlayerPrefs.SetInt("Cosmic_Unit_System", (int)activeUnitSystem);
        }
        public void ToggleStarryNight()
        {
            if (messierCatalog == null) return;
            if (SkyActive) messierCatalog.ExitStarryNight();
            else
            {
                engine?.StopTour();
                activeRightTab = 2;
                isRightPanelCollapsed = false;
                instrumentScroll = Vector2.zero;
                messierCatalog.EnsureDatabaseBuilt();
                if (messierCatalog.currentTarget == null && messierCatalog.Catalog.Count > 0) messierCatalog.currentTarget = messierCatalog.Catalog[0];
                messierCatalog.EnterObservatory();
                navigationScroll = Vector2.zero;
            }
        }
        public void SelectInstrument(int index) { activeRightTab = Mathf.Clamp(index, 0, 2); instrumentScroll = Vector2.zero; if (activeRightTab != 2) showLocationPicker = false; }
        public void OpenGallery(CelestialObjectData target = null) { showSettings = showLocationPicker = false; engine?.StopTour(); Gallery.Open(target); }
        public bool CloseOverlay()
        {
            if (Gallery.IsOpen) { Gallery.Close(); return true; }
            if (showLocationPicker) { showLocationPicker = false; return true; }
            if (!showSettings) return false;
            showSettings = false;
            return true;
        }

        public float InterfaceScale => Mathf.Max(0.4f, Mathf.Min(userScale * Mathf.Clamp(Screen.dpi > 0 ? Screen.dpi / 96f : 1, 1, 1.5f), Screen.width / 1120f, Screen.height / 700f));
        private void Layout()
        {
            width = Screen.width / InterfaceScale;
            height = Screen.height / InterfaceScale;
            header = new Rect(22, 18, width - 44, 78);
            navigation = new Rect(22, 116, 242, height - 242);
            instrument = new Rect(width - 384, 116, 362, height - 242);
            footer = new Rect(22, height - 103, width - 44, 83);
            settings = new Rect(width - 374, 99, 352, 494);
        }
        private bool OverUI(Vector2 p)
        {
            if (Gallery.IsOpen) return true;
            if (currentHUDMode == HUDViewMode.Hidden) return new Rect(width - 216, 18, 194, 34).Contains(p);
            if (header.Contains(p) || footer.Contains(p) || (showSettings && settings.Contains(p))) return true;
            if (currentHUDMode != HUDViewMode.Full) return false;
            Rect left = isLeftPanelCollapsed ? new Rect(navigation.x, navigation.y, 134, 34) : navigation;
            Rect right = isRightPanelCollapsed ? new Rect(instrument.xMax - 134, instrument.y, 134, 34) : instrument;
            return left.Contains(p) || right.Contains(p);
        }
        public bool IsPointerOverInterface()
        {
            Layout();
            return OverUI(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y) / InterfaceScale);
        }

        private void OnGUI()
        {
            // Unity text fields can consume keys before legacy Input sees them.
            // Handle overlay Escape before drawing the focused field.
            if (BlocksSceneInput && Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                CloseOverlay();
                overlayEscapeFrame = Time.frameCount;
                GUI.FocusControl(null);
                Event.current.Use();
                return;
            }
            art.Initialize();
            art.HighContrast = isHighContrast;
            Layout();
            Matrix4x4 old = GUI.matrix;
            bool oldEnabled = GUI.enabled;
            GUI.matrix = Matrix4x4.Scale(new Vector3(InterfaceScale, InterfaceScale, 1));
            try
            {
                if (Gallery.IsOpen)
                {
                    Gallery.Draw(art, new Rect(22, 18, width - 44, height - 38), messierCatalog);
                    return;
                }
                if (currentHUDMode == HUDViewMode.Hidden)
                {
                    if (art.Button(new Rect(width - 216, 18, 194, 34), "Restore instruments  [H]")) CycleHUDMode();
                    return;
                }
                DrawWorldLabels();
                DrawHeader();
                // Preferences sit above the science dock; its controls must not click
                // through to tabs or instruments underneath the drawer.
                GUI.enabled = oldEnabled && !showSettings;
                if (currentHUDMode == HUDViewMode.Full)
                {
                    if (isLeftPanelCollapsed)
                    {
                        if (art.Button(new Rect(navigation.x, navigation.y, 134, 34), "Navigation  +")) isLeftPanelCollapsed = false;
                    }
                    else DrawNavigation();
                    if (isRightPanelCollapsed)
                    {
                        if (art.Button(new Rect(instrument.xMax - 134, instrument.y, 134, 34), "Instruments  +")) isRightPanelCollapsed = false;
                    }
                    else DrawInstrument();
                }
                DrawViewportReadout();
                DrawFooter();
                GUI.enabled = oldEnabled;
                if (showSettings) DrawSettings();
            }
            finally { GUI.matrix = old; GUI.enabled = oldEnabled; }
        }

        private void DrawHeader()
        {
            art.Round(header, new Color(0.025f, 0.038f, 0.048f, 0.95f));
            Vector2 c = new Vector2(58, 56);
            art.Arc(c, 23, 30, 315, art.Amber, 2);
            art.Arc(c, 16, 200, 480, art.Teal);
            art.Line(c + new Vector2(-7, 9), c + new Vector2(0, -11), art.Ink, 2);
            art.Line(c + new Vector2(0, -11), c + new Vector2(7, 9), art.Ink, 2);
            art.Text(new Rect(98, 31, 290, 26), "A S T R O N A U T I C A", 19, art.Ink, bold: true);
            art.Text(new Rect(100, 62, 290, 16), "DEEP SPACE SURVEY  /  COSMIC EXPLORER", 10, art.Muted);
            if (width > 1320)
            {
                art.Text(new Rect(420, 32, width - 1040, 20), SkyActive ? "OBSERVATORY / CELESTIAL VAULT" : $"SECTOR 0{Stage} / {StageNames[Stage - 1].ToUpperInvariant()}", 12, art.Teal);
                art.Text(new Rect(420, 55, width - 1040, 19), "Explore the scale of the universe", 12, art.Muted);
            }
            float x = width - 598;
            if (art.Button(new Rect(x, 36, 132, 36), SkyActive ? "Return to flight [S]" : "Observatory [S]", SkyActive)) ToggleStarryNight();
            if (art.Button(new Rect(x + 140, 36, 124, 36), currentHUDMode == HUDViewMode.Full ? "View: full [H]" : "View: quiet [H]")) CycleHUDMode();
            if (art.Button(new Rect(x + 272, 36, 112, 36), "Settings", showSettings)) showSettings = !showSettings;
            if (art.Button(new Rect(x + 392, 36, 84, 36), "Gallery [G]")) OpenGallery();
            if (art.Button(new Rect(x + 484, 36, 70, 36), "Exit")) engine?.QuitApplication();
        }

        private void DrawNavigation()
        {
            if (SkyActive) { DrawSkyNavigation(); return; }
            art.Panel(navigation, "01", "NAVIGATION");
            if (art.Button(new Rect(navigation.xMax - 38, navigation.y + 12, 26, 26), "-")) isLeftPanelCollapsed = true;
            Rect view = new Rect(navigation.x + 14, navigation.y + 52, navigation.width - 28, navigation.height - 68);
            navigationScroll = GUI.BeginScrollView(view, navigationScroll, new Rect(0, 0, view.width - 16, 636), false, false);
            float w = view.width - 18;
            art.Text(new Rect(2, 0, w, 17), "CHOOSE A SCALE  /  1-4", 10, art.Muted);
            for (int i = 0; i < 4; i++)
            {
                Rect row = new Rect(0, 30 + i * 68, w, 60);
                bool active = Stage == i + 1 && !SkyActive;
                if (active) art.Round(row, new Color(0.085f, 0.16f, 0.17f));
                art.Arc(new Vector2(16, row.y + 23), active ? 8 : 5, 0, 360, active ? art.Teal : art.Edge, active ? 2 : 1);
                art.Text(new Rect(36, row.y + 8, w - 42, 23), StageNames[i], 15, active ? art.Teal : art.Ink, bold: active);
                art.Text(new Rect(36, row.y + 33, w - 40, 25), StageSpans[i], 10, art.Muted);
                if (GUI.Button(row, GUIContent.none, GUIStyle.none)) engine?.JumpToStage(i + 1);
                if (i < 3) art.Line(new Vector2(16, row.y + 35), new Vector2(16, row.y + 72), art.Edge);
            }
            art.Line(new Vector2(0, 312), new Vector2(w, 312), art.Edge);
            art.Text(new Rect(2, 330, w, 18), SkyActive ? "TELESCOPE" : "FOCUS A TARGET", 10, art.Amber);
            if (SkyActive)
                art.Text(new Rect(2, 358, w, 100), "Choose a catalog target in the Sky instrument, then aim the telescope. Drag the open sky to look around.", 13, art.Muted);
            else
            {
                string[] names = Stage == 1 ? new[] { "Sun", "Earth", "Jupiter", "Saturn", "Overview" } :
                    Stage == 2 ? new[] { "Galactic core", "Solar beacon", "Galaxy overview" } : Stage == 3 ? new[] { "Andromeda", "Group overview" } : new[] { "Web filaments", "Particle horizon" };
                for (int i = 0; i < names.Length; i++)
                {
                    Rect r = new Rect((i % 2) * (w / 2 + 2), 358 + (i / 2) * 40, w / 2 - 3, 34);
                    if (art.Button(r, names[i])) FocusTarget(i);
                }
                art.Text(new Rect(2, 532, w, 80), StageNotes[Stage - 1], 12, art.Muted);
            }
            GUI.EndScrollView();
        }
        private void FocusTarget(int i)
        {
            if (engine == null) return;
            engine.StopTour();
            if (Stage == 1)
            {
                Action[] actions = { engine.FocusOnSun, engine.FocusOnEarth, engine.FocusOnJupiter, engine.FocusOnSaturn, engine.FocusOnOverview };
                actions[i]();
            }
            else if (Stage == 2) { if (i == 0) engine.FocusOnSgrA(); else if (i == 1) engine.FocusOnOrionSpur(); else engine.FocusOnMilkyWay(); }
            else if (Stage == 3) { if (i == 0) engine.FocusOnAndromeda(); else engine.FocusOnLocalGroup(); }
            else { if (i == 0) engine.FocusOnCosmicFilaments(); else engine.FocusOnCMB(); }
        }

        private void DrawInstrument()
        {
            art.Panel(instrument, "02", SkyActive ? "SKY CATALOG" : "SCIENCE INSTRUMENTS");
            if (art.Button(new Rect(instrument.xMax - 38, instrument.y + 12, 26, 26), "-")) isRightPanelCollapsed = true;
            string[] tabs = { "Light transit", "Craft", "Sky" };
            if (!SkyActive)
                for (int i = 0; i < 3; i++)
                    if (art.Button(new Rect(instrument.x + 16 + i * 110, instrument.y + 50, 104, 34), tabs[i], activeRightTab == i)) SelectInstrument(i);
            float top = SkyActive ? 56 : 98;
            Rect view = new Rect(instrument.x + 18, instrument.y + top, instrument.width - 36, instrument.height - top - 18);
            int tab = SkyActive ? 2 : activeRightTab;
            float contentHeight = tab == 2 ? ObservatoryContentHeight() : tab == 1 ? 615 : 564;
            instrumentScroll = GUI.BeginScrollView(view, instrumentScroll, new Rect(0, 0, view.width - 17, contentHeight), false, false);
            float w = view.width - 20;
            if (tab == 0) DrawTransit(w);
            else if (tab == 1) DrawBenchmarks(w);
            else DrawObservatory(w);
            GUI.EndScrollView();
        }
        private void DrawTransit(float w)
        {
            double span = TravelTimeCalculator.GetSpanKmFromZoom(Stage);
            art.Text(new Rect(0, 0, w, 18), "LIGHT TRAVEL / ACROSS THIS SCALE", 10, art.Muted);
            art.Text(new Rect(0, 26, w, 38), TravelTimeCalculator.FormatDuration(span / TravelTimeCalculator.C_KM_S), 25, art.Amber);
            art.Text(new Rect(0, 74, w, 40), TravelTimeCalculator.FormatSpan(span, activeUnitSystem), 12, art.Muted);
            float progress = Pulse != null ? Pulse.ProgressNormalized : 0;
            bool running = Pulse != null && Pulse.IsPulseActive;
            bool paused = Pulse != null && Pulse.IsPaused;
            Vector2 c = new Vector2(w / 2, 204);
            art.Arc(c, 75, 135, 405, art.Edge, 2);
            art.Arc(c, 67, 135, 135 + 270 * progress, art.Teal, 3);
            for (int i = 0; i <= 30; i++)
            {
                float a = (135 + 9 * i) * Mathf.Deg2Rad;
                Vector2 d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                art.Line(c + d * 81, c + d * (i % 5 == 0 ? 88 : 84), i % 5 == 0 ? art.Muted : art.Edge);
            }
            art.Text(new Rect(0, 170, w, 48), $"{progress * 100:0}%", 32, art.Ink, TextAnchor.MiddleCenter);
            art.Text(new Rect(0, 222, w, 20), running ? (paused ? "PAUSED" : "WAVE IN TRANSIT") : progress >= 1 ? "TRANSIT COMPLETE" : "READY TO EMIT", 10, art.Teal, TextAnchor.MiddleCenter);
            art.Text(new Rect(0, 284, w, 36), running ? TravelTimeCalculator.FormatRawDistance(Pulse.CurrentDistanceKm, activeUnitSystem) + " traversed" : "c = " + TravelTimeCalculator.FormatRawDistance(TravelTimeCalculator.C_KM_S, activeUnitSystem) + "/s", 12, art.Muted, TextAnchor.MiddleCenter);
            if (art.Button(new Rect(0, 331, w, 42), running ? "Restart light pulse" : "Emit light pulse  [Space]", primary: true, enabled: !SkyActive)) engine?.FirePulse();
            if (art.Button(new Rect(0, 381, w, 34), paused ? "Resume wave" : "Pause wave", paused, enabled: running)) Pulse?.TogglePause();
            art.Text(new Rect(0, 433, w, 18), "PLAYBACK RATE", 10, art.Muted);
            float[] speeds = { 0.25f, 0.5f, 1 };
            for (int i = 0; i < 3; i++)
                if (art.Button(new Rect(i * (w + 6) / 3, 458, (w - 12) / 3, 30), speeds[i].ToString("0.##") + "x", Pulse != null && Mathf.Approximately(Pulse.SpeedMultiplier, speeds[i]))) Pulse?.SetSpeedMultiplier(speeds[i]);
            art.Text(new Rect(0, 505, w, 56), Stage == 4 ? "Illustrative playback. Distance / c assumes a static universe; cosmic expansion is not modeled." : "Illustrative playback: the full crossing is compressed into 10 seconds at 1x. Travel time above is physical time.", 12, art.Muted);
        }
        private void DrawBenchmarks(float w)
        {
            double span = TravelTimeCalculator.GetSpanKmFromZoom(Stage);
            art.Text(new Rect(0, 0, w, 20), "SAME DISTANCE / DIFFERENT SPEEDS", 10, art.Muted);
            string[] names = { "Photon", "Relativistic probe", "Parker Solar Probe", "Voyager 1", "Commercial jet" };
            double[] speeds = { TravelTimeCalculator.SPEED_PHOTON, TravelTimeCalculator.SPEED_RELATIVISTIC, TravelTimeCalculator.SPEED_PARKER_SOLAR_PROBE, TravelTimeCalculator.SPEED_VOYAGER_1, TravelTimeCalculator.SPEED_JETLINER };
            for (int i = 0; i < names.Length; i++)
            {
                float y = 36 + i * 102;
                art.Text(new Rect(0, y, w, 23), names[i], 15, i == 0 ? art.Teal : art.Ink);
                art.Text(new Rect(0, y + 27, w, 34), TravelTimeCalculator.FormatVelocity(speeds[i], activeUnitSystem), 11, art.Muted);
                art.Text(new Rect(0, y + 60, w, 23), TravelTimeCalculator.FormatDuration(span / speeds[i]), 16, art.Amber);
                art.Line(new Vector2(0, y + 92), new Vector2(w, y + 92), art.Edge);
            }
            art.Text(new Rect(0, 558, w, 55), "Constant-speed comparisons in the observer's frame. No acceleration or cosmic expansion is modeled.", 12, art.Muted);
        }
        private void DrawSkyNavigation()
        {
            art.Panel(navigation, "01", "LOOK AROUND");
            if (art.Button(new Rect(navigation.xMax - 38, navigation.y + 12, 26, 26), "-")) isLeftPanelCollapsed = true;
            Rect view = new Rect(navigation.x + 14, navigation.y + 52, navigation.width - 28, navigation.height - 68);
            navigationScroll = GUI.BeginScrollView(view, navigationScroll, new Rect(0, 0, view.width - 16, 632), false, false);
            float w = view.width - 18;
            Vector2 c = new Vector2(w / 2, 88);
            art.Arc(c, 55, 0, 360, art.Edge, 2);
            for (int i = 0; i < 24; i++)
            {
                float angle = i * 15 * Mathf.Deg2Rad;
                Vector2 d = new Vector2(Mathf.Sin(angle), -Mathf.Cos(angle));
                art.Line(c + d * 50, c + d * (i % 6 == 0 ? 40 : 46), i % 6 == 0 ? art.Amber : art.Muted);
            }
            string[] compass = { "N", "E", "S", "W" };
            for (int i = 0; i < 4; i++)
            {
                float a = i * Mathf.PI / 2;
                Vector2 p = c + new Vector2(Mathf.Sin(a), -Mathf.Cos(a)) * 70;
                art.Text(new Rect(p.x - 14, p.y - 10, 28, 20), compass[i], 12, art.Ink, TextAnchor.MiddleCenter);
            }
            float heading = messierCatalog.ViewAzimuth * Mathf.Deg2Rad;
            Vector2 needle = new Vector2(Mathf.Sin(heading), -Mathf.Cos(heading));
            art.Line(c - needle * 14, c + needle * 36, art.Teal, 2);
            art.Arc(c, 4, 0, 360, art.Amber, 2);
            art.Text(new Rect(0, 173, w, 24), $"{messierCatalog.ViewDirection} / {messierCatalog.ViewAzimuth:000}°", 15, art.Teal, TextAnchor.MiddleCenter);
            string[] names = { "North", "East", "South", "West" };
            for (int i = 0; i < 4; i++)
                if (art.Button(new Rect((i % 2) * (w + 6) / 2, 211 + (i / 2) * 40, (w - 6) / 2, 34), names[i])) messierCatalog.LookToward(i * 90);
            if (art.Button(new Rect(0, 299, w, 38), "Horizon overview  [R]", primary: true)) messierCatalog.ResetSkyView();
            art.Text(new Rect(0, 355, w, 68), "Drag the sky or use arrow keys to look around. Scroll to zoom. Select a target to aim at it.", 12, art.Ink);
            art.Text(new Rect(0, 434, w, 18), "SKY LAYERS", 10, art.Amber);
            if (art.Button(new Rect(0, 461, w, 32), "Ground + horizon", messierCatalog.showLocalHorizonPlane)) messierCatalog.showLocalHorizonPlane = !messierCatalog.showLocalHorizonPlane;
            if (art.Button(new Rect(0, 501, w, 32), "Constellation lines", messierCatalog.showConstellationLines)) messierCatalog.showConstellationLines = !messierCatalog.showConstellationLines;
            if (art.Button(new Rect(0, 541, w, 32), "Deep-sky markers", messierCatalog.showMessierMarkers)) messierCatalog.showMessierMarkers = !messierCatalog.showMessierMarkers;
            if (art.Button(new Rect(0, 581, w, 32), "Star names", messierCatalog.showStarLabels)) messierCatalog.showStarLabels = !messierCatalog.showStarLabels;
            GUI.EndScrollView();
        }

        private float ObservatoryContentHeight()
        {
            int count = 0;
            if (messierCatalog != null)
                foreach (var obj in messierCatalog.Catalog)
                    if (!aboveHorizonOnly || messierCatalog.CalculateAltAz(obj.raHours, obj.decDegrees).isAboveHorizon) count++;
            return 387 + count * 58 + (showLocationPicker ? 412 : 0);
        }

        private void DrawObservatory(float w)
        {
            if (messierCatalog == null) return;
            var loc = messierCatalog.currentObserver;
            art.Text(new Rect(0, 0, w, 16), "OBSERVER / LOCAL SKY", 10, art.Muted);
            art.Text(new Rect(0, 23, w, 40), loc.locationName, 16, art.Amber);
            art.Text(new Rect(0, 60, w, 19), $"UTC {DateTime.UtcNow:yyyy-MM-dd HH:mm}", 11, art.Muted);
            if (art.Button(new Rect(0, 86, w, 30), showLocationPicker ? "Close locations" : "Change observer location", showLocationPicker))
            {
                showLocationPicker = !showLocationPicker;
                latitudeText = loc.latitude.ToString("0.0000", CultureInfo.InvariantCulture);
                longitudeText = loc.longitude.ToString("0.0000", CultureInfo.InvariantCulture);
            }
            float y = 128;
            if (showLocationPicker)
            {
                for (int i = 0; i < CelestialMessierCatalog.BuiltinLocations.Length; i++)
                {
                    var place = CelestialMessierCatalog.BuiltinLocations[i];
                    Rect r = new Rect((i % 2) * (w + 6) / 2, y + (i / 2) * 48, (w - 6) / 2, 42);
                    if (art.Button(r, place.locationName, place.locationName == loc.locationName))
                    {
                        messierCatalog.SetObserverLocation(place.locationName, place.latitude, place.longitude, place.regionDesc);
                        showLocationPicker = false;
                    }
                }
                y += 296;
                art.Text(new Rect(0, y, w, 18), "CUSTOM LOCATION / LATITUDE, LONGITUDE", 10, art.Muted);
                latitudeText = GUI.TextField(new Rect(0, y + 24, (w - 8) / 2, 28), latitudeText);
                longitudeText = GUI.TextField(new Rect((w + 8) / 2, y + 24, (w - 8) / 2, 28), longitudeText);
                bool validLat = float.TryParse(latitudeText, NumberStyles.Float, CultureInfo.InvariantCulture, out float customLat) && customLat >= -90 && customLat <= 90;
                bool validLon = float.TryParse(longitudeText, NumberStyles.Float, CultureInfo.InvariantCulture, out float customLon) && customLon >= -180 && customLon <= 180;
                if (art.Button(new Rect(0, y + 62, w, 32), "Use coordinates (north / east positive)", enabled: validLat && validLon))
                {
                    messierCatalog.SetObserverLocation("Custom observer", customLat, customLon, "User-entered coordinates");
                    showLocationPicker = false;
                }
                y += 116;
            }
            var target = messierCatalog.currentTarget;
            if (target != null)
            {
                var (alt, az, above) = messierCatalog.CalculateAltAz(target.raHours, target.decDegrees);
                art.Text(new Rect(0, y, w, 26), target.commonName, 18, art.Ink);
                art.Text(new Rect(0, y + 28, w, 20), $"Altitude {alt:+0.0;-0.0}° / Azimuth {az:000.0}°", 11, art.Muted);
                if (art.Button(new Rect(0, y + 55, w, 34), above ? "Aim at " + target.id : target.id + " is below the horizon", primary: true, enabled: above)) messierCatalog.LockTelescopeOnTarget(target);
            }
            if (art.Button(new Rect(0, y + 98, w, 30), target != null && target.HasImage ? "View telescope image / " + target.id : "Browse image gallery [G]")) OpenGallery(target);
            y += 142;
            art.Text(new Rect(0, y, w, 18), "CATALOG / SELECT TO AIM", 10, art.Teal);
            y += 22;
            if (art.Button(new Rect(0, y, (w - 6) / 2, 30), "Above horizon", aboveHorizonOnly)) aboveHorizonOnly = true;
            if (art.Button(new Rect((w + 6) / 2, y, (w - 6) / 2, 30), "All targets", !aboveHorizonOnly)) aboveHorizonOnly = false;
            y += 34;
            foreach (var obj in messierCatalog.Catalog)
            {
                var (alt, az, above) = messierCatalog.CalculateAltAz(obj.raHours, obj.decDegrees);
                if (aboveHorizonOnly && !above) continue;
                string title = (obj.objectType == CelestialObjectType.MajorStar ? obj.commonName : obj.id + " / " + obj.commonName);
                string status = $"Alt {alt:+0.0;-0.0}°   Az {az:000.0}°" + (above ? "" : "   below");
                bool selected = target == obj;
                Rect row = new Rect(0, y, w, 52);
                if (selected) art.Round(row, new Color(0.085f, 0.16f, 0.17f));
                art.Text(new Rect(8, y + 4, w - 16, 23), title, 12, above ? art.Ink : art.Muted);
                art.Text(new Rect(8, y + 29, w - 16, 20), status, 11, above ? art.Teal : art.Muted);
                if (GUI.Button(row, GUIContent.none, GUIStyle.none))
                {
                    messierCatalog.currentTarget = obj;
                    if (above) messierCatalog.LockTelescopeOnTarget(obj);
                }
                y += 58;
            }
            art.Text(new Rect(0, y + 8, w, 54), "110 Messier objects + 12 highlights; decorative background stars. Above horizon does not account for daylight or weather.", 11, art.Muted);
        }

        private void DrawViewportReadout()
        {
            float left = currentHUDMode == HUDViewMode.Full && !isLeftPanelCollapsed ? navigation.xMax + 30 : 42;
            float right = currentHUDMode == HUDViewMode.Full && !isRightPanelCollapsed ? instrument.x - 30 : width - 42;
            if (right - left < 230) return;
            art.Text(new Rect(left, 129, right - left, 18), SkyActive ? "CELESTIAL OBSERVATION" : $"EXPLORATION / 0{Stage}", 10, art.Teal);
            art.Text(new Rect(left, 155, right - left, 34), SkyActive ? $"{messierCatalog.ViewDirection} / {messierCatalog.ViewAltitude:+0;-0}°" : StageNames[Stage - 1], 27, art.Ink);
            art.Line(new Vector2(left, 204), new Vector2(left + 52, 204), art.Amber, 2);
            float y = footer.y - 63;
            art.Text(new Rect(left, y, right - left, 20), SkyActive ? $"Bearing {messierCatalog.ViewAzimuth:000}°  /  Field of view {messierCatalog.ViewFieldOfView:0}°" : engine != null ? engine.currentTargetName : "Survey camera", 13, art.Ink);
            art.Text(new Rect(left, y + 27, right - left, 20), SkyActive ? "DRAG / LOOK     SCROLL / ZOOM     R / HORIZON" : "DRAG / ORBIT     SCROLL / INSPECT     R / RESET", 10, art.Muted);
        }
        private void DrawWorldLabels()
        {
            if (!SkyActive || Camera.main == null) return;
            DrawHorizonLabels();
            skyLabelBounds.Clear();
            // Reserve the selected target first; crowded neighbors remain selectable in the catalog.
            for (int i = -1; i < messierCatalog.Catalog.Count; i++)
            {
                var obj = i < 0 ? messierCatalog.currentTarget : messierCatalog.Catalog[i];
                if (obj == null || (i >= 0 && obj == messierCatalog.currentTarget)) continue;
                if (messierCatalog.showLocalHorizonPlane && !messierCatalog.CalculateAltAz(obj.raHours, obj.decDegrees).isAboveHorizon) continue;
                bool star = obj.objectType == CelestialObjectType.MajorStar;
                if (star ? !messierCatalog.showStarLabels : !messierCatalog.showMessierMarkers) continue;
                Vector3 screen = Camera.main.WorldToScreenPoint(messierCatalog.GetWorldPositionOfObject(obj));
                Vector2 p = new Vector2(screen.x, Screen.height - screen.y) / InterfaceScale;
                if (screen.z <= 0 || p.x < 26 || p.x > width - 150 || p.y < 220 || p.y > footer.y - 95 || OverUI(p) || OverUI(p + Vector2.right * 140)) continue;
                bool selected = messierCatalog.currentTarget == obj;
                art.Arc(p, selected ? 11 : 4, 0, 360, selected ? art.Amber : art.Teal);
                Rect labelBounds = new Rect(p.x - 12, p.y - 12, 180, 43);
                bool overlaps = false;
                foreach (Rect used in skyLabelBounds) if (used.Overlaps(labelBounds)) { overlaps = true; break; }
                if (overlaps) continue;
                skyLabelBounds.Add(labelBounds);
                art.Text(new Rect(p.x + 17, p.y - 9, 150, 20), star ? obj.commonName : obj.id, 11, selected ? art.Amber : art.Muted);
                var horizontal = messierCatalog.CalculateAltAz(obj.raHours, obj.decDegrees);
                art.Text(new Rect(p.x + 17, p.y + 10, 150, 19), $"Alt {horizontal.altitudeDeg:+0.0;-0.0}°  Az {horizontal.azimuthDeg:000}°", 10, art.Muted);
                if (!showSettings && GUI.Button(labelBounds, GUIContent.none, GUIStyle.none)) messierCatalog.LockTelescopeOnTarget(obj);
            }
        }
        private void DrawHorizonLabels()
        {
            if (!messierCatalog.showLocalHorizonPlane) return;
            string[] labels = { "NORTH", "NORTHEAST", "EAST", "SOUTHEAST", "SOUTH", "SOUTHWEST", "WEST", "NORTHWEST" };
            for (int i = 0; i < 24; i++)
            {
                Vector3 screen = Camera.main.WorldToScreenPoint(messierCatalog.HorizonDirection(i * 15, 0) * 5800);
                Vector2 p = new Vector2(screen.x, Screen.height - screen.y) / InterfaceScale;
                if (screen.z <= 0 || p.x < 90 || p.x > width - 90 || p.y < 230 || p.y > footer.y - 110 || OverUI(p) || OverUI(p + Vector2.right * 80) || OverUI(p - Vector2.right * 80)) continue;
                art.Line(p + Vector2.up * -7, p + Vector2.up * 7, i % 3 == 0 ? art.Amber : art.Muted, 2);
                if (i % 3 == 0) art.Text(new Rect(p.x - 80, p.y + 14, 160, 28), labels[i / 3] + $" / {i * 15:000}°", 11, art.Amber, TextAnchor.MiddleCenter);
            }
        }

        private void DrawFooter()
        {
            if (SkyActive)
            {
                art.Round(footer, new Color(0.025f, 0.038f, 0.048f, 0.97f));
                float x0 = footer.x + 18, y0 = footer.y + 25;
                art.Text(new Rect(x0, footer.y + 12, 280, 17), "OBSERVATORY / HORIZON VIEW", 10, art.Teal);
                art.Text(new Rect(x0, footer.y + 39, 350, 25), "Arrow keys to pan · S to return to flight", 12, art.Ink);
                float x1 = footer.xMax - 558;
                if (art.Button(new Rect(x1, y0, 100, 36), "Zoom out −")) messierCatalog.ZoomView(10);
                if (art.Button(new Rect(x1 + 108, y0, 100, 36), "Zoom in +")) messierCatalog.ZoomView(-10);
                if (art.Button(new Rect(x1 + 222, y0, 156, 36), "Horizon overview [R]", primary: true)) messierCatalog.ResetSkyView();
                if (art.Button(new Rect(x1 + 388, y0, 152, 36), "Return to flight [S]")) ToggleStarryNight();
                return;
            }
            art.Round(footer, new Color(0.025f, 0.038f, 0.048f, 0.97f));
            float x = footer.x + 18;
            art.Text(new Rect(x, footer.y + 12, 310, 16), "SURVEY SCALE / SELECT A SECTOR", 10, art.Muted);
            string[] labels = { "01  SOL", "02  GALAXY", "03  GROUP", "04  WEB" };
            for (int i = 0; i < 4; i++)
                if (art.Button(new Rect(x + i * 91, footer.y + 36, 85, 32), labels[i], Stage == i + 1 && !SkyActive)) engine?.JumpToStage(i + 1);
            float right = footer.xMax - 18;
            if (art.Button(new Rect(right - 122, footer.y + 25, 122, 38), "Reset view [R]")) engine?.ResetCamera();
            bool touring = engine != null && engine.isTourActive;
            if (art.Button(new Rect(right - 258, footer.y + 25, 128, 38), touring ? "Stop tour [T]" : "Guided tour [T]", touring)) engine?.ToggleTour();
            if (art.Button(new Rect(right - 414, footer.y + 25, 148, 38), "Light pulse [Space]", primary: true, enabled: !SkyActive)) engine?.FirePulse();
            if (width > 1510)
            {
                float tx = x + 394;
                art.Text(new Rect(tx, footer.y + 25, right - 446 - tx, 20), touring ? $"GUIDED TOUR / SECTOR {engine.tourCurrentStage}" : "FREE EXPLORATION", 11, art.Teal);
                art.Text(new Rect(tx, footer.y + 49, right - 446 - tx, 18), "SCHEMATIC SPATIAL SCALES", 10, art.Muted);
            }
        }
        private void DrawSettings()
        {
            bool previousContrast = art.HighContrast;
            art.HighContrast = true;
            art.Panel(settings, "03", "PREFERENCES");
            art.HighContrast = previousContrast;
            float x = settings.x + 16, y = settings.y + 54, w = settings.width - 32;
            string units = activeUnitSystem == UnitSystem.Kilometers ? "Metric" : activeUnitSystem == UnitSystem.Miles ? "Miles" : "Dual";
            if (art.Button(new Rect(x, y, w, 32), "Units: " + units + " [U]")) CycleUnitSystem();
            if (art.Button(new Rect(x, y + 42, 50, 32), "A-")) SetScale(userScale - 0.1f);
            art.Text(new Rect(x + 58, y + 42, 166, 32), $"Text scale {userScale:0.0}x", 12, art.Ink, TextAnchor.MiddleCenter);
            if (art.Button(new Rect(x + w - 50, y + 42, 50, 32), "A+")) SetScale(userScale + 0.1f);
            if (art.Button(new Rect(x, y + 84, w, 32), "High contrast", isHighContrast))
            {
                isHighContrast = !isHighContrast;
                PlayerPrefs.SetInt("Cosmic_A11y_Contrast", isHighContrast ? 1 : 0);
            }
            bool narration = audioController != null && audioController.IsNarratorAutoPlay;
            if (art.Button(new Rect(x, y + 126, w, 28), narration ? "Narration: on" : "Narration: off", narration)) audioController?.ToggleNarrator();
            if (audioController != null)
            {
                art.Text(new Rect(x, y + 168, w, 18), $"SPACE MUSIC / {audioController.TrackCount} INSTRUMENTALS", 10, art.Teal);
                art.Text(new Rect(x, y + 194, w, 36), audioController.GetCurrentTrackTitle(), 15, art.Ink);
                if (art.Button(new Rect(x, y + 240, (w - 8) / 2, 30), audioController.IsMusicEnabled ? "Pause music" : "Play music")) audioController.ToggleMusic();
                if (art.Button(new Rect(x + (w + 8) / 2, y + 240, (w - 8) / 2, 30), "Next track ›")) audioController.NextTrack();
                if (art.Button(new Rect(x, y + 280, (w - 8) / 2, 30), "Shuffle", audioController.ShuffleMusic)) audioController.ToggleShuffle();
                if (art.Button(new Rect(x + (w + 8) / 2, y + 280, (w - 8) / 2, 30), audioController.IsAudioMuted ? "Unmute audio" : "Mute all audio", audioController.IsAudioMuted)) audioController.ToggleAudioMute();
                art.Text(new Rect(x, y + 326, w, 18), $"Music volume / {audioController.musicTargetVolume * 100:0}%", 12, art.Muted);
                float volume = GUI.HorizontalSlider(new Rect(x, y + 358, w, 20), audioController.musicTargetVolume, 0, 1);
                if (!Mathf.Approximately(volume, audioController.musicTargetVolume)) audioController.SetMusicVolume(volume);
            }
            if (art.Button(new Rect(x, y + 390, w, 32), "Help & field guide [F1]")) OpenHelp();
        }

        public static string HelpPath => System.IO.Path.Combine(Application.streamingAssetsPath, "Help/index.html");

        public void OpenHelp()
        {
            if (!System.IO.File.Exists(HelpPath))
            {
                Debug.LogWarning("The offline field guide is missing. Reinstall the complete Astronautica package.");
                return;
            }
            Application.OpenURL(new System.Uri(HelpPath).AbsoluteUri);
        }
        private void SetScale(float value)
        {
            userScale = Mathf.Clamp(value, 0.9f, 1.8f);
            PlayerPrefs.SetFloat("Cosmic_A11y_Scale", userScale);
        }
    }
}

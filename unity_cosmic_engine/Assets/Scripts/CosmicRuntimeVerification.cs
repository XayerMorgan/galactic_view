using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CosmicZoom
{
    /// <summary>Opt-in standalone regression run. Captures the real rendered HUD after EndOfFrame.</summary>
    public sealed class CosmicRuntimeVerification : MonoBehaviour
    {
        private readonly List<string> results = new List<string>();
        private int failures;
        private string output;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--cosmic-qa") < 0) return;
            new GameObject("Runtime verification").AddComponent<CosmicRuntimeVerification>();
        }

        private void Check(bool condition, string message)
        {
            results.Add((condition ? "PASS " : "FAIL ") + message);
            if (!condition) failures++;
        }

        private void OnLog(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Exception && type != LogType.Error && type != LogType.Assert) return;
            failures++;
            results.Add("ERROR " + condition);
        }

        private IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();
            Texture2D frame = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine(output, name + ".png"), frame.EncodeToPNG());
            Destroy(frame);
        }

        private static bool MusicIsPlaying(CosmicAudioController audio)
        {
            foreach (var source in audio.GetComponents<AudioSource>())
            {
                // Music pause deliberately leaves narration and one-shot UI sounds alone.
                var clip = source.clip;
                if (clip == null) continue;
                bool music = Array.IndexOf(audio.additionalMusic, clip) >= 0
                    || clip == audio.acousticMovement1 || clip == audio.acousticMovement2
                    || clip == audio.acousticMovement3 || clip == audio.soothingAcoustic;
                if (music && source.isPlaying) return true;
            }
            return false;
        }

        private IEnumerator Start()
        {
            Application.logMessageReceived += OnLog;
            Application.runInBackground = true;
            output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../visual_tests/interface-repair"));
            Directory.CreateDirectory(output);
            Screen.SetResolution(1920, 1080, FullScreenMode.Windowed);
            yield return new WaitForSeconds(2);
            var engine = FindAnyObjectByType<CosmicZoomEngine>();
            var hud = CosmicHUD.Instance;
            var catalog = CelestialMessierCatalog.Instance;
            var audio = FindAnyObjectByType<CosmicAudioController>();
            Check(File.Exists(CosmicHUD.HelpPath), "The offline field guide is included in the player");
            Check(File.Exists(Path.Combine(Application.streamingAssetsPath, "Help/help.css")), "The offline guide stylesheet is included");
            Check(Application.version == File.ReadAllText(Path.Combine(Application.dataPath, "../VERSION")).Trim(), "The player reports the release version");
            if (audio != null && !audio.IsAudioMuted) audio.ToggleAudioMute();
            Check(audio != null && audio.TrackCount == 9, "Nine soundtrack selections are available");
            Check(audio.currentTrackIndex == 0 && MusicIsPlaying(audio), "The first streamed track starts without being skipped while loading");
            Check(audio.additionalMusic.Length == 5, "Five generated instrumental tracks are assigned");
            foreach (var clip in audio.additionalMusic)
                Check(clip != null && clip.length > 179 && clip.length < 181 && clip.loadType == AudioClipLoadType.Streaming,
                    "Three-minute instrumental streams without loading the entire decoded track: " + (clip != null ? clip.name : "missing"));
            audio.StopNarration();
            bool originalShuffle = audio.ShuffleMusic;
            if (!originalShuffle) audio.ToggleShuffle();
            var heard = new HashSet<int> { audio.currentTrackIndex };
            for (int i = 1; i < audio.TrackCount; i++)
            {
                audio.NextTrack();
                heard.Add(audio.currentTrackIndex);
                Check(audio.AllSourcesMuted, "Skipping while muted keeps both music decks muted");
            }
            Check(heard.Count == audio.TrackCount, "Shuffle visits every track before repeating");
            audio.ToggleMusic();
            yield return new WaitForSeconds(0.2f);
            bool anyPlaying = MusicIsPlaying(audio);
            Check(!anyPlaying, "Music pause stops both decks during a transition");
            if (anyPlaying)
                foreach (var source in audio.GetComponents<AudioSource>())
                    results.Add("AUDIO DIAGNOSTIC " + (source.clip != null ? source.clip.name : "no clip") + " playing=" + source.isPlaying);
            audio.NextTrack();
            yield return null;
            anyPlaying = MusicIsPlaying(audio);
            Check(!anyPlaying, "Skipping while paused stays paused");
            audio.ToggleMusic();
            yield return new WaitForSeconds(0.3f);
            anyPlaying = MusicIsPlaying(audio);
            Check(anyPlaying && audio.AllSourcesMuted, "Music resumes while retaining the global mute");
            if (audio.ShuffleMusic != originalShuffle) audio.ToggleShuffle();
            for (int i = 0; i < audio.additionalMusic.Length; i++)
            {
                audio.CrossfadeToTrack(i);
                yield return new WaitForSeconds(2.8f);
                bool clipPlaying = false;
                foreach (var source in audio.GetComponents<AudioSource>())
                    if (source.clip == audio.additionalMusic[i] && source.isPlaying && source.time > 0.5f) clipPlaying = true;
                Check(audio.currentTrackIndex == i && clipPlaying, "Generated track decodes and advances through playback: " + audio.additionalMusic[i].name);
            }

            var ids = new HashSet<string>();
            int photoCount = 0, referenceStars = 0;
            CelestialObjectData galleryTarget = null;
            foreach (var obj in catalog.Catalog)
            {
                Check(ids.Add(obj.id), "Unique catalog ID: " + obj.id);
                if (obj.objectType == CelestialObjectType.MajorStar) referenceStars++;
                if (!obj.HasImage) continue;
                photoCount++;
                var full = hud.Gallery.ImageFor(obj, false);
                var thumb = hud.Gallery.ImageFor(obj, true);
                Check(full != null && full.width >= 100 && thumb != null && thumb.width >= 100,
                    "Bundled photograph and thumbnail decode: " + obj.id);
                Check(!string.IsNullOrWhiteSpace(obj.imageCredit) && !string.IsNullOrWhiteSpace(obj.imageLicense)
                    && Uri.TryCreate(obj.imageSourceUrl, UriKind.Absolute, out _) && Uri.TryCreate(obj.imageLicenseUrl, UriKind.Absolute, out _),
                    "Image attribution and source are present: " + obj.id);
                if (obj.id == "M42") galleryTarget = obj;
                if (photoCount % 10 == 0) yield return null;
            }
            Check(photoCount == 122 && referenceStars == 10, "Complete 122-image gallery plus ten reference stars");
            for (int n = 1; n <= 110; n++) Check(ids.Contains("M" + n), "Messier entry present: M" + n);
            var positionFixture = CelestialMessierCatalog.CalculateAltAzAtSiderealTime(0, 0, 0, 0);
            Check(Mathf.Abs(positionFixture.altitudeDeg - 90) < 0.01f, "Equatorial transit reaches the zenith");
            positionFixture = CelestialMessierCatalog.CalculateAltAzAtSiderealTime(0, 0, 0, 6);
            Check(Mathf.Abs(positionFixture.altitudeDeg) < 0.01f && Mathf.Abs(positionFixture.azimuthDeg - 270) < 0.01f, "Positive six-hour angle is on the western horizon");
            positionFixture = CelestialMessierCatalog.CalculateAltAzAtSiderealTime(0, 0, 0, 18);
            Check(Mathf.Abs(positionFixture.altitudeDeg) < 0.01f && Mathf.Abs(positionFixture.azimuthDeg - 90) < 0.01f, "Negative six-hour angle is on the eastern horizon");
            positionFixture = CelestialMessierCatalog.CalculateAltAzAtSiderealTime(0, 0, 45, 0);
            Check(Mathf.Abs(positionFixture.altitudeDeg - 45) < 0.01f && Mathf.Abs(positionFixture.azimuthDeg - 180) < 0.01f, "Northern observer sees equatorial transit due south");
            positionFixture = CelestialMessierCatalog.CalculateAltAzAtSiderealTime(0, 20, 90, 6);
            Check(Mathf.Abs(positionFixture.altitudeDeg - 20) < 0.01f && !float.IsNaN(positionFixture.azimuthDeg), "Polar observers receive finite horizon coordinates");
            hud.userScale = 1.15f;
            hud.activeUnitSystem = UnitSystem.Kilometers;
            Check(Math.Abs(TravelTimeCalculator.GetLightTransitSeconds(TravelTimeCalculator.STAGE_2_GALAXY_KM) / (365.25 * 86400) - 100000) < 0.01, "Light travel uses the physical year conversion");
            Check(TravelTimeCalculator.FormatVelocity(0.25, UnitSystem.Kilometers).Contains("0.25"), "Sub-kilometer craft speeds retain their precision");
            foreach (var glow in FindObjectsByType<AtmosphereGlow>())
            {
                var body = glow.GetComponent<Renderer>();
                var shell = glow.transform.Find(glow.name + "_AtmosphereGlow").GetComponent<Renderer>();
                float ratio = shell.bounds.size.magnitude / body.bounds.size.magnitude;
                Check(ratio > 1 && ratio < 1.15f, "Atmosphere fits its imported body: " + ratio.ToString("0.000"));
            }
            GameObject[] roots = { engine.stage1SolarSystem, engine.stage2MilkyWay, engine.stage3LocalGroup, engine.stage4CosmicWeb };
            bool foundShip = false;
            foreach (Transform child in engine.stage1SolarSystem.GetComponentsInChildren<Transform>(true))
                if (child.name.Contains("Flagship") || child.name.Contains("Scout")) foundShip = true;
            Check(!foundShip, "No survey ships remain in the solar-system scene");
            for (int stage = 1; stage <= 4; stage++)
            {
                engine.JumpToStage(stage);
                yield return new WaitForSeconds(1.5f);
                for (int i = 0; i < roots.Length; i++) Check(roots[i].activeSelf == (i == stage - 1), "Stage " + stage + " visibility of root " + (i + 1));
                foreach (var renderer in roots[stage - 1].GetComponentsInChildren<Renderer>())
                    foreach (var material in renderer.sharedMaterials)
                        Check(material != null && material.shader != null && material.shader.isSupported && material.shader.name != "Hidden/InternalErrorShader", "Supported material on " + renderer.name);
                yield return Capture("stage" + stage + "_full");
            }
            engine.JumpToStage(1);
            engine.FocusOnEarth();
            yield return new WaitForSeconds(1.5f);
            Check(engine.currentTargetTransform != null && engine.currentTargetTransform.name == "Earth", "Earth focus resolves a nested FBX body");
            yield return Capture("earth_detail");
            Vector3 rootPosition = engine.transform.position;
            engine.FirePulse();
            yield return new WaitForSeconds(0.5f);
            Check(engine.transform.position == rootPosition, "Pulse never moves the engine root or celestial vault");
            Check(engine.lightPulseEmitter.IsPulseActive, "Pulse starts");
            engine.lightPulseEmitter.TogglePause();
            float progress = engine.lightPulseEmitter.ProgressNormalized;
            yield return new WaitForSeconds(0.3f);
            Check(engine.lightPulseEmitter.IsPaused && Mathf.Approximately(progress, engine.lightPulseEmitter.ProgressNormalized), "Paused pulse holds its progress");
            yield return Capture("pulse_paused");
            engine.JumpToStage(2);
            Check(!engine.lightPulseEmitter.IsPulseActive, "Changing scale cancels the previous pulse");
            engine.FocusOnOrionSpur();
            Check(engine.currentTargetTransform != null && engine.currentTargetTransform.name.Contains("Beacon"), "Solar beacon tracks the rotating galaxy");
            engine.JumpToStage(3);
            engine.FocusOnAndromeda();
            Check(engine.currentTargetTransform != null && engine.currentTargetTransform.name.Contains("Andromeda"), "Andromeda focus tracks the actual galaxy");
            hud.ToggleStarryNight();
            yield return new WaitForSeconds(1.6f);
            Check(catalog.isStarryNightActive && catalog.vault3DRoot.activeSelf, "Observatory enters with an active celestial vault");
            foreach (var root in roots) Check(!root.activeSelf, "Observatory isolates sky from " + root.name);
            Check(Camera.main.transform.position.sqrMagnitude < 0.01f, "Sky camera is centered on celestial coordinates");
            Check(Mathf.Approximately(catalog.ViewFieldOfView, 75) && Mathf.Approximately(catalog.ViewAltitude, 12), "Observatory opens with the wide horizon overview");
            var ground = catalog.vault3DRoot.transform.Find("Observer_Horizon_Ring").GetComponent<Renderer>();
            Check(ground != null && ground.sharedMaterial.shader.name == "Cosmic/ObserverHorizon" && ground.sharedMaterial.shader.isSupported, "Ground has a supported depth-occluding horizon shader");
            foreach (var obj in catalog.Catalog)
            {
                var position = catalog.CalculateAltAz(obj.raHours, obj.decDegrees);
                Check(Vector3.Angle(obj.GetUnitSpherePosition(), catalog.HorizonDirection(position.azimuthDeg, position.altitudeDeg)) < 0.1f, "Horizon coordinates agree with equatorial coordinates: " + obj.id);
            }
            for (int azimuth = 0; azimuth < 360; azimuth += 90)
            {
                catalog.LookToward(azimuth);
                Check(Vector3.Angle(Camera.main.transform.forward, catalog.HorizonDirection(azimuth, 12)) < 0.1f && Mathf.Abs(Vector3.Dot(Camera.main.transform.right, catalog.GetObserverZenithUnitVector())) < 0.001f, "Cardinal view stays level at " + azimuth + " degrees");
            }
            catalog.ZoomView(-500);
            Check(Mathf.Approximately(catalog.ViewFieldOfView, 10), "Telescope zoom has a usable lower limit");
            catalog.ZoomView(500);
            Check(Mathf.Approximately(catalog.ViewFieldOfView, 90), "Telescope zoom has a usable wide-angle limit");
            catalog.LookToward(75, 100);
            Check(catalog.ViewAltitude < 90, "Looking up never crosses the zenith or flips the camera");
            engine.ResetCamera();
            Check(catalog.isStarryNightActive && Mathf.Approximately(catalog.ViewAzimuth, 180) && Mathf.Approximately(catalog.ViewFieldOfView, 75), "Reset restores the horizon without leaving the observatory");
            yield return Capture("observatory_horizon");
            foreach (var obj in catalog.Catalog)
            {
                var position = catalog.CalculateAltAz(obj.raHours, obj.decDegrees);
                if (position.altitudeDeg < 10 || position.altitudeDeg > 75) continue;
                catalog.LockTelescopeOnTarget(obj);
                yield return new WaitForSeconds(1.4f);
                Check(Vector3.Angle(Camera.main.transform.forward, obj.GetUnitSpherePosition()) < 0.1f, "Target aiming centers an above-horizon catalog object");
                yield return Capture("observatory_target");
                break;
            }
            catalog.ResetSkyView();
            catalog.showConstellationLines = false;
            catalog.showMessierMarkers = false;
            catalog.showLocalHorizonPlane = false;
            yield return null;
            Check(!catalog.vault3DRoot.transform.Find("Constellations").gameObject.activeSelf && !catalog.vault3DRoot.transform.Find("Messier_Nodes").gameObject.activeSelf && !catalog.vault3DRoot.transform.Find("Observer_Horizon_Ring").gameObject.activeSelf, "Sky filters change rendered objects");
            catalog.showConstellationLines = catalog.showMessierMarkers = catalog.showLocalHorizonPlane = true;
            yield return Capture("observatory");
            hud.OpenGallery();
            Check(hud.BlocksSceneInput && hud.Gallery.Filter(catalog) == 122, "Gallery opens with all images and captures scene input");
            hud.Gallery.Search = "M 31";
            Check(hud.Gallery.Filter(catalog) == 1, "Messier search accepts a spaced identifier");
            hud.Gallery.Search = "NGC 224";
            Check(hud.Gallery.Filter(catalog) == 1, "NGC search accepts a spaced identifier without catalog padding");
            hud.Gallery.Search = "no-such-object";
            Check(hud.Gallery.Filter(catalog) == 0, "Unknown search returns an empty result");
            yield return Capture("gallery_empty");
            hud.Gallery.Search = "Swan Nebula";
            Check(hud.Gallery.Filter(catalog) == 1, "Alternate common names are searchable");
            hud.Gallery.Search = "";
            yield return Capture("gallery_1920");
            hud.OpenGallery(galleryTarget);
            yield return Capture("gallery_detail_1920");
            Check(hud.CloseOverlay() && !hud.Gallery.IsOpen && catalog.isStarryNightActive, "Escape closes the gallery and preserves the sky session");
            hud.ToggleStarryNight();
            yield return new WaitForSeconds(1.5f);
            Check(!catalog.isStarryNightActive && roots[2].activeSelf && Mathf.Approximately(Camera.main.fieldOfView, 45), "Observatory returns to the previous sector and lens");
            hud.ToggleStarryNight();
            engine.JumpToStage(1);
            yield return null;
            Check(!catalog.isStarryNightActive && !catalog.vault3DRoot.activeSelf && roots[0].activeSelf, "Sector jump cancels an in-progress telescope slew");
            engine.FocusOnSun();
            yield return new WaitForSeconds(1.4f);
            Check(engine.currentTargetTransform != null && engine.currentTargetTransform.name.Contains("Sun_Photosphere"), "Sun focus resolves the nested FBX body");
            yield return Capture("sun_detail");
            engine.FocusOnSaturn();
            yield return new WaitForSeconds(1.4f);
            Check(engine.currentTargetTransform != null && engine.currentTargetTransform.name == "Saturn", "Saturn focus resolves the body rather than an orbit or ring");
            yield return Capture("saturn_detail");
            engine.FocusOnJupiter();
            yield return new WaitForSeconds(1.4f);
            Check(engine.currentTargetTransform != null && engine.currentTargetTransform.name == "Jupiter", "Jupiter focus resolves the body rather than an orbit");
            yield return Capture("jupiter_detail");

            engine.JumpToStage(1);
            hud.SelectInstrument(1);
            hud.activeUnitSystem = UnitSystem.Dual;
            yield return new WaitForSeconds(1.5f);
            yield return Capture("benchmarks_dual");
            Screen.SetResolution(1280, 720, FullScreenMode.Windowed);
            hud.userScale = 1.8f;
            yield return new WaitForSeconds(1);
            yield return Capture("compact_1280_high_scale");
            hud.ToggleStarryNight();
            yield return new WaitForSeconds(0.2f);
            yield return Capture("observatory_1280");
            hud.OpenGallery();
            yield return Capture("gallery_1280");
            hud.OpenGallery(galleryTarget);
            yield return Capture("gallery_detail_1280");
            hud.CloseOverlay();
            hud.ToggleStarryNight();
            yield return new WaitForSeconds(1.5f);
            hud.CycleHUDMode();
            Check(hud.currentHUDMode == CosmicHUD.HUDViewMode.Minimal, "Full to minimal mode");
            yield return Capture("minimal_1280");
            hud.CycleHUDMode();
            yield return Capture("cinematic_1280");
            hud.CycleHUDMode();
            Check(hud.currentHUDMode == CosmicHUD.HUDViewMode.Full, "HUD cycle restores full mode");
            hud.isLeftPanelCollapsed = hud.isRightPanelCollapsed = true;
            yield return Capture("collapsed_1280");
            hud.isLeftPanelCollapsed = hud.isRightPanelCollapsed = false;
            Screen.SetResolution(2560, 1080, FullScreenMode.Windowed);
            hud.userScale = 1.15f;
            hud.SelectInstrument(0);
            yield return new WaitForSeconds(1);
            yield return Capture("ultrawide_2560");
            results.Add("Failures: " + failures);
            File.WriteAllLines(Path.Combine(output, "verification.txt"), results);
            Application.logMessageReceived -= OnLog;
            Application.Quit(failures == 0 ? 0 : 1);
        }
    }
}

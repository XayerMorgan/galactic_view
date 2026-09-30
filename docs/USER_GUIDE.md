# Astronautica user guide

The complete illustrated field guide is included in every Windows release.
Press **F1** in the app, choose **Settings → Help & field guide**, or use the
**Astronautica Field Guide** Start menu shortcut. It opens locally in your
default browser and works offline.

When working from source, open
[`Assets/StreamingAssets/Help/index.html`](../unity_cosmic_engine/Assets/StreamingAssets/Help/index.html)
from your cloned project in a browser. GitHub displays HTML as source; download
or clone the repository, or install a release, to view the rendered guide.

## First flight

1. Launch Astronautica and choose a target in Navigation.
2. Drag the open scene to orbit; use the wheel to zoom. **R** resets your view.
3. **1–4** select solar system, Milky Way, Local Group and cosmic web. **T** starts
   or stops the narrated tour; **Space** launches an illustrative light pulse.
4. **S** opens the observatory. Set **Sky → Change observer location** before
   interpreting local altitude and azimuth. The initial preset is Mauna Kea,
   not your detected location.
5. **G** opens the offline gallery. Search for **M42**, choose the photograph,
   and use **Locate in the sky** when it is above the horizon.

**H** cycles interface visibility; **U** cycles units. **Escape** closes an
overlay, otherwise leaves the observatory, otherwise exits the app. Flight
shortcuts pause while modal controls are open. **F1** remains available.

## Your sky

Observer coordinates use decimal degrees: north/east positive, south/west
negative. Latitude ranges from −90 to +90; longitude from −180 to +180.
Time comes from the computer's current UTC clock.

Altitude is height above the horizon (0° horizon, +90° overhead); a negative
value is below it. Azimuth is clockwise from north: N 0°, E 90°, S 180°, W 270°.
Use cardinal buttons or **R** to reorient. The All targets filter includes
below-horizon objects for reading; the ground hides them from the sky view.

Positions are approximate J2000/sidereal-time calculations without precession
or atmospheric refraction. The star backdrop and landscape are decorative.
This is an orientation aid, not precision telescope guidance. Daylight, weather
and physical obstructions are not modeled.

## Gallery and sound

The gallery contains all 110 Messier objects plus 12 curated nebulae, galaxies
and clusters, each with a photograph, credits and source/usage links. Search by
name, catalog ID, constellation abbreviation or type. Filter by Messier,
Highlights or Above horizon. Some photos show a detail or wider field. M102
uses the traditional, disputed NGC 5866 identification.

Settings provides text scale, high contrast, units, narration, music volume,
Pause/Play, Next and Shuffle. **Mute all audio** also mutes narration and effects.
The nine-track soundtrack streams locally and crossfades; narration lowers its
volume temporarily. Text scale, contrast, units, observer, music volume and
shuffle persist. A 1280×720 window or larger is recommended.

## Scientific scope

The Local Group shows its three major spirals: Milky Way, Andromeda and
Triangulum (M33). Each has a label and a focus control. Dwarf galaxies are not
modeled, and spacing, sizes and orientations are schematic.

The Milky Way uses NASA/JPL’s annotated artist map. Our Sun sits in the Orion
Spur, between Sagittarius and Perseus, about 26,000 light-years from the center.
Select **Orion Spur** to inspect the neighborhood. Click the gold Sun beacon
or its label to return to the solar-system overview. Dragging still orbits.
The map remains still so its arm labels stay readable.

The 3D solar system contains Sun, Earth, Jupiter, Saturn and Neptune with schematic sizes/spacing.
Travel times use distance ÷ speed without acceleration, relativistic traveler
time or cosmic expansion. Pulses compress travel into ten playback seconds at
1×; the displayed transit time is separate.

## Installing, updating and troubleshooting

- [Download a release](https://github.com/XayerMorgan/galactic_view/releases/latest).
  The installer defaults to `%LOCALAPPDATA%\Programs\Astronautica` and needs no
  administrator rights. The release is unsigned; verify its SHA256 checksum
  against `SHA256SUMS.txt` if checking download integrity.
- Portable users must extract the entire ZIP and keep the EXE, data folders
  and DLLs together. The EXE alone is insufficient.
- Close the app before updating. Install over the same location, or extract a
  portable update into a new folder. There is no automatic updater.
- Uninstall via Windows Settings → Apps or the Start menu entry. Local
  preferences and diagnostic logs are retained.
- Missing images/help: reinstall the complete package. No sound: check Settings
  and the Windows volume mixer. Lost view: close overlays, press R, then H if
  instruments are hidden. Wrong altitude/azimuth: verify observer and time.
- [Report issues](https://github.com/XayerMorgan/galactic_view/issues) with release
  version, reproduction steps and screenshots. Unity's player log is under
  `%USERPROFILE%\AppData\LocalLow\DefaultCompany\Astronautica - Cosmic Explorer`.
  Review it for machine details before sharing.

## Privacy and licenses

The app works offline, requires no login and has no application analytics,
advertising or location service. Coordinates/preferences remain local. External
source and support links open your browser; Unity can write diagnostic files.

Code/docs are [MIT licensed](../LICENSE). Images, OpenNGC data, generated music,
art textures and the Unity runtime have [separate terms](../THIRD_PARTY_NOTICES.md).
The gallery displays individual image credits. Created in collaboration with
ElevenLabs. Astronautica was made with Unity®. Unity is a trademark or registered
trademark of Unity Technologies. Copyright © 2005-2026 Unity Technologies.
All rights reserved.

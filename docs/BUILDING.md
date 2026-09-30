# Build and release

Use Windows x64, PowerShell 5.1 or later, and Unity **6000.6.0f1** with Windows
Build Support. Activate the editor through Unity Hub using your own eligible
Unity license. No paid Unity features are required by this code. Python and
Blender are optional for regenerating assets, not for building the app.

```powershell
git clone https://github.com/XayerMorgan/galactic_view.git
cd galactic_view
# Downloads the pinned Inno Setup compiler, verifies SHA256 and its publisher,
# and installs it for the current user into the ignored build/tools/inno folder.
.\scripts\bootstrap-inno.ps1
.\scripts\build-release.ps1
```

To use other local editor/compiler locations, pass `-UnityEditor` and
`-InnoCompiler` to `build-release.ps1`. An activated editor is required even in
batch mode. Close both the app and any editor with this project open before a
build. The first import can take several minutes.

Outputs are under `dist/<VERSION>/`: installer EXE, portable ZIP and SHA256 sums.
The player build is under `CosmicZoomEngine_App/`. `-SkipPlayerBuild` packages an
existing player only after checking its version and required content.
`VERSION` controls the player and installer version. Preserve the installer's
AppId between releases so upgrades use the same uninstall entry.

The scene is assembled by `CosmicSceneBuilder.BuildStandalonePlayer` from source
scripts and imported models. Edit the builder for lasting scene changes; direct
edits to the generated scene may be replaced. Inno Setup installs per-user and
does not require elevated permissions. Build products, compiler downloads and
caches are ignored by Git. The installer is unsigned; signing requires the
maintainer's own trusted code-signing certificate and is not configured here.

## Validation

```powershell
.\scripts\test-release.ps1
```

The script installs into a unique test folder under `build/`, verifies every
payload hash, runs the native player's opt-in regression suite, tests installing
over the same version, then uninstalls. It uses a separate test Start menu group,
does not create a desktop shortcut, and refuses to run if a normal Astronautica
installation is already registered. It records results under
`visual_tests/release/`. Close the app first. The QA run temporarily exercises
audio, display sizes and observer/UI settings. Its final window size may persist.

Before publishing, also inspect the installer interactively, the Settings help
button at 1280×720, F1 from the gallery, the offline help page and image credits.
Use Gitleaks to scan both all Git history and a clean export of the release tree.
Do not publish compiler caches, credentials, local shortcuts, logs or user data.

## Assets and licensing

`LICENSE` covers original code/docs. See `THIRD_PARTY_NOTICES.md` before reusing
media. Gallery credits and the OpenNGC license are bundled in StreamingAssets.
Generated audio has provider-specific terms, not MIT. To build a version without
audio, remove the MP3 files in a separate checkout and rebuild the scene; missing
clips are handled by the player, but soundtrack-specific QA assertions will fail.

Gallery regeneration: install `requests`, `beautifulsoup4`, `Pillow`, then run
`python asset_pipeline/build_deep_sky_gallery.py`. OpenNGC is revision-pinned.
Audio normalization: `python asset_pipeline/normalize_music.py` requires NumPy
and `imageio-ffmpeg`, plus original audio in the private ignored cache. The source
tree retains finished audio; generation is not repeated during a build.

## Publishing

Update `VERSION`, build and validate, commit the intended source tree, then tag
that exact commit. Create a GitHub release with the two packages and SHA256SUMS.
The build script does not upload or publish anything. Download and checksum the
published artifacts to verify delivery. Do not label third-party media MIT.

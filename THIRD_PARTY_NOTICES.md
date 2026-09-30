# Licenses and acknowledgments

The root MIT license covers Astronautica's original application code, shaders,
build scripts, procedural geometry and documentation. It does not relicense
the telescope photographs, catalog database, audio recordings, supplied art
textures, Unity engine, or other third-party components listed below.

## Telescope photographs

The offline gallery contains 122 photographs (110 Messier objects and 12 other
deep-sky highlights). Every photograph has its own original source URL, credit,
usage URL and processing description in
`unity_cosmic_engine/Assets/StreamingAssets/Gallery/CREDITS.txt` and
`unity_cosmic_engine/Assets/Resources/DeepSkyCatalog.json`.
In installed builds, these credits are under
`CosmicZoomEngine_Data/StreamingAssets/Gallery/` and in each gallery detail view.

100 photographs were sourced from NASA science pages. Retain their specific
NASA/ESA/STScI and contributor credits. NASA hosting alone does not make an
image public domain; third-party rights and NASA's media guidelines apply:
https://www.nasa.gov/nasa-brand-center/images-and-media/

22 photographs were sourced from NOIRLab and retain their individual
NOIRLab/NSF/AURA, telescope and contributor credits under CC BY 4.0:
https://noirlab.edu/public/copyright/
https://creativecommons.org/licenses/by/4.0/

Gallery processing resizes and JPEG-encodes originals and creates thumbnails.
These illustrative color composites are scientific telescope imagery, separate
from the artistic 3D textures. No NASA, ESA, JPL or observatory endorsement is implied.

## OpenNGC catalog

Coordinates, types, constellations, catalog associations and selected aliases
are adapted from OpenNGC by Mattia Verga and contributors, revision
`75ca7ff090e1d0081a5b08be70eb3bc45ccd9e06`:
https://github.com/mattiaverga/OpenNGC

The adapted database remains CC BY-SA 4.0, independently of the MIT code.
Changes include selecting objects, converting coordinates, mapping type names,
adding common names and associating separately licensed photographs. The full
license is in `Assets/StreamingAssets/Gallery/OpenNGC-CC-BY-SA-4.0.txt` in the
Unity project. M102 uses the traditional, disputed NGC 5866 identification.

## Generated soundtrack, narration and sound effects

Audio is supplied as part of the Astronautica experience, not under MIT and not
as a stock music library or a grant of standalone music redistribution rights.
Created in collaboration with ElevenLabs. The five additional instrumentals
were generated through Magnific / ElevenLabs Music v2 on September 30, 2026:
Asterism, Drift Beyond Orion, Ion Trails, Pale Blue Horizon and The Long Return.
Their prompts and provenance are in `asset_pipeline/music_prompts.json` and
`asset_pipeline/music_manifest.json`. Processing: loudness normalization,
peak limiting and MP3 encoding; no vocals or artist imitation was requested.
Earlier acoustic music, narration and effects were supplied during the original
ElevenLabs-assisted development; their original subscription details are not
recorded in this repository.

Provider terms, rather than the code license, govern reuse of generated audio:
https://www.magnific.com/legal/terms-of-use#ai-products
https://elevenlabs.io/music-terms
https://elevenlabs.io/eleven-music-model-specific-terms

Do not infer that an MIT code license permits uploading this soundtrack to music
streaming services, selling isolated tracks or creating a reusable music library.
Replace the recordings with appropriately licensed audio for uses that require
broader rights. The application can be built without audio clips.

## Illustrative art and legacy assets

Planet/galaxy/cosmic-web textures under `assets/textures` and the Unity texture
folders, and legacy artwork under `GALAXY`, predate the telescope gallery.
They are artistic development assets; a complete per-file generation/license
record was not preserved. They are excluded from the MIT grant. The unused
spacecraft files remain in the source workbench but are not displayed by the app.
Do not use these illustrations as scientific observations or assume they carry
the telescope gallery's NASA/NOIRLab permissions.

## Unity and runtime dependencies

The Windows player is built with Unity 6000.6.0f1. Unity's proprietary engine
and its dependencies are not MIT-licensed by this project. Unity software terms:
https://unity.com/legal/editor-terms-of-service/software
The official version-specific Windows Mono Player third-party notices are
bundled in `Licenses/Unity-Player-Windows-Mono-6000.6.0f1.pdf`.

Astronautica was made with Unity®. Unity is a trademark or registered trademark
of Unity Technologies. Copyright © 2005-2026 Unity Technologies. All rights reserved.

Unity's package versions and package licenses are available in the Unity project
(`Packages/manifest.json`, `Packages/packages-lock.json`, and each restored
package's LICENSE/Third Party Notices files). The release package includes
`Licenses/` for the runtime and package notices collected at build time.

Inno Setup builds the installer. Its license and the notices for the installer
engine are included in the release's `Licenses/Inno-Setup-License.txt`.
https://jrsoftware.org/isinfo.php

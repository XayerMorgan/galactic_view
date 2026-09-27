# Cosmic Zoom Engine 🌌🔭

An interactive 3D WebGL visualization and relativistic transit calculator that transitions across four distinct scales of the known universe, providing precise travel time calculations based on the constant speed of light ($c = 299,792.458\text{ km/s}$).

---

## 🌟 Overview & Scale Regimes

The engine operates as a continuous logarithmic zoom model, transitioning smoothly from sub-light-hour solar scales to the 93-billion-light-year cosmological horizon.

### Stage 1: The Solar System Scale (~8.33 Light-Hours)
* **View**: Precise heliocentric 3D model of the major planetary bodies (Sun to Neptune) viewed obliquely to highlight the ecliptic plane.
* **Key Markers**: 
  - Dynamic solar corona with radial light falloff.
  - Earth highlighted with orbital tracking and targeting reticle.
  - Neptune as the outer system boundary ($30.07\text{ AU}$ radius, $60.14\text{ AU}$ diameter).
* **Scale**: Light-Hours ($\text{LH}$).
* **Relativistic Calculation**:
  - Distance: $8.996 \times 10^9\text{ km}$ ($60.14\text{ AU}$).
  - Transit time at $c$: **~8 hours, 20 minutes, 7 seconds** ($30,008\text{ seconds}$).

### Stage 2: The Milky Way Galaxy Scale (~100,000 Light-Years)
* **View**: As the camera zooms out, the Solar System model collapses into a pulsating stellar beacon labeled `"Our Position (Sun)"` on the Orion Spur. The barred spiral structure of the Milky Way unfolds with over 100,000 GPU-accelerated stars.
* **Key Markers**:
  - Central Galactic Bulge & **Sagittarius A\*** supermassive black hole.
  - Major density wave spiral arms (Perseus, Scutum-Centaurus, Sagittarius, Orion).
  - Interstellar gas lanes and pink H II star-forming nebulae.
* **Scale**: Light-Years ($\text{LY}$).
* **Relativistic Calculation**:
  - Distance: $9.461 \times 10^{17}\text{ km}$ ($30.7\text{ kpc}$).
  - Transit time at $c$: **~100,000 Earth Years**.

### Stage 3: The Local Group Scale (~10 Million Light-Years)
* **View**: The Milky Way contracts into a concentrated spiral disk within the local cluster filament. Highlights mutual gravitational attraction and galactic kinematics.
* **Key Markers**:
  - Milky Way Galaxy (Local anchor).
  - Andromeda Galaxy (M31, $2.54\text{ MLY}$ distance, tilted disk of 1 trillion stars).
  - Triangulum Galaxy (M33, $2.73\text{ MLY}$ distance).
  - Dwarf satellite galaxies (Large & Small Magellanic Clouds, Leo, Draco, Sculptor).
  - Dynamic gravitational interaction vectors (illustrating the MW–M31 collision course at $110\text{ km/s}$).
* **Scale**: Mega-Light-Years ($\text{MLY}$).
* **Relativistic Calculation**:
  - Distance: $9.461 \times 10^{19}\text{ km}$ ($3.07\text{ Mpc}$).
  - Transit time at $c$: **~10,000,000 Years**.

### Stage 4: The Macro Cosmic Web & Boundary (~93 Billion Light-Years)
* **View**: The Local Group becomes an infinitesimal point within the sponge-like cosmic web. Filaments of dark matter and galaxy clusters connect massive voids. The scene is enveloped by the spherical shell of the Observable Universe Boundary (the cosmological particle horizon).
* **Key Markers**:
  - Laniakea & Virgo Supercluster node.
  - Boötes Supervoid ("The Great Nothing").
  - Observable Universe Horizon ($r = 46.5\text{ GLY}$, diameter $93\text{ GLY}$) featuring Cosmic Microwave Background (CMB) thermal fluctuation rendering.
* **Scale**: Giga-Light-Years ($\text{GLY}$) / Billion Light-Years ($\text{BLY}$).
* **Relativistic Calculation**:
  - Distance: $8.798 \times 10^{23}\text{ km}$ ($28.5\text{ Gpc}$).
  - Transit time at $c$: **~93,000,000,000 Years** (exceeds universe age due to metric expansion of spacetime).

---

## ⚡ Real-Time Relativistic Transit Engine

The HUD computes transit durations dynamically across the current field of view for:
1. **Photon ($1.0c$)**: $299,792.458\text{ km/s}$
2. **Relativistic Probe ($0.1c$)**: $29,979\text{ km/s}$
3. **Parker Solar Probe**: $192\text{ km/s}$ (fastest human vehicle)
4. **Voyager 1 Interstellar Probe**: $17\text{ km/s}$
5. **Commercial Jet**: $900\text{ km/h}$

### Interactive "Fire Light Pulse"
Clicking **Fire Light Pulse (c)** emits an expanding spherical photon wavefront traveling at light speed, illustrating the difference between localized human perception of speed and galactic immensity.

---

## 🎙️ ElevenLabs Narration & Audio System

Generated using ElevenLabs voice synthesis (George) and procedural sound effects:
* `narration_stage1.mp3`: Solar system scale narration.
* `narration_stage2.mp3`: Milky Way galaxy scale narration.
* `narration_stage3.mp3`: Local Group cluster narration.
* `narration_stage4.mp3`: Cosmic web & horizon narration.
* `warp_whoosh.mp3`: Cinematic hyperspace scale shift SFX.
* `ambient_space.mp3`: Ethereal deep space continuous synthesizer drone.
* `ui_ping.mp3`: Holographic interface ping.

---

## 🚀 How to Run Locally

You can run the application directly in any modern browser:

### Option A: Direct Open
Double click `index.html` or open in your browser:
```
file:///d:/Vibe Code Repo/galactic_view/index.html
```

### Option B: Local Python HTTP Server
```bash
python -m http.server 8080
```
Then visit `http://localhost:8080` in your web browser.

---

## 🎮 Controls

* **Left Click + Drag**: Orbit / Rotate 3D perspective
* **Right Click + Drag**: Pan camera
* **Mouse Scroll**: Continuous smooth zoom through all 4 scales
* **Scale Zoom Slider**: Continuous scrub bar from Stage 1 to Stage 4
* **Stage 1–4 Cards**: Immediate jump to stage with cinematic camera flight
* **Click Any 3D Celestial Body**: Focus camera and populate holographic Inspector dossier
* **Tour Button**: Automated cinematic flythrough across the cosmos

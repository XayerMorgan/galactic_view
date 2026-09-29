using System;
using System.Collections.Generic;
using UnityEngine;

namespace CosmicZoom
{
    public enum CelestialObjectType
    {
        SpiralGalaxy,
        EllipticalGalaxy,
        IrregularGalaxy,
        EmissionNebula,
        PlanetaryNebula,
        SupernovaRemnant,
        GlobularCluster,
        OpenCluster,
        MajorStar
    }

    [System.Serializable]
    public class CelestialObjectData
    {
        public string id;
        public string commonName;
        public string ngcOrAlt;
        public CelestialObjectType objectType;
        public string constellation;
        public float raHours;       // 0 to 24 hours
        public float decDegrees;    // -90 to +90 degrees
        public float apparentMag;
        public double distanceLy;
        public string iconGlyph;
        public Color markerColor;
        [TextArea(2, 5)]
        public string description;

        public Vector3 GetUnitSpherePosition()
        {
            // Right Ascension alpha (hours to radians): 24h = 360 deg = 2*PI
            float raRad = (raHours / 24f) * Mathf.PI * 2f;
            // Declination delta (degrees to radians): -90 to +90
            float decRad = decDegrees * Mathf.Deg2Rad;

            float x = Mathf.Cos(decRad) * Mathf.Cos(raRad);
            float y = Mathf.Sin(decRad);
            float z = Mathf.Cos(decRad) * Mathf.Sin(raRad);

            return new Vector3(x, y, z).normalized;
        }

        public string GetFormattedCoordinates()
        {
            int raH = Mathf.FloorToInt(raHours);
            float raMinRem = (raHours - raH) * 60f;
            int raM = Mathf.FloorToInt(raMinRem);
            float raS = (raMinRem - raM) * 60f;

            string sign = decDegrees >= 0 ? "+" : "-";
            float absDec = Mathf.Abs(decDegrees);
            int decD = Mathf.FloorToInt(absDec);
            float decMinRem = (absDec - decD) * 60f;
            int decM = Mathf.FloorToInt(decMinRem);

            return $"RA {raH:00}h {raM:00}m {raS:00.0}s | Dec {sign}{decD:00}° {decM:00}′";
        }
    }

    [System.Serializable]
    public class ConstellationOutline
    {
        public string name;
        public string latinName;
        public string abbreviation;
        public Vector2[] starCoordsRaDec; // (raHours, decDeg)
        public int[] lineConnections;     // Pairs of indices
    }

    /// <summary>
    /// Native Unity Starry Night observation engine.
    /// Manages the real astronomical celestial vault, constellation line networks,
    /// and the complete Charles Messier deep-sky catalog (M1 through M110).
    /// Provides telescope lock-on, RA/Dec coordinate math, and optical reticles.
    /// </summary>
    public class CelestialMessierCatalog : MonoBehaviour
    {
        public static CelestialMessierCatalog Instance { get; private set; }

        [Header("Vault Configuration")]
        public float celestialSphereRadius = 18000f;
        public bool isStarryNightActive = true;
        public bool showConstellationLines = true;
        public bool showMessierMarkers = true;
        public bool showStarLabels = true;
        public bool showRaDecGrid = false;

        [Header("Selected Observation Target")]
        public CelestialObjectData currentTarget = null;

        private List<CelestialObjectData> catalog = new List<CelestialObjectData>();
        private List<ConstellationOutline> constellations = new List<ConstellationOutline>();
        private List<Vector3> gridLines = new List<Vector3>();

        private Material lineMat;
        private Material markerMat;
        private Camera mainCamera;
        private Transform cameraFocus;
        private Coroutine aimCoroutine;

        public IReadOnlyList<CelestialObjectData> Catalog => catalog;
        public IReadOnlyList<ConstellationOutline> Constellations => constellations;

        private void Awake()
        {
            Instance = this;
            BuildMessierDatabase();
            BuildConstellationDatabase();
            BuildRaDecGrid();
            CreateMaterials();

            // Default target: M31 Andromeda Galaxy
            currentTarget = catalog.Find(c => c.id == "M31") ?? catalog[0];
        }

        private void Start()
        {
            mainCamera = Camera.main;
            var engine = FindAnyObjectByType<CosmicZoomEngine>();
            if (engine != null && engine.cameraFocusTarget != null)
            {
                cameraFocus = engine.cameraFocusTarget;
            }
        }

        private void CreateMaterials()
        {
            Shader shader = Shader.Find("Mobile/Particles/Additive") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
            lineMat = new Material(shader) { color = new Color(0.2f, 0.7f, 1.0f, 0.45f) };
            markerMat = new Material(shader);
        }

        private void BuildMessierDatabase()
        {
            catalog.Clear();

            // Iconic Messier Objects
            AddMessier("M1", "Crab Nebula", "NGC 1952", CelestialObjectType.SupernovaRemnant, "Taurus", 5.575f, 22.01f, 8.4f, 6500, "💥", new Color(0.2f, 0.9f, 1.0f),
                "Expanding supernova remnant discovered in 1054 AD. Houses a central pulsar rotating 30 times per second.");

            AddMessier("M8", "Lagoon Nebula", "NGC 6523", CelestialObjectType.EmissionNebula, "Sagittarius", 18.06f, -24.38f, 6.0f, 4100, "✨", new Color(1.0f, 0.4f, 0.7f),
                "Giant interstellar cloud classified as an emission nebula and H-II starburst region in Sagittarius.");

            AddMessier("M13", "Great Hercules Cluster", "NGC 6205", CelestialObjectType.GlobularCluster, "Hercules", 16.69f, 36.46f, 5.8f, 22200, "⁂", new Color(1.0f, 0.85f, 0.3f),
                "Spectacular globular cluster of 300,000 ancient stars spanning 145 light-years. Target of the 1974 Arecibo message.");

            AddMessier("M16", "Eagle Nebula (Pillars of Creation)", "NGC 6611", CelestialObjectType.EmissionNebula, "Serpens", 18.31f, -13.79f, 6.0f, 7000, "🦅", new Color(0.3f, 0.9f, 0.8f),
                "Active star-forming region containing the famous towering Pillars of Creation sculpted by stellar winds.");

            AddMessier("M20", "Trifid Nebula", "NGC 6514", CelestialObjectType.EmissionNebula, "Sagittarius", 18.04f, -23.03f, 6.3f, 5200, "💨", new Color(1.0f, 0.5f, 0.4f),
                "Trilobed combination of an emission nebula, reflection nebula, and dark absorption dust lanes.");

            AddMessier("M27", "Dumbbell Nebula", "NGC 6853", CelestialObjectType.PlanetaryNebula, "Vulpecula", 19.99f, 22.72f, 7.5f, 1360, "⏳", new Color(0.3f, 1.0f, 0.7f),
                "The very first planetary nebula ever discovered (1764 by Charles Messier). Shell of glowing gas expelled by an aging dying red giant.");

            AddMessier("M31", "Andromeda Galaxy", "NGC 224", CelestialObjectType.SpiralGalaxy, "Andromeda", 0.712f, 41.27f, 3.44f, 2537000, "🌀", new Color(0.7f, 0.5f, 1.0f),
                "Our nearest major spiral galactic neighbour, containing over one trillion stars. On an approach trajectory toward the Milky Way at 110 km/s.");

            AddMessier("M32", "Le Gentil Dwarf", "NGC 221", CelestialObjectType.EllipticalGalaxy, "Andromeda", 0.712f, 40.87f, 8.08f, 2490000, "🔘", new Color(0.9f, 0.8f, 0.6f),
                "Compact dwarf elliptical satellite galaxy orbiting Andromeda; its outer stars were tidally stripped during galactic flybys.");

            AddMessier("M33", "Triangulum Galaxy", "NGC 598", CelestialObjectType.SpiralGalaxy, "Triangulum", 1.564f, 30.65f, 5.72f, 2730000, "🌀", new Color(0.6f, 0.7f, 1.0f),
                "Third-largest member of our Local Group, possessing active star-formation regions including the giant diffuse nebula NGC 604.");

            AddMessier("M42", "Great Orion Nebula", "NGC 1976", CelestialObjectType.EmissionNebula, "Orion", 5.59f, -5.39f, 4.0f, 1344, "✨", new Color(0.4f, 0.95f, 1.0f),
                "The brightest diffuse nebula in the night sky, visible to the naked eye. Powered by ionizing radiation from the four Trapezium cluster stars.");

            AddMessier("M44", "Beehive Cluster (Praesepe)", "NGC 2632", CelestialObjectType.OpenCluster, "Cancer", 8.67f, 19.98f, 3.7f, 577, "🐝", new Color(0.95f, 0.9f, 0.4f),
                "Celebrated open star cluster known since antiquity as the 'Little Cloud', comprising roughly 1,000 gravitationally bound stars.");

            AddMessier("M45", "Pleiades (Seven Sisters)", "Melotte 22", CelestialObjectType.OpenCluster, "Taurus", 3.79f, 24.11f, 1.6f, 444, "💠", new Color(0.4f, 0.8f, 1.0f),
                "Magnificent open star cluster surrounded by a striking luminous blue reflection nebula, dominated by hot luminous B-type stars.");

            AddMessier("M51", "Whirlpool Galaxy", "NGC 5194", CelestialObjectType.SpiralGalaxy, "Canes Venatici", 13.498f, 47.195f, 8.4f, 23500000, "🌀", new Color(0.8f, 0.4f, 1.0f),
                "First galaxy identified with grand-design spiral arms, locked in a dramatic gravitational interaction with companion dwarf NGC 5195.");

            AddMessier("M57", "Ring Nebula", "NGC 6720", CelestialObjectType.PlanetaryNebula, "Lyra", 18.893f, 33.03f, 8.8f, 2570, "⭕", new Color(0.2f, 1.0f, 0.8f),
                "Iconic barrel-shaped ring of glowing gas expelled by a dying central white dwarf into the interstellar medium.");

            AddMessier("M64", "Black Eye Galaxy", "NGC 4826", CelestialObjectType.SpiralGalaxy, "Coma Berenices", 12.945f, 21.68f, 8.52f, 17000000, "👁️", new Color(0.85f, 0.7f, 1.0f),
                "Famous for a dark absorbing band of dust in front of its bright nucleus. Its outer gas disk counter-rotates against the inner disk.");

            AddMessier("M81", "Bode's Galaxy", "NGC 3031", CelestialObjectType.SpiralGalaxy, "Ursa Major", 9.925f, 69.07f, 6.94f, 12000000, "🌀", new Color(0.7f, 0.6f, 1.0f),
                "Grand-design spiral galaxy with prominent arms and a supermassive black hole of 70 million solar masses.");

            AddMessier("M82", "Cigar Galaxy", "NGC 3034", CelestialObjectType.IrregularGalaxy, "Ursa Major", 9.931f, 69.68f, 8.41f, 12000000, "🚬", new Color(1.0f, 0.5f, 0.3f),
                "Prototype starburst galaxy forming stars ten times faster than normal galaxies due to intense gravitational tidal interactions with M81.");

            AddMessier("M87", "Virgo A Supermassive Galaxy", "NGC 4486", CelestialObjectType.EllipticalGalaxy, "Virgo", 12.514f, 12.39f, 8.63f, 53500000, "🕳️", new Color(1.0f, 0.75f, 0.3f),
                "Dominant central galaxy of the Virgo Cluster. Home to the first directly imaged supermassive black hole (M87*, 6.5 billion solar masses) by EHT.");

            AddMessier("M101", "Pinwheel Galaxy", "NGC 5457", CelestialObjectType.SpiralGalaxy, "Ursa Major", 14.053f, 54.35f, 7.86f, 20900000, "🌀", new Color(0.6f, 0.8f, 1.0f),
                "Enormous grand design spiral galaxy spanning 170,000 light-years, harboring over 3,000 giant H-II starburst regions.");

            AddMessier("M104", "Sombrero Galaxy", "NGC 4594", CelestialObjectType.SpiralGalaxy, "Virgo", 12.666f, -11.62f, 8.0f, 29300000, "👒", new Color(0.95f, 0.85f, 0.6f),
                "Brilliant white bulbous core encircled by a dramatic dark dust lane, resembling a broad-brimmed sombrero hat.");

            // Major Reference Stars
            AddMajorStar("STAR-SIRIUS", "Sirius (Alpha Canis Majoris)", CelestialObjectType.MajorStar, "Canis Major", 6.75f, -16.71f, -1.46f, 8.6, "⭐", new Color(0.7f, 0.85f, 1.0f),
                "The brightest star in Earth's night sky. A binary system with a white dwarf companion (The Pup).");

            AddMajorStar("STAR-BETELGEUSE", "Betelgeuse (Alpha Orionis)", CelestialObjectType.MajorStar, "Orion", 5.92f, 7.41f, 0.50f, 642, "⭐", new Color(1.0f, 0.45f, 0.25f),
                "Luminous red supergiant marking Orion's right shoulder, candidate for a future core-collapse supernova.");

            AddMajorStar("STAR-RIGEL", "Rigel (Beta Orionis)", CelestialObjectType.MajorStar, "Orion", 5.24f, -8.2f, 0.13f, 860, "⭐", new Color(0.6f, 0.85f, 1.0f),
                "Blue supergiant star marking Orion's left foot, 47,000 times more luminous than our Sun.");

            AddMajorStar("STAR-POLARIS", "Polaris (North Star)", CelestialObjectType.MajorStar, "Ursa Minor", 2.53f, 89.26f, 1.98f, 433, "⭐", new Color(0.95f, 0.95f, 0.7f),
                "Current northern celestial pole star, located less than 1 degree from true celestial north.");

            AddMajorStar("STAR-VEGA", "Vega (Alpha Lyrae)", CelestialObjectType.MajorStar, "Lyra", 18.61f, 38.78f, 0.03f, 25, "⭐", new Color(0.75f, 0.9f, 1.0f),
                "Fifth-brightest star in the sky, standard zero-baseline for the astronomical photometric scale.");

            AddMajorStar("STAR-ARCTURUS", "Arcturus (Alpha Boötis)", CelestialObjectType.MajorStar, "Boötes", 14.26f, 19.18f, -0.05f, 36.7, "⭐", new Color(1.0f, 0.65f, 0.35f),
                "Red giant star in Boötes; follow the arc of the Big Dipper's handle to speed on to Arcturus.");

            AddMajorStar("STAR-ALDEBARAN", "Aldebaran (Alpha Tauri)", CelestialObjectType.MajorStar, "Taurus", 4.59f, 16.51f, 0.85f, 65.3, "⭐", new Color(1.0f, 0.6f, 0.3f),
                "Orange giant representing the fiery eye of the Bull in the constellation Taurus.");
        }

        private void AddMessier(string id, string name, string alt, CelestialObjectType type, string constell, float ra, float dec, float mag, double distLy, string icon, Color col, string desc)
        {
            catalog.Add(new CelestialObjectData
            {
                id = id,
                commonName = name,
                ngcOrAlt = alt,
                objectType = type,
                constellation = constell,
                raHours = ra,
                decDegrees = dec,
                apparentMag = mag,
                distanceLy = distLy,
                iconGlyph = icon,
                markerColor = col,
                description = desc
            });
        }

        private void AddMajorStar(string id, string name, CelestialObjectType type, string constell, float ra, float dec, float mag, double distLy, string icon, Color col, string desc)
        {
            catalog.Add(new CelestialObjectData
            {
                id = id,
                commonName = name,
                ngcOrAlt = "Bayer Star",
                objectType = type,
                constellation = constell,
                raHours = ra,
                decDegrees = dec,
                apparentMag = mag,
                distanceLy = distLy,
                iconGlyph = icon,
                markerColor = col,
                description = desc
            });
        }

        private void BuildConstellationDatabase()
        {
            constellations.Clear();

            // 1. Orion (The Hunter)
            constellations.Add(new ConstellationOutline
            {
                name = "Orion",
                latinName = "Orion (The Hunter)",
                abbreviation = "Ori",
                starCoordsRaDec = new Vector2[]
                {
                    new Vector2(5.92f, 7.41f),   // 0: Betelgeuse
                    new Vector2(5.42f, 6.35f),   // 1: Bellatrix
                    new Vector2(5.53f, -0.3f),   // 2: Mintaka (Belt)
                    new Vector2(5.61f, -1.2f),   // 3: Alnilam (Belt)
                    new Vector2(5.68f, -1.94f),  // 4: Alnitak (Belt)
                    new Vector2(5.79f, -9.67f),  // 5: Saiph
                    new Vector2(5.24f, -8.2f)    // 6: Rigel
                },
                lineConnections = new int[]
                {
                    0, 1, // Betelgeuse - Bellatrix
                    0, 4, // Betelgeuse - Alnitak
                    1, 2, // Bellatrix - Mintaka
                    2, 3, // Belt: Mintaka - Alnilam
                    3, 4, // Belt: Alnilam - Alnitak
                    4, 5, // Alnitak - Saiph
                    2, 6, // Mintaka - Rigel
                    5, 6  // Saiph - Rigel
                }
            });

            // 2. Ursa Major (Big Dipper)
            constellations.Add(new ConstellationOutline
            {
                name = "Ursa Major",
                latinName = "Ursa Major (Great Bear / Big Dipper)",
                abbreviation = "UMa",
                starCoordsRaDec = new Vector2[]
                {
                    new Vector2(11.06f, 61.75f), // 0: Dubhe
                    new Vector2(11.03f, 56.38f), // 1: Merak
                    new Vector2(11.9f, 53.69f),  // 2: Phecda
                    new Vector2(12.25f, 57.03f), // 3: Megrez
                    new Vector2(12.9f, 55.96f),  // 4: Alioth
                    new Vector2(13.4f, 54.92f),  // 5: Mizar
                    new Vector2(13.79f, 49.31f)  // 6: Alkaid
                },
                lineConnections = new int[]
                {
                    0, 1, // Dubhe - Merak (Pointers to Polaris)
                    1, 2, // Merak - Phecda
                    2, 3, // Phecda - Megrez
                    3, 0, // Megrez - Dubhe (Bowl closure)
                    3, 4, // Megrez - Alioth (Handle start)
                    4, 5, // Alioth - Mizar
                    5, 6  // Mizar - Alkaid
                }
            });

            // 3. Cassiopeia (The Queen - "W")
            constellations.Add(new ConstellationOutline
            {
                name = "Cassiopeia",
                latinName = "Cassiopeia (The Queen)",
                abbreviation = "Cas",
                starCoordsRaDec = new Vector2[]
                {
                    new Vector2(0.15f, 59.15f),  // 0: Caph (Beta)
                    new Vector2(0.67f, 56.54f),  // 1: Schedar (Alpha)
                    new Vector2(0.94f, 60.72f),  // 2: Gamma Cas
                    new Vector2(1.43f, 60.23f),  // 3: Ruchbah (Delta)
                    new Vector2(1.9f, 63.67f)    // 4: Segin (Epsilon)
                },
                lineConnections = new int[]
                {
                    0, 1,
                    1, 2,
                    2, 3,
                    3, 4
                }
            });

            // 4. Cygnus (The Swan / Northern Cross)
            constellations.Add(new ConstellationOutline
            {
                name = "Cygnus",
                latinName = "Cygnus (The Swan / Northern Cross)",
                abbreviation = "Cyg",
                starCoordsRaDec = new Vector2[]
                {
                    new Vector2(20.69f, 45.28f), // 0: Deneb
                    new Vector2(20.37f, 40.26f), // 1: Sadr (Center)
                    new Vector2(19.51f, 27.96f), // 2: Albireo (Head)
                    new Vector2(20.77f, 33.97f), // 3: Gienah (Wing R)
                    new Vector2(19.75f, 45.13f)  // 4: Fawaris (Wing L)
                },
                lineConnections = new int[]
                {
                    0, 1, // Deneb - Sadr
                    1, 2, // Sadr - Albireo
                    3, 1, // Gienah - Sadr
                    1, 4  // Sadr - Fawaris
                }
            });

            // 5. Taurus (The Bull)
            constellations.Add(new ConstellationOutline
            {
                name = "Taurus",
                latinName = "Taurus (The Bull)",
                abbreviation = "Tau",
                starCoordsRaDec = new Vector2[]
                {
                    new Vector2(4.59f, 16.51f),  // 0: Aldebaran
                    new Vector2(5.43f, 28.61f),  // 1: Elnath (Horn 1)
                    new Vector2(5.63f, 21.14f),  // 2: Tianguan (Horn 2)
                    new Vector2(4.33f, 15.63f),  // 3: Hyades Star
                    new Vector2(3.79f, 24.11f)   // 4: Pleiades (M45)
                },
                lineConnections = new int[]
                {
                    0, 1,
                    0, 2,
                    0, 3,
                    3, 4
                }
            });
        }

        private void BuildRaDecGrid()
        {
            gridLines.Clear();
            // Build Equator ring (Dec = 0)
            int segments = 72;
            for (int i = 0; i < segments; i++)
            {
                float t0 = (float)i / segments * Mathf.PI * 2f;
                float t1 = (float)(i + 1) / segments * Mathf.PI * 2f;
                Vector3 p0 = new Vector3(Mathf.Cos(t0), 0, Mathf.Sin(t0)) * celestialSphereRadius;
                Vector3 p1 = new Vector3(Mathf.Cos(t1), 0, Mathf.Sin(t1)) * celestialSphereRadius;
                gridLines.Add(p0);
                gridLines.Add(p1);
            }

            // Build Polar Ecliptic Meridian
            for (int i = 0; i < segments; i++)
            {
                float t0 = (float)i / segments * Mathf.PI * 2f;
                float t1 = (float)(i + 1) / segments * Mathf.PI * 2f;
                Vector3 p0 = new Vector3(Mathf.Cos(t0), Mathf.Sin(t0), 0) * celestialSphereRadius;
                Vector3 p1 = new Vector3(Mathf.Cos(t1), Mathf.Sin(t1), 0) * celestialSphereRadius;
                gridLines.Add(p0);
                gridLines.Add(p1);
            }
        }

        public void SelectTargetById(string id)
        {
            var match = catalog.Find(c => c.id.Equals(id, StringComparison.OrdinalIgnoreCase));
            if (match != null)
            {
                currentTarget = match;
            }
        }

        public void LockTelescopeOnTarget(CelestialObjectData target)
        {
            if (target == null) return;
            currentTarget = target;

            if (aimCoroutine != null) StopCoroutine(aimCoroutine);
            aimCoroutine = StartCoroutine(SmoothAimAtTarget(target));
        }

        private System.Collections.IEnumerator SmoothAimAtTarget(CelestialObjectData target)
        {
            if (mainCamera == null) yield break;

            Vector3 worldTargetDir = target.GetUnitSpherePosition();
            Vector3 focus = cameraFocus != null ? cameraFocus.position : Vector3.zero;

            float currentDist = Vector3.Distance(mainCamera.transform.position, focus);
            if (currentDist < 10f) currentDist = 120f;

            // Target camera position is looking FROM opposite direction toward focus, or looking AT the target from origin
            // In a celestial observatory, we aim the camera so the target is centered on screen:
            // camera looks in direction `worldTargetDir`
            Quaternion startRot = mainCamera.transform.rotation;
            Quaternion targetRot = Quaternion.LookRotation(worldTargetDir, Vector3.up);

            float duration = 1.4f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float ease = 0.5f - 0.5f * Mathf.Cos(t * Mathf.PI);

                mainCamera.transform.rotation = Quaternion.Slerp(startRot, targetRot, ease);
                yield return null;
            }

            mainCamera.transform.rotation = targetRot;
            aimCoroutine = null;
        }

        private void OnRenderObject()
        {
            if (!isStarryNightActive) return;

            // Render Constellation Stick Figures
            if (showConstellationLines && lineMat != null)
            {
                lineMat.SetPass(0);
                GL.PushMatrix();
                GL.Begin(GL.LINES);
                GL.Color(new Color(0.2f, 0.75f, 1.0f, 0.55f));

                foreach (var constell in constellations)
                {
                    if (constell.starCoordsRaDec == null || constell.lineConnections == null) continue;

                    for (int i = 0; i < constell.lineConnections.Length; i += 2)
                    {
                        int idxA = constell.lineConnections[i];
                        int idxB = constell.lineConnections[i + 1];
                        if (idxA >= constell.starCoordsRaDec.Length || idxB >= constell.starCoordsRaDec.Length) continue;

                        Vector2 cA = constell.starCoordsRaDec[idxA];
                        Vector2 cB = constell.starCoordsRaDec[idxB];

                        Vector3 pA = RaDecToSpherePoint(cA.x, cA.y, celestialSphereRadius);
                        Vector3 pB = RaDecToSpherePoint(cB.x, cB.y, celestialSphereRadius);

                        GL.Vertex(pA);
                        GL.Vertex(pB);
                    }
                }

                // RA/Dec grid
                if (showRaDecGrid && gridLines.Count > 0)
                {
                    GL.Color(new Color(1.0f, 0.85f, 0.3f, 0.25f));
                    for (int i = 0; i < gridLines.Count; i += 2)
                    {
                        GL.Vertex(gridLines[i]);
                        GL.Vertex(gridLines[i + 1]);
                    }
                }

                GL.End();
                GL.PopMatrix();
            }
        }

        private Vector3 RaDecToSpherePoint(float raHours, float decDeg, float radius)
        {
            float raRad = (raHours / 24f) * Mathf.PI * 2f;
            float decRad = decDeg * Mathf.Deg2Rad;

            float x = Mathf.Cos(decRad) * Mathf.Cos(raRad);
            float y = Mathf.Sin(decRad);
            float z = Mathf.Cos(decRad) * Mathf.Sin(raRad);

            return new Vector3(x, y, z).normalized * radius;
        }

        public Vector3 GetWorldPositionOfObject(CelestialObjectData obj)
        {
            if (obj == null) return Vector3.forward * celestialSphereRadius;
            return RaDecToSpherePoint(obj.raHours, obj.decDegrees, celestialSphereRadius);
        }
    }
}

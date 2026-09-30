using System;
using System.Collections;
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
        MajorStar,
        DoubleStar,
        StarCloud,
        Nebula,
        Galaxy
    }

    [System.Serializable]
    public class CelestialObjectData
    {
        public string id;
        public string commonName;
        public string aliases;
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
        public string objectClass;
        public bool magnitudeKnown;
        public string coordinateEpoch;
        public string catalogSourceUrl;
        public string catalogLicense;
        public string imageFile;
        public string imageCredit;
        public string imageSourceUrl;
        public string imageLicense;
        public string imageLicenseUrl;
        public string imageProvider;
        public string imageAlt;
        public string imageProcessing;
        public bool HasImage => !string.IsNullOrEmpty(imageFile);
        public string ClassLabel => !string.IsNullOrEmpty(objectClass) ? objectClass : objectType == CelestialObjectType.MajorStar ? "Reference star" : objectType.ToString();

        public Vector3 GetUnitSpherePosition()
        {
            float raRad = (raHours / 24f) * Mathf.PI * 2f;
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
    public class ObserverLocation
    {
        public string locationName;
        public float latitude;  // -90 to +90
        public float longitude; // -180 to +180
        public string regionDesc;
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
    /// and the complete Charles Messier deep-sky catalog.
    /// Provides telescope lock-on, RA/Dec coordinate math, optical reticles,
    /// and 100% privacy-preserving local offline horizontal ephemeris (Alt/Az & LST).
    /// </summary>
    public class CelestialMessierCatalog : MonoBehaviour
    {
        public static CelestialMessierCatalog Instance { get; private set; }

        [Header("Privacy-Preserving Local Observer Location (100% Offline)")]
        public ObserverLocation currentObserver = new ObserverLocation
        {
            locationName = "Mauna Kea Dark-Sky Observatory",
            latitude = 19.82f,
            longitude = -155.47f,
            regionDesc = "Hawaii (Premier Optical Observatory)"
        };

        public bool onlyShowVisibleTonight = false;
        public bool showLocalHorizonPlane = true;

        public static readonly ObserverLocation[] BuiltinLocations = new ObserverLocation[]
        {
            new ObserverLocation { locationName = "Mauna Kea Observatory", latitude = 19.82f, longitude = -155.47f, regionDesc = "Hawaii (Premier Optical Observatory)" },
            new ObserverLocation { locationName = "Paranal VLT Observatory", latitude = -24.63f, longitude = -70.40f, regionDesc = "Atacama Desert, Chile (Ultra Dark Sky)" },
            new ObserverLocation { locationName = "Palomar Observatory", latitude = 33.36f, longitude = -116.86f, regionDesc = "California, USA" },
            new ObserverLocation { locationName = "Royal Observatory Greenwich", latitude = 51.48f, longitude = 0.00f, regionDesc = "London, UK (Prime Meridian)" },
            new ObserverLocation { locationName = "New York City", latitude = 40.71f, longitude = -74.01f, regionDesc = "US East Coast" },
            new ObserverLocation { locationName = "Chicago / Midwest", latitude = 41.88f, longitude = -87.63f, regionDesc = "US Midwest" },
            // Approximate city reference point: U.S. Census TIGERweb BAS26, place 4865600.
            new ObserverLocation { locationName = "San Marcos, TX", latitude = 29.8736f, longitude = -97.9367f, regionDesc = "Texas, USA" },
            new ObserverLocation { locationName = "Los Angeles", latitude = 34.05f, longitude = -118.24f, regionDesc = "US West Coast" },
            new ObserverLocation { locationName = "London", latitude = 51.51f, longitude = -0.13f, regionDesc = "United Kingdom" },
            new ObserverLocation { locationName = "Paris", latitude = 48.86f, longitude = 2.35f, regionDesc = "France" },
            new ObserverLocation { locationName = "Tokyo", latitude = 35.68f, longitude = 139.69f, regionDesc = "Japan" },
            new ObserverLocation { locationName = "Sydney", latitude = -33.87f, longitude = 151.21f, regionDesc = "Australia" }
        };

        [Header("Vault Configuration")]
        public float celestialSphereRadius = 8000f;
        public bool isStarryNightActive = false;
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
        private Material starMat;
        private Material markerMat;
        private Camera mainCamera;
        private Transform cameraFocus;
        private Coroutine aimCoroutine;

        // 3D Vault hierarchy container
        public GameObject vault3DRoot;
        private GameObject targetReticle3D;

        private Vector3 savedFlightCameraPos;
        private Quaternion savedFlightCameraRot;
        private float savedFlightFov = 45f;
        private bool hasSavedFlightCamera = false;
        private bool isReturningToFlight;
        private Transform constellationRoot, markerRoot, horizonRoot;
        private float nextHorizonUpdate;
        private Material horizonMaterial;
        private float viewAzimuth = 180, viewAltitude = 12;
        public float ViewAzimuth => viewAzimuth;
        public float ViewAltitude => viewAltitude;
        public float ViewFieldOfView => mainCamera != null ? mainCamera.fieldOfView : 75;
        public string ViewDirection => new[] { "North", "Northeast", "East", "Southeast", "South", "Southwest", "West", "Northwest" }[Mathf.RoundToInt(viewAzimuth / 45) % 8];

        public IReadOnlyList<CelestialObjectData> Catalog => catalog;
        public IReadOnlyList<ConstellationOutline> Constellations => constellations;

        private void Awake()
        {
            Instance = this;
            LoadObserverLocation();
            EnsureDatabaseBuilt();
            CreateMaterials();

            currentTarget = catalog.Find(c => c.id == "M31") ?? (catalog.Count > 0 ? catalog[0] : null);
        }

        private void Start()
        {
            mainCamera = Camera.main;
            var engine = FindAnyObjectByType<CosmicZoomEngine>();
            if (engine != null && engine.cameraFocusTarget != null)
            {
                cameraFocus = engine.cameraFocusTarget;
            }

            // Runtime meshes/materials and private reticle references are not scene assets.
            Build3DVault();
        }

        private void Update()
        {
            if (!isStarryNightActive || isReturningToFlight) return;
            ApplyVisibility();
            if (CosmicHUD.Instance != null && CosmicHUD.Instance.BlocksSceneInput) return;

            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera == null) return;

            // 1. Mouse Drag / Right-Click Look Controls
            if ((Input.GetMouseButton(1) || Input.GetMouseButton(0)) && (CosmicHUD.Instance == null || !CosmicHUD.Instance.IsPointerOverInterface()))
            {
                float mx = Input.GetAxis("Mouse X");
                float my = Input.GetAxis("Mouse Y");
                if (Mathf.Abs(mx) > 0.01f || Mathf.Abs(my) > 0.01f)
                {
                    if (aimCoroutine != null)
                    {
                        StopCoroutine(aimCoroutine);
                        aimCoroutine = null;
                    }

                    LookToward(viewAzimuth + mx * ViewFieldOfView / 34f, viewAltitude + my * ViewFieldOfView / 34f);
                }
            }

            // 2. Keyboard Pan Controls (Arrow keys / WASD)
            float h = (Input.GetKey(KeyCode.RightArrow) ? 1 : 0) - (Input.GetKey(KeyCode.LeftArrow) ? 1 : 0);
            float v = (Input.GetKey(KeyCode.UpArrow) ? 1 : 0) - (Input.GetKey(KeyCode.DownArrow) ? 1 : 0);
            if (Mathf.Abs(h) > 0.05f || Mathf.Abs(v) > 0.05f)
            {
                if (aimCoroutine != null)
                {
                    StopCoroutine(aimCoroutine);
                    aimCoroutine = null;
                }
                LookToward(viewAzimuth + h * ViewFieldOfView * 0.6f * Time.deltaTime, viewAltitude + v * ViewFieldOfView * 0.6f * Time.deltaTime);
            }

            // 3. Telescope Optical Magnification Zoom (Scroll wheel)
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.001f && (CosmicHUD.Instance == null || !CosmicHUD.Instance.IsPointerOverInterface()))
            {
                ZoomView(-scroll * 30f);
            }
            if (aimCoroutine == null) ApplyViewRotation();
            // 5. Update 3D Target Reticle Animation
            if (targetReticle3D != null && currentTarget != null)
            {
                Vector3 targetPos = RaDecToSpherePoint(currentTarget.raHours, currentTarget.decDegrees, celestialSphereRadius * 0.96f);
                targetReticle3D.transform.position = targetPos;
                targetReticle3D.transform.LookAt(mainCamera.transform.position);
                targetReticle3D.transform.Rotate(Vector3.forward, 45f * Time.deltaTime);

                float pulse = 1.0f + 0.12f * Mathf.Sin(Time.time * 4f);
                targetReticle3D.transform.localScale = Vector3.one * (65f * pulse);
            }
        }

        public void EnsureDatabaseBuilt()
        {
            if (catalog.Count == 0) BuildMessierDatabase();
            if (constellations.Count == 0) BuildConstellationDatabase();
            if (gridLines.Count == 0) BuildRaDecGrid();
        }

        private void CreateMaterials()
        {
            Shader spriteShader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
            lineMat = new Material(spriteShader) { color = new Color(0.22f, 0.74f, 0.97f, 0.65f) };

            // Use Sprites/Default or Unlit/Transparent for universal D3D12 Windows standalone support
            Shader transShader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Transparent") ?? Shader.Find("Unlit/Color");
            starMat = new Material(transShader);
            starMat.mainTexture = MakeStarTexture(64);
            if (starMat.HasProperty("_TintColor")) starMat.SetColor("_TintColor", Color.white);

            markerMat = new Material(transShader);
            markerMat.mainTexture = MakeReticleTexture(64);
            if (markerMat.HasProperty("_TintColor")) markerMat.SetColor("_TintColor", new Color(0.22f, 0.85f, 0.97f, 0.9f));
        }

        public void Build3DVault()
        {
            EnsureDatabaseBuilt();
            CreateMaterials();

            Transform existing = transform.Find("Celestial_Vault_3D");
            if (existing != null)
            {
                if (Application.isPlaying) Destroy(existing.gameObject);
                else DestroyImmediate(existing.gameObject);
            }

            vault3DRoot = new GameObject("Celestial_Vault_3D");
            vault3DRoot.transform.SetParent(transform);
            vault3DRoot.transform.localPosition = Vector3.zero;
            vault3DRoot.transform.localRotation = Quaternion.identity;

            // 1. Build Constellation Lines
            GameObject constellationsObj = new GameObject("Constellations");
            constellationsObj.transform.SetParent(vault3DRoot.transform);
            constellationsObj.transform.localPosition = Vector3.zero;

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

                    Vector3 pA = RaDecToSpherePoint(cA.x, cA.y, celestialSphereRadius * 0.99f);
                    Vector3 pB = RaDecToSpherePoint(cB.x, cB.y, celestialSphereRadius * 0.99f);

                    GameObject segObj = new GameObject($"{constell.name}_Seg_{idxA}_{idxB}");
                    segObj.transform.SetParent(constellationsObj.transform);
                    segObj.transform.localPosition = Vector3.zero;

                    LineRenderer lr = segObj.AddComponent<LineRenderer>();
                    lr.useWorldSpace = false;
                    lr.positionCount = 2;
                    lr.SetPositions(new Vector3[] { pA, pB });
                    lr.startWidth = 4.0f;
                    lr.endWidth = 4.0f;
                    lr.startColor = new Color(0.22f, 0.74f, 0.97f, 0.65f);
                    lr.endColor = new Color(0.22f, 0.74f, 0.97f, 0.65f);
                    lr.sharedMaterial = lineMat;
                }
            }

            // 2. Build Major Stars (Brilliant 3D billboard points)
            GameObject starsObj = new GameObject("Major_Stars");
            starsObj.transform.SetParent(vault3DRoot.transform);
            starsObj.transform.localPosition = Vector3.zero;

            foreach (var obj in catalog)
            {
                if (obj.objectType != CelestialObjectType.MajorStar) continue;

                Vector3 pos = RaDecToSpherePoint(obj.raHours, obj.decDegrees, celestialSphereRadius * 0.985f);
                GameObject starQuad = CreateBillboardQuad($"{obj.id}_{obj.commonName}", pos, 48f, obj.markerColor, starMat);
                starQuad.transform.SetParent(starsObj.transform);
            }

            // 3. Build Messier Object Target Nodes
            GameObject messierObj = new GameObject("Messier_Nodes");
            messierObj.transform.SetParent(vault3DRoot.transform);
            messierObj.transform.localPosition = Vector3.zero;

            foreach (var obj in catalog)
            {
                if (obj.objectType == CelestialObjectType.MajorStar) continue;

                Vector3 pos = RaDecToSpherePoint(obj.raHours, obj.decDegrees, celestialSphereRadius * 0.97f);
                GameObject nodeQuad = CreateBillboardQuad($"Node_{obj.id}_{obj.commonName}", pos, 38f, obj.markerColor, markerMat);
                nodeQuad.transform.SetParent(messierObj.transform);
            }

            // 4. Build Active Telescope Target Reticle
            targetReticle3D = CreateBillboardQuad("Active_Telescope_Target_Reticle", Vector3.forward * (celestialSphereRadius * 0.96f), 65f, new Color(0.2f, 0.95f, 1.0f, 0.95f), markerMat);
            targetReticle3D.transform.SetParent(vault3DRoot.transform);

            // An opaque local landscape with a soft atmospheric rim anchors the celestial vault.
            GameObject horizonObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            horizonObj.name = "Observer_Horizon_Ring";
            horizonObj.transform.SetParent(vault3DRoot.transform, false);
            horizonObj.transform.localScale = Vector3.one * celestialSphereRadius * 1.5f;
            var collider = horizonObj.GetComponent<Collider>();
            if (Application.isPlaying) Destroy(collider); else DestroyImmediate(collider);
            horizonMaterial = new Material(Shader.Find("Cosmic/ObserverHorizon"));
            horizonObj.GetComponent<Renderer>().sharedMaterial = horizonMaterial;
            constellationRoot = constellationsObj.transform;
            markerRoot = messierObj.transform;
            horizonRoot = horizonObj.transform;
            nextHorizonUpdate = 0;
            ApplyVisibility();
            if (vault3DRoot != null)
            {
                vault3DRoot.SetActive(isStarryNightActive);
            }

            Debug.Log($"[CelestialMessierCatalog] 3D Vault built with {constellations.Count} constellations and {catalog.Count} celestial objects. Active: {isStarryNightActive}");
        }

        private GameObject CreateBillboardQuad(string name, Vector3 pos, float size, Color col, Material baseMat)
        {
            GameObject quad = new GameObject(name);
            quad.transform.position = pos;
            quad.transform.localScale = Vector3.one * size;

            MeshFilter mf = quad.AddComponent<MeshFilter>();
            MeshRenderer mr = quad.AddComponent<MeshRenderer>();

            Mesh mesh = new Mesh { name = name + "_Mesh" };
            mesh.vertices = new Vector3[]
            {
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3( 0.5f, -0.5f, 0f),
                new Vector3( 0.5f,  0.5f, 0f),
                new Vector3(-0.5f,  0.5f, 0f)
            };
            mesh.uv = new Vector2[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0f, 1f)
            };
            mesh.triangles = new int[] { 0, 2, 1, 0, 3, 2 };
            mesh.colors = new Color[] { col, col, col, col };
            mesh.RecalculateNormals();
            mf.sharedMesh = mesh;

            Material mat = new Material(baseMat);
            mat.color = col;
            if (mat.HasProperty("_TintColor")) mat.SetColor("_TintColor", col);
            mr.sharedMaterial = mat;

            // Orient directly toward origin
            quad.transform.LookAt(Vector3.zero);
            quad.transform.Rotate(0, 180f, 0);

            return quad;
        }

        private Texture2D MakeStarTexture(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float center = (size - 1) / 2.0f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - center) / center;
                    float dy = (y - center) / center;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);

                    if (r >= 1.0f)
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                    else
                    {
                        float core = Mathf.Pow(1.0f - r, 2.8f);
                        float flareH = Mathf.Pow(Mathf.Clamp01(1.0f - Mathf.Abs(dy) * 7f), 3f) * Mathf.Pow(Mathf.Clamp01(1.0f - Mathf.Abs(dx)), 2f) * 0.6f;
                        float flareV = Mathf.Pow(Mathf.Clamp01(1.0f - Mathf.Abs(dx) * 7f), 3f) * Mathf.Pow(Mathf.Clamp01(1.0f - Mathf.Abs(dy)), 2f) * 0.6f;
                        float a = Mathf.Clamp01(core + flareH + flareV);
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                    }
                }
            }
            tex.Apply();
            return tex;
        }

        private Texture2D MakeReticleTexture(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float center = (size - 1) / 2.0f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Abs((x - center) / center);
                    float dy = Mathf.Abs((y - center) / center);
                    float diamondDist = dx + dy;

                    float alpha = 0f;
                    // Diamond border: ~0.78 to 0.94
                    if (diamondDist >= 0.78f && diamondDist <= 0.94f)
                    {
                        alpha = 0.95f;
                    }
                    // Central luminous core
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    if (r < 0.22f)
                    {
                        alpha = Mathf.Max(alpha, (1.0f - r / 0.22f));
                    }

                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();
            return tex;
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
            AddMajorStar("STAR-SIRIUS", "Sirius (Alpha Canis Majoris)", CelestialObjectType.MajorStar, "Canis Major", 6.75f, -16.71f, -1.46f, 8.6, "⭐", new Color(0.75f, 0.88f, 1.0f),
                "The brightest star in Earth's night sky. A brilliant binary system with a white dwarf companion (The Pup).");

            AddMajorStar("STAR-BETELGEUSE", "Betelgeuse (Alpha Orionis)", CelestialObjectType.MajorStar, "Orion", 5.92f, 7.41f, 0.50f, 642, "⭐", new Color(1.0f, 0.45f, 0.25f),
                "Luminous red supergiant marking Orion's right shoulder, candidate for a future core-collapse supernova.");

            AddMajorStar("STAR-RIGEL", "Rigel (Beta Orionis)", CelestialObjectType.MajorStar, "Orion", 5.24f, -8.2f, 0.13f, 860, "⭐", new Color(0.65f, 0.88f, 1.0f),
                "Blue supergiant star marking Orion's left foot, 47,000 times more luminous than our Sun.");

            AddMajorStar("STAR-POLARIS", "Polaris (North Star)", CelestialObjectType.MajorStar, "Ursa Minor", 2.53f, 89.26f, 1.98f, 433, "⭐", new Color(0.95f, 0.95f, 0.7f),
                "Current northern celestial pole star, located less than 1 degree from true celestial north.");

            AddMajorStar("STAR-VEGA", "Vega (Alpha Lyrae)", CelestialObjectType.MajorStar, "Lyra", 18.61f, 38.78f, 0.03f, 25, "⭐", new Color(0.85f, 0.95f, 1.0f),
                "Fifth-brightest star in the sky, standard zero-baseline for the astronomical photometric scale.");

            AddMajorStar("STAR-ARCTURUS", "Arcturus (Alpha Boötis)", CelestialObjectType.MajorStar, "Boötes", 14.26f, 19.18f, -0.05f, 36.7, "⭐", new Color(1.0f, 0.65f, 0.35f),
                "Red giant star in Boötes; follow the arc of the Big Dipper's handle to speed on to Arcturus.");

            AddMajorStar("STAR-ALDEBARAN", "Aldebaran (Alpha Tauri)", CelestialObjectType.MajorStar, "Taurus", 4.59f, 16.51f, 0.85f, 65.3, "⭐", new Color(1.0f, 0.6f, 0.3f),
                "Orange giant representing the fiery eye of the Bull in the constellation Taurus.");

            AddMajorStar("STAR-DENEB", "Deneb (Alpha Cygni)", CelestialObjectType.MajorStar, "Cygnus", 20.69f, 45.28f, 1.25f, 2600, "⭐", new Color(0.9f, 0.95f, 1.0f),
                "Extremely luminous white supergiant marking the tail of Cygnus the Swan and vertex of the Summer Triangle.");

            AddMajorStar("STAR-ALTAIR", "Altair (Alpha Aquilae)", CelestialObjectType.MajorStar, "Aquila", 19.84f, 8.87f, 0.77f, 16.7, "⭐", new Color(0.95f, 0.98f, 1.0f),
                "Rapidly rotating white main-sequence star, flattened at its poles by centrifugal rotation.");

            AddMajorStar("STAR-SPICA", "Spica (Alpha Virginis)", CelestialObjectType.MajorStar, "Virgo", 13.42f, -11.16f, 0.98f, 250, "⭐", new Color(0.7f, 0.85f, 1.0f),
                "Bright spectroscopic binary star in Virgo; spike down from Arcturus to find Spica.");
            LoadCompleteGalleryCatalog();
        }

        [Serializable] private class CatalogFile { public CelestialObjectData[] entries; }

        private void LoadCompleteGalleryCatalog()
        {
            TextAsset data = Resources.Load<TextAsset>("DeepSkyCatalog");
            if (data == null) { Debug.LogError("DeepSkyCatalog resource is missing."); return; }
            var complete = JsonUtility.FromJson<CatalogFile>(data.text);
            foreach (var obj in complete.entries)
            {
                var original = catalog.Find(old => old.id == obj.id);
                obj.markerColor = original != null ? original.markerColor : new Color(0.45f, 0.85f, 0.90f);
                if (original != null)
                {
                    obj.distanceLy = original.distanceLy;
                    if (obj.commonName.StartsWith("Messier ")) obj.commonName = original.commonName;
                }
            }
            catalog.RemoveAll(obj => obj.objectType != CelestialObjectType.MajorStar);
            catalog.InsertRange(0, complete.entries);
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

            // 6. Ursa Minor (Little Dipper)
            constellations.Add(new ConstellationOutline
            {
                name = "Ursa Minor",
                latinName = "Ursa Minor (Little Bear / Little Dipper)",
                abbreviation = "UMi",
                starCoordsRaDec = new Vector2[]
                {
                    new Vector2(2.53f, 89.26f),  // 0: Polaris
                    new Vector2(16.96f, 82.04f), // 1: Yildun
                    new Vector2(15.73f, 77.79f), // 2: Anwar al Farkadain
                    new Vector2(15.34f, 71.83f), // 3: Akhfa al Farkadain
                    new Vector2(15.14f, 74.16f), // 4: Kochab
                    new Vector2(15.85f, 72.82f)  // 5: Pherkad
                },
                lineConnections = new int[]
                {
                    0, 1,
                    1, 2,
                    2, 3,
                    3, 4,
                    4, 5,
                    5, 2
                }
            });

            // 7. Leo (The Lion)
            constellations.Add(new ConstellationOutline
            {
                name = "Leo",
                latinName = "Leo (The Lion)",
                abbreviation = "Leo",
                starCoordsRaDec = new Vector2[]
                {
                    new Vector2(10.14f, 11.97f), // 0: Regulus
                    new Vector2(10.33f, 19.84f), // 1: Algieba
                    new Vector2(10.28f, 23.42f), // 2: Adhafera
                    new Vector2(9.88f, 26.01f),  // 3: Rasalas
                    new Vector2(11.23f, 15.43f), // 4: Chertan
                    new Vector2(11.82f, 14.57f)  // 5: Denebola
                },
                lineConnections = new int[]
                {
                    0, 1,
                    1, 2,
                    2, 3,
                    1, 4,
                    4, 5
                }
            });
        }

        private void BuildRaDecGrid()
        {
            gridLines.Clear();
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
        }

        public void SelectTargetById(string id)
        {
            EnsureDatabaseBuilt();
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
            EnterObservatory();
            if (aimCoroutine != null) StopCoroutine(aimCoroutine);
            aimCoroutine = StartCoroutine(SmoothAimAtTarget(target));
        }

        public void EnterObservatory()
        {
            if (isStarryNightActive && !isReturningToFlight) return;

            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera != null && !isStarryNightActive)
            {
                savedFlightCameraPos = mainCamera.transform.position;
                savedFlightCameraRot = mainCamera.transform.rotation;
                savedFlightFov = mainCamera.fieldOfView;
                hasSavedFlightCamera = true;
            }

            var engine = FindAnyObjectByType<CosmicZoomEngine>();
            if (engine != null)
            {
                engine.StopTour();
                engine.lightPulseEmitter?.CancelPulse();
                if (engine.stage1SolarSystem != null) engine.stage1SolarSystem.SetActive(false);
                if (engine.stage2MilkyWay != null) engine.stage2MilkyWay.SetActive(false);
                if (engine.stage3LocalGroup != null) engine.stage3LocalGroup.SetActive(false);
                if (engine.stage4CosmicWeb != null) engine.stage4CosmicWeb.SetActive(false);
            }
            isReturningToFlight = false;
            isStarryNightActive = true;
            if (mainCamera != null) mainCamera.transform.position = Vector3.zero;
            if (vault3DRoot != null) vault3DRoot.SetActive(true);

            ResetSkyView();
        }

        public Vector3 HorizonDirection(float azimuth, float altitude)
        {
            Vector3 up = GetObserverZenithUnitVector();
            float lst = (float)GetLocalSiderealTimeHours() * 15 * Mathf.Deg2Rad;
            Vector3 east = new Vector3(-Mathf.Sin(lst), 0, Mathf.Cos(lst));
            Vector3 north = Vector3.Cross(east, up).normalized;
            float az = azimuth * Mathf.Deg2Rad, alt = altitude * Mathf.Deg2Rad;
            return up * Mathf.Sin(alt) + (north * Mathf.Cos(az) + east * Mathf.Sin(az)) * Mathf.Cos(alt);
        }

        private void ApplyViewRotation()
        {
            if (mainCamera != null)
                mainCamera.transform.rotation = Quaternion.LookRotation(HorizonDirection(viewAzimuth, viewAltitude), GetObserverZenithUnitVector());
        }

        public void LookToward(float azimuth, float altitude = 12)
        {
            if (!isStarryNightActive || isReturningToFlight) return;
            if (aimCoroutine != null) StopCoroutine(aimCoroutine);
            aimCoroutine = null;
            viewAzimuth = Mathf.Repeat(azimuth, 360);
            viewAltitude = Mathf.Clamp(altitude, -15, 89.5f);
            ApplyViewRotation();
        }

        public void ZoomView(float delta)
        {
            if (mainCamera != null && isStarryNightActive && !isReturningToFlight)
                mainCamera.fieldOfView = Mathf.Clamp(mainCamera.fieldOfView + delta, 10, 90);
        }

        public void ResetSkyView()
        {
            if (!isStarryNightActive || isReturningToFlight) return;
            showLocalHorizonPlane = true;
            LookToward(180, 12);
            if (mainCamera != null) mainCamera.fieldOfView = 75;
            nextHorizonUpdate = 0;
            ApplyVisibility();
        }

        public void LeaveObservatoryImmediately()
        {
            if (!isStarryNightActive) return;
            if (aimCoroutine != null) StopCoroutine(aimCoroutine);
            aimCoroutine = null;
            isStarryNightActive = false;
            isReturningToFlight = false;
            if (vault3DRoot != null) vault3DRoot.SetActive(false);
            if (mainCamera != null && hasSavedFlightCamera)
            {
                mainCamera.transform.position = savedFlightCameraPos;
                mainCamera.transform.rotation = savedFlightCameraRot;
                mainCamera.fieldOfView = savedFlightFov;
            }
            hasSavedFlightCamera = false;
        }

        private void ApplyVisibility()
        {
            if (constellationRoot != null) constellationRoot.gameObject.SetActive(showConstellationLines);
            if (markerRoot != null) markerRoot.gameObject.SetActive(showMessierMarkers);
            if (horizonRoot == null) return;
            horizonRoot.gameObject.SetActive(showLocalHorizonPlane);
            if (!showLocalHorizonPlane || Time.unscaledTime < nextHorizonUpdate) return;
            nextHorizonUpdate = Time.unscaledTime + 1;
            if (horizonMaterial != null)
            {
                horizonMaterial.SetVector("_Zenith", GetObserverZenithUnitVector());
                horizonMaterial.SetVector("_North", HorizonDirection(0, 0));
                horizonMaterial.SetVector("_East", HorizonDirection(90, 0));
            }
        }

        public void ExitStarryNight()
        {
            if (!isStarryNightActive || isReturningToFlight) return;
            isReturningToFlight = true;

            if (aimCoroutine != null) StopCoroutine(aimCoroutine);
            aimCoroutine = StartCoroutine(SmoothReturnToFlightDeck());
        }

        private IEnumerator SmoothReturnToFlightDeck()
        {
            if (mainCamera == null) mainCamera = Camera.main;

            if (mainCamera != null && hasSavedFlightCamera)
            {
                Vector3 startPos = mainCamera.transform.position;
                Quaternion startRot = mainCamera.transform.rotation;
                float startFov = mainCamera.fieldOfView;

                float duration = 1.0f;
                float elapsed = 0f;

                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);
                    float ease = 0.5f - 0.5f * Mathf.Cos(t * Mathf.PI);

                    mainCamera.transform.position = Vector3.Lerp(startPos, savedFlightCameraPos, ease);
                    mainCamera.transform.rotation = Quaternion.Slerp(startRot, savedFlightCameraRot, ease);
                    mainCamera.fieldOfView = Mathf.Lerp(startFov, savedFlightFov, ease);
                    yield return null;
                }

                mainCamera.transform.position = savedFlightCameraPos;
                mainCamera.transform.rotation = savedFlightCameraRot;
                mainCamera.fieldOfView = savedFlightFov;
            }

            isStarryNightActive = false;
            isReturningToFlight = false;
            hasSavedFlightCamera = false;
            aimCoroutine = null;

            if (vault3DRoot != null)
            {
                vault3DRoot.SetActive(false);
            }

            var engine = FindAnyObjectByType<CosmicZoomEngine>();
            if (engine != null)
            {
                engine.JumpToStage(engine.activeStageIndex);
            }
        }

        private IEnumerator SmoothAimAtTarget(CelestialObjectData target)
        {
            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera == null) yield break;

            var targetAltAz = CalculateAltAz(target.raHours, target.decDegrees);
            float startAzimuth = viewAzimuth, startAltitude = viewAltitude;
            float startFov = mainCamera.fieldOfView;
            float elapsed = 0;
            while (elapsed < 1.2f)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / 1.2f);
                float ease = 0.5f - 0.5f * Mathf.Cos(t * Mathf.PI);
                viewAzimuth = Mathf.Repeat(Mathf.LerpAngle(startAzimuth, targetAltAz.azimuthDeg, ease), 360);
                viewAltitude = Mathf.Lerp(startAltitude, Mathf.Clamp(targetAltAz.altitudeDeg, -15, 89.5f), ease);
                mainCamera.fieldOfView = Mathf.Lerp(startFov, 45, ease);
                ApplyViewRotation();
                yield return null;
            }
            aimCoroutine = null;
        }

        public void SetObserverLocation(string name, float lat, float lon, string region)
        {
            nextHorizonUpdate = 0;
            currentObserver.locationName = name;
            currentObserver.latitude = Mathf.Clamp(lat, -90f, 90f);
            currentObserver.longitude = Mathf.Clamp(lon, -180f, 180f);
            currentObserver.regionDesc = region;

            PlayerPrefs.SetString("Cosmic_Obs_Name", currentObserver.locationName);
            PlayerPrefs.SetFloat("Cosmic_Obs_Lat", currentObserver.latitude);
            PlayerPrefs.SetFloat("Cosmic_Obs_Lon", currentObserver.longitude);
            PlayerPrefs.SetString("Cosmic_Obs_Region", currentObserver.regionDesc);
            PlayerPrefs.Save();
            if (isStarryNightActive) ResetSkyView();
        }

        public void LoadObserverLocation()
        {
            if (PlayerPrefs.HasKey("Cosmic_Obs_Name"))
            {
                currentObserver.locationName = PlayerPrefs.GetString("Cosmic_Obs_Name", currentObserver.locationName);
                currentObserver.latitude = PlayerPrefs.GetFloat("Cosmic_Obs_Lat", currentObserver.latitude);
                currentObserver.longitude = PlayerPrefs.GetFloat("Cosmic_Obs_Lon", currentObserver.longitude);
                currentObserver.regionDesc = PlayerPrefs.GetString("Cosmic_Obs_Region", currentObserver.regionDesc);
            }
        }

        public static double GetCurrentGMSTHours()
        {
            DateTime utc = DateTime.UtcNow;
            DateTime j2000 = new DateTime(2000, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            double d = (utc - j2000).TotalDays;

            double gmst = 18.697374558 + 24.06570982441908 * d;
            gmst = (gmst % 24.0 + 24.0) % 24.0;
            return gmst;
        }

        public double GetLocalSiderealTimeHours()
        {
            double gmst = GetCurrentGMSTHours();
            double lst = gmst + (currentObserver.longitude / 15.0);
            return (lst % 24.0 + 24.0) % 24.0;
        }

        public (float altitudeDeg, float azimuthDeg, bool isAboveHorizon) CalculateAltAz(float raHours, float decDeg)
        {
            return CalculateAltAzAtSiderealTime(raHours, decDeg, currentObserver.latitude, GetLocalSiderealTimeHours());
        }

        public static (float altitudeDeg, float azimuthDeg, bool isAboveHorizon) CalculateAltAzAtSiderealTime(float raHours, float decDeg, float latitude, double lstHours)
        {
            double hourAngleHours = lstHours - raHours;
            double haRad = (hourAngleHours * 15.0) * Mathf.Deg2Rad;
            double latRad = latitude * Mathf.Deg2Rad;
            double decRad = decDeg * Mathf.Deg2Rad;

            double sinAlt = Math.Sin(latRad) * Math.Sin(decRad) + Math.Cos(latRad) * Math.Cos(decRad) * Math.Cos(haRad);
            sinAlt = Math.Max(-1.0, Math.Min(1.0, sinAlt));
            double altRad = Math.Asin(sinAlt);
            float altDeg = (float)(altRad * Mathf.Rad2Deg);

            // atan2 resolves all quadrants without dividing by cos(latitude) at the poles.
            double east = -Math.Cos(decRad) * Math.Sin(haRad);
            double north = Math.Sin(decRad) * Math.Cos(latRad) - Math.Cos(decRad) * Math.Sin(latRad) * Math.Cos(haRad);
            float azDeg = Mathf.Repeat((float)(Math.Atan2(east, north) * Mathf.Rad2Deg), 360);

            return (altDeg, azDeg, altDeg > 0f);
        }

        public Vector3 GetObserverZenithUnitVector()
        {
            double lst = GetLocalSiderealTimeHours();
            float raRad = (float)(lst / 24.0 * Math.PI * 2.0);
            float decRad = currentObserver.latitude * Mathf.Deg2Rad;

            float x = Mathf.Cos(decRad) * Mathf.Cos(raRad);
            float y = Mathf.Sin(decRad);
            float z = Mathf.Cos(decRad) * Mathf.Sin(raRad);

            return new Vector3(x, y, z).normalized;
        }

        public Vector3 RaDecToSpherePoint(float raHours, float decDeg, float radius)
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

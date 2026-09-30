using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CosmicZoom
{
    /// <summary>Offline, attributed telescope-image gallery. Textures are loaded on demand and bounded.</summary>
    public sealed class CosmicGallery : IDisposable
    {
        public bool IsOpen { get; private set; }
        public CelestialObjectData Selected { get; private set; }
        public string Search { get; set; } = "";
        private int collection;
        private bool aboveOnly;
        private Vector2 scroll, detailScroll;
        private readonly List<CelestialObjectData> filtered = new List<CelestialObjectData>();
        private readonly Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();
        private readonly Queue<string> loaded = new Queue<string>();
        private GUIStyle searchStyle;

        public void Open(CelestialObjectData target = null)
        {
            IsOpen = true;
            Selected = target != null && target.HasImage ? target : null;
            scroll = detailScroll = Vector2.zero;
        }
        public void Close() { IsOpen = false; }
        public void Dispose()
        {
            foreach (var texture in textures.Values) if (texture != null) UnityEngine.Object.Destroy(texture);
            textures.Clear(); loaded.Clear();
        }
        public Texture2D ImageFor(CelestialObjectData target, bool thumbnail)
        {
            if (target == null || !target.HasImage) return null;
            string key = (thumbnail ? "thumbs/" : "images/") + Path.GetFileName(target.imageFile);
            if (textures.TryGetValue(key, out var cached)) return cached;
            string path = Path.Combine(Application.streamingAssetsPath, "Gallery", key);
            Texture2D image = null;
            if (File.Exists(path))
            {
                image = new Texture2D(2, 2, TextureFormat.RGB24, false);
                if (!image.LoadImage(File.ReadAllBytes(path), true)) { UnityEngine.Object.Destroy(image); image = null; }
            }
            while (loaded.Count >= 40)
            {
                string old = loaded.Dequeue();
                if (textures[old] != null) UnityEngine.Object.Destroy(textures[old]);
                textures.Remove(old);
            }
            textures[key] = image;
            loaded.Enqueue(key);
            return image;
        }
        public int Filter(CelestialMessierCatalog catalog)
        {
            filtered.Clear();
            foreach (var obj in catalog.Catalog)
            {
                if (!obj.HasImage) continue;
                bool messier = obj.id.StartsWith("M", StringComparison.Ordinal);
                if (collection == 1 && !messier || collection == 2 && messier) continue;
                if (aboveOnly && !catalog.CalculateAltAz(obj.raHours, obj.decDegrees).isAboveHorizon) continue;
                string haystack = obj.id + " " + obj.commonName + " " + obj.aliases + " " + obj.ngcOrAlt + " " + obj.ClassLabel + " " + obj.constellation;
                // Ignore spaces in identifiers so M 31 / NGC 224 work as expected.
                if (!string.IsNullOrWhiteSpace(Search) && haystack.Replace(" ", "").IndexOf(Search.Trim().Replace(" ", ""), StringComparison.OrdinalIgnoreCase) < 0) continue;
                filtered.Add(obj);
            }
            return filtered.Count;
        }
        internal void Draw(CosmicConsoleDrawing art, Rect bounds, CelestialMessierCatalog catalog)
        {
            if (!IsOpen || catalog == null) return;
            art.Fill(new Rect(0, 0, Screen.width / CosmicHUD.Instance.InterfaceScale, Screen.height / CosmicHUD.Instance.InterfaceScale), new Color(0.005f, 0.012f, 0.018f, 0.97f));
            art.Panel(bounds, "04", "DEEP SKY GALLERY");
            if (art.Button(new Rect(bounds.xMax - 122, bounds.y + 14, 104, 32), "Close [Esc]")) { Close(); return; }
            if (Selected != null) { DrawDetail(art, bounds, catalog); return; }
            float x = bounds.x + 22, y = bounds.y + 62, w = bounds.width - 44;
            if (searchStyle == null)
            {
                searchStyle = new GUIStyle(GUI.skin.textField) { fontSize = 15, padding = new RectOffset(12, 10, 8, 8) };
                searchStyle.normal.textColor = Color.white;
                searchStyle.focused.textColor = Color.white;
            }
            art.Text(new Rect(x, y, 270, 20), "SEARCH BY NAME, NUMBER OR TYPE", 10, art.Muted);
            GUI.SetNextControlName("GallerySearch");
            string query = GUI.TextField(new Rect(x, y + 25, 310, 36), Search, searchStyle);
            if (query != Search) { Search = query; scroll = Vector2.zero; }
            string[] groups = { "All objects", "Messier 110", "Highlights 12" };
            for (int i = 0; i < 3; i++)
                if (art.Button(new Rect(x + 332 + i * 120, y + 25, 112, 36), groups[i], collection == i)) { collection = i; scroll = Vector2.zero; GUI.FocusControl(null); }
            if (art.Button(new Rect(x + w - 174, y + 25, 174, 36), "Above horizon now", aboveOnly)) { aboveOnly = !aboveOnly; scroll = Vector2.zero; }
            int count = Filter(catalog);
            art.Text(new Rect(x, y + 74, w, 22), $"{count} {(count == 1 ? "object" : "objects")}  /  {catalog.currentObserver.locationName}  /  UTC {DateTime.UtcNow:yyyy-MM-dd HH:mm}", 12, art.Teal);
            Rect viewport = new Rect(x, y + 108, w, bounds.height - 214);
            int columns = Mathf.Clamp(Mathf.FloorToInt(w / 240), 3, 6);
            float cardWidth = (w - 22 - (columns - 1) * 16) / columns;
            float cardHeight = cardWidth * 0.61f + 100;
            int rows = (count + columns - 1) / columns;
            scroll = GUI.BeginScrollView(viewport, scroll, new Rect(0, 0, w - 20, Mathf.Max(viewport.height, rows * (cardHeight + 18))), false, false);
            for (int i = 0; i < filtered.Count; i++)
            {
                float cy = (i / columns) * (cardHeight + 18);
                if (cy + cardHeight < scroll.y || cy > scroll.y + viewport.height) continue;
                var obj = filtered[i];
                Rect card = new Rect((i % columns) * (cardWidth + 16), cy, cardWidth, cardHeight);
                art.Round(card, new Color(0.027f, 0.048f, 0.059f));
                Texture2D image = ImageFor(obj, true);
                if (image != null) GUI.DrawTexture(new Rect(card.x + 6, cy + 6, cardWidth - 12, cardWidth * 0.61f), image, ScaleMode.ScaleToFit);
                float ty = cy + cardWidth * 0.61f + 12;
                art.Text(new Rect(card.x + 12, ty, cardWidth - 24, 38), obj.id + " / " + obj.commonName, 13, art.Ink);
                var (alt, az, above) = catalog.CalculateAltAz(obj.raHours, obj.decDegrees);
                art.Text(new Rect(card.x + 12, ty + 48, cardWidth - 24, 32), $"Altitude {alt:+0.0;-0.0}°  /  Azimuth {az:000.0}°", 11, above ? art.Teal : art.Muted);
                if (GUI.Button(card, GUIContent.none, GUIStyle.none)) { Selected = obj; detailScroll = Vector2.zero; GUI.FocusControl(null); }
            }
            GUI.EndScrollView();
            if (count == 0) art.Text(new Rect(x, viewport.y + 35, w, 70), "No matching objects. Try another name or turn off the above-horizon filter.", 17, art.Muted, TextAnchor.MiddleCenter);
            art.Text(new Rect(x, bounds.yMax - 31, w, 20), "Real telescope imagery · open an image for its full credit and source · approximate sky positions", 11, art.Muted);
        }
        private void DrawDetail(CosmicConsoleDrawing art, Rect b, CelestialMessierCatalog catalog)
        {
            float x = b.x + 22, top = b.y + 62;
            if (art.Button(new Rect(x, top, 154, 32), "‹  Browse all images")) { Selected = null; return; }
            var obj = Selected;
            art.Text(new Rect(x + 175, top, b.width - 340, 32), obj.id + " / " + obj.commonName, 22, art.Ink);
            float sidebarWidth = 316, gap = 26;
            float imageWidth = b.width - sidebarWidth - gap - 44;
            var creditStyle = new GUIStyle(GUI.skin.label) { fontSize = 11, wordWrap = true, padding = new RectOffset() };
            float creditHeight = Mathf.Max(34, creditStyle.CalcHeight(new GUIContent(obj.imageCredit), imageWidth));
            Rect frame = new Rect(x, top + 51, imageWidth, b.height - 170 - creditHeight);
            art.Round(frame, new Color(0.012f, 0.021f, 0.029f));
            Texture2D image = ImageFor(obj, false);
            if (image != null) GUI.DrawTexture(frame, image, ScaleMode.ScaleToFit);
            else art.Text(frame, "Image unavailable", 20, art.Muted, TextAnchor.MiddleCenter);
            art.Text(new Rect(x, frame.yMax + 12, imageWidth, creditHeight), obj.imageCredit, 11, art.Ink);
            float rx = frame.xMax + gap;
            var (alt, az, above) = catalog.CalculateAltAz(obj.raHours, obj.decDegrees);
            art.Text(new Rect(rx, frame.y, sidebarWidth, 22), "POSITION FROM YOUR OBSERVER", 10, art.Muted);
            art.Text(new Rect(rx, frame.y + 31, 150, 20), "ALTITUDE", 11, art.Teal);
            art.Text(new Rect(rx + 166, frame.y + 31, 150, 20), "AZIMUTH", 11, art.Teal);
            art.Text(new Rect(rx, frame.y + 55, 150, 42), $"{alt:+0.0;-0.0}°", 29, art.Ink);
            art.Text(new Rect(rx + 166, frame.y + 55, 150, 42), $"{az:000.0}°", 29, art.Ink);
            art.Text(new Rect(rx, frame.y + 105, sidebarWidth, 24), above ? "Above the horizon now" : "Below the horizon now", 13, above ? art.Teal : art.Amber);
            if (art.Button(new Rect(rx, frame.y + 143, sidebarWidth, 38), "Locate in the sky / " + obj.id, primary: true, enabled: above))
            {
                catalog.LockTelescopeOnTarget(obj);
                CosmicHUD.Instance.SelectInstrument(2);
                Close();
            }
            Rect info = new Rect(rx, frame.y + 197, sidebarWidth, frame.height - 197);
            detailScroll = GUI.BeginScrollView(info, detailScroll, new Rect(0, 0, sidebarWidth - 20, 666), false, false);
            float w = sidebarWidth - 24;
            art.Text(new Rect(0, 0, w, 50), catalog.currentObserver.locationName + $"\nUTC {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}", 12, art.Muted);
            art.Text(new Rect(0, 60, w, 60), "Altitude: 0° at the horizon, 90° overhead. Azimuth: 0° north, 90° east, 180° south, 270° west.", 12, art.Ink);
            art.Text(new Rect(0, 135, w, 62), obj.ClassLabel + " / " + obj.constellation + "\n" + obj.GetFormattedCoordinates(), 12, art.Teal);
            art.Text(new Rect(0, 205, w, 75), obj.description, 12, art.Muted);
            art.Text(new Rect(0, 294, w, 18), "IMAGE CREDIT", 10, art.Amber);
            art.Text(new Rect(0, 320, w, 126), obj.imageCredit, 12, art.Ink);
            art.Text(new Rect(0, 452, w, 55), obj.imageLicense, 11, art.Muted);
            if (art.Button(new Rect(0, 513, w, 30), "Open original source ↗")) Application.OpenURL(obj.imageSourceUrl);
            if (art.Button(new Rect(0, 551, w, 30), "Image usage terms ↗")) Application.OpenURL(obj.imageLicenseUrl);
            art.Text(new Rect(0, 594, w, 66), "J2000 catalog positions; approximate alt/az. Refraction, daylight and weather are not modeled. Telescope images may show a detail of the object.", 11, art.Muted);
            GUI.EndScrollView();
            art.Text(new Rect(x, b.yMax - 33, frame.width, 22), obj.imageProvider + " / aspect ratio preserved", 11, art.Muted);
        }
    }
}

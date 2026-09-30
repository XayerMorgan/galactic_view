using System.Collections.Generic;
using UnityEngine;

namespace CosmicZoom
{
    // Resolution-independent instrument marks. No bitmap lettering or platform emoji.
    internal sealed class CosmicConsoleDrawing
    {
        public readonly Color Ink = new Color(0.88f, 0.91f, 0.90f);
        public readonly Color Muted = new Color(0.55f, 0.64f, 0.67f);
        public readonly Color Teal = new Color(0.43f, 0.84f, 0.81f);
        public readonly Color Amber = new Color(0.91f, 0.67f, 0.40f);
        public readonly Color Edge = new Color(0.20f, 0.30f, 0.33f, 0.75f);
        private readonly Dictionary<int, GUIStyle> labels = new Dictionary<int, GUIStyle>();
        private readonly Dictionary<string, Texture2D> arcMasks = new Dictionary<string, Texture2D>();
        private Texture2D rounded;
        private GUIStyle plate;
        public bool HighContrast;

        public void Initialize()
        {
            if (rounded != null) return;
            rounded = new Texture2D(64, 64, TextureFormat.RGBA32, false) { name = "Console rounded mask", hideFlags = HideFlags.HideAndDontSave };
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 64; x++)
                {
                    Vector2 q = new Vector2(Mathf.Abs(x - 31.5f) - 15.5f, Mathf.Abs(y - 31.5f) - 15.5f);
                    float d = new Vector2(Mathf.Max(q.x, 0), Mathf.Max(q.y, 0)).magnitude - 15f;
                    rounded.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(0.5f - d)));
                }
            rounded.Apply();
            plate = new GUIStyle { border = new RectOffset(16, 16, 16, 16), normal = { background = rounded } };
        }

        public void Dispose()
        {
            if (rounded != null) Object.Destroy(rounded);
            foreach (var mask in arcMasks.Values) Object.Destroy(mask);
            arcMasks.Clear();
        }

        public void Text(Rect r, string value, int size = 12, Color? color = null, TextAnchor anchor = TextAnchor.UpperLeft, bool bold = false)
        {
            int key = size * 100 + (int)anchor * 2 + (bold ? 1 : 0);
            if (!labels.TryGetValue(key, out GUIStyle style))
            {
                style = new GUIStyle(GUI.skin.label) { fontSize = size, alignment = anchor, fontStyle = bold ? FontStyle.Bold : FontStyle.Normal, wordWrap = true, clipping = TextClipping.Clip, padding = new RectOffset(0, 0, 0, 0) };
                labels.Add(key, style);
            }
            style.normal.textColor = color ?? Ink;
            GUI.Label(r, value, style);
        }

        public void Fill(Rect r, Color color)
        {
            Color old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = old;
        }

        public void Round(Rect r, Color color)
        {
            if (Event.current.type != EventType.Repaint) return;
            Color old = GUI.color;
            GUI.color = color;
            plate.Draw(r, GUIContent.none, false, false, false, false);
            GUI.color = old;
        }

        public void Line(Vector2 a, Vector2 b, Color color, float width = 1)
        {
            if (Event.current.type != EventType.Repaint) return;
            // Keep GUI.matrix unchanged so Unity's nested scroll clipping stays valid.
            if (Mathf.Abs(a.y - b.y) < 0.1f) { Fill(new Rect(Mathf.Min(a.x, b.x), a.y - width / 2, Mathf.Abs(a.x - b.x), width), color); return; }
            if (Mathf.Abs(a.x - b.x) < 0.1f) { Fill(new Rect(a.x - width / 2, Mathf.Min(a.y, b.y), width, Mathf.Abs(a.y - b.y)), color); return; }
            int steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(a, b) * 1.5f));
            for (int i = 0; i <= steps; i++)
            {
                Vector2 p = Vector2.Lerp(a, b, (float)i / steps);
                Fill(new Rect(p.x - width / 2, p.y - width / 2, width, width), color);
            }
        }

        public void Arc(Vector2 center, float radius, float from, float to, Color color, float width = 1)
        {
            if (Event.current.type != EventType.Repaint) return;
            float sweep = Mathf.Clamp(to - from, 0, 360);
            if (sweep < 0.1f) return;
            sweep = Mathf.Round(sweep); // Bound the cache during animated progress.
            string key = radius + ":" + from + ":" + sweep + ":" + width;
            float extent = radius + width + 1;
            if (!arcMasks.TryGetValue(key, out Texture2D mask))
            {
                if (arcMasks.Count >= 128)
                {
                    foreach (var previous in arcMasks.Values) Object.Destroy(previous);
                    arcMasks.Clear();
                }
                int size = Mathf.CeilToInt(extent * 4);
                mask = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "Instrument arc", hideFlags = HideFlags.HideAndDontSave, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
                var pixels = new Color32[size * size];
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        float dx = ((x + 0.5f) / size * 2 - 1) * extent;
                        float dy = (1 - (y + 0.5f) / size * 2) * extent;
                        float angle = Mathf.Repeat(Mathf.Atan2(dy, dx) * Mathf.Rad2Deg - from, 360);
                        float coverage = Mathf.Clamp01((width / 2 - Mathf.Abs(Mathf.Sqrt(dx * dx + dy * dy) - radius)) * 2 + 0.5f);
                        pixels[y * size + x] = new Color32(255, 255, 255, (byte)(angle <= sweep ? coverage * 255 : 0));
                    }
                mask.SetPixels32(pixels);
                mask.Apply(false, true);
                arcMasks.Add(key, mask);
            }
            Color old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(new Rect(center.x - extent, center.y - extent, extent * 2, extent * 2), mask);
            GUI.color = old;
        }

        public void Panel(Rect r, string number, string title)
        {
            Round(r, new Color(0.026f, 0.042f, 0.052f, HighContrast ? 1 : 0.94f));
            Arc(new Vector2(r.x + 26, r.y + 26), 25, 180, 270, Amber, 3);
            Line(new Vector2(r.x + 26, r.y + 1), new Vector2(r.xMax - 24, r.y + 1), Edge);
            Text(new Rect(r.x + 18, r.y + 19, 27, 18), number, 11, Amber);
            Text(new Rect(r.x + 51, r.y + 17, r.width - 100, 20), title, 13, Ink, bold: true);
        }

        public bool Button(Rect r, string label, bool selected = false, bool primary = false, bool enabled = true)
        {
            bool hover = enabled && r.Contains(Event.current.mousePosition);
            Color accent = primary ? Amber : Teal;
            Color bg = primary ? new Color(0.35f, 0.24f, 0.14f, 0.95f) : new Color(0.075f, 0.12f, 0.14f, 0.92f);
            if (hover || selected) bg = Color.Lerp(bg, accent, hover ? 0.22f : 0.12f);
            if (!enabled) bg.a *= 0.45f;
            Round(r, bg);
            if (selected) Fill(new Rect(r.x + 12, r.yMax - 3, r.width - 24, 2), accent);
            Text(r, label, 12, enabled ? (selected || primary ? accent : Ink) : Muted * 0.7f, TextAnchor.MiddleCenter);
            bool old = GUI.enabled;
            GUI.enabled = old && enabled;
            bool clicked = GUI.Button(r, GUIContent.none, GUIStyle.none);
            GUI.enabled = old;
            return clicked;
        }
    }
}

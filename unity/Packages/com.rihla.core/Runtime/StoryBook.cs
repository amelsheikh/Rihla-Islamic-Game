using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Rihla
{
    /// <summary>
    /// Jiddo's old book, open on the screen (content/storybook.json): on the left page a hand-drawn map of the lands,
    /// with the place of this page marked; on the right page its name in Arabic and English, a few lines about it,
    /// and when the child will visit it. Space, the arrow keys or the button turn the page (in Arabic the book reads
    /// right to left, so the left arrow turns forward). Esc closes the book. All text comes from content/.
    /// </summary>
    public class StoryBook
    {
        static readonly Color Leather = new Color(0.27f, 0.13f, 0.08f), Parchment = new Color(0.95f, 0.9f, 0.78f),
            Ink = new Color(0.24f, 0.16f, 0.09f), Sea = new Color(0.33f, 0.5f, 0.6f), Gold = new Color(0.78f, 0.58f, 0.2f),
            Red = new Color(0.66f, 0.15f, 0.1f);

        readonly RihlaUI ui; readonly JObject book; readonly JArray pages; readonly Action done;
        readonly VisualElement layer, mapPage, textPage, labels, map; readonly Label number, name, other, text, when, keys;
        readonly Button nextBtn, prevBtn;
        int page; float turn = -1f; int turnTo; bool closed;
        public bool Open => !closed;
        public int Page => page;

        public StoryBook(RihlaUI ui, JObject book, Action done)
        {
            this.ui = ui; this.book = book; this.done = done; pages = (JArray)book["pages"];
            layer = new VisualElement(); layer.style.position = Position.Absolute;
            layer.style.left = layer.style.right = layer.style.top = layer.style.bottom = 0;
            layer.style.alignItems = Align.Center; layer.style.justifyContent = Justify.Center;
            layer.style.backgroundColor = new Color(0.05f, 0.03f, 0.02f, 0.55f);

            // The open book: a leather cover with two parchment pages.
            var cover = new VisualElement(); cover.style.width = 1120; cover.style.height = 600;
            cover.style.backgroundColor = Leather; cover.style.flexDirection = FlexDirection.Row;
            Pad(cover, 14, 14); Round(cover, 16);
            mapPage = Paper(); mapPage.style.borderTopRightRadius = mapPage.style.borderBottomRightRadius = 2;
            textPage = Paper(); textPage.style.borderTopLeftRadius = textPage.style.borderBottomLeftRadius = 2;
            textPage.style.transformOrigin = new TransformOrigin(0, Length.Percent(50));
            cover.Add(mapPage); cover.Add(textPage);
            layer.Add(cover);

            // The map, drawn in ink, and the names on it.
            map = new VisualElement(); map.style.flexGrow = 1; map.style.marginLeft = map.style.marginRight = 16;
            map.style.marginTop = map.style.marginBottom = 16;
            map.style.overflow = Overflow.Hidden;
            map.generateVisualContent += DrawMap;
            labels = new VisualElement(); labels.style.position = Position.Absolute;
            labels.style.left = labels.style.right = labels.style.top = labels.style.bottom = 0; labels.pickingMode = PickingMode.Ignore;
            map.Add(labels);
            map.RegisterCallback<GeometryChangedEvent>(_ => PlaceLabels());
            mapPage.Add(map);

            // The words.
            textPage.style.paddingLeft = textPage.style.paddingRight = 40; textPage.style.paddingTop = 34;
            number = Center(ui.Text("", 16, false, Gold));
            name = Center(ui.Text("", 44, true, Ink));
            other = Center(ui.Text("", 24, false, new Color(0.45f, 0.32f, 0.18f)));
            var rule = new VisualElement(); rule.style.height = 2; rule.style.backgroundColor = Gold; rule.style.marginTop = 10;
            rule.style.marginBottom = 18; rule.style.marginLeft = rule.style.marginRight = 60;
            text = ui.Text("", 24, false, Ink); text.style.flexGrow = 1;
            when = Center(ui.Text("", 18, true, Red));
            textPage.Add(number); textPage.Add(name); textPage.Add(other); textPage.Add(rule); textPage.Add(text); textPage.Add(when);

            // Buttons and keys under the book.
            var row = new VisualElement(); row.style.flexDirection = FlexDirection.Row; row.style.marginTop = 14; row.style.alignItems = Align.Center;
            prevBtn = Btn(() => Turn(-1)); nextBtn = Btn(() => Turn(+1));
            keys = ui.Text("", 15, false, new Color(0.9f, 0.85f, 0.75f)); keys.style.marginLeft = keys.style.marginRight = 16;
            row.Add(prevBtn); row.Add(keys); row.Add(nextBtn);
            layer.Add(row);

            ui.AddLayer(layer);
            Lang.Changed += Fill;
            Fill();
        }

        VisualElement Paper()
        {
            var p = new VisualElement(); p.style.flexGrow = 1; p.style.flexBasis = 0; p.style.backgroundColor = Parchment;
            Round(p, 8); p.style.borderLeftWidth = p.style.borderRightWidth = 1;
            p.style.borderLeftColor = p.style.borderRightColor = new Color(0.7f, 0.6f, 0.45f);
            return p;
        }

        static Label Center(Label l) { l.AddToClassList("center"); l.style.unityTextAlign = TextAnchor.MiddleCenter; return l; }

        Button Btn(Action click)
        {
            var b = new Button(click); b.style.fontSize = 20; b.style.color = Parchment;
            b.style.unityFontDefinition = FontDefinition.FromFont(ui.bold ? ui.bold : ui.regular);
            b.style.unityTextGenerator = TextGeneratorType.Advanced;
            b.style.backgroundColor = Leather; Pad(b, 6, 20); Round(b, 10);
            b.style.borderTopWidth = b.style.borderBottomWidth = b.style.borderLeftWidth = b.style.borderRightWidth = 2;
            b.style.borderTopColor = b.style.borderBottomColor = b.style.borderLeftColor = b.style.borderRightColor = Gold;
            return b;
        }

        static void Pad(VisualElement e, float v, float h) { e.style.paddingTop = e.style.paddingBottom = v; e.style.paddingLeft = e.style.paddingRight = h; }
        static void Round(VisualElement e, float r) { e.style.borderTopLeftRadius = e.style.borderTopRightRadius = e.style.borderBottomLeftRadius = e.style.borderBottomRightRadius = r; }

        JToken Ui(string key) => book["ui"]?[key];

        void Fill()
        {
            var p = pages[page];
            bool ar = Lang.Arabic;
            number.text = Lang.Pick(Ui("page")) + " " + Lang.Num(page + 1) + " / " + Lang.Num(pages.Count);
            name.text = Lang.Pick(p["name"]);
            other.text = (string)p["name"]?[ar ? "en" : "ar"];
            text.text = Lang.Pick(p["text"]);
            // Long pages (a verse with its meaning under the words) get smaller letters so they fit on the page.
            int len = text.text.Length;
            text.style.fontSize = len <= 240 ? 24 : len <= 380 ? 20 : len <= 520 ? 17 : 15;
            text.style.unityTextAlign = ar ? TextAnchor.UpperRight : TextAnchor.UpperLeft;
            int ch = (int?)p["chapter"] ?? -1;
            when.text = ch == 1 ? Lang.Pick(Ui("chapter1")) : ch == 2 ? Lang.Pick(Ui("chapter2")) : ch == 0 ? Lang.Pick(Ui("later")) : "";
            prevBtn.text = Lang.Pick(Ui("prev")); nextBtn.text = Lang.Pick(Ui("next"));
            prevBtn.style.visibility = page > 0 ? Visibility.Visible : Visibility.Hidden;
            keys.text = Lang.Pick(Ui("keys"));
            map.MarkDirtyRepaint();
            PlaceLabels();
        }

        /// <summary>Called every frame by the opening: keys and the page-turn animation.</summary>
        public void Tick()
        {
            if (closed) return;
            if (turn >= 0f)
            {
                turn += Time.unscaledDeltaTime / 0.42f;
                float half = turn < 0.5f ? 1f - turn * 2f : (turn - 0.5f) * 2f;
                if (turn >= 0.5f && page != turnTo) { page = turnTo; Fill(); }
                textPage.style.scale = new Scale(new Vector3(Mathf.Max(0.02f, half), 1f, 1f));
                mapPage.style.opacity = 0.35f + 0.65f * half;
                if (turn >= 1f) { turn = -1f; textPage.style.scale = new Scale(Vector3.one); mapPage.style.opacity = 1f; }
                return;
            }
            if (ui.ModalOpen) return;
            var kb = Keyboard.current; if (kb == null) return;
            bool fwd = Lang.Arabic ? kb.leftArrowKey.wasPressedThisFrame : kb.rightArrowKey.wasPressedThisFrame;
            bool back = Lang.Arabic ? kb.rightArrowKey.wasPressedThisFrame : kb.leftArrowKey.wasPressedThisFrame;
            if (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || fwd) Turn(+1);
            else if (back) Turn(-1);
        }

        public void Turn(int by)
        {
            if (closed || turn >= 0f) return;
            int to = page + by;
            if (to < 0) return;
            if (to >= pages.Count) { Close(); return; }
            turnTo = to; turn = 0f;
            PageSound();
        }

        public void Close()
        {
            if (closed) return;
            closed = true; Lang.Changed -= Fill;
            layer.RemoveFromHierarchy();
            done?.Invoke();
        }

        // ------------------------------------------------------------------ the map

        Vector2 Project(float lon, float lat, Rect r)
        {
            var b = book["map"]["bounds"]; float w = (float)b[0], s = (float)b[1], e = (float)b[2], n = (float)b[3];
            float k = Mathf.Cos(Mathf.Deg2Rad * (s + n) / 2f);
            float mw = (e - w) * k, mh = n - s, scale = Mathf.Min(r.width / mw, r.height / mh);
            float ox = r.x + (r.width - mw * scale) / 2f, oy = r.y + (r.height - mh * scale) / 2f;
            return new Vector2(ox + (lon - w) * k * scale, oy + (n - lat) * scale);
        }

        Vector2 P(JToken pt, Rect r) => Project((float)pt[0], (float)pt[1], r);

        void DrawMap(MeshGenerationContext ctx)
        {
            var el = ctx.visualElement; var r = el.contentRect; if (r.width < 10f) return;
            var data = book["map"]; var p2 = ctx.painter2D;
            p2.lineJoin = LineJoin.Round; p2.lineCap = LineCap.Round;

            // A frame like an old map.
            p2.strokeColor = Ink; p2.lineWidth = 2f;
            p2.BeginPath(); p2.MoveTo(new Vector2(r.xMin, r.yMin)); p2.LineTo(new Vector2(r.xMax, r.yMin));
            p2.LineTo(new Vector2(r.xMax, r.yMax)); p2.LineTo(new Vector2(r.xMin, r.yMax)); p2.ClosePath(); p2.Stroke();

            // Coasts: the shore in ink, with fading water lines along it, as old maps draw the sea.
            foreach (float off in new[] { 9f, 6f, 3f })
            {
                p2.strokeColor = new Color(Sea.r, Sea.g, Sea.b, 0.16f + (9f - off) * 0.04f); p2.lineWidth = off * 1.6f;
                foreach (var line in data["coast"]) Stroke(p2, line, r);
            }
            p2.strokeColor = Ink; p2.lineWidth = 1.6f;
            foreach (var line in data["coast"]) Stroke(p2, line, r);

            // Rivers and lakes.
            p2.strokeColor = Sea; p2.lineWidth = 1.8f;
            foreach (var line in data["rivers"]) Stroke(p2, line, r);
            p2.fillColor = new Color(Sea.r, Sea.g, Sea.b, 0.75f);
            foreach (var lake in data["lakes"])
            {
                p2.BeginPath(); bool first = true;
                foreach (var pt in lake) { var v = P(pt, r); if (first) p2.MoveTo(v); else p2.LineTo(v); first = false; }
                p2.ClosePath(); p2.Fill();
            }

            // All the places of the book as small dots; this page's place as a red mark.
            var here = pages[page];
            foreach (var pg in pages)
            {
                if ((string)pg["id"] == "al-quds-home") continue;
                var v = P(pg["at"], r);
                p2.fillColor = Ink; p2.BeginPath(); p2.Arc(v, 3f, 0f, 360f); p2.Fill();
            }
            // The Night Journey page draws the way from Makkah to Al-Quds.
            if (here["link"] != null)
            {
                foreach (var pg in pages)
                    if ((string)pg["id"] == (string)here["link"])
                    {
                        Vector2 a = P(pg["at"], r), b = P(here["at"], r);
                        p2.strokeColor = Gold; p2.lineWidth = 2.5f;
                        for (float t = 0f; t < 1f; t += 0.06f)
                        { p2.BeginPath(); p2.MoveTo(Vector2.Lerp(a, b, t)); p2.LineTo(Vector2.Lerp(a, b, Mathf.Min(1f, t + 0.03f))); p2.Stroke(); }
                    }
            }
            var h = P(here["at"], r);
            p2.strokeColor = Red; p2.lineWidth = 3f; p2.BeginPath(); p2.Arc(h, 12f, 0f, 360f); p2.Stroke();
            p2.fillColor = Red; p2.BeginPath(); p2.Arc(h, 5f, 0f, 360f); p2.Fill();
        }

        void Stroke(Painter2D p2, JToken line, Rect r)
        {
            p2.BeginPath(); bool first = true;
            foreach (var pt in line) { var v = P(pt, r); if (first) p2.MoveTo(v); else p2.LineTo(v); first = false; }
            p2.Stroke();
        }

        readonly List<Label> placed = new List<Label>();

        void PlaceLabels()
        {
            var r = labels.parent.contentRect; if (r.width < 10f) return;
            labels.Clear(); placed.Clear();
            foreach (var reg in book["map"]["regions"])
            {
                bool sea = (bool?)reg["sea"] ?? false;
                Add(Lang.Pick(reg["name"]), P(reg["at"], r), sea ? 15 : 17, sea ? Sea : new Color(0.5f, 0.36f, 0.2f), !sea, true);
            }
            var here = pages[page];
            Add(Lang.Pick(here["name"]), P(here["at"], r) + new Vector2(0f, -36f), 20, Red, true, true);
        }

        void Add(string s, Vector2 at, float size, Color c, bool bold, bool centre)
        {
            var l = ui.Text(s, size, bold, c); l.AddToClassList("center");
            l.style.position = Position.Absolute; l.style.whiteSpace = WhiteSpace.NoWrap;
            l.style.left = at.x; l.style.top = at.y - size * 0.7f;
            if (centre) l.style.translate = new Translate(Length.Percent(-50), 0);
            labels.Add(l); placed.Add(l);
        }

        // ------------------------------------------------------------------ the sound of a page

        static AudioClip pageClip; static AudioSource source;

        public static void PageSound()
        {
            if (!pageClip)
            {
                int rate = 22050, n = rate * 35 / 100; var d = new float[n]; var rnd = new System.Random(7); float lp = 0f;
                for (int i = 0; i < n; i++)
                {
                    float t = (float)i / n, env = Mathf.Sin(Mathf.PI * Mathf.Pow(t, 0.6f)) * (0.6f + 0.4f * Mathf.Sin(t * 40f));
                    lp += ((float)rnd.NextDouble() * 2f - 1f - lp) * 0.35f; d[i] = lp * env * 0.5f;
                }
                pageClip = AudioClip.Create("page", n, 1, rate, false); pageClip.SetData(d, 0);
            }
            if (!source) { var go = new GameObject("Page sound"); source = go.AddComponent<AudioSource>(); source.spatialBlend = 0f; }
            source.PlayOneShot(pageClip, 0.8f);
        }
    }
}

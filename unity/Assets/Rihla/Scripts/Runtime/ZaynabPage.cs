using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Rihla
{
    /// <summary>
    /// Page 1 of Zaynab's Rihla, taken from Jiddo's chest (content/storybook.json, "chest"). First her letter, with
    /// her riddle; then the riddle on a map of the land around Al-Quds, with three places to choose from. A wrong
    /// choice gives a kind hint and the child tries again; the right one draws the road and ends with a button to
    /// drive there. Laid out like Jiddo's book (StoryBook), on older paper.
    /// </summary>
    public class ZaynabPage
    {
        static readonly Color Leather = new Color(0.22f, 0.13f, 0.08f), Paper = new Color(0.9f, 0.82f, 0.64f),
            Ink = new Color(0.22f, 0.14f, 0.08f), Sea = new Color(0.33f, 0.5f, 0.6f), Gold = new Color(0.72f, 0.52f, 0.16f),
            Red = new Color(0.62f, 0.13f, 0.09f), Green = new Color(0.2f, 0.45f, 0.24f);

        readonly RihlaUI ui; readonly JObject book, chest; readonly Action<string> hint; readonly Action done;
        readonly VisualElement layer, left, right, map, labels; readonly Button main;
        Label feedback; bool riddle, solved, closed; string picked;
        public bool Open => !closed;

        /// <param name="hint">Called with Jiddo's words when the child chooses a place.</param>
        public ZaynabPage(RihlaUI ui, JObject book, Action<string> hint, Action done, JObject section = null)
        {
            this.ui = ui; this.book = book; chest = section ?? (JObject)book["chest"]; this.hint = hint; this.done = done;
            layer = new VisualElement(); layer.style.position = Position.Absolute;
            layer.style.left = layer.style.right = layer.style.top = layer.style.bottom = 0;
            layer.style.alignItems = Align.Center; layer.style.justifyContent = Justify.Center;
            layer.style.backgroundColor = new Color(0.05f, 0.03f, 0.02f, 0.6f);
            var cover = new VisualElement(); cover.style.width = 1120; cover.style.height = 600;
            cover.style.backgroundColor = Leather; cover.style.flexDirection = Lang.Arabic ? FlexDirection.RowReverse : FlexDirection.Row;
            Pad(cover, 14, 14); Round(cover, 16);
            left = Sheet(); right = Sheet();
            cover.Add(left); cover.Add(right); layer.Add(cover);

            map = new VisualElement(); map.style.flexGrow = 1; map.style.overflow = Overflow.Hidden;
            map.generateVisualContent += DrawMap;
            labels = new VisualElement(); labels.style.position = Position.Absolute;
            labels.style.left = labels.style.right = labels.style.top = labels.style.bottom = 0;
            map.Add(labels);
            map.RegisterCallback<GeometryChangedEvent>(_ => PlaceLabels());

            main = Btn(Next); main.style.marginTop = 14;
            layer.Add(main);
            ui.AddLayer(layer);
            Letter();
        }

        JToken Page => chest["page"];
        JToken Ui(string k) => chest["ui"]?[k];

        // ------------------------------------------------------------------ the two stages

        void Letter()
        {
            left.Clear(); right.Clear();
            left.Add(Center(ui.Text(Lang.Pick(Page["number"]), 16, false, Gold)));
            left.Add(Center(ui.Text(Lang.Pick(Page["title"]), 30, true, Ink)));
            left.Add(Rule());
            left.Add(Para(Lang.Pick(Page["writes"]), 18, true, new Color(0.45f, 0.3f, 0.15f)));
            var letter = Para(Lang.Pick(Page["letter"]), 19, false, Ink); letter.style.unityFontStyleAndWeight = FontStyle.Italic;
            left.Add(letter);
            right.style.justifyContent = Justify.Center;
            right.Add(Center(ui.Text(Lang.Pick(Page["riddleTitle"]), 28, true, Red)));
            right.Add(Rule());
            var r = Para(Lang.Pick(Page["riddle"]), 27, false, Ink); r.style.unityFontStyleAndWeight = FontStyle.Italic;
            r.style.unityTextAlign = TextAnchor.MiddleCenter; r.style.marginTop = 10;
            right.Add(r);
            right.Add(Seal());
            main.text = Lang.Pick(Ui("toRiddle"));
        }

        void Riddle()
        {
            riddle = true;
            left.Clear(); right.Clear(); right.style.justifyContent = Justify.FlexStart;
            left.Add(Center(ui.Text(Lang.Pick(Page["riddleTitle"]), 26, true, Red)));
            var r = Para(Lang.Pick(Page["riddle"]), 22, false, Ink); r.style.unityFontStyleAndWeight = FontStyle.Italic; left.Add(r);
            left.Add(Rule());
            left.Add(Para(Lang.Pick(Ui("choose")), 20, true, Ink));
            var row = new VisualElement(); row.style.flexDirection = FlexDirection.Row; row.style.flexWrap = Wrap.Wrap; row.style.justifyContent = Justify.Center;
            foreach (var c in chest["choices"]) { var id = (string)c["id"]; var b = Btn(() => Choose(id)); b.text = Lang.Pick(c["name"]); b.style.marginLeft = b.style.marginRight = 6; b.style.marginTop = 8; row.Add(b); }
            left.Add(row);
            feedback = Para("", 20, false, Ink); feedback.style.marginTop = 14; left.Add(feedback);
            map.style.marginLeft = map.style.marginRight = map.style.marginTop = map.style.marginBottom = 14;
            right.Add(map);
            main.style.display = DisplayStyle.None;
            map.MarkDirtyRepaint(); PlaceLabels();
        }

        public void Choose(string id)
        {
            if (solved) return;
            JToken c = null; foreach (var x in chest["choices"]) if ((string)x["id"] == id) c = x;
            if (c == null) return;
            picked = id;
            if ((bool?)c["correct"] ?? false)
            {
                solved = true; Soundscape.Chime();
                feedback.text = Lang.Pick(c["yes"]); feedback.style.color = Green;
                main.text = Lang.Pick(Ui("drive")); main.style.display = DisplayStyle.Flex;
                var s = SaveData.Current; var pages = new List<string>(s.pages ?? new string[0]);
                string pid = (string)Page["id"]; if (!pages.Contains(pid)) { pages.Add(pid); s.pages = pages.ToArray(); }
                SaveData.Save();
            }
            else { feedback.text = Lang.Pick(c["hint"]); feedback.style.color = Red; hint?.Invoke(Lang.Pick(c["hint"])); }
            map.MarkDirtyRepaint(); PlaceLabels();
        }

        void Next()
        {
            if (closed) return;
            if (!riddle) { StoryBookSound(); Riddle(); return; }
            if (solved) Close();
        }

        public void Tick()
        {
            if (closed) return;
            var kb = Keyboard.current; if (kb == null) return;
            if ((kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame) && (!riddle || solved)) Next();
        }

        public void Close()
        {
            if (closed) return;
            closed = true; layer.RemoveFromHierarchy(); done?.Invoke();
        }

        public bool Solved => solved;
        public bool AtRiddle => riddle;
        public void Advance() => Next();

        // ------------------------------------------------------------------ the map

        Vector2 Project(JToken pt, Rect r)
        {
            var b = chest["map"]["bounds"]; float w = (float)b[0], s = (float)b[1], e = (float)b[2], n = (float)b[3];
            float k = Mathf.Cos(Mathf.Deg2Rad * (s + n) / 2f), mw = (e - w) * k, mh = n - s, scale = Mathf.Min(r.width / mw, r.height / mh);
            float ox = r.x + (r.width - mw * scale) / 2f, oy = r.y + (r.height - mh * scale) / 2f;
            return new Vector2(ox + ((float)pt[0] - w) * k * scale, oy + (n - (float)pt[1]) * scale);
        }

        void DrawMap(MeshGenerationContext ctx)
        {
            var r = ctx.visualElement.contentRect; if (r.width < 10f) return;
            var data = book["map"]; var p2 = ctx.painter2D; p2.lineJoin = LineJoin.Round; p2.lineCap = LineCap.Round;
            p2.strokeColor = Ink; p2.lineWidth = 2f;
            p2.BeginPath(); p2.MoveTo(new Vector2(r.xMin, r.yMin)); p2.LineTo(new Vector2(r.xMax, r.yMin)); p2.LineTo(new Vector2(r.xMax, r.yMax)); p2.LineTo(new Vector2(r.xMin, r.yMax)); p2.ClosePath(); p2.Stroke();
            foreach (float off in new[] { 12f, 8f, 4f })
            {
                p2.strokeColor = new Color(Sea.r, Sea.g, Sea.b, 0.14f + (12f - off) * 0.03f); p2.lineWidth = off * 1.6f;
                foreach (var line in data["coast"]) Line(p2, line, r);
            }
            p2.strokeColor = Ink; p2.lineWidth = 1.8f; foreach (var line in data["coast"]) Line(p2, line, r);
            p2.strokeColor = Sea; p2.lineWidth = 2.4f; foreach (var line in data["rivers"]) Line(p2, line, r);
            p2.fillColor = new Color(Sea.r, Sea.g, Sea.b, 0.8f);
            foreach (var lake in data["lakes"])
            {
                p2.BeginPath(); bool first = true;
                foreach (var pt in lake) { var v = Project(pt, r); if (first) p2.MoveTo(v); else p2.LineTo(v); first = false; }
                p2.ClosePath(); p2.Fill();
            }
            var home = Project(chest["map"]["home"]["at"], r);
            foreach (var c in chest["choices"])
            {
                var v = Project(c["at"], r); bool chosen = (string)c["id"] == picked, right = (bool?)c["correct"] ?? false;
                Color col = chosen ? (right ? Green : Red) : Gold;
                // Faint dashes from Al-Quds to every choice; the road to the right one, once found, in full.
                p2.strokeColor = new Color(col.r, col.g, col.b, solved && right ? 1f : 0.45f); p2.lineWidth = solved && right ? 4f : 2f;
                for (float t = 0.08f; t < 0.92f; t += 0.07f) { p2.BeginPath(); p2.MoveTo(Vector2.Lerp(home, v, t)); p2.LineTo(Vector2.Lerp(home, v, t + 0.035f)); p2.Stroke(); }
                p2.strokeColor = col; p2.lineWidth = 3f; p2.BeginPath(); p2.Arc(v, 13f, 0f, 360f); p2.Stroke();
                p2.fillColor = col; p2.BeginPath(); p2.Arc(v, 6f, 0f, 360f); p2.Fill();
            }
            p2.fillColor = Ink; p2.BeginPath(); p2.Arc(home, 6f, 0f, 360f); p2.Fill();
        }

        void Line(Painter2D p2, JToken line, Rect r)
        {
            p2.BeginPath(); bool first = true;
            foreach (var pt in line) { var v = Project(pt, r); if (first) p2.MoveTo(v); else p2.LineTo(v); first = false; }
            p2.Stroke();
        }

        void PlaceLabels()
        {
            var r = map.contentRect; if (r.width < 10f) return;
            labels.Clear();
            foreach (var s in chest["map"]["seas"]) Label(Lang.Pick(s["name"]), Project(s["at"], r), 15, Sea, false);
            Label(Lang.Pick(chest["map"]["home"]["name"]), Project(chest["map"]["home"]["at"], r) + new Vector2(-6f, -22f), 18, Ink, true);
            foreach (var c in chest["choices"])
            {
                var id = (string)c["id"]; var at = Project(c["at"], r);
                // Each place on the map can be clicked as well as its button.
                var b = new Button(() => Choose(id)) { text = Lang.Pick(c["name"]) };
                b.style.position = Position.Absolute; b.style.left = at.x; b.style.top = at.y - 46;
                b.style.translate = new Translate(Length.Percent(-50), 0);
                b.style.fontSize = 19; b.style.color = id == picked ? (((bool?)c["correct"] ?? false) ? Green : Red) : Ink;
                b.style.unityFontDefinition = FontDefinition.FromFont(ui.bold ? ui.bold : ui.regular);
                b.style.unityTextGenerator = TextGeneratorType.Advanced;
                b.style.backgroundColor = new Color(Paper.r, Paper.g, Paper.b, 0.85f);
                b.style.borderTopWidth = b.style.borderBottomWidth = b.style.borderLeftWidth = b.style.borderRightWidth = 0;
                Pad(b, 2, 8); Round(b, 6);
                labels.Add(b);
            }
        }

        void Label(string s, Vector2 at, float size, Color c, bool bold)
        {
            var l = ui.Text(s, size, bold, c); l.style.position = Position.Absolute; l.style.whiteSpace = WhiteSpace.NoWrap;
            l.style.left = at.x; l.style.top = at.y - size * 0.7f; l.style.translate = new Translate(Length.Percent(-50), 0);
            l.pickingMode = PickingMode.Ignore; labels.Add(l);
        }

        // ------------------------------------------------------------------ pieces

        VisualElement Sheet()
        {
            var p = new VisualElement(); p.style.flexGrow = 1; p.style.flexBasis = 0; p.style.backgroundColor = Paper;
            Round(p, 6); p.style.paddingLeft = p.style.paddingRight = 34; p.style.paddingTop = 28; p.style.paddingBottom = 20;
            p.style.borderLeftWidth = p.style.borderRightWidth = p.style.borderTopWidth = p.style.borderBottomWidth = 1;
            p.style.borderLeftColor = p.style.borderRightColor = p.style.borderTopColor = p.style.borderBottomColor = new Color(0.62f, 0.48f, 0.3f);
            return p;
        }

        Label Para(string s, float size, bool bold, Color c)
        {
            var l = ui.Text(s, size, bold, c); l.style.whiteSpace = WhiteSpace.Normal; l.style.marginTop = 6;
            l.style.unityTextAlign = Lang.Arabic ? TextAnchor.UpperRight : TextAnchor.UpperLeft;
            return l;
        }

        static Label Center(Label l) { l.AddToClassList("center"); l.style.unityTextAlign = TextAnchor.MiddleCenter; return l; }

        static VisualElement Rule()
        {
            var r = new VisualElement(); r.style.height = 2; r.style.backgroundColor = Gold;
            r.style.marginTop = 10; r.style.marginBottom = 12; r.style.marginLeft = r.style.marginRight = 50; return r;
        }

        /// <summary>A round wax seal under the riddle, red, with a small eight-pointed star.</summary>
        static VisualElement Seal()
        {
            var s = new VisualElement(); s.style.width = 70; s.style.height = 70; s.style.alignSelf = Align.Center; s.style.marginTop = 24;
            s.generateVisualContent += ctx =>
            {
                var p = ctx.painter2D; var c = new Vector2(35, 35);
                p.fillColor = Red; p.BeginPath(); p.Arc(c, 32f, 0f, 360f); p.Fill();
                p.strokeColor = new Color(0.95f, 0.8f, 0.6f, 0.8f); p.lineWidth = 2.5f;
                for (int k = 0; k < 2; k++)
                {
                    p.BeginPath();
                    for (int i = 0; i < 4; i++) { float a = (i * 90f + k * 45f) * Mathf.Deg2Rad; var v = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 18f; if (i == 0) p.MoveTo(v); else p.LineTo(v); }
                    p.ClosePath(); p.Stroke();
                }
            };
            return s;
        }

        Button Btn(Action click)
        {
            var b = new Button(click); b.style.fontSize = 21; b.style.color = Paper;
            b.style.unityFontDefinition = FontDefinition.FromFont(ui.bold ? ui.bold : ui.regular);
            b.style.unityTextGenerator = TextGeneratorType.Advanced;
            b.style.backgroundColor = Leather; Pad(b, 7, 22); Round(b, 10);
            b.style.borderTopWidth = b.style.borderBottomWidth = b.style.borderLeftWidth = b.style.borderRightWidth = 2;
            b.style.borderTopColor = b.style.borderBottomColor = b.style.borderLeftColor = b.style.borderRightColor = Gold;
            return b;
        }

        static void Pad(VisualElement e, float v, float h) { e.style.paddingTop = e.style.paddingBottom = v; e.style.paddingLeft = e.style.paddingRight = h; }
        static void Round(VisualElement e, float r) { e.style.borderTopLeftRadius = e.style.borderTopRightRadius = e.style.borderBottomLeftRadius = e.style.borderBottomRightRadius = r; }

        static void StoryBookSound() => StoryBook.PageSound();
    }
}

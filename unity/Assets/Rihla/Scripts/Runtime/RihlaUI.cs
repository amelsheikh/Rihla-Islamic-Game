using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Rihla
{
    /// <summary>
    /// The game's interface in English and Arabic, built with UI Toolkit. Text uses the Advanced text generator,
    /// which joins Arabic letters and lays out right-to-left text properly, and the Amiri typeface. The whole
    /// layout mirrors when the language is Arabic. Controllers (Teaching, AlQudsFlyover) fill it every frame.
    /// Keys: Esc or M the menu, L language, Q picture (Fast or Best), I the box at the top narrow or full, J journal, H history, Esc or Enter closes a page.
    /// </summary>
    public class RihlaUI : MonoBehaviour
    {
        public PanelSettings panelSettings;
        public Font regular, bold;

        public static RihlaUI Instance { get; private set; }
        public bool ModalOpen => modal != null && modal.style.display == DisplayStyle.Flex;
        /// <summary>Called when the Journal or History button (or J / H) is used.</summary>
        public Action OnJournal, OnHistory;

        static readonly Color Ink = new Color(0.96f, 0.93f, 0.87f), Gold = new Color(0.91f, 0.78f, 0.42f),
            Panel = new Color(0.09f, 0.07f, 0.05f, 0.74f), Paper = new Color(0.95f, 0.91f, 0.82f), PaperInk = new Color(0.17f, 0.13f, 0.09f);

        VisualElement root, hud, buttons, banner, modal, sheet, body, subtitle, goal, fade;
        Label goalText, goalSub; float fadeFrom, fadeTo, fadeTime, fadeLength;
        Label speed, line1, line2, placeMain, placeSub, hint, fps, bannerTitle, bannerSub, subWho, subText;
        Button menuBtn, hudBtn, langBtn, qualityBtn, journalBtn, historyBtn, closeBtn;
        float bannerTimer, subtitleTimer; Action onClose, rebuild; float savedTimeScale = 1f;

        void Awake()
        {
            Instance = this;
            Time.timeScale = 1f; // a page left open when the last scene closed must not keep this one paused
            Lang.Set(SaveData.Current.lang);
            var doc = gameObject.AddComponent<UIDocument>();
            doc.panelSettings = panelSettings;
            root = doc.rootVisualElement;
            root.style.flexGrow = 1;
            root.pickingMode = PickingMode.Ignore;
            Build();
            Lang.Changed += Relayout;
            Relayout();
        }

        // The content library loads in its own Awake, so the button labels are filled again once it has.
        void Start() => Relayout();

        void OnDestroy() { Lang.Changed -= Relayout; Quality.Changed -= Relayout; if (ModalOpen) Time.timeScale = 1f; if (Instance == this) Instance = null; }

        // ------------------------------------------------------------------ building blocks

        public Label Text(string s, float size, bool isBold = false, Color? color = null)
        {
            var l = new Label(s);
            l.style.unityFontDefinition = FontDefinition.FromFont(isBold && bold ? bold : regular);
            l.style.unityTextGenerator = TextGeneratorType.Advanced;
            l.style.fontSize = size;
            l.style.color = color ?? Ink;
            l.style.whiteSpace = WhiteSpace.Normal;
            l.style.marginTop = l.style.marginBottom = 1; l.style.paddingLeft = l.style.paddingRight = 0;
            l.pickingMode = PickingMode.Ignore;
            return l;
        }

        Button MakeButton(string s, Action click)
        {
            var b = new Button(click) { text = s };
            b.style.unityFontDefinition = FontDefinition.FromFont(bold ? bold : regular);
            b.style.unityTextGenerator = TextGeneratorType.Advanced;
            b.style.fontSize = 17; b.style.color = Ink;
            b.style.backgroundColor = Panel;
            b.style.borderTopWidth = b.style.borderBottomWidth = b.style.borderLeftWidth = b.style.borderRightWidth = 1;
            b.style.borderTopColor = b.style.borderBottomColor = b.style.borderLeftColor = b.style.borderRightColor = Gold;
            b.style.borderTopLeftRadius = b.style.borderTopRightRadius = b.style.borderBottomLeftRadius = b.style.borderBottomRightRadius = 8;
            b.style.paddingLeft = b.style.paddingRight = 14; b.style.paddingTop = b.style.paddingBottom = 4;
            b.style.marginLeft = b.style.marginRight = 4; b.style.marginTop = b.style.marginBottom = 4;
            return b;
        }

        static void Pad(VisualElement e, float v, float h)
        {
            e.style.paddingTop = e.style.paddingBottom = v; e.style.paddingLeft = e.style.paddingRight = h;
        }

        static void Round(VisualElement e, float r)
        {
            e.style.borderTopLeftRadius = e.style.borderTopRightRadius = e.style.borderBottomLeftRadius = e.style.borderBottomRightRadius = r;
        }

        void Build()
        {
            // The HUD box (top left in English, top right in Arabic).
            hud = new VisualElement(); hud.style.position = Position.Absolute; hud.style.top = 14; hud.style.width = 380;
            hud.style.backgroundColor = Panel; Pad(hud, 10, 14); Round(hud, 10); hud.pickingMode = PickingMode.Ignore;
            speed = Text("", 30, true, Gold); line1 = Text("", 17); line2 = Text("", 17);
            placeMain = Text("", 22, true); placeSub = Text("", 15, false, new Color(0.85f, 0.82f, 0.75f));
            hint = Text("", 15, false, new Color(0.85f, 0.82f, 0.75f)); fps = Text("", 12, false, new Color(0.7f, 0.68f, 0.62f));
            placeMain.style.marginTop = 6;
            // The first row: the speed (or the walk's title) and the small button that folds the box (or I).
            var top = new VisualElement(); top.style.flexDirection = FlexDirection.Row; top.style.alignItems = Align.Center;
            top.pickingMode = PickingMode.Ignore; speed.style.flexGrow = 1; speed.style.flexShrink = 1;
            hudBtn = new Button(ToggleHudSize); hudBtn.style.unityFontDefinition = FontDefinition.FromFont(bold ? bold : regular);
            hudBtn.style.fontSize = 18; hudBtn.style.color = Gold; hudBtn.style.backgroundColor = new Color(0f, 0f, 0f, 0.25f);
            hudBtn.style.width = 26; hudBtn.style.height = 24; hudBtn.style.paddingLeft = hudBtn.style.paddingRight = 0;
            hudBtn.style.paddingTop = hudBtn.style.paddingBottom = 0; hudBtn.style.marginLeft = hudBtn.style.marginRight = 6;
            hudBtn.style.borderTopWidth = hudBtn.style.borderBottomWidth = hudBtn.style.borderLeftWidth = hudBtn.style.borderRightWidth = 1;
            hudBtn.style.borderTopColor = hudBtn.style.borderBottomColor = hudBtn.style.borderLeftColor = hudBtn.style.borderRightColor = new Color(0.91f, 0.78f, 0.42f, 0.6f);
            Round(hudBtn, 6); hudBtn.style.alignSelf = Align.FlexStart;
            top.Add(speed); top.Add(hudBtn);
            hud.Add(top); hud.Add(line1); hud.Add(line2); hud.Add(placeMain); hud.Add(placeSub); hud.Add(hint); hud.Add(fps);
            root.Add(hud);

            // Buttons (the opposite top corner).
            buttons = new VisualElement(); buttons.style.position = Position.Absolute; buttons.style.top = 10;
            buttons.style.flexDirection = FlexDirection.Row;
            menuBtn = MakeButton("", OpenMenu);
            langBtn = MakeButton("", Lang.Toggle);
            qualityBtn = MakeButton("", Quality.Toggle);
            Quality.Changed += Relayout;
            journalBtn = MakeButton("", () => OnJournal?.Invoke());
            historyBtn = MakeButton("", () => OnHistory?.Invoke());
            buttons.Add(historyBtn); buttons.Add(journalBtn); buttons.Add(qualityBtn); buttons.Add(langBtn); buttons.Add(menuBtn);
            root.Add(buttons);
            SetJournalVisible(false); SetHistoryVisible(false);

            // The banner, centred near the top.
            banner = new VisualElement(); banner.style.position = Position.Absolute; banner.style.top = 90;
            banner.style.left = new Length(50, LengthUnit.Percent); banner.style.translate = new Translate(new Length(-50, LengthUnit.Percent), 0);
            banner.style.minWidth = 420; banner.style.maxWidth = 760; banner.style.alignItems = Align.Center;
            banner.style.backgroundColor = Panel; Pad(banner, 12, 24); Round(banner, 12);
            banner.style.borderBottomWidth = 2; banner.style.borderBottomColor = Gold; banner.pickingMode = PickingMode.Ignore;
            bannerTitle = Text("", 30, true, Gold); bannerSub = Text("", 18);
            bannerTitle.style.unityTextAlign = bannerSub.style.unityTextAlign = TextAnchor.MiddleCenter;
            banner.Add(bannerTitle); banner.Add(bannerSub);
            banner.style.display = DisplayStyle.None;
            root.Add(banner);

            // Subtitles: who is speaking and what they say, near the bottom.
            subtitle = new VisualElement(); subtitle.style.position = Position.Absolute; subtitle.style.bottom = 40;
            subtitle.style.left = new Length(50, LengthUnit.Percent); subtitle.style.translate = new Translate(new Length(-50, LengthUnit.Percent), 0);
            subtitle.style.width = 820; subtitle.style.backgroundColor = Panel; Pad(subtitle, 10, 20); Round(subtitle, 10);
            subtitle.pickingMode = PickingMode.Ignore;
            subWho = Text("", 17, true, Gold); subText = Text("", 21);
            subtitle.Add(subWho); subtitle.Add(subText); subtitle.style.display = DisplayStyle.None;
            root.Add(subtitle);

            // The page sheet for the journal, history and the opening card.
            modal = new VisualElement(); modal.style.position = Position.Absolute;
            modal.style.left = modal.style.right = modal.style.top = modal.style.bottom = 0;
            modal.style.backgroundColor = new Color(0f, 0f, 0f, 0.45f);
            modal.style.alignItems = Align.Center; modal.style.justifyContent = Justify.Center;
            sheet = new VisualElement(); sheet.style.width = new Length(70, LengthUnit.Percent); sheet.style.maxWidth = 900;
            sheet.style.height = new Length(84, LengthUnit.Percent);
            sheet.style.backgroundColor = Paper; Round(sheet, 14); Pad(sheet, 18, 28);
            sheet.style.borderTopWidth = sheet.style.borderBottomWidth = sheet.style.borderLeftWidth = sheet.style.borderRightWidth = 3;
            sheet.style.borderTopColor = sheet.style.borderBottomColor = sheet.style.borderLeftColor = sheet.style.borderRightColor = Gold;
            var scroll = new ScrollView(ScrollViewMode.Vertical); scroll.style.flexGrow = 1;
            body = new VisualElement(); scroll.Add(body);
            closeBtn = MakeButton("", CloseModal); closeBtn.style.alignSelf = Align.Center; closeBtn.style.marginTop = 10;
            sheet.Add(scroll); sheet.Add(closeBtn); modal.Add(sheet);
            modal.style.display = DisplayStyle.None;
            root.Add(modal);

            // The goal: what to do now, always shown at the top centre while walking.
            goal = new VisualElement(); goal.style.position = Position.Absolute; goal.style.top = 58; // below the buttons, which reach the middle on a narrow screen
            goal.style.left = new Length(50, LengthUnit.Percent); goal.style.translate = new Translate(new Length(-50, LengthUnit.Percent), 0);
            goal.style.maxWidth = 520; goal.style.alignItems = Align.Center;
            goal.style.backgroundColor = new Color(0.12f, 0.09f, 0.04f, 0.82f); Pad(goal, 7, 18); Round(goal, 10);
            goal.style.borderTopWidth = goal.style.borderBottomWidth = goal.style.borderLeftWidth = goal.style.borderRightWidth = 2;
            goal.style.borderTopColor = goal.style.borderBottomColor = goal.style.borderLeftColor = goal.style.borderRightColor = Gold;
            goal.pickingMode = PickingMode.Ignore;
            goalText = Text("", 20, true, Ink); goalSub = Text("", 15, false, Gold);
            goalText.AddToClassList("center"); goalSub.AddToClassList("center");
            goalText.style.unityTextAlign = goalSub.style.unityTextAlign = TextAnchor.MiddleCenter;
            goal.Add(goalText); goal.Add(goalSub); goal.style.display = DisplayStyle.None;
            root.Insert(root.IndexOf(banner), goal);
            banner.style.top = 172;

            // Black over everything, for fading between scenes.
            fade = new VisualElement(); fade.style.position = Position.Absolute;
            fade.style.left = fade.style.right = fade.style.top = fade.style.bottom = 0;
            fade.style.backgroundColor = Color.black; fade.style.opacity = 0f; fade.pickingMode = PickingMode.Ignore;
            root.Add(fade);
        }

        /// <summary>The goal at the top of the screen; null or empty hides it.</summary>
        public void SetGoal(string text, string sub = null)
        {
            bool show = !string.IsNullOrEmpty(text);
            goal.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            if (!show) return;
            if (goalText.text != text) Debug.Log($"[Rihla] goal ({Lang.Current}): {text}");
            goalText.text = text; goalSub.text = sub ?? "";
            goalSub.style.display = string.IsNullOrEmpty(sub) ? DisplayStyle.None : DisplayStyle.Flex;
        }

        /// <summary>Fades the screen to black (1) or back (0) over unscaled seconds.</summary>
        public void Fade(float to, float seconds)
        {
            fadeFrom = fade.resolvedStyle.opacity; fadeTo = to; fadeTime = 0f; fadeLength = Mathf.Max(0.01f, seconds);
            if (seconds <= 0f) { fade.style.opacity = to; fadeLength = 0f; }
        }

        /// <summary>Hides the box at the top and the other buttons (the opening at home); the menu and the language
        /// button always stay, so both can be reached anywhere in the game.</summary>
        public void SetHudVisible(bool v)
        {
            hudShown = v;
            hud.style.display = v ? DisplayStyle.Flex : DisplayStyle.None;
            qualityBtn.style.display = v ? DisplayStyle.Flex : DisplayStyle.None;
            if (!v) { journalBtn.style.display = DisplayStyle.None; historyBtn.style.display = DisplayStyle.None; }
        }
        bool hudShown = true;

        // ------------------------------------------------------------------ the menu (Esc, M or the button)

        readonly List<(Func<string> label, Action act)> extra = new List<(Func<string>, Action)>();
        /// <summary>A choice of this scene at the top of the menu (for example "Skip the opening").</summary>
        public void AddMenuItem(Func<string> label, Action act) => extra.Add((label, act));
        public bool MenuOpen { get; private set; }

        public void OpenMenu()
        {
            if (MenuOpen) { CloseModal(); return; }
            if (ModalOpen) CloseModal();
            MenuOpen = true;
            Debug.Log("[Rihla] menu opened");
            OpenModal(BuildMenu, "m.continue", () => MenuOpen = false);
        }

        void RefreshModal() { if (ModalOpen && rebuild != null) { body.Clear(); rebuild(); AlignBody(); } }

        void BuildMenu()
        {
            H1(Lang.T("m.menu"));
            foreach (var (label, act) in extra) { var a = act; Choice(label(), () => { CloseModal(); a(); }); }
            Choice(Lang.T("m.continue"), CloseModal);
            Choice(Lang.Arabic ? "English" : "العربية", Lang.Toggle);
            Choice(Lang.T(Quality.Best ? "q.best" : "q.fast") + "   (" + Lang.T("m.change") + ")", () => { Quality.Toggle(); RefreshModal(); });
            Choice(Lang.T(SaveData.Current.hudSmall ? "u.hudBig" : "u.hudSmall"), () => { ToggleHudSize(); RefreshModal(); });
            if (journalBtn.style.display == DisplayStyle.Flex) Choice(Lang.T("journal"), () => { CloseModal(); OnJournal?.Invoke(); });

            H2(Lang.T("m.journey"));
            P(Lang.T("m.journeyHelp"));
            string here = Journey.Current();
            foreach (var part in Journey.Parts)
            {
                var pp = part;
                string label = (Lang.Arabic ? pp.ar : pp.en) + (pp.id == here ? "   ← " + Lang.T("m.here") : "");
                if (Lang.Arabic && pp.id == here) label = pp.ar + "   → " + Lang.T("m.here");
                var b = MakeButton(label, () => { CloseModal(); Journey.Jump(pp.id); });
                b.style.alignSelf = Align.Stretch; b.style.marginTop = 4; b.SetEnabled(pp.ready);
                if (pp.id == here) { b.style.borderLeftWidth = b.style.borderRightWidth = 3; }
                body.Add(b);
            }

            H2(Lang.T("m.keysTitle"));
            P(Lang.T("m.keys"));
            Choice(Lang.T("m.quit"), () => { SaveData.Save(); Application.Quit(); });
        }

        /// <summary>Adds a full layer (such as the storybook) above the HUD and below pages and the fade.</summary>
        public void AddLayer(VisualElement e) => root.Insert(root.IndexOf(modal), e);
        public void HideSubtitle() { subtitle.style.display = DisplayStyle.None; subtitleTimer = 0f; }

        /// <summary>Mirrors the layout for the current language and refreshes fixed labels and any open page.</summary>
        void Relayout()
        {
            bool ar = Lang.Arabic;
            root.languageDirection = ar ? LanguageDirection.RTL : LanguageDirection.LTR;
            hud.style.left = ar ? StyleKeyword.Auto : 14; hud.style.right = ar ? 14 : StyleKeyword.Auto;
            buttons.style.right = ar ? StyleKeyword.Auto : 10; buttons.style.left = ar ? 10 : StyleKeyword.Auto;
            buttons.style.flexDirection = ar ? FlexDirection.RowReverse : FlexDirection.Row;
            hudBtn.parent.style.flexDirection = ar ? FlexDirection.RowReverse : FlexDirection.Row;
            ApplyHudSize();
            langBtn.text = ar ? "English" : "عربي";
            qualityBtn.text = Lang.T(Quality.Best ? "q.best" : "q.fast");
            menuBtn.text = Lang.T("m.menu");
            journalBtn.text = Lang.T("journal");
            historyBtn.text = Lang.T("history");
            root.Query<Label>().ForEach(l =>
            {
                if (l == bannerTitle || l == bannerSub || l.ClassListContains("center")) return;
                l.style.unityTextAlign = ar ? TextAnchor.UpperRight : TextAnchor.UpperLeft;
            });
            if (ModalOpen && rebuild != null) { body.Clear(); rebuild(); AlignBody(); }
        }

        void AlignBody()
        {
            body.Query<Label>().ForEach(l => { if (!l.ClassListContains("center")) l.style.unityTextAlign = Lang.Arabic ? TextAnchor.UpperRight : TextAnchor.UpperLeft; });
        }

        // ------------------------------------------------------------------ what controllers call

        /// <summary>
        /// The box at the top in full (speed, height, trip, place in both languages, the keys, fps) or narrow
        /// (speed, height and the place only), chosen with the small button in its corner or I, and saved.
        /// </summary>
        public void ToggleHudSize() { SaveData.Current.hudSmall = !SaveData.Current.hudSmall; SaveData.Save(); ApplyHudSize(); }

        void ApplyHudSize()
        {
            bool small = SaveData.Current.hudSmall;
            hud.style.width = small ? StyleKeyword.Auto : 380;
            hud.style.minWidth = small ? 170 : StyleKeyword.Auto; hud.style.maxWidth = small ? 300 : StyleKeyword.None;
            Pad(hud, small ? 6 : 10, small ? 12 : 14);
            speed.style.fontSize = small ? 22 : 30;
            line1.style.fontSize = small ? 15 : 17;
            placeMain.style.fontSize = small ? 17 : 22; placeMain.style.marginTop = small ? 2 : 6;
            hudBtn.text = small ? "+" : "–";
            hudBtn.tooltip = Lang.T(small ? "u.hudBig" : "u.hudSmall");
            ShowHudLines();
        }

        void ShowHudLines()
        {
            bool small = SaveData.Current.hudSmall;
            DisplayStyle Show(bool v) => v ? DisplayStyle.Flex : DisplayStyle.None;
            line2.style.display = Show(!small && !string.IsNullOrEmpty(line2.text));
            hint.style.display = Show(!small && !string.IsNullOrEmpty(hint.text));
            placeSub.style.display = fps.style.display = Show(!small);
        }

        public void SetHud(string big, string a, string b, string place, string placeOther, string hintText)
        {
            speed.text = big; line1.text = a; line2.text = b;
            placeMain.text = place; placeSub.text = placeOther; hint.text = hintText;
            ShowHudLines();
        }

        /// <summary>Shows a line spoken by someone (for example Jiddo) at the bottom of the screen.</summary>
        public void Say(string who, string text, float seconds = 7f)
        {
            subWho.text = who; subText.text = text;
            subtitle.style.display = DisplayStyle.Flex; subtitleTimer = seconds;
            Debug.Log($"[Rihla] says ({Lang.Current}): {who}: {text}");
        }

        public void SetFps(float value) => fps.text = Lang.Num(value) + " fps";
        public void SetJournalVisible(bool v) => journalBtn.style.display = v ? DisplayStyle.Flex : DisplayStyle.None;
        public void SetHistoryVisible(bool v) => historyBtn.style.display = v ? DisplayStyle.Flex : DisplayStyle.None;

        public void Banner(string title, string sub, float seconds = 5f)
        {
            bannerTitle.text = title; bannerSub.text = sub;
            bannerSub.style.display = string.IsNullOrEmpty(sub) ? DisplayStyle.None : DisplayStyle.Flex;
            banner.style.display = DisplayStyle.Flex; bannerTimer = seconds;
            Debug.Log($"[Rihla] banner ({Lang.Current}): {title}" + (string.IsNullOrEmpty(sub) ? "" : $" / {sub}"));
        }

        /// <summary>Opens a page. <paramref name="build"/> fills it and is called again if the language changes.</summary>
        public void OpenModal(Action build, string closeKey, Action closed)
        {
            if (ModalOpen) { onClose = null; CloseModal(); }
            rebuild = () => { build(); closeBtn.text = Lang.T(closeKey); };
            onClose = closed;
            body.Clear(); rebuild(); AlignBody();
            modal.style.display = DisplayStyle.Flex;
            savedTimeScale = Time.timeScale; Time.timeScale = 0f;
        }

        public void CloseModal()
        {
            if (!ModalOpen) return;
            modal.style.display = DisplayStyle.None; Time.timeScale = savedTimeScale <= 0f ? 1f : savedTimeScale;
            var c = onClose; onClose = null; rebuild = null; c?.Invoke();
        }

        // Page content helpers.
        public void H1(string s) { var l = Text(s, 32, true, PaperInk); l.style.marginBottom = 8; body.Add(l); }
        public void H2(string s) { var l = Text(s, 21, true, new Color(0.55f, 0.36f, 0.08f)); l.style.marginTop = 12; body.Add(l); }
        public void P(string s, bool italic = false)
        {
            var l = Text(s, 19, false, PaperInk); l.style.marginBottom = 6;
            if (italic) l.style.unityFontStyleAndWeight = FontStyle.Italic;
            body.Add(l);
        }

        /// <summary>A Quran verse or hadith: the Arabic always, its reference, and the meaning in English.</summary>
        public void Verse(string arabic, string reference, string meaning)
        {
            var box = new VisualElement(); box.style.backgroundColor = new Color(1f, 0.98f, 0.92f); Pad(box, 10, 16); Round(box, 10);
            box.style.borderLeftWidth = box.style.borderRightWidth = 4; box.style.borderLeftColor = box.style.borderRightColor = Gold;
            box.style.marginTop = 6; box.style.marginBottom = 8;
            var ar = Text(arabic, 28, false, PaperInk); ar.languageDirection = LanguageDirection.RTL;
            ar.style.unityTextAlign = TextAnchor.MiddleCenter; ar.AddToClassList("center"); box.Add(ar);
            var r = Text(reference, 15, false, new Color(0.45f, 0.35f, 0.2f)); r.style.unityTextAlign = TextAnchor.MiddleCenter; r.AddToClassList("center"); box.Add(r);
            if (!string.IsNullOrEmpty(meaning)) { var m = Text(meaning, 18, false, PaperInk); m.style.marginTop = 6; box.Add(m); }
            body.Add(box);
        }

        public void Facts(System.Collections.Generic.IEnumerable<string> facts)
        {
            var row = new VisualElement(); row.style.flexDirection = FlexDirection.Row; row.style.flexWrap = Wrap.Wrap; row.style.marginTop = 8;
            foreach (var f in facts)
            {
                var chip = Text(f, 16, false, PaperInk); chip.style.backgroundColor = new Color(0.91f, 0.84f, 0.66f);
                Pad(chip, 4, 10); Round(chip, 12); chip.style.marginRight = chip.style.marginLeft = 3; chip.style.marginBottom = 6;
                row.Add(chip);
            }
            body.Add(row);
        }

        public void Era(string when, string text)
        {
            var row = new VisualElement(); row.style.marginTop = 6; row.style.paddingBottom = 6;
            row.style.borderBottomWidth = 1; row.style.borderBottomColor = new Color(0.8f, 0.7f, 0.5f);
            row.Add(Text(when, 18, true, new Color(0.55f, 0.36f, 0.08f)));
            row.Add(Text(text, 18, false, PaperInk));
            body.Add(row);
        }

        /// <summary>
        /// A card to pick a person: a small figure in their colours (headwear, face, clothes), the name, and a gold
        /// border when chosen. Cards added one after another sit side by side.
        /// </summary>
        public void PersonCard(string name, Color headwear, Color skin, Color clothes, bool scarf, bool chosen, Action pick)
        {
            VisualElement row = body.childCount > 0 && body[body.childCount - 1].name == "cards" ? body[body.childCount - 1] : null;
            if (row == null)
            {
                row = new VisualElement { name = "cards" };
                row.style.flexDirection = Lang.Arabic ? FlexDirection.RowReverse : FlexDirection.Row;
                row.style.justifyContent = Justify.Center; row.style.marginTop = 10; row.style.marginBottom = 4;
                body.Add(row);
            }
            var card = new Button(pick) { text = "" };
            card.style.width = 230; card.style.height = 300; card.style.marginLeft = card.style.marginRight = 14;
            card.style.backgroundColor = chosen ? new Color(1f, 0.96f, 0.84f) : new Color(0.97f, 0.93f, 0.83f);
            int bw = chosen ? 4 : 1;
            card.style.borderTopWidth = card.style.borderBottomWidth = card.style.borderLeftWidth = card.style.borderRightWidth = bw;
            card.style.borderTopColor = card.style.borderBottomColor = card.style.borderLeftColor = card.style.borderRightColor = chosen ? Gold : new Color(0.8f, 0.7f, 0.5f);
            Round(card, 14); card.style.alignItems = Align.Center; card.style.justifyContent = Justify.FlexEnd; Pad(card, 10, 8);

            // The figure: a head (in a scarf, or with hair on top), and the clothes below.
            var fig = new VisualElement(); fig.style.alignItems = Align.Center; fig.style.justifyContent = Justify.FlexEnd; fig.style.height = 200; fig.pickingMode = PickingMode.Ignore;
            VisualElement Blob(float w, float h, Color c, float r)
            {
                var e = new VisualElement(); e.style.width = w; e.style.height = h; e.style.backgroundColor = c; Round(e, r);
                e.pickingMode = PickingMode.Ignore; return e;
            }
            var head = Blob(scarf ? 84 : 74, scarf ? 92 : 78, scarf ? headwear : skin, 40); head.style.alignItems = Align.Center;
            if (scarf) { var face = Blob(50, 56, skin, 25); face.style.marginTop = 16; head.Add(face); }
            else { var hair = Blob(74, 30, headwear, 15); hair.style.borderBottomLeftRadius = hair.style.borderBottomRightRadius = 5; head.Add(hair); }
            fig.Add(head);
            var clothes1 = Blob(scarf ? 124 : 104, scarf ? 100 : 108, clothes, 22); clothes1.style.marginTop = scarf ? -14 : 5;
            clothes1.style.borderBottomLeftRadius = clothes1.style.borderBottomRightRadius = scarf ? 8 : 12;
            fig.Add(clothes1);
            card.Add(fig);
            var label = Text(name, 28, true, PaperInk); label.style.marginTop = 12; label.style.unityTextAlign = TextAnchor.MiddleCenter;
            card.Add(label);
            row.Add(card);
        }

        public void Choice(string label, Action pick)
        {
            var b = MakeButton(label, pick); b.style.alignSelf = Align.Stretch; b.style.marginTop = 6; body.Add(b);
        }

        // ------------------------------------------------------------------ measuring builds

        // "-quitafter 60": log the frame rate every 10 s and the average, then quit after 60 s of play in total.
        static float quitAfter = float.NaN, played; float logTimer, sceneTime; int logFrames, sceneFrames;

        void Measure()
        {
            if (float.IsNaN(quitAfter))
            {
                quitAfter = -1f; var a = Environment.GetCommandLineArgs();
                for (int i = 0; i < a.Length - 1; i++)
                    if (a[i] == "-quitafter") float.TryParse(a[i + 1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out quitAfter);
            }
            if (quitAfter <= 0f || Time.timeSinceLevelLoad < 3f) return;
            float dt = Time.unscaledDeltaTime; played += dt;
            logFrames++; logTimer += dt; sceneFrames++; sceneTime += dt;
            string scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (logTimer >= 10f) { Debug.Log($"[Rihla] FPS {logFrames / logTimer:0.0} at {Screen.width}x{Screen.height} in {scene} ({(Quality.Best ? "Best" : "Fast")})"); logFrames = 0; logTimer = 0f; }
            if (played > quitAfter)
            {
                Debug.Log($"[Rihla] average FPS {sceneFrames / sceneTime:0.0} over {sceneTime:0} s in {scene} ({(Quality.Best ? "Best" : "Fast")})");
                Application.Quit();
            }
        }

        // ------------------------------------------------------------------ per frame

        void Update()
        {
            Measure();
            if (bannerTimer > 0f && (bannerTimer -= Time.unscaledDeltaTime) <= 0f) banner.style.display = DisplayStyle.None;
            if (subtitleTimer > 0f && (subtitleTimer -= Time.unscaledDeltaTime) <= 0f) subtitle.style.display = DisplayStyle.None;
            if (fadeLength > 0f)
            {
                fadeTime += Time.unscaledDeltaTime;
                fade.style.opacity = Mathf.Lerp(fadeFrom, fadeTo, Mathf.SmoothStep(0f, 1f, fadeTime / fadeLength));
                if (fadeTime >= fadeLength) fadeLength = 0f;
            }
            var kb = Keyboard.current; if (kb == null) return;
            if (ModalOpen)
            {
                if (kb.escapeKey.wasPressedThisFrame || (kb.enterKey.wasPressedThisFrame && !MenuOpen)) CloseModal();
                else if (kb.mKey.wasPressedThisFrame && MenuOpen) CloseModal();
                if (kb.lKey.wasPressedThisFrame) Lang.Toggle();
                return;
            }
            if (kb.escapeKey.wasPressedThisFrame || kb.mKey.wasPressedThisFrame) { OpenMenu(); return; }
            if (kb.lKey.wasPressedThisFrame) Lang.Toggle();
            if (kb.qKey.wasPressedThisFrame) Quality.Toggle();
            if (kb.iKey.wasPressedThisFrame) ToggleHudSize();
            if (kb.jKey.wasPressedThisFrame && journalBtn.style.display == DisplayStyle.Flex) OnJournal?.Invoke();
            if (kb.hKey.wasPressedThisFrame && historyBtn.style.display == DisplayStyle.Flex) OnHistory?.Invoke();
        }
    }
}

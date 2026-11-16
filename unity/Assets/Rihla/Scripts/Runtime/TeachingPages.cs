using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Rihla
{
    /// <summary>
    /// The pages the game teaches with, laid out as in the browser prototype (src/game/journal.ts): a page of the
    /// Rihla, the story of a place through time, and the opening card. All text comes from content/.
    /// </summary>
    public static class TeachingPages
    {
        static ContentLibrary C => ContentLibrary.Instance;
        static RihlaUI UI => RihlaUI.Instance;

        public static JObject FirstPage => C?.Journal?["pages"] is JArray a && a.Count > 0 ? (JObject)a[0] : null;

        public static void ShowPage(JObject page, Action closed = null)
        {
            if (page == null || UI == null) return;
            UI.OpenModal(() =>
            {
                UI.H1(Lang.Pick(page["title"]));
                UI.H2(Lang.T("ancestorSays"));
                UI.P(Lang.Pick(page["ancestor"]), true);
                UI.H2(Lang.T("jiddoSays"));
                if (page["jiddo"]?[Lang.Current] is JArray paras) foreach (var p in paras) UI.P((string)p);
                AddText((string)page["verse"], page["verseIntro"]);
                var facts = new List<string>();
                if (page["facts"] is JArray fs) foreach (var f in fs) facts.Add(Lang.Pick(f));
                UI.Facts(facts);
            }, "close", closed);
        }

        /// <summary>True when content/history.json has the story of this place.</summary>
        public static bool HasHistory(string siteId) => C?.History?["sites"]?[siteId] != null;

        public static void ShowHistory(string siteId, Action closed = null)
        {
            if (!(C?.History?["sites"]?[siteId] is JObject site) || UI == null) return;
            UI.OpenModal(() =>
            {
                UI.H1(Lang.Pick(site["title"]));
                UI.P(Lang.Pick(site["intro"]));
                AddText((string)site["verse"], null);
                if (site["eras"] is JArray eras)
                    foreach (var e in eras) UI.Era(Lang.Digits(Lang.Pick(e["when"])), Lang.Pick(e["text"]));
            }, "close", closed);
        }

        /// <summary>A list of places to choose from, each opening its story through time.</summary>
        public static void ChooseHistory(IEnumerable<string> siteIds, string titleKeyOrText)
        {
            if (UI == null) return;
            UI.OpenModal(() =>
            {
                UI.H1(titleKeyOrText);
                foreach (var id in siteIds)
                    if (HasHistory(id)) { string s = id; UI.Choice(Lang.Pick(C.History["sites"][s]["title"]), () => ShowHistory(s)); }
            }, "close", null);
        }

        public static void ShowIntro(Action closed)
        {
            if (UI == null) return;
            UI.OpenModal(() =>
            {
                UI.H1(Lang.T("intro.title"));
                UI.P(Lang.T("intro.1")); UI.P(Lang.T("intro.2")); UI.P(Lang.T("intro.3"), true);
            }, "begin", closed);
        }

        /// <summary>A Quran verse or hadith from religious_texts.json, with its introduction if given.</summary>
        static void AddText(string id, JToken intro)
        {
            if (string.IsNullOrEmpty(id) || !(C?.ReligiousTexts?["texts"]?[id] is JObject v)) return;
            if (intro != null) UI.P(Lang.Pick(intro));
            string source = Lang.Pick(v["surah"] ?? v["collection"]);
            string reference = Lang.Digits(source + Lang.Sep + (string)v["reference"]);
            UI.Verse((string)v["ar"], reference, Lang.Arabic ? "" : Lang.T("meaning") + ": " + (string)v["en"]);
        }
    }
}

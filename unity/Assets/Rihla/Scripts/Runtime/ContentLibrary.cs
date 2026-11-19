using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Rihla
{
    /// <summary>
    /// The game's teaching and story content, read unchanged from the JSON files in content/
    /// (copied to Assets/Rihla/Content by tools/export-unity.ts). Nothing here is hard-coded,
    /// so teachers and scholars keep editing the same files the browser prototype uses.
    /// </summary>
    public class ContentLibrary : MonoBehaviour
    {
        public TextAsset places, history, journal, religiousTexts, english, arabic, walk, storybook, drive;

        public JObject Places { get; private set; }
        public JObject History { get; private set; }
        public JObject Journal { get; private set; }
        public JObject ReligiousTexts { get; private set; }
        public JObject Walk { get; private set; }
        public JObject Storybook { get; private set; }
        public JObject Drive { get; private set; }
        readonly Dictionary<string, JObject> strings = new Dictionary<string, JObject>();

        public static ContentLibrary Instance { get; private set; }

        void Awake()
        {
            Instance = this;
            Places = Parse(places);
            History = Parse(history);
            Journal = Parse(journal);
            ReligiousTexts = Parse(religiousTexts);
            Walk = Parse(walk);
            Storybook = Parse(storybook);
            Drive = Parse(drive);
            strings["en"] = Parse(english);
            strings["ar"] = Parse(arabic);
            Debug.Log($"[Rihla] content: {Count(Places, "places")} places, {Count(History, "sites")} history sites, " +
                      $"{Count(Journal, "pages")} journal pages, {Count(ReligiousTexts, "texts")} religious texts, " +
                      $"{strings["en"]?.Count ?? 0} English and {strings["ar"]?.Count ?? 0} Arabic strings");
        }

        static JObject Parse(TextAsset a) => a == null ? null : JObject.Parse(a.text);
        static int Count(JObject o, string key) => o?[key] is JContainer c ? c.Count : 0;

        /// <summary>A UI string by key, in "en" or "ar"; falls back to English, then to the key.</summary>
        public string T(string key, string lang = "en")
        {
            if (strings.TryGetValue(lang, out var s) && s?[key] is JValue v) return (string)v;
            if (lang != "en") return T(key, "en");
            return key;
        }

        /// <summary>The name of a place from places.json, or null.</summary>
        public string PlaceName(string id, string lang = "en")
        {
            if (Places?["places"] is JArray arr)
                foreach (var p in arr)
                    if ((string)p["id"] == id) return (string)p["name"]?[lang] ?? (string)p["name"]?["en"];
            return null;
        }
    }
}

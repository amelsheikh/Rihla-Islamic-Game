using System;
using System.Globalization;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Rihla
{
    /// <summary>The current language ("en" or "ar"), with helpers for numbers and bilingual JSON fields.</summary>
    public static class Lang
    {
        public static string Current { get; private set; } = "en";
        public static bool Arabic => Current == "ar";
        public static event Action Changed;

        public static void Set(string lang)
        {
            lang = lang == "ar" ? "ar" : "en";
            if (lang == Current) return;
            Current = lang;
            SaveData.Current.lang = lang; SaveData.Save();
            Changed?.Invoke();
        }

        public static void Toggle() => Set(Arabic ? "en" : "ar");

        /// <summary>
        /// The separator between parts of a line. Arabic uses a dash, because a middle dot looks exactly like the
        /// Arabic zero (٠) and a child could read it as a number.
        /// </summary>
        public static string Sep => Arabic ? " — " : " · ";

        /// <summary>
        /// True when the text speaks to Maryam in Arabic, which needs the feminine forms ("وجدتِ", "اضغطي").
        /// English needs none.
        /// </summary>
        public static bool Feminine => Arabic && SaveData.Current.hero == "maryam";

        /// <summary>
        /// A UI string from content/i18n. When speaking to Maryam in Arabic, the key with ".f" added is used if
        /// ar.json has it (for example "w.look.f").
        /// </summary>
        public static string T(string key)
        {
            var lib = ContentLibrary.Instance;
            if (!lib) return key;
            if (Feminine) { string f = lib.T(key + ".f", "ar"); if (f != key + ".f") return Clean(f); }
            return Clean(lib.T(key, Current));
        }

        /// <summary>
        /// The current language's text from a {"en": ..., "ar": ...} field; "ar_f" when speaking to Maryam in Arabic,
        /// if the field has one.
        /// </summary>
        public static string Pick(JToken field)
        {
            if (field == null) return "";
            if (field.Type == JTokenType.String) return Clean((string)field);
            if (Feminine && field["ar_f"] is JValue f) return Clean((string)f);
            return Clean((string)field[Current] ?? (string)field["en"] ?? "");
        }

        /// <summary>The paragraphs of a {"en": [...], "ar": [...]} field, with "ar_f" for Maryam in Arabic.</summary>
        public static JArray Paragraphs(JToken field) =>
            (Feminine ? field?["ar_f"] as JArray : null) ?? field?[Current] as JArray;

        /// <summary>
        /// In Arabic: a middle dot between parts of a line is shown as a dash, since "·" reads as the zero "٠", and
        /// Western digits in the text become Eastern Arabic ones (full stops are left alone).
        /// </summary>
        static string Clean(string s)
        {
            if (!Arabic || string.IsNullOrEmpty(s)) return s;
            var b = new StringBuilder(s.Replace(" · ", " — "));
            for (int i = 0; i < b.Length; i++) if (b[i] >= '0' && b[i] <= '9') b[i] = (char)('٠' + (b[i] - '0'));
            return b.ToString();
        }

        /// <summary>A number with the given decimals, in Eastern Arabic digits when the language is Arabic.</summary>
        public static string Num(double value, int decimals = 0) => Digits(value.ToString("F" + decimals, CultureInfo.InvariantCulture));

        /// <summary>Turns the Western digits in a string into Eastern Arabic ones (٠١٢٣٤٥٦٧٨٩) in Arabic.</summary>
        public static string Digits(string s)
        {
            if (!Arabic || string.IsNullOrEmpty(s)) return s;
            var b = new StringBuilder(s.Length);
            foreach (char c in s) b.Append(c >= '0' && c <= '9' ? (char)('٠' + (c - '0')) : c == '.' ? '٫' : c);
            return b.ToString();
        }
    }

    /// <summary>What the game remembers between sessions: language, pages found, and where the car was.</summary>
    [Serializable]
    public class SaveData
    {
        public string lang = "en";
        public string[] pages = new string[0];
        public bool introSeen;
        public string[] clues = new string[0];
        public bool walkDone;
        /// <summary>Which child the player chose: "yusuf" or "maryam" (STORY section 1).</summary>
        public string hero = "yusuf";
        public bool heroChosen;
        public bool hasPosition;
        public float x, y, z, yaw;

        const string Key = "rihla.save.v1";
        static SaveData current;
        public static SaveData Current => current ??= Load();

        /// <summary>
        /// Reads the save once per run, then applies the command line, in this order so that nothing undoes it:
        /// -newgame starts afresh, -arabic picks Arabic, -maryam picks Maryam.
        /// </summary>
        static SaveData Load()
        {
            var s = Read();
            var args = Environment.GetCommandLineArgs();
            bool Has(string a) => Array.IndexOf(args, a) >= 0;
            if (Has("-newgame")) s = new SaveData();
            if (Has("-arabic")) s.lang = "ar";
            if (Has("-maryam")) { s.hero = "maryam"; s.heroChosen = true; }
            current = s;
            if (Has("-newgame") || Has("-arabic") || Has("-maryam")) Save();
            return s;
        }

        static SaveData Read()
        {
            try
            {
                string json = UnityEngine.PlayerPrefs.GetString(Key, "");
                if (json.Length > 0) return UnityEngine.JsonUtility.FromJson<SaveData>(json) ?? new SaveData();
            }
            catch (Exception e) { UnityEngine.Debug.LogWarning("[Rihla] save could not be read, starting fresh: " + e.Message); }
            return new SaveData();
        }

        public static void Save()
        {
            UnityEngine.PlayerPrefs.SetString(Key, UnityEngine.JsonUtility.ToJson(Current));
            UnityEngine.PlayerPrefs.Save();
        }

        public bool HasPage(string id) => Array.IndexOf(pages, id) >= 0;

        public void AddPage(string id)
        {
            if (HasPage(id)) return;
            Array.Resize(ref pages, pages.Length + 1); pages[pages.Length - 1] = id; Save();
        }

        /// <summary>Clears everything (the -newgame command-line flag).</summary>
        public static void Reset() { current = new SaveData(); Save(); }
    }
}

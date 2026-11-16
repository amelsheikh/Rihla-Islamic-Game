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

        /// <summary>A UI string from content/i18n.</summary>
        public static string T(string key) => Clean(ContentLibrary.Instance ? ContentLibrary.Instance.T(key, Current) : key);

        /// <summary>The current language's text from a {"en": ..., "ar": ...} field.</summary>
        public static string Pick(JToken field) =>
            Clean(field == null ? "" : field.Type == JTokenType.String ? (string)field : (string)field[Current] ?? (string)field["en"] ?? "");

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
        public bool hasPosition;
        public float x, y, z, yaw;

        const string Key = "rihla.save.v1";
        static SaveData current;
        public static SaveData Current => current ??= Load();

        static SaveData Load()
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

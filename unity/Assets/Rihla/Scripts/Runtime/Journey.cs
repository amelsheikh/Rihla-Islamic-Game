using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Rihla
{
    /// <summary>
    /// The parts of the journey, in order, so the player can skip to any of them from the menu (the owner, 20 November
    /// 2026: "allow skipping parts of the journey"). Jumping sets what the game remembers as if the earlier parts had
    /// been played (pages found, stops passed), clears the later ones so they play again, and loads the scene.
    /// </summary>
    public static class Journey
    {
        public const string Home = "C1_Home", Walk = "E2_AlQuds_Walk", Road = "E0_JerichoRoad";

        public class Part
        {
            public string id, en, ar;
            public bool ready = true;
        }

        public static readonly Part[] Parts =
        {
            new Part { id = "book", en = "Evening at Jiddo's house: the old book", ar = "مساءً في بيت جدّو: الكتاب القديم" },
            new Part { id = "walk", en = "At dawn: the walk to al-Masjid al-Aqsa", ar = "عند الفجر: المشي إلى المسجد الأقصى" },
            new Part { id = "chest", en = "The family chest: Zaynab's first page", ar = "صندوق العائلة: صفحة زينب الأولى" },
            new Part { id = "road", en = "The road down from Al-Quds", ar = "الطريق نازلًا من القدس" },
            new Part { id = "khan", en = "Khan al-Ahmar", ar = "الخان الأحمر" },
            new Part { id = "flock", en = "The shepherd and his flock", ar = "الراعي وقطيعه" },
            new Part { id = "ariha", en = "Ariha, the city of palms", ar = "أريحا، مدينة النخيل" },
            new Part { id = "nabi-musa", en = "Maqam al-Nabi Musa", ar = "مقام النبي موسى" },
            new Part { id = "dead-sea", en = "The Dead Sea (coming soon)", ar = "البحر الميت (قريبًا)", ready = false },
        };

        /// <summary>Metres along the road where the car starts after a jump (read once by the road scene), or -1.</summary>
        public static float PendingDist = -1f;
        /// <summary>The place whose visit starts at once after a jump ("nabi-musa"), or null.</summary>
        public static string PendingVisit;

        /// <summary>Where the player is now in the journey (the part to mark in the menu).</summary>
        public static string Current()
        {
            var s = SaveData.Current; string scene = SceneManager.GetActiveScene().name;
            if (scene == Home) return s.walkDone ? "chest" : "book";
            if (scene == Walk) return "walk";
            if (s.HasStop("ariha")) return "nabi-musa";
            if (s.HasStop("flock")) return "ariha";
            if (s.HasStop("khan-al-ahmar")) return "flock";
            return "road";
        }

        public static void Jump(string id)
        {
            var s = SaveData.Current;
            int at = Array.FindIndex(Parts, p => p.id == id);
            bool After(string part) => at > Array.FindIndex(Parts, p => p.id == part);
            Debug.Log("[Rihla] journey: skip to " + id);

            s.prologueSeen = After("book");
            if (After("book")) s.heroChosen = true;
            s.walkDone = After("walk");
            s.chestSeen = After("chest");
            var pages = new List<string>(s.pages ?? new string[0]);
            void Page(string p, bool has) { if (has && !pages.Contains(p)) pages.Add(p); if (!has) pages.Remove(p); }
            Page("zaynab-1", After("chest"));
            Page("zaynab-2", After("ariha"));
            Page("zaynab-3", After("nabi-musa"));
            s.pages = pages.ToArray();
            var stops = new List<string>();
            if (After("khan")) stops.Add("khan-al-ahmar");
            if (After("flock")) stops.Add("flock");
            if (After("ariha")) stops.Add("ariha");
            if (After("nabi-musa")) stops.Add("nabi-musa");
            s.stops = stops.ToArray();
            // Clues of the place jumped to (and later ones) are found again.
            s.clues = (s.clues ?? new string[0]).Where(c =>
                !(c.StartsWith("ariha-") && !After("ariha")) && !(c.StartsWith("musa-") && !After("nabi-musa"))).ToArray();
            s.hasPosition = false;
            SaveData.Save();

            PendingDist = -1f; PendingVisit = null;
            string scene;
            switch (id)
            {
                case "book": case "chest": scene = Home; break;
                case "walk": scene = Walk; break;
                case "nabi-musa": scene = Road; PendingVisit = "nabi-musa"; break;
                default:
                    scene = Road;
                    PendingDist = id switch { "khan" => -2f, "flock" => -3f, "ariha" => -4f, _ => 0f };
                    break;
            }
            Time.timeScale = 1f;
            SceneManager.LoadScene(scene);
        }
    }
}

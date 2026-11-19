using System.Collections;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Rihla
{
    /// <summary>
    /// The opening of the game (content/storybook.json): evening in Jiddo's house in Al-Quds. Jiddo calls Yusuf and
    /// Maryam, opens his old book, and the camera goes down to its pages: a page for each place, turned by the
    /// player. Then Jiddo says they will walk to al-Masjid al-Aqsa at dawn, the player chooses who walks with him,
    /// and the walk begins. Esc skips to the choice. "-walk" and "-drive" start those scenes directly, for testing.
    /// </summary>
    public class StoryOpening : MonoBehaviour
    {
        public Camera cam;
        /// <summary>Camera shots: wide room, closer on Jiddo and the children, and down onto the book.</summary>
        public Transform wide, close, onBook;
        public Transform bookCover;
        /// <summary>C1a: the shot of the family chest, its lid (on a hinge) and Zaynab's page inside it.</summary>
        public Transform chestShot, chestLid, chestPage;
        /// <summary>Set by the walk when it ends: this time the room opens on the chest, not the story.</summary>
        public static bool chestNext;
        public string walkScene = "E2_AlQuds_Walk", roadScene = "E0_JerichoRoad";

        RihlaUI ui; JObject book; StoryBook pages; bool skipping, leaving;
        static bool Arg(string a) => System.Array.IndexOf(System.Environment.GetCommandLineArgs(), a) >= 0;
        static bool started;
        bool Auto => WalkDirector.autoWalk;

        void Start()
        {
            if (!started && Time.realtimeSinceStartup < 30f)
            {
                started = true;
                if (Arg("-walk")) { SceneManager.LoadScene(walkScene); return; }
                if (Arg("-drive")) { SceneManager.LoadScene(roadScene); return; }
                if (Arg("-chest")) chestNext = true;
            }
            ui = RihlaUI.Instance; book = ContentLibrary.Instance.Storybook;
            ui.SetHudVisible(false);
            ui.Fade(1f, 0f); ui.Fade(0f, 2.5f);
            Place(wide);
            var s = SaveData.Current;
            bool chest = chestShot && (chestNext || (s.prologueSeen && s.walkDone && !s.chestSeen));
            chestNext = false;
            StartCoroutine(chest ? RunChest() : Run());
        }

        void Place(Transform shot) { cam.transform.SetPositionAndRotation(shot.position, shot.rotation); }

        string Jiddo => Lang.Arabic ? "جدّو إبراهيم" : "Jiddo Ibrahim";

        IEnumerator Run()
        {
            ui.Banner(Lang.Pick(book["title"]), Lang.Pick(book["bookTitle"]), 4f);
            var lines = (JArray)book["opening"];
            yield return Move(wide, close, 7f, lines.Count > 0 ? lines[0] : null);
            for (int i = 1; i < lines.Count && !skipping; i++) yield return Line(lines[i], 5.5f);

            // Down onto the book; Jiddo opens it.
            if (!skipping)
            {
                ui.HideSubtitle();
                yield return Move(close, onBook, 3.2f, null);
                yield return OpenCover(1.2f);
            }
            if (!skipping)
            {
                bool closed = false;
                pages = new StoryBook(ui, book, () => closed = true);
                float auto = 0f;
                while (!closed)
                {
                    pages.Tick();
                    if (Auto && (auto += Time.unscaledDeltaTime) > 2.5f) { auto = 0f; pages.Turn(+1); }
                    if (skipping) pages.Close();
                    yield return null;
                }
            }

            // Back in the room: tomorrow at dawn, and who walks with Jiddo.
            Place(close);
            var end = (JArray)book["closing"];
            if (!skipping) yield return Line(end[0], 6f);
            ui.Say(Jiddo, Lang.Pick(end[1]), 30f);
            bool chosen = false;
            if (Auto) { SaveData.Current.heroChosen = true; SaveData.Save(); chosen = true; }
            else TeachingPages.ChooseHero(() => chosen = true);
            while (!chosen) yield return null;
            ui.Say(Jiddo, Lang.Pick(book["goodnight"]), 4f);
            yield return Wait(3f);
            Leave();
        }

        // ------------------------------------------------------------------ the chest (after the walk)

        ZaynabPage zaynab;

        /// <summary>
        /// Back home after the walk: Jiddo opens the family chest and the child reads Zaynab's first page, then
        /// solves her riddle on the map. Then the road down to Ariha.
        /// </summary>
        IEnumerator RunChest()
        {
            var ch = (JObject)book["chest"]; var lines = (JArray)ch["lines"];
            ui.Banner(Lang.Pick(ch["title"]), Lang.Pick(ch["when"]), 4f);
            yield return Move(wide, close, 6f, lines[0]);
            if (!skipping) yield return Line(lines[1], 7.5f);
            ui.HideSubtitle(); skipping = false;
            yield return Move(close, chestShot, 3f, null);
            yield return OpenChest(1.8f);
            ui.Say(Jiddo, Lang.Pick(lines[2]), 4f);
            for (float t = 0f; t < 2f; t += Time.unscaledDeltaTime) yield return null;

            bool closed = false, riddleSaid = false; float auto = 0f; int autoStep = 0;
            // The page shows Jiddo's hints itself, so his subtitle is hidden while it is open (it would cover the button).
            ui.HideSubtitle();
            zaynab = new ZaynabPage(ui, book, null, () => closed = true);
            while (!closed)
            {
                zaynab.Tick();
                if (zaynab.AtRiddle && !riddleSaid) { riddleSaid = true; Debug.Log("[Rihla] says (" + Lang.Current + "): " + Lang.Pick(ch["riddleLine"])); }
                if (Auto && (auto += Time.unscaledDeltaTime) > 2.5f)
                {
                    auto = 0f;
                    if (!zaynab.AtRiddle || zaynab.Solved) zaynab.Advance();
                    else zaynab.Choose(autoStep++ == 0 ? "yafa" : "ariha");
                }
                yield return null;
            }
            SaveData.Current.chestSeen = true; SaveData.Save();
            Place(close); skipping = false;
            ui.Say(Jiddo, Lang.Pick(ch["solvedLine"]), 6f);
            yield return Wait(5f);
            if (leaving) yield break;
            leaving = true;
            ui.Fade(1f, 1.5f);
            for (float t = 0f; t < 1.6f; t += Time.unscaledDeltaTime) yield return null;
            SceneManager.LoadScene(roadScene);
        }

        IEnumerator OpenChest(float seconds)
        {
            if (!chestLid) yield break;
            Creak();
            var start = chestLid.localRotation; var open = start * Quaternion.Euler(-105f, 0f, 0f);
            Vector3 p0 = chestPage ? chestPage.localPosition : Vector3.zero;
            if (chestPage) chestPage.gameObject.SetActive(true);
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / seconds)
            {
                float k = Mathf.SmoothStep(0f, 1f, t);
                chestLid.localRotation = Quaternion.Slerp(start, open, k);
                if (chestPage) chestPage.localPosition = p0 + Vector3.up * 0.22f * Mathf.SmoothStep(0f, 1f, (t - 0.4f) / 0.6f);
                yield return null;
            }
            chestLid.localRotation = open;
            Soundscape.Chime();
        }

        /// <summary>The creak of an old wooden lid: a rough, wavering low tone with a little grit.</summary>
        static void Creak()
        {
            int rate = 22050, n = rate * 9 / 10; var d = new float[n]; var rnd = new System.Random(11); float ph = 0f, lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / n, f = 120f + 70f * Mathf.Sin(t * 9f) + 40f * t + 25f * (float)rnd.NextDouble();
                ph += f / rate; float saw = (ph % 1f) * 2f - 1f;
                lp += (saw - lp) * 0.25f;
                float env = Mathf.Sin(Mathf.PI * t) * (0.6f + 0.4f * Mathf.Sin(t * 60f));
                d[i] = (lp * 0.6f + ((float)rnd.NextDouble() * 2f - 1f) * 0.08f) * env * 0.35f;
            }
            var clip = AudioClip.Create("creak", n, 1, rate, false); clip.SetData(d, 0);
            var go = new GameObject("Creak"); var src = go.AddComponent<AudioSource>(); src.spatialBlend = 0f; src.PlayOneShot(clip, 0.8f);
            Destroy(go, 2f);
        }

        IEnumerator Line(JToken line, float seconds)
        {
            ui.Say(Jiddo, Lang.Pick(line), seconds + 0.5f);
            yield return Wait(seconds);
        }

        IEnumerator Wait(float seconds)
        {
            for (float t = 0f; t < seconds && !skipping; t += Time.unscaledDeltaTime) yield return null;
        }

        IEnumerator Move(Transform from, Transform to, float seconds, JToken line)
        {
            if (line != null) ui.Say(Jiddo, Lang.Pick(line), seconds + 0.5f);
            for (float t = 0f; t < 1f && !skipping; t += Time.unscaledDeltaTime / seconds)
            {
                float k = Mathf.SmoothStep(0f, 1f, t);
                cam.transform.SetPositionAndRotation(Vector3.Lerp(from.position, to.position, k), Quaternion.Slerp(from.rotation, to.rotation, k));
                yield return null;
            }
            if (!skipping) Place(to);
        }

        IEnumerator OpenCover(float seconds)
        {
            if (!bookCover) yield break;
            var start = bookCover.localRotation; var open = start * Quaternion.Euler(0f, 0f, 178f);
            for (float t = 0f; t < 1f && !skipping; t += Time.unscaledDeltaTime / seconds)
            {
                bookCover.localRotation = Quaternion.Slerp(start, open, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }
            bookCover.localRotation = open;
        }

        void Leave()
        {
            if (leaving) return;
            leaving = true;
            SaveData.Current.prologueSeen = true; SaveData.Save();
            StartCoroutine(FadeOut());
        }

        IEnumerator FadeOut()
        {
            ui.Fade(1f, 1.5f);
            for (float t = 0f; t < 1.6f; t += Time.unscaledDeltaTime) yield return null;
            SceneManager.LoadScene(walkScene);
        }

        void Update()
        {
            if (ui == null || leaving) return;
            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame && !ui.ModalOpen && !skipping)
            {
                skipping = true; ui.HideSubtitle();
                Debug.Log("[Rihla] opening skipped");
            }
            // A gentle drift so the room never feels frozen while Jiddo speaks.
            if (pages == null || !pages.Open)
                cam.transform.position += new Vector3(Mathf.Sin(Time.time * 0.21f), Mathf.Sin(Time.time * 0.17f) * 0.4f, 0f) * 0.0006f;
        }
    }
}

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
            }
            ui = RihlaUI.Instance; book = ContentLibrary.Instance.Storybook;
            ui.SetHudVisible(false);
            ui.Fade(1f, 0f); ui.Fade(0f, 2.5f);
            Place(wide);
            StartCoroutine(Run());
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

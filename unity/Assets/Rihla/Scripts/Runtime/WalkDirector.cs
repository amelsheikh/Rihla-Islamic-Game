using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Rihla
{
    /// <summary>
    /// The walk with Jiddo from Bab al-Asbat to al-Masjid al-Aqsa (Phase E2, content/walk.json). Jiddo walks ahead
    /// along the route and waits for the child; at each step he says his line, and some steps wait for a clue to be
    /// found or for the Time Lens reveal. Clues are found by walking up to the glowing marker (or E when close).
    /// Keys: V first person, T Time Lens (after the reveal), G back to the car (at the start, or when done).
    /// </summary>
    public class WalkDirector : MonoBehaviour
    {
        public Walker child;
        public FollowCamera cam;
        public Transform jiddo;
        public TimeLens lens;
        /// <summary>Jiddo's route, and for each step of walk.json the index of the route point where it happens.</summary>
        public Vector3[] path;
        public int[] stepAt;
        /// <summary>Where each clue of walk.json is found, in the same order.</summary>
        public Transform[] clueSpots;
        /// <summary>The outline of al-Masjid al-Aqsa (x, z), to name where the child is.</summary>
        public Vector2[] compound;
        public Vector3 domeOfTheRock, alQibli, babAlAsbat;
        public string roadScene = "E0_JerichoRoad";
        public float jiddoSpeed = 1.3f;

        RihlaUI ui; JObject walk; JArray steps, clues;
        int step, target; bool waiting, lineSaid, revealQueued; float stepTimer;

        static string[] Found => SaveData.Current.clues ?? new string[0];

        void Start()
        {
            ui = RihlaUI.Instance; walk = ContentLibrary.Instance.Walk;
            steps = (JArray)walk["steps"]; clues = (JArray)walk["clues"];
            ui.OnHistory = () => { string s = NearestSite(out _); if (s != null) TeachingPages.ShowHistory(s); };
            var page = TeachingPages.FirstPage;
            ui.OnJournal = () => TeachingPages.ShowPage(page);
            ui.SetHistoryVisible(true);
            cam.Snap();
            ui.Banner(Lang.Pick(walk["title"]), Lang.Pick(ContentLibrary.Instance.Places?["places"]?.FirstOrDefault(p => (string)p["id"] == "al-quds")?["name"]), 6f);
            for (int i = 0; i < clueSpots.Length; i++) if (clueSpots[i]) clueSpots[i].gameObject.SetActive(!Has((string)clues[i]["id"]));
        }

        static bool Has(string clue) => System.Array.IndexOf(Found, clue) >= 0;

        // "-autowalk": the child follows Jiddo and walks to each clue by itself, and pages close after a moment,
        // so a build can be tested and measured over the whole walk without anyone at the keys.
        public static bool autoWalk = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-autowalk") >= 0;
        float modalTime;

        float autoLog;

        void AutoWalk()
        {
            if ((autoLog += Time.unscaledDeltaTime) > 10f)
            {
                autoLog = 0f;
                Debug.Log($"[Rihla] autowalk: step {step} waiting {waiting} said {lineSaid} target {target} jiddo {jiddo.position:F1} child {child.transform.position:F1} timeScale {Time.timeScale} modal {ui.ModalOpen}");
            }
            if (ui.ModalOpen) { if ((modalTime += Time.unscaledDeltaTime) > 3f) { modalTime = 0f; ui.CloseModal(); } return; }
            string want = step < steps.Count && waiting ? (string)steps[step]["id"] switch { "gate" => "lions", "tiles" => "tiles", "qibli" => "qibli-dome", _ => null } : null;
            int ci = want == null || Has(want) ? -1 : clues.ToList().FindIndex(c => (string)c["id"] == want);
            Vector3 goal = ci >= 0 ? clueSpots[ci].position : jiddo.position - jiddo.forward * 2.5f;
            child.enabled = false; // the test moves the child itself; the walker's gravity on slopes would fight it
            var cc = child.GetComponent<CharacterController>();
            Vector3 p = Vector3.MoveTowards(child.transform.position, goal, 3f * Time.deltaTime);
            Vector3 dir = goal - child.transform.position; dir.y = 0f;
            cc.enabled = false; child.transform.position = Ground(p) + Vector3.up * 0.05f;
            if (dir.sqrMagnitude > 0.01f) child.transform.rotation = Quaternion.LookRotation(dir);
            cc.enabled = true;
        }

        void Update()
        {
            if (ui == null) return;
            if (autoWalk) AutoWalk();
            if (ui.ModalOpen) return;
            Vector3 c = child.transform.position;
            Keys(c);
            Guide(c);
            Clues(c);
            Hud(c);
        }

        void Keys(Vector3 c)
        {
            var kb = Keyboard.current; if (kb == null) return;
            if (kb.vKey.wasPressedThisFrame) { cam.firstPerson = !cam.firstPerson; cam.Snap(); }
            if (kb.tKey.wasPressedThisFrame) lens.Toggle();
            if (kb.gKey.wasPressedThisFrame && Time.timeSinceLevelLoad > 1f && CanLeave(c)) SceneManager.LoadScene(roadScene);
        }

        bool CanLeave(Vector3 c) => SaveData.Current.walkDone || Flat(c, path[0]) < 25f;

        // ------------------------------------------------------------------ Jiddo

        void Guide(Vector3 c)
        {
            if (step >= steps.Count) return;
            Vector3 j = jiddo.position, goal = path[target];
            float toChild = Flat(j, c);
            if (!waiting)
            {
                // Walk on while the child keeps up; turn to look back when they fall behind.
                if (toChild < 9f)
                {
                    Vector3 d = goal - j; d.y = 0f;
                    float move = jiddoSpeed * Time.deltaTime;
                    if (d.magnitude <= move) { jiddo.position = Ground(goal); Arrive(); }
                    else { jiddo.position = Ground(j + d.normalized * move); jiddo.rotation = Quaternion.Slerp(jiddo.rotation, Quaternion.LookRotation(d), 6f * Time.deltaTime); }
                }
                else Face(c);
                return;
            }
            Face(c);
            stepTimer += Time.deltaTime;
            var s = steps[step]; string id = (string)s["id"];
            if (!lineSaid && toChild < 7f) { Say(s); lineSaid = true; stepTimer = 0f; }
            if (!lineSaid) return;
            bool done = id switch
            {
                "gate" => Has("lions"),
                "tiles" => Has("tiles"),
                "qibli" => Has("qibli-dome"),
                "lens" => lens.Revealed && stepTimer > lens.revealSeconds + 2f,
                "end" => false,
                _ => stepTimer > 4f,
            };
            if (id == "lens" && !revealQueued && stepTimer > 4f) { revealQueued = true; lens.Reveal(); }
            if (id == "end" && !SaveData.Current.walkDone) { SaveData.Current.walkDone = true; SaveData.Save(); }
            if (done) { waiting = false; lineSaid = false; step++; target = Mathf.Min(target + 1, path.Length - 1); }
        }

        void Arrive()
        {
            if (step < stepAt.Length && target == stepAt[step]) { waiting = true; stepTimer = 0f; }
            else target = Mathf.Min(target + 1, path.Length - 1);
        }

        void Say(JToken s) => ui.Say(Lang.Arabic ? "جدّو إبراهيم" : "Jiddo Ibrahim", Lang.Pick(s["jiddo"]), 9f);

        void Face(Vector3 c)
        {
            Vector3 d = c - jiddo.position; d.y = 0f;
            if (d.sqrMagnitude > 0.01f) jiddo.rotation = Quaternion.Slerp(jiddo.rotation, Quaternion.LookRotation(d), 4f * Time.deltaTime);
        }

        static Vector3 Ground(Vector3 p)
        {
            // From just above the feet, so arches and roofs overhead are not taken for the ground.
            if (Physics.Raycast(new Vector3(p.x, p.y + 1.5f, p.z), Vector3.down, out var hit, 6f, ~0, QueryTriggerInteraction.Ignore)) p.y = hit.point.y;
            return p;
        }

        // ------------------------------------------------------------------ clues

        int nearClue = -1;

        void Clues(Vector3 c)
        {
            nearClue = -1;
            for (int i = 0; i < clueSpots.Length; i++)
            {
                string id = (string)clues[i]["id"];
                if (!clueSpots[i] || Has(id)) continue;
                float d = Flat(c, clueSpots[i].position);
                if (d < 4f) nearClue = i;
                var kb = Keyboard.current;
                if (d < 1.8f || (d < 4f && kb != null && kb.eKey.wasPressedThisFrame)) { FindClue(i); return; }
            }
        }

        void FindClue(int i)
        {
            var clue = clues[i]; string id = (string)clue["id"];
            var list = Found.ToList(); list.Add(id); SaveData.Current.clues = list.ToArray(); SaveData.Save();
            clueSpots[i].gameObject.SetActive(false);
            Soundscape.Chime();
            var era = ContentLibrary.Instance.History?["sites"]?[(string)clue["site"]]?["eras"]?[(int)clue["era"]];
            ui.OpenModal(() =>
            {
                ui.H1(Lang.Pick(clue["title"]));
                if (era != null) { ui.Era(Lang.Pick(era["when"]), Lang.Pick(era["text"])); }
            }, "close", () => ui.Banner(Lang.T("w.clueFound"), Lang.T("w.clues") + ": " + Lang.Num(Found.Length) + " / " + Lang.Num(clues.Count), 4f));
        }

        // ------------------------------------------------------------------ HUD

        void Hud(Vector3 c)
        {
            string site = NearestSite(out float d);
            string main, other;
            var places = ContentLibrary.Instance.Places?["places"];
            string Name(string id, string lang) => (string)places?.FirstOrDefault(p => (string)p["id"] == id)?["name"]?[lang];
            if (site != null && d < 45f) { main = Name(site, Lang.Current); other = Name(site, Lang.Arabic ? "en" : "ar"); }
            else if (Inside(new Vector2(c.x, c.z))) { main = Lang.T("w.aqsa"); other = ContentLibrary.Instance.T("w.aqsa", Lang.Arabic ? "en" : "ar"); }
            else { main = Name("al-quds", Lang.Current); other = Name("al-quds", Lang.Arabic ? "en" : "ar"); }

            if (Soundscape.Instance) Soundscape.Instance.birdiness = Inside(new Vector2(c.x, c.z)) ? 0.85f : 0.25f;
            string hint = Lang.T("w.walkHint") + "\n" + Lang.T("w.view");
            if (lens.Revealed) hint += Lang.Sep + Lang.T("w.lens");
            if (nearClue >= 0) hint = Lang.T("w.look") + "\n" + hint;
            if (CanLeave(c)) hint += "\n" + Lang.T("w.toCar");
            ui.SetHud(Lang.Pick(walk["title"]), Lang.T("w.clues") + ": " + Lang.Num(Found.Length) + " / " + Lang.Num(clues.Count),
                lens.Label, main, other, hint);
            fpsTimer += Time.unscaledDeltaTime; frames++;
            if (fpsTimer > 0.5f) { ui.SetFps(frames / fpsTimer); frames = 0; fpsTimer = 0f; }
            ui.SetJournalVisible(TeachingPages.FirstPage != null && SaveData.Current.HasPage((string)TeachingPages.FirstPage["id"]));
        }

        float fpsTimer; int frames;

        string NearestSite(out float d)
        {
            d = float.MaxValue; string best = null; Vector3 c = child.transform.position;
            foreach (var (id, p) in new[] { ("bab-al-asbat", babAlAsbat), ("dome-of-the-rock", domeOfTheRock), ("al-qibli", alQibli) })
            {
                float e = Flat(c, p);
                if (e < d) { d = e; best = id; }
            }
            return best;
        }

        bool Inside(Vector2 p)
        {
            bool inside = false;
            for (int i = 0, j = compound.Length - 1; i < compound.Length; j = i++)
                if ((compound[i].y > p.y) != (compound[j].y > p.y) &&
                    p.x < (compound[j].x - compound[i].x) * (p.y - compound[i].y) / (compound[j].y - compound[i].y) + compound[i].x) inside = !inside;
            return inside;
        }

        static float Flat(Vector3 a, Vector3 b) => Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
    }
}

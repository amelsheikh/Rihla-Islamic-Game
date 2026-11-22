using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Rihla
{
    /// <summary>
    /// Maqam al-Nabi Musa on foot (C1c, STORY section 5), from content/nabimusa.json. The morning after Ariha, the car
    /// waits at the foot of the small road up to the Maqam; the child drives up, parks outside the gate and walks in
    /// with Jiddo. Three clues (the inscription of Baybars over the gate, the minaret, the small domes of the pilgrims'
    /// rooms); the keeper, Hajj Mahmoud, at the mosque door; his three banners blown about the courtyard, to find and
    /// bring back; his question (which sultan built the first mosque?); Page 3 of Zaynab's Rihla, whose riddle points
    /// to the Dead Sea. The Prophet Musa, peace be upon him, is never shown, and the shrine room is not entered.
    /// With -autopilot the whole visit plays by itself, for tests.
    /// </summary>
    public class NabiMusaVisit : MonoBehaviour
    {
        public static NabiMusaVisit Instance { get; private set; }

        public CarController car;
        public PlayerSwitch player;
        /// <summary>The main road down to Ariha, and the small road from it up to the Maqam.</summary>
        public RoadPath road, side;
        public Transform parking, keeper, keeperSpot, jiddo, siteCentre;
        public Transform[] clueSpots, banners, bannerStands;
        public Material guideMaterial;
        /// <summary>Metres along the small road where the car starts in the morning, and where it parks.</summary>
        public float startDist = 12f, parkDist;

        public enum Step { Off, Drive, GetOut, Clues, Keeper, Banners, BringBack, Question, Page, Done }
        public Step Now { get; private set; } = Step.Off;
        public bool Active => Now != Step.Off;
        public string Goal { get; private set; }
        public string GoalSub { get; private set; }
        public float Hold { get; private set; } = float.MaxValue;

        RihlaUI ui; JObject data; WalkGuide guide;
        readonly HashSet<string> found = new HashSet<string>();
        readonly Queue<(string who, JToken text, float secs)> lines = new Queue<(string, JToken, float)>();
        float busy, stepTime, autoTimer; bool auto, morning; int carried; ZaynabPage page;
        readonly List<Vector3> route = new List<Vector3>();

        void Awake() => Instance = this;

        void Start()
        {
            ui = RihlaUI.Instance;
            data = ContentLibrary.Instance ? ContentLibrary.Instance.NabiMusa : null;
            if (data == null) { enabled = false; return; }
            var g = new GameObject("Nabi Musa guide"); g.SetActive(false); g.transform.SetParent(transform, false);
            guide = g.AddComponent<WalkGuide>(); guide.material = guideMaterial; g.SetActive(true);
            foreach (var c in clueSpots) if (c && SaveData.Current.clues.Contains("musa-" + c.name)) { found.Add(c.name); c.gameObject.SetActive(false); }
            if (jiddo) jiddo.gameObject.SetActive(false);
            bool done = SaveData.Current.HasStop("nabi-musa");
            foreach (var s in bannerStands) s.gameObject.SetActive(done);
            foreach (var b in banners) b.gameObject.SetActive(!done);
            if (Journey.PendingVisit == "nabi-musa") { Journey.PendingVisit = null; StartCoroutine(Morning(0.1f)); }
        }

        string Name(string who) => Lang.Pick(data["names"]?[who]);
        void Say(string who, JToken text, float secs = 8f) => lines.Enqueue((who, text, secs));
        void SayAll(JToken list) { foreach (var l in list) Say((string)l["who"], l["text"], 8f); }
        void Go(Step s) { Now = s; stepTime = 0f; autoTimer = 0f; Debug.Log("[Rihla] Nabi Musa: " + s); }
        bool Talking => lines.Count > 0 || busy > 0f;
        void SetGoal(string key, string sub = null) { Goal = Lang.Pick(data["goals"][key]); GoalSub = sub; }

        /// <summary>The next morning: the car at the foot of the small road, ready to drive up (after Ariha, or from
        /// the menu).</summary>
        public void BeginMorning() { if (!morning && !SaveData.Current.HasStop("nabi-musa")) StartCoroutine(Morning(1.4f)); }

        IEnumerator Morning(float fade)
        {
            morning = true;
            if (fade > 0.2f) { ui.Fade(1f, fade); for (float t = 0f; t < fade + 0.1f; t += Time.unscaledDeltaTime) yield return null; }
            else ui.Fade(1f, 0f);
            if (player && player.OnFoot) player.Drive();
            car.SetRoad(side);
            Vector3 p = side.At(startDist), q = side.At(startDist + 6f), d = q - p; d.y = 0f;
            car.transform.SetPositionAndRotation(p + Vector3.up * 0.8f, Quaternion.LookRotation(d));
            var rb = car.GetComponent<Rigidbody>(); rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero;
            Physics.SyncTransforms();
            if (player && player.cam) player.cam.Snap();
            yield return null;
            ui.Fade(0f, 2f);
            ui.Banner(Lang.Pick(data["morning"]["title"]), Lang.Pick(data["morning"]["sub"]), 5f);
            Go(Step.Drive); SayAll(data["arrive"]);
        }

        void Update()
        {
            if (ui == null) return;
            float dt = Time.unscaledDeltaTime; stepTime += dt;
            auto = car.autopilot || WalkDirector.autoWalk;
            bool onFoot = player && player.OnFoot;
            Vector3 p = onFoot ? player.walker.transform.position : car.transform.position;

            // The car follows whichever road it is nearer: the small road up to the Maqam, or the main road.
            if (!onFoot)
            {
                bool onSide = Flat(side.points[side.Nearest(p)], p) < Flat(road.points[road.Nearest(p)], p);
                car.SetRoad(onSide ? side : road);
            }

            busy -= dt;
            if (busy <= 0f && lines.Count > 0 && !ui.ModalOpen)
            {
                var (who, text, secs) = lines.Dequeue();
                ui.Say(Name(who), Lang.Pick(text), secs); busy = secs + 0.3f;
            }

            Hold = float.MaxValue;
            route.Clear(); Vector3? beacon = null;
            if (onFoot) player.walker.slow = InsideWalls(p);
            switch (Now)
            {
                case Step.Off:
                    // Driving up the small road by oneself after Ariha also starts the visit.
                    if (!onFoot && SaveData.Current.HasStop("ariha") && !SaveData.Current.HasStop("nabi-musa")
                        && side.distances[side.Nearest(p)] > 40f && Flat(side.points[side.Nearest(p)], p) < 12f)
                    { Go(Step.Drive); SayAll(data["arrive"]); }
                    break;

                case Step.Drive:
                {
                    SetGoal(Flat(parking.position, p) < 60f ? "park" : "drive");
                    float along = side.distances[side.Nearest(p)];
                    if (car.autopilot) Hold = Mathf.Max(0f, (parkDist - along - 3f) * 0.6f);
                    if (!onFoot && Flat(parking.position, p) < 14f && car.SpeedKmh < 3f) Go(Step.GetOut);
                    if (onFoot && Flat(parking.position, p) < 50f) Go(Step.Clues);
                    break;
                }

                case Step.GetOut:
                    SetGoal("getOut"); Hold = 0f;
                    if (onFoot) { Go(Step.Clues); break; }
                    if (auto && stepTime > 3f) player.GetOut();
                    break;

                case Step.Clues:
                {
                    SetGoal("clues", Lang.Num(found.Count) + " / " + Lang.Num(clueSpots.Length));
                    if (!onFoot) break;
                    Transform next = Nearest(clueSpots.Where(c => !found.Contains(c.name)), p, out float best);
                    if (next == null) { Go(Step.Keeper); break; }
                    if (best < 2.4f && !ui.ModalOpen) { Find(next); break; }
                    if (!ui.ModalOpen) Way(p, next.position); beacon = next.position;
                    break;
                }

                case Step.Keeper:
                    SetGoal("keeper");
                    if (!onFoot) break;
                    Way(p, keeperSpot.position); beacon = keeper.position;
                    if (Flat(keeperSpot.position, p) < 2.6f && !Talking)
                    {
                        player.walker.autoTarget = null;
                        SayAll(data["keeper"]["greet"]); Go(Step.Banners);
                    }
                    break;

                case Step.Banners:
                {
                    SetGoal("banners", Lang.Num(carried) + " / " + Lang.Num(banners.Length));
                    if (!onFoot || Talking && stepTime < 2f) break;
                    Transform next = Nearest(banners.Where(b => b.gameObject.activeSelf), p, out float best);
                    if (next == null) { Go(Step.BringBack); break; }
                    if (best < 2.2f)
                    {
                        next.gameObject.SetActive(false); carried++; Soundscape.Chime();
                        ui.Banner(Lang.Pick(data["keeper"]["task"]["found"]), Lang.Num(carried) + " / " + Lang.Num(banners.Length), 3f);
                        player.walker.autoTarget = null;
                        break;
                    }
                    Way(p, next.position); beacon = next.position;
                    break;
                }

                case Step.BringBack:
                    SetGoal("bringBack");
                    if (!onFoot) break;
                    Way(p, keeperSpot.position); beacon = keeper.position;
                    if (Flat(keeperSpot.position, p) < 2.6f && !Talking)
                    {
                        player.walker.autoTarget = null;
                        foreach (var s in bannerStands) s.gameObject.SetActive(true);
                        ui.Say(Name("keeper"), Lang.Pick(data["keeper"]["task"]["done"]), 6f); busy = 6.3f;
                        Ask();
                    }
                    break;

                case Step.Question:
                    if (auto && ui.ModalOpen && (autoTimer += dt) > 2.5f) { autoTimer = -999f; Answer(data["keeper"]["question"]["choices"][1]); }
                    break;

                case Step.Page:
                    if (page == null && !Talking && stepTime > 1f)
                    {
                        var book = ContentLibrary.Instance.Storybook;
                        page = new ZaynabPage(ui, book, h => Say("jiddo", JValue.CreateString(h), 6f), EndVisit, (JObject)data["page"]);
                    }
                    if (page != null)
                    {
                        page.Tick();
                        if (auto && (autoTimer += dt) > 5f) { autoTimer = 0f; if (!page.AtRiddle) page.Advance(); else if (!page.Solved) page.Choose("dead-sea"); else page.Close(); }
                    }
                    break;

                case Step.Done:
                    SetGoal("done");
                    break;
            }

            if (onFoot && Active && Now != Step.Done && Now != Step.Page && Now != Step.Question && Flat(siteCentre.position, p) > 200f) SetGoal("back");
            guide.Show(route.Count >= 2 && Flat(route[0], route[route.Count - 1]) > 3f ? route : null, beacon);
            FollowJiddo(onFoot);
            if (auto && ui.ModalOpen && (Now == Step.Clues || Now == Step.Keeper || Now == Step.Banners) && (autoTimer += dt) > 2.5f) { autoTimer = 0f; ui.CloseModal(); }
        }

        /// <summary>The way to a point inside the Maqam: through the gate when the child is outside its walls.</summary>
        void Way(Vector3 p, Vector3 to)
        {
            // The courtyard is 48 m by 39 m inside walls 8 m deep; the gate passage runs from 20 to 28 m north of the
            // centre. Outside, the way goes round the corners of the walls rather than into them.
            Vector2 Loc(Vector3 q) => new Vector2(Vector3.Dot(q - siteCentre.position, siteCentre.right), Vector3.Dot(q - siteCentre.position, siteCentre.forward));
            Vector3 World(Vector2 l) { var q = siteCentre.position + siteCentre.right * l.x + siteCentre.forward * l.y; q.y = p.y; return q; }
            bool Inside(Vector2 l) => Mathf.Abs(l.x) < 24.5f && l.y > -20f && l.y < 24f;
            Vector2 a = Loc(p), b = Loc(to);
            var pts = new List<Vector2> { a };
            Vector2 gateOut = new Vector2(0f, 31.5f), gateIn = new Vector2(0f, 17f);
            bool inA = Inside(a), inB = Inside(b);
            // The gate passage counts as inside; gate points the child has already passed are left out (otherwise it
            // would turn back to them).
            bool passage = Mathf.Abs(a.x) < 3.5f && a.y >= 16f && a.y < 29.5f;
            bool atGate = Mathf.Abs(a.x) < 3.5f && a.y >= 16f && a.y < 33.5f;
            inA = inA || passage;
            if (inA && inB) pts.Add(b);
            else if (inA) { if (a.y < 17f) pts.Add(gateIn); pts.Add(gateOut); Around(pts, gateOut, b); }
            else if (inB) { if (!atGate) Around(pts, a, gateOut); if (a.y > 18f) pts.Add(gateIn); pts.Add(b); }
            else if (atGate) { var front = new Vector2(0f, 34f); pts.Add(front); Around(pts, front, b); }
            else Around(pts, a, b);
            foreach (var l in pts) route.Add(World(l));
            route[0] = p; route[route.Count - 1] = to;
            if (auto)
            {
                Vector3 next = route[route.Count - 1];
                for (int k = 1; k < route.Count; k++) if (Flat(route[k], p) > 1.3f) { next = route[k]; break; }
                player.walker.autoTarget = next;
            }
        }

        /// <summary>Adds the way from a to b outside the walls, through one or two corners when the straight line
        /// would cross the building (its walls, 3 m to spare).</summary>
        static void Around(List<Vector2> pts, Vector2 a, Vector2 b)
        {
            const float X = 35.5f, Z = 31.5f;
            var corners = new[] { new Vector2(-X, Z), new Vector2(X, Z), new Vector2(X, -Z), new Vector2(-X, -Z) };
            bool Clear(Vector2 p, Vector2 q) => !Crosses(p, q, X - 0.5f, Z - 0.5f);
            if (Clear(a, b)) { pts.Add(b); return; }
            float best = float.MaxValue; int c1 = -1, c2 = -1;
            for (int i = 0; i < 4; i++)
            {
                if (Clear(a, corners[i]) && Clear(corners[i], b))
                {
                    float d = Vector2.Distance(a, corners[i]) + Vector2.Distance(corners[i], b);
                    if (d < best) { best = d; c1 = i; c2 = -1; }
                }
                int n = (i + 1) % 4;
                foreach (var (u, v) in new[] { (i, n), (n, i) })
                    if (Clear(a, corners[u]) && Clear(corners[v], b))
                    {
                        float d = Vector2.Distance(a, corners[u]) + Vector2.Distance(corners[u], corners[v]) + Vector2.Distance(corners[v], b);
                        if (d < best) { best = d; c1 = u; c2 = v; }
                    }
            }
            if (c1 >= 0) pts.Add(corners[c1]);
            if (c2 >= 0) pts.Add(corners[c2]);
            pts.Add(b);
        }

        /// <summary>Whether the segment p-q passes through the rectangle |x| &lt; hx, |y| &lt; hz.</summary>
        static bool Crosses(Vector2 p, Vector2 q, float hx, float hz)
        {
            float t0 = 0f, t1 = 1f; Vector2 d = q - p;
            bool Clip(float den, float num)
            {
                if (Mathf.Abs(den) < 1e-6f) return num >= 0f;
                float t = num / den;
                if (den > 0f) { if (t < t1) t1 = t; } else { if (t > t0) t0 = t; }
                return t0 <= t1;
            }
            return Clip(-d.x, p.x + hx) && Clip(d.x, hx - p.x) && Clip(-d.y, p.y + hz) && Clip(d.y, hz - p.y) && t1 - t0 > 1e-4f;
        }

        /// <summary>Within the walls of the Maqam (its courtyard and the gate passage).</summary>
        bool InsideWalls(Vector3 q)
        {
            float f = Vector3.Dot(q - siteCentre.position, siteCentre.forward), r = Vector3.Dot(q - siteCentre.position, siteCentre.right);
            return Mathf.Abs(r) < 32f && f > -28f && f < 28f;
        }

        static Transform Nearest(IEnumerable<Transform> list, Vector3 p, out float best)
        {
            Transform next = null; best = float.MaxValue;
            foreach (var c in list) { float d = Flat(c.position, p); if (d < best) { best = d; next = c; } }
            return next;
        }

        void Find(Transform c)
        {
            found.Add(c.name);
            var list = SaveData.Current.clues.ToList(); list.Add("musa-" + c.name); SaveData.Current.clues = list.ToArray(); SaveData.Save();
            var box = c.GetComponent<ClueBox>(); if (box) box.Open(); else c.gameObject.SetActive(false);
            Soundscape.Chime();
            player.walker.autoTarget = null;
            var clue = data["clues"].First(x => (string)x["id"] == c.name);
            ui.OpenModal(() => { ui.H1(Lang.Pick(clue["title"])); ui.P(Lang.Pick(clue["text"])); }, "close",
                () => { Say("jiddo", clue["jiddo"], 6f); ui.Banner(Lang.T("w.clueFound"), Lang.Num(found.Count) + " / " + Lang.Num(clueSpots.Length), 4f); });
        }

        void Ask()
        {
            Go(Step.Question);
            var q = data["keeper"]["question"];
            void Build()
            {
                ui.H1(Lang.Pick(q["title"])); ui.P(Lang.Pick(q["ask"]));
                foreach (var c in q["choices"]) { var cc = c; ui.Choice(Lang.Pick(c["text"]), () => Answer(cc)); }
            }
            ui.OpenModal(Build, "close", () => { if (Now == Step.Question) Ask(); });
        }

        void Answer(JToken c)
        {
            if ((bool?)c["correct"] ?? false)
            {
                Soundscape.Chime();
                Go(Step.Page);
                ui.CloseModal();
                Say("keeper", c["yes"], 8f);
                SayAll(data["keeper"]["givePage"]);
            }
            else ui.Say(Name("keeper"), Lang.Pick(c["hint"]), 6f);
        }

        void EndVisit()
        {
            Go(Step.Done);
            SaveData.Current.AddStop("nabi-musa");
            ui.SetJournalVisible(true);
            ui.Banner(Lang.T("pageFound"), Lang.Pick(data["page"]["page"]["title"]), 6f);
            SayAll(data["keeper"]["end"]);
        }

        // ------------------------------------------------------------------ Jiddo on foot

        void FollowJiddo(bool onFoot)
        {
            if (!jiddo) return;
            bool show = onFoot && Now >= Step.Clues;
            if (jiddo.gameObject.activeSelf != show)
            {
                jiddo.gameObject.SetActive(show);
                if (show) jiddo.position = Ground(car.transform.position + car.transform.right * 2.2f);
            }
            if (!show) return;
            var child = player.walker.transform;
            bool atKeeper = (Now == Step.Keeper || Now == Step.Question || Now == Step.Page || Now == Step.BringBack) && Flat(keeperSpot.position, child.position) < 8f;
            Vector3 want = atKeeper ? keeperSpot.position + siteCentre.right * 1.8f
                                    : child.position - child.forward * 1.6f + child.right * 1.1f;
            Vector3 d = want - jiddo.position; d.y = 0f;
            float speed = Mathf.Clamp(d.magnitude * 1.2f, 0f, 3.4f);
            if (d.magnitude > 0.3f)
            {
                jiddo.position = Ground(jiddo.position + d.normalized * speed * Time.deltaTime);
                jiddo.rotation = Quaternion.Slerp(jiddo.rotation, Quaternion.LookRotation(d), Time.deltaTime * 4f);
            }
            else
            {
                Vector3 look = (atKeeper ? keeper.position : child.position) - jiddo.position; look.y = 0f;
                if (look.sqrMagnitude > 0.01f) jiddo.rotation = Quaternion.Slerp(jiddo.rotation, Quaternion.LookRotation(look), Time.deltaTime * 2f);
            }
        }

        static Vector3 Ground(Vector3 p)
        {
            if (Physics.Raycast(new Vector3(p.x, p.y + 3f, p.z), Vector3.down, out var hit, 10f, ~0, QueryTriggerInteraction.Ignore)) p.y = hit.point.y;
            return p;
        }

        static float Flat(Vector3 a, Vector3 b) => Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
    }
}

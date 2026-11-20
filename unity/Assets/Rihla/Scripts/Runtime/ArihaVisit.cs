using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Rihla
{
    /// <summary>
    /// Ariha on foot (C1c, STORY section 5), from content/ariha.json. At the end of the road down, Jiddo parks by the
    /// palms at the spring; the child gets out and, with Jiddo walking beside, finds three clues in glowing boxes (the
    /// spring of Ein al-Sultan, the tower of Tell al-Sultan, the date palms); then meets the keeper, Umm Khalil, at
    /// her fruit stall, helps her sort ripe dates for the market, answers her question (how old is Ariha?), and
    /// receives Page 2 of Zaynab's Rihla, whose riddle points to Maqam al-Nabi Musa.
    /// With -autopilot the whole visit plays by itself, for tests.
    /// </summary>
    public class ArihaVisit : MonoBehaviour
    {
        public CarController car;
        public PlayerSwitch player;
        public RoadPath road;
        public Transform parking, stall, keeper, jiddo;
        public Transform[] clueSpots;
        public Material guideMaterial;
        /// <summary>Metres along the road where the visit takes over the goal box.</summary>
        public float startDist, parkDist;

        public enum Step { Off, Park, GetOut, Clues, Keeper, Task, Question, Page, Done }
        public Step Now { get; private set; } = Step.Off;
        public bool Active => Now != Step.Off;
        public string Goal { get; private set; }
        public string GoalSub { get; private set; }
        /// <summary>The most Jiddo drives at (km/h) while parking; read by DriveStory.</summary>
        public float Hold { get; private set; } = float.MaxValue;
        public float ParkDist => parkDist;

        RihlaUI ui; JObject data; WalkGuide guide; int hint = -1;
        readonly HashSet<string> found = new HashSet<string>();
        readonly Queue<(string who, JToken text, float secs)> lines = new Queue<(string, JToken, float)>();
        float busy, stepTime, autoTimer;
        bool auto, taskOpen; ZaynabPage page; VisualElement taskLayer;
        readonly List<Vector3> route = new List<Vector3>();

        void Start()
        {
            ui = RihlaUI.Instance;
            data = ContentLibrary.Instance ? ContentLibrary.Instance.Ariha : null;
            if (data == null) { enabled = false; return; }
            var g = new GameObject("Ariha guide"); g.SetActive(false); g.transform.SetParent(transform, false);
            guide = g.AddComponent<WalkGuide>(); guide.material = guideMaterial; g.SetActive(true);
            foreach (var c in clueSpots) if (c && SaveData.Current.clues.Contains("ariha-" + c.name)) { found.Add(c.name); c.gameObject.SetActive(false); }
            if (jiddo) jiddo.gameObject.SetActive(false);
        }

        string Name(string who) => Lang.Pick(data["names"]?[who]);
        void Say(string who, JToken text, float secs = 8f) => lines.Enqueue((who, text, secs));
        void SayAll(JToken list) { foreach (var l in list) Say((string)l["who"], l["text"], 8f); }
        void Go(Step s) { Now = s; stepTime = 0f; autoTimer = 0f; Debug.Log("[Rihla] Ariha: " + s); }
        bool Talking => lines.Count > 0 || busy > 0f;

        void Update()
        {
            if (ui == null) return;
            float dt = Time.unscaledDeltaTime; stepTime += dt;
            auto = car.autopilot || WalkDirector.autoWalk;
            bool onFoot = player && player.OnFoot;
            Vector3 p = onFoot ? player.walker.transform.position : car.transform.position;
            int idx = road.Nearest(p, hint, 120);
            if (hint < 0 || Flat(road.points[idx], p) > 60f) idx = road.Nearest(p);
            hint = idx;
            float along = road.distances[idx];

            busy -= dt;
            if (busy <= 0f && lines.Count > 0 && !ui.ModalOpen)
            {
                var (who, text, secs) = lines.Dequeue();
                ui.Say(Name(who), Lang.Pick(text), secs); busy = secs + 0.3f;
            }

            Hold = float.MaxValue;
            route.Clear(); Vector3? beacon = null;
            switch (Now)
            {
                case Step.Off:
                    if (!onFoot && along > startDist && along < parkDist + 400f)
                    {
                        if (SaveData.Current.HasStop("ariha")) { Go(Step.Done); SetGoal("done"); break; }
                        Go(Step.Park); SayAll(data["arrive"]);
                    }
                    break;

                case Step.Park:
                    SetGoal("park");
                    if (car.autopilot) { float ahead = parkDist - along; Hold = Mathf.Max(0f, (ahead - 4f) * 0.5f); }
                    if (!onFoot && Flat(parking.position, p) < 16f && car.SpeedKmh < 3f) Go(Step.GetOut);
                    if (onFoot && Flat(parking.position, p) < 40f) Go(Step.Clues);
                    break;

                case Step.GetOut:
                    SetGoal("getOut"); Hold = 0f;
                    if (onFoot) { Go(Step.Clues); break; }
                    if (auto && stepTime > 3f) player.GetOut();
                    break;

                case Step.Clues:
                {
                    SetGoal("clues", Lang.Num(found.Count) + " / " + Lang.Num(clueSpots.Length));
                    if (!onFoot) break;
                    Transform next = null; float best = float.MaxValue;
                    foreach (var c in clueSpots) { if (found.Contains(c.name)) continue; float d = Flat(c.position, p); if (d < best) { best = d; next = c; } }
                    if (next == null) { Go(Step.Keeper); break; }
                    if (best < 2.4f && !ui.ModalOpen) { Find(next); break; }
                    route.Add(p); route.Add(next.position); beacon = next.position;
                    if (auto && !ui.ModalOpen) player.walker.autoTarget = next.position;
                    break;
                }

                case Step.Keeper:
                    SetGoal("keeper");
                    if (!onFoot) break;
                {
                    // Come round to the front of the table (where customers stand), not behind it beside her.
                    Vector3 front = stall.position - keeper.position; front.y = 0f; front.Normalize();
                    Vector3 table = (stall.position + keeper.position) / 2f;
                    bool frontSide = Vector3.Dot(p - table, front) > 0.3f;
                    Vector3 target = stall.position;
                    route.Add(p);
                    if (!frontSide)
                    {
                        Vector3 side = Vector3.Cross(Vector3.up, front); float s = Vector3.Dot(p - table, side) >= 0f ? 1f : -1f;
                        target = table + side * s * 3.4f + front * 1.0f; route.Add(target);
                    }
                    route.Add(stall.position); beacon = keeper.position;
                    if (auto) player.walker.autoTarget = Flat(target, p) < 0.9f ? stall.position : target;
                }
                    if (AtFront() && Flat(stall.position, p) < 2.6f && !Talking)
                    {
                        player.walker.autoTarget = null;
                        SayAll(data["keeper"]["greet"]); Go(Step.Task);
                    }
                    break;

                case Step.Task:
                    Goal = Lang.Pick(data["keeper"]["task"]["title"]); GoalSub = null; ui.SetGoal(Goal, null);
                    if (!taskOpen && !Talking && stepTime > 1f) OpenTask();
                    if (taskOpen && auto && (autoTimer += dt) > 1.4f) { autoTimer = 0f; AutoPick(); }
                    break;

                case Step.Question:
                    if (auto && ui.ModalOpen && (autoTimer += dt) > 2.5f) { autoTimer = -999f; Answer(data["keeper"]["question"]["choices"].Last()); }
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
                        if (auto && (autoTimer += dt) > 5f) { autoTimer = 0f; if (!page.AtRiddle) page.Advance(); else if (!page.Solved) page.Choose("nabi-musa"); else page.Close(); }
                    }
                    break;

                case Step.Done:
                    SetGoal("done");
                    // After Jiddo's last words, the next morning: the road up to Maqam al-Nabi Musa (C1c, second part).
                    if (endedHere && !Talking && stepTime > 6f && NabiMusaVisit.Instance) { endedHere = false; NabiMusaVisit.Instance.BeginMorning(); }
                    break;
            }

            if (onFoot && Active && Now != Step.Done && Now != Step.Page && Flat(parking.position, p) > 260f) SetGoal("back");
            guide.Show(route.Count >= 2 && Flat(route[0], route[1]) > 3f ? route : null, beacon);
            FollowJiddo(onFoot, dt);
            if (auto && ui.ModalOpen && (Now == Step.Clues || Now == Step.Keeper) && (autoTimer += dt) > 2.5f) { autoTimer = 0f; ui.CloseModal(); }
        }

        bool AtFront()
        {
            Vector3 front = stall.position - keeper.position; front.y = 0f; front.Normalize();
            Vector3 p = player.walker.transform.position;
            return Vector3.Dot(p - (stall.position + keeper.position) / 2f, front) > 0.3f;
        }

        void SetGoal(string key, string sub = null)
        {
            Goal = Lang.Pick(data["goals"][key]); GoalSub = sub;
        }

        void Find(Transform c)
        {
            found.Add(c.name);
            var list = SaveData.Current.clues.ToList(); list.Add("ariha-" + c.name); SaveData.Current.clues = list.ToArray(); SaveData.Save();
            var box = c.GetComponent<ClueBox>(); if (box) box.Open(); else c.gameObject.SetActive(false);
            Soundscape.Chime();
            player.walker.autoTarget = null;
            var clue = data["clues"].First(x => (string)x["id"] == c.name);
            ui.OpenModal(() => { ui.H1(Lang.Pick(clue["title"])); ui.P(Lang.Pick(clue["text"])); }, "close",
                () => { Say("jiddo", clue["jiddo"], 6f); ui.Banner(Lang.T("w.clueFound"), Lang.Num(found.Count) + " / " + Lang.Num(clueSpots.Length), 4f); });
        }

        // ------------------------------------------------------------------ Umm Khalil's dates

        readonly List<(Button b, bool ripe)> dates = new List<(Button, bool)>();
        int inBasket, ripeCount; Label basketLabel, note;

        void OpenTask()
        {
            taskOpen = true;
            var t = data["keeper"]["task"];
            taskLayer = new VisualElement(); taskLayer.style.position = Position.Absolute;
            taskLayer.style.left = taskLayer.style.right = taskLayer.style.top = taskLayer.style.bottom = 0;
            taskLayer.style.alignItems = Align.Center; taskLayer.style.justifyContent = Justify.Center;
            var card = new VisualElement(); card.style.width = 760; card.style.backgroundColor = new Color(0.95f, 0.9f, 0.78f);
            card.style.paddingLeft = card.style.paddingRight = 28; card.style.paddingTop = card.style.paddingBottom = 22;
            card.style.borderTopLeftRadius = card.style.borderTopRightRadius = card.style.borderBottomLeftRadius = card.style.borderBottomRightRadius = 16;
            card.Add(Centre(ui.Text(Lang.Pick(t["title"]), 30, true, new Color(0.3f, 0.18f, 0.08f))));
            var txt = ui.Text(Lang.Pick(t["text"]), 20, false, new Color(0.25f, 0.16f, 0.08f)); txt.style.whiteSpace = WhiteSpace.Normal; txt.style.marginTop = 8; card.Add(txt);
            // A tray of dates: soft brown ripe ones and hard yellow ones, mixed.
            var tray = new VisualElement(); tray.style.flexDirection = FlexDirection.Row; tray.style.flexWrap = Wrap.Wrap; tray.style.justifyContent = Justify.Center;
            tray.style.marginTop = 16; tray.style.backgroundColor = new Color(0.62f, 0.45f, 0.26f); tray.style.paddingTop = tray.style.paddingBottom = 12;
            tray.style.borderTopLeftRadius = tray.style.borderTopRightRadius = tray.style.borderBottomLeftRadius = tray.style.borderBottomRightRadius = 12;
            bool[] pattern = { true, false, true, true, false, true, false, true, true, false };
            dates.Clear(); inBasket = 0; ripeCount = pattern.Count(x => x);
            foreach (bool ripe in pattern)
            {
                var b = new Button(); b.text = "";
                b.style.width = 40; b.style.height = 62; b.style.marginLeft = b.style.marginRight = 10; b.style.marginTop = b.style.marginBottom = 6;
                b.style.borderTopLeftRadius = b.style.borderTopRightRadius = b.style.borderBottomLeftRadius = b.style.borderBottomRightRadius = 20;
                b.style.borderLeftWidth = b.style.borderRightWidth = b.style.borderTopWidth = b.style.borderBottomWidth = 0;
                b.style.backgroundColor = ripe ? new Color(0.36f, 0.19f, 0.09f) : new Color(0.93f, 0.76f, 0.2f);
                var entry = (b, ripe);
                b.clicked += () => Pick(entry);
                dates.Add(entry); tray.Add(b);
            }
            card.Add(tray);
            basketLabel = ui.Text("", 22, true, new Color(0.3f, 0.18f, 0.08f)); basketLabel.style.marginTop = 12; card.Add(Centre(basketLabel));
            note = ui.Text("", 19, false, new Color(0.55f, 0.15f, 0.08f)); note.style.whiteSpace = WhiteSpace.Normal; card.Add(Centre(note));
            taskLayer.Add(card);
            ui.AddLayer(taskLayer);
            UpdateBasket();
        }

        static Label Centre(Label l) { l.style.unityTextAlign = TextAnchor.MiddleCenter; l.AddToClassList("center"); return l; }

        void UpdateBasket()
        {
            basketLabel.text = Lang.Pick(data["keeper"]["task"]["basket"]) + ": " + Lang.Num(inBasket) + " / " + Lang.Num(ripeCount);
        }

        void Pick((Button b, bool ripe) d)
        {
            if (!d.b.enabledSelf) return;
            var t = data["keeper"]["task"];
            if (!d.ripe) { note.text = Lang.Pick(t["notYet"]); return; }
            d.b.SetEnabled(false); d.b.style.opacity = 0.15f; inBasket++; note.text = ""; UpdateBasket();
            Soundscape.Chime();
            if (inBasket >= ripeCount)
            {
                note.style.color = new Color(0.2f, 0.45f, 0.24f); note.text = Lang.Pick(t["done"]);
                var next = new Button(() => { taskLayer.RemoveFromHierarchy(); taskOpen = false; Ask(); }) { text = Lang.Pick(t["next"]) };
                next.style.fontSize = 22; next.style.marginTop = 12; next.style.alignSelf = Align.Center;
                taskLayer[0].Add(next);
                if (auto) { taskLayer.RemoveFromHierarchy(); taskOpen = false; Ask(); }
            }
        }

        void AutoPick()
        {
            // A test picks one yellow date first (to see the hint), then the ripe ones.
            var y = dates.FirstOrDefault(x => !x.ripe && x.b.enabledSelf);
            if (note.text.Length == 0 && inBasket == 0 && y.b != null) { Pick(y); return; }
            var r = dates.FirstOrDefault(x => x.ripe && x.b.enabledSelf);
            if (r.b != null) Pick(r);
        }

        // ------------------------------------------------------------------ the question and the page

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

        bool endedHere;

        void EndVisit()
        {
            Go(Step.Done); endedHere = true;
            SaveData.Current.AddStop("ariha");
            ui.SetJournalVisible(true);
            ui.Banner(Lang.T("pageFound"), Lang.Pick(data["page"]["page"]["title"]), 6f);
            SayAll(data["keeper"]["end"]);
        }

        // ------------------------------------------------------------------ Jiddo on foot

        void FollowJiddo(bool onFoot, float dt)
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
            Vector3 want = Now >= Step.Keeper && Flat(stall.position, child.position) < 8f
                ? stall.position + (child.position - stall.position).normalized * 2.5f + Vector3.Cross(Vector3.up, (child.position - stall.position).normalized) * 1.6f
                : child.position - child.forward * 1.6f + child.right * 1.1f;
            Vector3 d = want - jiddo.position; d.y = 0f;
            float speed = Mathf.Clamp(d.magnitude * 1.2f, 0f, 3f);
            if (d.magnitude > 0.3f)
            {
                jiddo.position = Ground(jiddo.position + d.normalized * speed * Time.deltaTime);
                jiddo.rotation = Quaternion.Slerp(jiddo.rotation, Quaternion.LookRotation(d), Time.deltaTime * 4f);
            }
            else
            {
                Vector3 look = child.position - jiddo.position; look.y = 0f;
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

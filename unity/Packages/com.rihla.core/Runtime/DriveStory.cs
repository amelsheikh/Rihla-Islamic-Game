using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Rihla
{
    /// <summary>
    /// The story of the road down to Ariha (C1b, STORY section 5), from content/drive.json:
    /// Jiddo's short remarks about the land at places along the road (a sentence or two, never while something else
    /// is being said); the stop at Khan al-Ahmar, where Jiddo tells what a khan was; and the shepherd crossing with his
    /// flock, where the child learns to wait. When Jiddo drives (autopilot) he slows and stops for both.
    /// DriveGuide reads the state here for the goal box and the arrow.
    /// </summary>
    public class DriveStory : MonoBehaviour
    {
        public CarController car;
        public PlayerSwitch player;
        public RoadPath road;
        /// <summary>The ruin of the khan, made by the scene builder (the arrow points to the road beside it).</summary>
        public Transform khan;
        public Flock flock;
        public ArihaVisit visit;

        RihlaUI ui; JObject data; int hint = -1;
        readonly HashSet<string> said = new HashSet<string>();
        float busy, gapUntil;
        readonly Queue<(string who, JToken text, float secs)> lines = new Queue<(string, JToken, float)>();

        // Khan al-Ahmar.
        public float KhanRoadDist { get; private set; } = -1f;
        public Vector3 KhanStop { get; private set; }
        public bool KhanPending => KhanRoadDist >= 0f && !khanDone;
        public bool KhanTalking { get; private set; }
        bool khanDone;

        // The flock.
        float flockDist = -1f, fastTime; bool flockTold, flockFast, flockEnd;
        /// <summary>The flock is crossing the road just ahead: wait.</summary>
        public bool FlockWaiting { get; private set; }

        void Start()
        {
            ui = RihlaUI.Instance;
            data = ContentLibrary.Instance ? ContentLibrary.Instance.Drive : null;
            if (data == null) { enabled = false; return; }
            var kp = road.places.FirstOrDefault(p => p.id == (string)data["khan"]?["place"]);
            if (kp.id != null) { KhanRoadDist = kp.roadDist; KhanStop = road.At(kp.roadDist); }
            khanDone = SaveData.Current.HasStop("khan-al-ahmar");
            flockDist = flock ? road.distances[road.Nearest((flock.from + flock.to) * 0.5f)] : -1f;
            if (flock && SaveData.Current.HasStop("flock")) { flock.AlreadyCrossed(); flockEnd = true; }
            // Remarks already behind the car (a saved position further down) stay unsaid.
            float along = road.distances[road.Nearest(car.transform.position)];
            foreach (var r in data["remarks"]) if ((float)r["at"] < along - 50f) said.Add((string)r["id"]);
        }

        string Name(string who) => Lang.Pick(data["names"]?[who]);

        void Say(string who, JToken text, float secs = 8.5f) => lines.Enqueue((who, text, secs));

        void Update()
        {
            if (ui == null) return;
            float dt = Time.deltaTime;
            bool onFoot = player && player.OnFoot;
            Vector3 p = onFoot ? player.walker.transform.position : car.transform.position;
            int idx = road.Nearest(p, hint, 120);
            if (hint < 0 || Flat(road.points[idx], p) > 60f) idx = road.Nearest(p);
            hint = idx;
            float along = road.distances[idx], kmh = onFoot ? 0f : car.SpeedKmh;
            float hold = float.MaxValue;

            // Lines waiting to be said, one after another.
            busy -= dt;
            if (busy <= 0f && lines.Count > 0 && !ui.ModalOpen)
            {
                var (who, text, secs) = lines.Dequeue();
                ui.Say(Name(who), Lang.Pick(text), secs);
                busy = secs + 0.4f; gapUntil = Time.time + secs + 6f;
            }
            if (KhanTalking && lines.Count == 0 && busy <= 0f)
            {
                KhanTalking = false; khanDone = true; SaveData.Current.AddStop("khan-al-ahmar");
            }

            // Khan al-Ahmar: stop by it (or walk up to it) and Jiddo tells about it.
            if (KhanPending && !KhanTalking)
            {
                float toStop = Flat(KhanStop, p);
                bool there = onFoot ? (khan ? Flat(khan.position, p) < 30f : toStop < 25f) || toStop < 15f : toStop < 45f && kmh < 8f;
                if (car.autopilot && !onFoot)
                {
                    float ahead = KhanRoadDist - along;
                    if (ahead > -10f && ahead < 140f) hold = Mathf.Max(0f, (ahead - 6f) * 0.45f);
                    if (toStop < 45f && kmh < 3f) there = true;
                }
                if (there)
                {
                    KhanTalking = true;
                    var k = data["khan"];
                    ui.Banner(Lang.Pick(k["banner"]), Lang.Pick(k["bannerSub"]), 7f);
                    lines.Clear(); busy = Mathf.Min(busy, 1.2f);
                    foreach (var l in k["lines"]) Say((string)l["who"], l["text"], 9f);
                }
                else if (along > KhanRoadDist + 220f)
                {
                    // Driven past: no going back for it; it waits for another drive.
                    khanDone = true; Say("jiddo", data["khan"]["missed"], 6f);
                }
            }
            if (KhanTalking && car.autopilot) hold = 0f;

            // The shepherd and his flock.
            FlockWaiting = false; car.forceStop = false;
            if (flock && flockDist > 0f && !flockEnd)
            {
                float toFlock = Flat(flock.from + (flock.to - flock.from) * 0.5f, p);
                float ahead = flockDist - along;
                if (flock.Now == Flock.State.Grazing && toFlock < 95f && !KhanTalking)
                {
                    flock.Cross();
                    lines.Clear(); busy = Mathf.Min(busy, 0.5f);
                    Say("jiddo", data["flock"]["start"], 7f); flockTold = true;
                }
                if (flock.Now == Flock.State.Crossing && toFlock < 120f)
                {
                    FlockWaiting = true;
                    if (!onFoot)
                    {
                        if (car.autopilot) hold = Mathf.Min(hold, Mathf.Max(0f, (toFlock - 22f) * 0.6f));
                        else
                        {
                            if (toFlock < 60f && kmh > 22f && !flockFast) { flockFast = true; busy = 0f; lines.Clear(); Say("jiddo", data["flock"]["tooFast"], 5f); }
                            if (toFlock < 28f && Mathf.Abs(ahead) < 40f) { car.forceStop = true; if (kmh > 8f && (fastTime += dt) > 0.6f) flockFast = true; }
                        }
                    }
                }
                if (flock.Now == Flock.State.Done && flockTold)
                {
                    flockEnd = true; SaveData.Current.AddStop("flock");
                    Say("jiddo", data["flock"]["greet"], 3f);
                    Say("shepherd", data["flock"]["reply"], 5.5f);
                    Say("jiddo", data["flock"][flockFast ? "impatient" : "patient"], 7f);
                }
            }

            // Jiddo's remarks about the land, at their places along the road.
            if (!(visit && visit.Active) && !(NabiMusaVisit.Instance && NabiMusaVisit.Instance.Active) && !KhanTalking && !FlockWaiting && lines.Count == 0 && busy <= 0f && Time.time > gapUntil && Time.timeSinceLevelLoad > 4f)
            {
                foreach (var r in data["remarks"])
                {
                    string id = (string)r["id"]; float at = (float)r["at"];
                    if (said.Contains(id)) continue;
                    if (along >= at && along < at + 450f) { said.Add(id); Say("jiddo", r["jiddo"], 9f); break; }
                    if (along >= at + 450f) said.Add(id);
                }
            }

            if (visit) hold = Mathf.Min(hold, visit.Hold);
            if (NabiMusaVisit.Instance) hold = Mathf.Min(hold, NabiMusaVisit.Instance.Hold);
            car.holdKmh = hold;
        }

        static float Flat(Vector3 a, Vector3 b) => Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
    }
}

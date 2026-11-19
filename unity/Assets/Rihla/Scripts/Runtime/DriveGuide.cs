using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Rihla
{
    /// <summary>
    /// Guidance on the road down to Ariha (C1b), as in the walk: a box at the top of the screen always says what to do
    /// now, big golden arrows run along the road ahead to the next goal, and a golden arrow floats over the goal.
    /// The goals in order: the page at Khan al-Hatruri; the place where the road goes below the level of the sea;
    /// Ariha at the end of the road. Also: back to the road when the car is off it, back to the car on foot, and
    /// Jiddo driving. Texts are the "d." strings of content/i18n.
    /// </summary>
    public class DriveGuide : MonoBehaviour
    {
        public CarController car;
        public PlayerSwitch player;
        public RoadPath road;
        public Transform pageMarker;
        public Material material;
        /// <summary>Game heights are compressed; as in Teaching.</summary>
        public float verticalScale = 2.5f;

        RihlaUI ui; WalkGuide guide; string pageId, pagePlace;
        float pageDist = -1f, seaDist = -1f; int hint = -1;
        readonly List<Vector3> route = new List<Vector3>();

        void Start()
        {
            ui = RihlaUI.Instance;
            var page = TeachingPages.FirstPage;
            pageId = (string)page?["id"]; pagePlace = (string)page?["place"];
            var pl = road.places.FirstOrDefault(x => x.id == pagePlace);
            if (pl.id != null) pageDist = pl.roadDist;
            // Where the road first goes below the level of the sea.
            for (int i = 1; i < road.points.Length; i++)
                if (road.points[i - 1].y * verticalScale >= 0f && road.points[i].y * verticalScale < 0f) { seaDist = road.distances[i]; break; }
            var g = new GameObject("Drive guide"); g.SetActive(false);
            guide = g.AddComponent<WalkGuide>(); guide.material = material;
            guide.count = 14; guide.spacing = 9f; guide.start = 14f; guide.arrowScale = 3.2f;
            guide.beaconHeight = 9f; guide.beaconScale = 5f; guide.slideSpeed = 7f;
            guide.tilt = 60f; guide.lift = 1.1f;
            g.SetActive(true);
        }

        bool Found => pageId != null && SaveData.Current.HasPage(pageId);

        void Update()
        {
            if (ui == null || road == null) return;
            bool onFoot = player && player.OnFoot;
            Vector3 p = onFoot ? player.walker.transform.position : car.transform.position;
            int idx = road.Nearest(p, hint, 120);
            if (hint < 0 || Flat(road.points[idx], p) > 60f) idx = road.Nearest(p);
            hint = idx;
            float along = road.distances[idx], fromRoad = Flat(road.points[idx], p);

            // The goal now, along the road.
            string key; float goalDist; Vector3 goal;
            bool arrows = true;
            if (!Found && pageDist >= 0f && pageMarker)
            {
                goalDist = pageDist; goal = pageMarker.position;
                float toPage = Flat(goal, p);
                key = onFoot ? "d.walkToPage" : toPage < 120f ? "d.stopAtPage" : "d.toKhan";
                if (toPage < 40f) arrows = false;
            }
            else if (seaDist > 0f && along < seaDist - 15f)
            {
                goalDist = seaDist; goal = road.At(seaDist); key = "d.toSea";
            }
            else
            {
                goalDist = road.Length; goal = road.points[road.points.Length - 1]; key = "d.toAriha";
                if (road.Length - along < 150f) { key = "d.arrived"; arrows = false; }
            }
            if (onFoot && key != "d.walkToPage") { key = "d.backToCar"; goal = car.transform.position; arrows = false; }
            else if (!onFoot && fromRoad > 22f) key = "d.offRoad";
            else if (!onFoot && car.autopilot && key != "d.arrived") key = "d.jiddo";

            // Arrows along the road from here towards the goal, at most 130 m ahead.
            route.Clear();
            if (arrows && !onFoot)
            {
                route.Add(p);
                float dir = Mathf.Sign(goalDist - along), end = along + dir * Mathf.Min(130f, Mathf.Abs(goalDist - along));
                if (fromRoad > 6f) route.Add(road.points[idx]);
                for (float d = along + dir * 6f; dir > 0 ? d < end : d > end; d += dir * 6f) route.Add(road.At(d));
                route.Add(road.At(end));
            }
            float metres = Mathf.Abs(goalDist - along);
            bool near = Flat(goal, p) < 600f;
            guide.Show(route.Count > 2 && metres > 20f ? route : null, near || key == "d.backToCar" ? goal : (Vector3?)null);

            string sub;
            if (Time.timeSinceLevelLoad < 30f && !onFoot && !car.autopilot) sub = Lang.T("d.keys");
            else if (key == "d.arrived" || key == "d.backToCar") sub = null;
            else sub = metres >= 1000f ? Lang.Num(metres / 1000f, 1) + " " + Lang.T("u.km") : Lang.Num(metres) + " " + Lang.T("u.m");
            ui.SetGoal(Lang.T(key), sub);
        }

        static float Flat(Vector3 a, Vector3 b) => Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
    }
}

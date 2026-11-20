using System.Linq;
using UnityEngine;

namespace Rihla
{
    /// <summary>
    /// Guidance on the road down to Ariha (C1b). A box at the top of the screen always says what to do now, and one
    /// soft golden arrow floats in the air a little way ahead on the road, turning with the road, with a pool of
    /// light on the road under it. Near a goal the arrow glides over to it and points down. It dims while the child
    /// drives the right way and brightens when the car stops, turns back or leaves the road.
    /// The goals in order: the stop at Khan al-Ahmar (DriveStory); the page at Khan al-Hatruri; the place where the
    /// road goes below the level of the sea; Ariha. Also: waiting for the shepherd's flock, back to the road, back to
    /// the car on foot, and Jiddo driving. Texts are the "d." strings of content/i18n.
    /// </summary>
    public class DriveGuide : MonoBehaviour
    {
        public CarController car;
        public PlayerSwitch player;
        public RoadPath road;
        public Transform pageMarker;
        public DriveStory story;
        public ArihaVisit visit;
        /// <summary>An additive (glowing, see-through) unlit material for the arrow and the light on the road.</summary>
        public Material material;
        /// <summary>Game heights are compressed; as in Teaching.</summary>
        public float verticalScale = 2.5f;
        public float height = 2.6f, arrowSize = 1.5f, tilt = 48f;
        public Color colour = new Color(1f, 0.74f, 0.32f);

        RihlaUI ui; string pageId, pagePlace;
        float pageDist = -1f, seaDist = -1f, arihaDist = -1f, glow, goodTime; int hint = -1;
        Transform arrow, pool; Material arrowMat, poolMat; Vector3 arrowVel; bool placed;

        void Start()
        {
            ui = RihlaUI.Instance;
            var page = TeachingPages.FirstPage;
            pageId = (string)page?["id"]; pagePlace = (string)page?["place"];
            var pl = road.places.FirstOrDefault(x => x.id == pagePlace);
            if (pl.id != null) pageDist = pl.roadDist;
            var ar = road.places.FirstOrDefault(x => x.id == "ariha");
            arihaDist = ar.id != null ? ar.roadDist : road.Length;
            // Where the road first goes below the level of the sea.
            for (int i = 1; i < road.points.Length; i++)
                if (road.points[i - 1].y * verticalScale >= 0f && road.points[i].y * verticalScale < 0f) { seaDist = road.distances[i]; break; }

            arrowMat = new Material(material) { name = "Drive arrow" };
            poolMat = new Material(material) { name = "Road light" };
            poolMat.SetTexture("_BaseMap", SoftSpot());
            arrow = Part("Guide arrow", ArrowMesh(), arrowMat);
            pool = Part("Guide light on the road", Quad(), poolMat);
        }

        Transform Part(string name, Mesh mesh, Material m)
        {
            var go = new GameObject(name); go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            go.SetActive(false);
            return go.transform;
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

            // In Ariha the visit sets the goal; the arrow shows only where to park.
            if (visit && visit.Active)
            {
                bool park = visit.Now == ArihaVisit.Step.Park && !onFoot && !car.autopilot;
                float kmhV = onFoot ? 0f : car.SpeedKmh;
                glow = Mathf.MoveTowards(glow, park ? 0.8f : 0f, Time.deltaTime * 0.8f);
                Place(p, idx, along, visit.ParkDist, visit.parking.position, fromRoad, kmhV, false);
                ui.SetGoal(visit.Goal, visit.GoalSub);
                return;
            }

            // The goal now, along the road.
            string key; float goalDist; Vector3 goal;
            bool show = true;
            if (story && story.KhanPending)
            {
                goalDist = story.KhanRoadDist; goal = story.KhanStop;
                key = story.KhanTalking ? "d.atAhmar" : onFoot ? "d.walkToAhmar" : Flat(goal, p) < 150f ? "d.stopAtAhmar" : "d.toAhmar";
                if (story.KhanTalking) show = false;
            }
            else if (!Found && pageDist >= 0f && pageMarker)
            {
                goalDist = pageDist; goal = pageMarker.position;
                key = onFoot ? "d.walkToPage" : Flat(goal, p) < 120f ? "d.stopAtPage" : "d.toKhan";
            }
            else if (seaDist > 0f && along < seaDist - 15f)
            {
                goalDist = seaDist; goal = road.At(seaDist); key = "d.toSea";
            }
            else
            {
                goalDist = arihaDist; goal = road.At(arihaDist); key = "d.toAriha";
                if (along > arihaDist - 150f) { key = "d.arrived"; show = false; }
            }
            if (story && story.FlockWaiting && !onFoot) { key = "d.waitFlock"; show = false; }
            else if (onFoot && key != "d.walkToPage" && key != "d.walkToAhmar" && key != "d.atAhmar") { key = "d.backToCar"; show = false; }
            else if (!onFoot && fromRoad > 22f) key = "d.offRoad";
            else if (!onFoot && car.autopilot && key != "d.arrived" && key != "d.atAhmar") { key = "d.jiddo"; show = false; }

            // How bright: dim while driving the right way, bright when stopped, turned back or off the road.
            float dir = Mathf.Sign(goalDist - along);
            Vector3 roadDir = road.At(along + dir * 4f) - road.At(along); roadDir.y = 0f;
            float kmh = onFoot ? 0f : car.SpeedKmh;
            bool rightWay = !onFoot && kmh > 15f && Vector3.Dot(car.transform.forward, roadDir.normalized) > 0.5f && fromRoad < 10f;
            goodTime = rightWay ? goodTime + Time.deltaTime : 0f;
            float want = !show ? 0f : goodTime > 5f ? 0.32f : rightWay ? 0.55f : 0.9f;
            glow = Mathf.MoveTowards(glow, want, Time.deltaTime * 0.8f);
            Place(p, idx, along, goalDist, goal, fromRoad, kmh, key == "d.offRoad");

            float metres = Mathf.Abs(goalDist - along);
            string sub;
            if (Time.timeSinceLevelLoad < 30f && !onFoot && !car.autopilot && key != "d.waitFlock") sub = Lang.T("d.keys");
            else if (!show || key == "d.backToCar") sub = null;
            else sub = metres >= 1000f ? Lang.Num(metres / 1000f, 1) + " " + Lang.T("u.km") : Lang.Num(metres) + " " + Lang.T("u.m");
            ui.SetGoal(Lang.T(key), sub);
        }

        /// <summary>Moves the arrow and the light under it: ahead on the road, over the goal when it is close, or
        /// over the road when the car has left it.</summary>
        void Place(Vector3 p, int idx, float along, float goalDist, Vector3 goal, float fromRoad, float kmh, bool offRoad)
        {
            bool on = glow > 0.01f;
            if (arrow.gameObject.activeSelf != on) { arrow.gameObject.SetActive(on); pool.gameObject.SetActive(on); }
            if (!on) { placed = false; return; }

            float dir = Mathf.Sign(goalDist - along), ahead = Mathf.Clamp(16f + kmh * 0.35f, 16f, 38f);
            Vector3 target, point; Quaternion rot; bool overGoal = false;
            if (offRoad)
            {
                // Over the nearest bit of road, pointing back to it from the car.
                Vector3 back = road.points[idx] - p; back.y = 0f;
                point = road.points[idx];
                rot = Quaternion.LookRotation(back.sqrMagnitude > 0.01f ? back.normalized : Vector3.forward) * Quaternion.Euler(-tilt, 0f, 0f);
            }
            else if (Mathf.Abs(goalDist - along) < ahead + 6f || Flat(goal, p) < ahead + 6f)
            {
                // Over the goal, pointing down to it, bobbing gently.
                overGoal = true; point = goal;
                rot = Quaternion.Euler(0f, Time.time * 40f, 0f) * Quaternion.Euler(90f, 0f, 0f);
            }
            else
            {
                float d = along + dir * ahead;
                point = road.At(d);
                Vector3 fwd = road.At(d + dir * 6f) - road.At(d - dir * 2f); fwd.y = 0f;
                rot = Quaternion.LookRotation(fwd.sqrMagnitude > 0.01f ? fwd.normalized : Vector3.forward) * Quaternion.Euler(-tilt, 0f, 0f);
            }
            point = Ground(point);
            float bob = Mathf.Sin(Time.time * 2.2f) * 0.15f;
            target = point + Vector3.up * ((overGoal ? height + 1.2f : height) + bob);
            if (!placed) { arrow.position = target; arrow.rotation = rot; placed = true; }
            arrow.position = Vector3.SmoothDamp(arrow.position, target, ref arrowVel, 0.25f);
            arrow.rotation = Quaternion.Slerp(arrow.rotation, rot, Time.deltaTime * 5f);
            arrow.localScale = Vector3.one * arrowSize;

            Vector3 under = arrow.position; under.y = point.y + 0.06f;
            pool.position = under;
            pool.rotation = Quaternion.Euler(0f, arrow.eulerAngles.y, 0f);
            pool.localScale = new Vector3(4.2f, 1f, 6.5f);

            float pulse = 0.85f + 0.15f * Mathf.Sin(Time.time * 3f);
            arrowMat.SetColor("_BaseColor", colour * (1.6f * glow * pulse));
            poolMat.SetColor("_BaseColor", colour * (0.55f * glow));
        }

        static float Flat(Vector3 a, Vector3 b) => Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));

        static Vector3 Ground(Vector3 p)
        {
            if (Physics.Raycast(new Vector3(p.x, p.y + 3f, p.z), Vector3.down, out var hit, 12f, ~0, QueryTriggerInteraction.Ignore)) p.y = hit.point.y;
            return p;
        }

        // ------------------------------------------------------------------ meshes

        /// <summary>A slim arrow pointing along +z, 1 m long, with a little thickness so it reads from the side.</summary>
        static Mesh ArrowMesh()
        {
            var outline = new[]
            {
                new Vector2(0f, 0.5f), new Vector2(0.36f, 0.08f), new Vector2(0.13f, 0.08f), new Vector2(0.13f, -0.5f),
                new Vector2(-0.13f, -0.5f), new Vector2(-0.13f, 0.08f), new Vector2(-0.36f, 0.08f),
            };
            var v = new System.Collections.Generic.List<Vector3>(); var t = new System.Collections.Generic.List<int>();
            float h = 0.03f;
            // Top and bottom faces: a fan from the tip works for this outline (the head, then the shaft as a quad).
            void Face(float y, bool up)
            {
                int b = v.Count;
                foreach (var o in outline) v.Add(new Vector3(o.x, y, o.y));
                int[] tris = { 0, 1, 6, 2, 3, 4, 2, 4, 5 };
                for (int i = 0; i < tris.Length; i += 3)
                    if (up) t.AddRange(new[] { b + tris[i], b + tris[i + 2], b + tris[i + 1] });
                    else t.AddRange(new[] { b + tris[i], b + tris[i + 1], b + tris[i + 2] });
            }
            Face(h, true); Face(-h, false);
            for (int i = 0; i < outline.Length; i++)
            {
                Vector2 a = outline[i], c = outline[(i + 1) % outline.Length]; int b = v.Count;
                v.Add(new Vector3(a.x, h, a.y)); v.Add(new Vector3(c.x, h, c.y)); v.Add(new Vector3(c.x, -h, c.y)); v.Add(new Vector3(a.x, -h, a.y));
                t.AddRange(new[] { b, b + 1, b + 2, b, b + 2, b + 3 });
            }
            var m = new Mesh { name = "Drive arrow" }; m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }

        /// <summary>A flat 1 x 1 square on the ground, for the light on the road.</summary>
        static Mesh Quad()
        {
            var m = new Mesh { name = "Road light" };
            m.vertices = new[] { new Vector3(-0.5f, 0f, -0.5f), new Vector3(-0.5f, 0f, 0.5f), new Vector3(0.5f, 0f, 0.5f), new Vector3(0.5f, 0f, -0.5f) };
            m.uv = new[] { new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f) };
            m.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            m.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
            m.RecalculateBounds();
            return m;
        }

        /// <summary>A soft round spot of light, bright in the middle and fading to nothing at the edge.</summary>
        static Texture2D SoftSpot()
        {
            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "Soft spot" };
            var px = new Color[n * n];
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
            {
                float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                float a = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy)); a = a * a * (3f - 2f * a);
                px[y * n + x] = new Color(a, a, a, a);
            }
            tex.SetPixels(px); tex.Apply();
            return tex;
        }
    }
}

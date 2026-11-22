using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Rihla.EditorTools
{
    /// <summary>
    /// Ariha's spring and Tell al-Sultan beside the end of the road down (C1c): a gravel lay-by to park in, a path,
    /// the spring of Ein al-Sultan in a stone basin with a channel running to the fields, the mound of Tell al-Sultan
    /// cut open by the diggers with its round stone tower, date palms heavy with fruit, Umm Khalil's fruit stall, the
    /// three clue boxes and Jiddo to walk beside the child. Laid out for the game near the place in places.json: the
    /// real spring and tell are on the north-west edge of the town, across the road from each other (H23).
    /// </summary>
    public static partial class RihlaSceneBuilder
    {
        static ArihaVisit ArihaSite(RoadPath road, out Vector3 siteCentre)
        {
            siteCentre = Vector3.zero;
            int pi = System.Array.FindIndex(road.places, p => p.id == "ariha");
            if (pi < 0) return null;
            var place = road.places[pi];
            float hw = road.width / 2f, dist = place.roadDist;
            Vector3 r = road.At(dist), dir = road.At(dist + 5f) - road.At(dist - 5f); dir.y = 0f; dir.Normalize();
            Vector3 F = Vector3.Cross(Vector3.up, dir); if (Vector3.Dot(F, place.position - r) < 0f) F = -F; // away from the road
            Vector3 X = dir;
            Vector3 O = r + F * hw; O.y = r.y;
            Vector3 At(float x, float f) { var q = O + X * x + F * f; q.y = Ground(q.x, q.z); return q; }

            var root = new GameObject("Ariha: the spring and Tell al-Sultan").transform;
            var gravel = TexMat("ariha_gravel", "rihla_shoulder", 3f, new Color(1.05f, 1.02f, 0.98f), 0.05f);
            var stone = TexMat("ariha_stone", "sandstone_blocks_08", 2.5f, new Color(1f, 0.95f, 0.86f), 0.12f);
            var earth = TexMat("tell_earth", "rihla_tell_earth", 5f, Color.white, 0.05f);
            var towerStone = TexMat("tell_tower", "rihla_tower_stone", 1.6f, Color.white, 0.1f);
            var water = Mat("spring_water", new Color(0.16f, 0.36f, 0.36f), 0.95f);
            var wood = Mat("stall_wood", new Color(0.45f, 0.3f, 0.18f), 0.25f);
            var cloth = Mat("stall_cloth", new Color(0.72f, 0.2f, 0.15f), 0.1f);
            var dateMat = Mat("dates_ripe", new Color(0.35f, 0.17f, 0.08f), 0.5f);
            var dateYellow = Mat("dates_yellow", new Color(0.9f, 0.68f, 0.18f), 0.4f);
            var orange = Mat("fruit_orange", new Color(0.95f, 0.5f, 0.08f), 0.45f);
            var banana = Mat("fruit_banana", new Color(0.95f, 0.85f, 0.3f), 0.35f);
            var rail = Mat("guard_rail", new Color(0.62f, 0.64f, 0.66f), 0.55f);
            var dark = Mat("khan_shadow", new Color(0.2f, 0.16f, 0.13f), 0.05f);

            var mb = new MB();
            // Quads that lie on the ground face up, whichever side of the road the site is on.
            void Up(Material m, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2? uv = null)
            {
                if (Vector3.Cross(b - a, c - a).y < 0f) mb.Quad(m, a, d, c, b, uv); else mb.Quad(m, a, b, c, d, uv);
            }
            // A strip of ground-hugging quads: the lay-by and the path.
            // One continuous sheet (shared corners, texture in metres), so no seams show.
            void Strip(float x0, float x1, float f0, float f1, Material m, float step = 2f)
            {
                int nx = Mathf.Max(1, Mathf.CeilToInt((x1 - x0) / step)), nf = Mathf.Max(1, Mathf.CeilToInt((f1 - f0) / step));
                int b0 = mb.v.Count;
                for (int j = 0; j <= nf; j++) for (int i = 0; i <= nx; i++)
                {
                    float x = Mathf.Lerp(x0, x1, i / (float)nx), f = Mathf.Lerp(f0, f1, j / (float)nf);
                    var q = At(x, f); if (f < 8f) q.y = Mathf.Max(q.y, r.y - 0.2f); q.y += 0.07f;
                    mb.v.Add(q); mb.uv.Add(new Vector2(x, f));
                }
                Vector3 c00 = mb.v[b0], c10 = mb.v[b0 + 1], c11 = mb.v[b0 + nx + 2];
                bool flip = Vector3.Cross(c10 - c00, c11 - c00).y < 0f;
                var t = mb.T(m);
                for (int j = 0; j < nf; j++) for (int i = 0; i < nx; i++)
                {
                    int a = b0 + j * (nx + 1) + i, bb = a + 1, c = a + nx + 2, d = a + nx + 1;
                    if (flip) t.AddRange(new[] { a, c, bb, a, d, c }); else t.AddRange(new[] { a, bb, c, a, c, d });
                }
            }
            Strip(-9f, 9f, 0.3f, 7.5f, gravel);           // the lay-by
            Strip(-1.4f, 1.4f, 7.5f, 70f, gravel, 2.8f);  // the path in
            Strip(-16f, -1.4f, 38f, 40.8f, gravel, 2.8f); // to the spring
            Strip(1.4f, 14f, 64f, 66.8f, gravel, 2.8f);   // towards the tell

            // The spring: a stone basin with the water just below its rim, and a channel running off to the fields.
            Vector3 S = At(-20f, 42f);
            var springSound = new GameObject("Spring water (sound)"); springSound.transform.SetParent(root, false);
            springSound.transform.position = S + Vector3.up * 0.8f; springSound.AddComponent<WaterSound>();
            Vector3 Lp(float x, float f) => S + X * x + F * f;
            foreach (var (c, size) in new[] { (Lp(0f, 3f), new Vector3(9f, 1.6f, 0.6f)), (Lp(0f, -3f), new Vector3(9f, 1.6f, 0.6f)) })
                mb.Box(stone, c + Vector3.up * 0.2f, X, new Vector3(size.z, size.y, size.x));
            mb.Box(stone, Lp(-4.5f, 0f) + Vector3.up * 0.2f, X, new Vector3(6.6f, 1.6f, 0.6f));
            mb.Box(stone, Lp(4.5f, 0f) + Vector3.up * 0.2f, X, new Vector3(6.6f, 1.6f, 0.6f));
            Up(water, Lp(-4.2f, -2.7f) + Vector3.up * 0.75f, Lp(4.2f, -2.7f) + Vector3.up * 0.75f, Lp(4.2f, 2.7f) + Vector3.up * 0.75f, Lp(-4.2f, 2.7f) + Vector3.up * 0.75f);
            for (float x = -4.5f; x > -40f; x -= 3f)
            {
                Vector3 a = Lp(x, 0f), b = Lp(x - 3f, 0f); a.y = Ground(a.x, a.z); b.y = Ground(b.x, b.z);
                Vector3 side = F * 0.55f;
                Up(water, a - side + Vector3.up * 0.12f, b - side + Vector3.up * 0.12f, b + side + Vector3.up * 0.12f, a + side + Vector3.up * 0.12f);
                mb.Box(stone, (a + b) / 2f + side * 1.3f + Vector3.up * 0.1f, X, new Vector3(0.3f, 0.5f, 3.05f));
                mb.Box(stone, (a + b) / 2f - side * 1.3f + Vector3.up * 0.1f, X, new Vector3(0.3f, 0.5f, 3.05f));
            }

            // Tell al-Sultan: a broad mound of earth, cut open on the side towards the path; in the cut, the tower.
            Vector3 T = At(14f, 92f); float baseY = T.y;
            const int arcs = 40; float gap = 24f * Mathf.Deg2Rad;
            var prof = new[] { (40f, -0.6f), (36f, 0.4f), (31f, 1.8f), (26f, 3.4f), (21f, 4.9f), (16f, 6.1f), (11f, 6.9f), (6f, 7.4f), (0f, 7.6f) };
            Vector3 toPath = -F;
            float a0 = Mathf.Atan2(toPath.z, toPath.x) + gap, a1 = a0 + 2f * Mathf.PI - 2f * gap;
            Vector3 MP(float ang, float rad, float h) => new Vector3(T.x + Mathf.Cos(ang) * rad, baseY + h + (h > 0f && rad > 1f ? (Mathf.PerlinNoise(ang * 3f, rad * 0.15f) - 0.5f) * 1.4f : 0f), T.z + Mathf.Sin(ang) * rad);
            for (int i = 0; i < arcs; i++)
            {
                float t0 = Mathf.Lerp(a0, a1, i / (float)arcs), t1 = Mathf.Lerp(a0, a1, (i + 1) / (float)arcs);
                for (int k = 0; k < prof.Length - 1; k++)
                    mb.Quad(earth, MP(t1, prof[k].Item1, prof[k].Item2), MP(t0, prof[k].Item1, prof[k].Item2), MP(t0, prof[k + 1].Item1, prof[k + 1].Item2), MP(t1, prof[k + 1].Item1, prof[k + 1].Item2), new Vector2(3f, 2f));
            }
            // The cut faces of the trench, both sides, and its back wall.
            foreach (var (ang, flip) in new[] { (a0, false), (a1, true) })
                for (int k = 0; k < prof.Length - 1; k++)
                {
                    Vector3 p0 = MP(ang, prof[k].Item1, -0.5f), p1 = MP(ang, prof[k + 1].Item1, -0.5f), p2 = MP(ang, prof[k + 1].Item1, prof[k + 1].Item2), p3 = MP(ang, prof[k].Item1, prof[k].Item2);
                    if (flip) mb.Quad(earth, p1, p0, p3, p2, new Vector2(2f, 2f)); else mb.Quad(earth, p0, p1, p2, p3, new Vector2(2f, 2f));
                }
            // The tower: round, of rough stones, about 8.5 m tall, its foot at the bottom of the cut, with a dark doorway.
            Vector3 TW = T + toPath * 8f; TW.y = baseY - 0.5f;
            const int tseg = 20; const float tr = 4.3f, th = 8.2f;
            for (int s = 0; s < tseg; s++)
            {
                float u0 = s * Mathf.PI * 2f / tseg, u1 = (s + 1) * Mathf.PI * 2f / tseg;
                Vector3 b0 = TW + new Vector3(Mathf.Cos(u0), 0f, Mathf.Sin(u0)) * tr, b1 = TW + new Vector3(Mathf.Cos(u1), 0f, Mathf.Sin(u1)) * tr;
                Vector3 c0 = TW + new Vector3(Mathf.Cos(u0), 0f, Mathf.Sin(u0)) * (tr - 0.25f) + Vector3.up * th, c1 = TW + new Vector3(Mathf.Cos(u1), 0f, Mathf.Sin(u1)) * (tr - 0.25f) + Vector3.up * th;
                mb.Quad(towerStone, b1, b0, c0, c1, new Vector2(2.7f / 2f, th / 2f));
                mb.Quad(towerStone, c1, c0, TW + Vector3.up * th, TW + Vector3.up * th);
            }
            mb.Box(dark, TW + toPath * (tr - 0.02f) + Vector3.up * 1.0f, toPath, new Vector3(0.9f, 1.8f, 0.08f));
            // A railing at the mouth of the cut, as at a dig open to visitors.
            Vector3 m0 = MP(a0, 15f, 0f), m1 = MP(a1, 15f, 0f); m0.y = Ground(m0.x, m0.z); m1.y = Ground(m1.x, m1.z);
            for (int k = 0; k <= 8; k++)
            {
                Vector3 q = Vector3.Lerp(m0, m1, k / 8f); q.y = Ground(q.x, q.z);
                mb.Box(rail, q + Vector3.up * 0.5f, X, new Vector3(0.06f, 1f, 0.06f));
                if (k < 8) { Vector3 q2 = Vector3.Lerp(m0, m1, (k + 1) / 8f); q2.y = Ground(q2.x, q2.z); mb.Box(rail, (q + q2) / 2f + Vector3.up * 1f, q2 - q, new Vector3(0.05f, 0.05f, Vector3.Distance(q, q2)), false); }
            }

            // Umm Khalil's stall beside the path (the owner, 20 November 2026: more realistic shops): see Stall().
            Vector3 K = At(5f, 22f);
            var rng = new System.Random(5);
            var sphere = Resources.GetBuiltinResource<Mesh>("New-Sphere.fbx");
            mb.Make("Ariha site", root, true, false, "ariha_site");
            Stall(root, K, -F, 0, new System.Random(19), out _);

            // Palms: around the spring, along the path, and a grove with hanging dates (the third clue).
            int seed = 1;
            void PalmAt(Vector3 q, float s, bool fruit)
            {
                var go = Palm(root, q, s, seed++);
                if (!fruit) return;
                var dm = new MB();
                for (int k = 0; k < 3; k++)
                {
                    float ang = k * 2.1f + seed;
                    Vector3 c = new Vector3(0.35f + Mathf.Cos(ang) * 0.55f, 8.3f, Mathf.Sin(ang) * 0.55f);
                    for (int j = 0; j < 22; j++)
                    {
                        Vector3 o = c + new Vector3(((float)rng.NextDouble() - 0.5f) * 0.35f, -(float)rng.NextDouble() * 0.7f, ((float)rng.NextDouble() - 0.5f) * 0.35f);
                        dm.Mesh(dateMat, sphere, Matrix4x4.TRS(o, Quaternion.identity, new Vector3(0.06f, 0.09f, 0.06f)));
                    }
                }
                var dg = dm.Make("Dates", go.transform, false, false, "ariha_dates_" + seed);
                dg.transform.localPosition = Vector3.zero; dg.transform.localRotation = Quaternion.identity; dg.transform.localScale = Vector3.one;
            }
            for (int k = 0; k < 9; k++) { float a = k * 0.7f; PalmAt(S + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * (7.5f + (k % 3) * 2.5f) - Vector3.up * 0.05f, 0.85f + (k % 4) * 0.08f, false); }
            for (float f = 12f; f < 68f; f += 9f) { PalmAt(At(-4f, f), 0.9f, false); PalmAt(At(4.5f, f + 4f), 0.95f, false); }
            Vector3 G = At(-34f, 24f);
            for (int k = 0; k < 12; k++) { Vector3 q = G + X * ((k % 4) - 1.5f) * 6.5f + F * ((k / 4) - 1f) * 6.5f; q.y = Ground(q.x, q.z); PalmAt(q, 0.82f + (k % 3) * 0.08f, true); }

            // The clue boxes.
            Vector3 spp = S + X * 6.4f; spp.y = Ground(spp.x, spp.z);
            var spring = RihlaWalkBuilder.Marker("spring", spp); spring.SetParent(root, true);
            var tower = RihlaWalkBuilder.Marker("tower", Vector3.zero); tower.SetParent(root, true);
            Vector3 tp = TW + toPath * (tr + 6.5f); tp.y = Ground(tp.x, tp.z); tower.position = tp;
            var palms = RihlaWalkBuilder.Marker("palms", Vector3.zero); palms.SetParent(root, true);
            Vector3 pp = G + F * -4.5f + X * 2f; pp.y = Ground(pp.x, pp.z); palms.position = pp;

            // Umm Khalil behind her stall, and Jiddo, who walks beside the child.
            var keeper = new GameObject("Umm Khalil"); keeper.SetActive(false); keeper.transform.SetParent(root, false);
            var kf = keeper.AddComponent<FigureBody>(); kf.look = FigureBody.Look.WomanElder; kf.seed = 3;
            Vector3 kp = K + F * 0.8f; kp.y = Ground(kp.x, kp.z);
            keeper.transform.SetPositionAndRotation(kp, Quaternion.LookRotation(-F)); keeper.SetActive(true);
            var jiddo = new GameObject("Jiddo (Ariha)"); jiddo.SetActive(false); jiddo.transform.SetParent(root, false);
            jiddo.AddComponent<FigureBody>().look = FigureBody.Look.Jiddo; jiddo.SetActive(true);

            var parking = new GameObject("Ariha parking").transform; parking.SetParent(root, false);
            parking.position = At(0f, 3.5f);
            var stall = new GameObject("Umm Khalil's stall (where to stand)").transform; stall.SetParent(root, false);
            Vector3 sp = K - F * 1.6f; sp.y = Ground(sp.x, sp.z); stall.position = sp;

            var visit = new GameObject("Ariha visit").AddComponent<ArihaVisit>();
            visit.parking = parking; visit.stall = stall; visit.keeper = keeper.transform; visit.jiddo = jiddo.transform;
            visit.clueSpots = new[] { spring, tower, palms };
            visit.parkDist = dist; visit.startDist = dist - 320f;
            siteCentre = O + F * 55f;
            foreach (var (n, q) in new[] { ("stall", K), ("spring", S), ("tell", T), ("tower", TW), ("grove", G) })
                Debug.Log($"[Rihla] Ariha site: {n} is {Flat2(road.points[road.Nearest(q)], q):0} m from the road");
            return visit;
        }
    }
}

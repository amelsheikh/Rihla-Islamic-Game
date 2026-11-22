using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Rihla.EditorTools
{
    /// <summary>
    /// Maqam al-Nabi Musa (C1c, second part), on its hill south of the road at the place in places.json: a small road
    /// up from the main road, a car park outside the gate, and the complex: a walled courtyard ringed by the
    /// pilgrims' rooms, each under its own small white dome, a mosque on the qibla side with a larger dome, a minaret,
    /// and the gate with an inscription panel over it. Outside the gate, two stalls (tea and sweets, dried fruit and
    /// nuts); inside, a few visitors, strings of little flags for the coming festival, and Hajj Mahmoud's three
    /// banners blown off the walls onto the hillside (the clues and the banners are all outside the walls: nothing to collect inside a sacred precinct, brief section 5). The plan is the game's own, built from descriptions and photographs, not a
    /// survey (NEEDS_REVIEW H24). The shrine room is not modelled inside; the mosque door is as far as the child goes.
    /// </summary>
    public static partial class RihlaSceneBuilder
    {
        // The frame of the complex, set by ShapeNabiMusa and used by NabiMusaSite.
        static Vector3 musaC, musaN, musaR; static float musaY;
        static List<Vector3> musaRoad;
        const float SideWidth = 5.6f;

        static Vector3 ML(float x, float z, float y = 0f) => musaC + musaR * x + musaN * z + Vector3.up * y;

        static float TerrainY(List<Terrain> terrains, float x, float z)
        {
            foreach (var t in terrains)
            {
                Vector3 p = t.transform.position, s = t.terrainData.size;
                if (x >= p.x && x <= p.x + s.x && z >= p.z && z <= p.z + s.z) return t.SampleHeight(new Vector3(x, 0f, z)) + p.y;
            }
            return 0f;
        }

        /// <summary>
        /// Levels a platform for the Maqam and its car park, and a bed for the small road up to it, in the terrain
        /// itself (before the colliders are read), so nothing pokes through and the road climbs evenly.
        /// </summary>
        static void ShapeNabiMusa(RoadPath road, List<Terrain> terrains, Vector3 place)
        {
            // The qibla from here is a little east of south (about 157 degrees); the gate faces away from it, towards
            // the road in the north.
            Vector3 qibla = new Vector3(Mathf.Sin(157f * Mathf.Deg2Rad), 0f, Mathf.Cos(157f * Mathf.Deg2Rad));
            musaN = -qibla; musaR = Vector3.Cross(Vector3.up, musaN);
            musaC = new Vector3(place.x, 0f, place.z);
            musaY = TerrainY(terrains, place.x, place.z); musaC.y = musaY;

            // The small road: from the main road where it passes nearest, curving up to the car park before the gate.
            int ji = road.Nearest(place);
            Vector3 J = road.points[ji], jd = road.points[Mathf.Min(ji + 1, road.points.Length - 1)] - road.points[Mathf.Max(ji - 1, 0)]; jd.y = 0f;
            Vector3 toSite = Vector3.Cross(Vector3.up, jd.normalized); if (Vector3.Dot(toSite, place - J) < 0f) toSite = -toSite;
            Vector3 P = ML(0f, 46f);
            Vector3 B0 = J, B1 = J + toSite * 120f, B2 = P + musaN * 110f, B3 = P;
            var pts = new List<Vector3>();
            float approx = Vector3.Distance(B0, B3) * 1.3f; int n = Mathf.CeilToInt(approx / 4f);
            for (int k = 0; k <= n; k++)
            {
                float t = k / (float)n, u = 1f - t;
                Vector3 q = u * u * u * B0 + 3f * u * u * t * B1 + 3f * u * t * t * B2 + t * t * t * B3;
                q.y = TerrainY(terrains, q.x, q.z);
                pts.Add(q);
            }
            pts[0] = new Vector3(pts[0].x, J.y, pts[0].z);
            // Smooth the heights so it climbs evenly; the start meets the main road, the end the platform.
            var ys = new float[pts.Count];
            for (int k = 0; k < pts.Count; k++)
            {
                float sum = 0f; int c = 0;
                for (int w = -8; w <= 8; w++) { int q = Mathf.Clamp(k + w, 0, pts.Count - 1); sum += pts[q].y; c++; }
                ys[k] = sum / c;
            }
            for (int k = 0; k < pts.Count; k++)
            {
                float t = k / (float)(pts.Count - 1);
                float y = ys[k];
                y = Mathf.Lerp(J.y, y, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.12f)));
                y = Mathf.Lerp(y, musaY, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.8f) / 0.2f)));
                pts[k] = new Vector3(pts[k].x, y, pts[k].z);
            }
            musaRoad = pts;

            // Platform (the complex and the car park) and road bed.
            float minX = float.MaxValue, maxX = float.MinValue, minZ = float.MaxValue, maxZ = float.MinValue;
            void Grow(Vector3 q, float m) { minX = Mathf.Min(minX, q.x - m); maxX = Mathf.Max(maxX, q.x + m); minZ = Mathf.Min(minZ, q.z - m); maxZ = Mathf.Max(maxZ, q.z + m); }
            foreach (var q in pts) Grow(q, 20f);
            foreach (var (x, z) in new[] { (-45f, -40f), (45f, -40f), (-45f, 62f), (45f, 62f) }) Grow(ML(x, z), 25f);

            foreach (var t in terrains)
            {
                var td = t.terrainData; Vector3 tp = t.transform.position, ts = td.size; int res = td.heightmapResolution;
                if (maxX < tp.x || minX > tp.x + ts.x || maxZ < tp.z || minZ > tp.z + ts.z) continue;
                var h = td.GetHeights(0, 0, res, res); bool changed = false;
                for (int r = 0; r < res; r++)
                {
                    float wz = tp.z + r / (float)(res - 1) * ts.z; if (wz < minZ || wz > maxZ) continue;
                    for (int c = 0; c < res; c++)
                    {
                        float wx = tp.x + c / (float)(res - 1) * ts.x; if (wx < minX || wx > maxX) continue;
                        Vector3 w = new Vector3(wx, 0f, wz) - musaC;
                        float lx = Vector3.Dot(w, musaR), lz = Vector3.Dot(w, musaN);
                        float ox = Mathf.Max(0f, Mathf.Abs(lx) - 40f), oz = Mathf.Max(0f, Mathf.Max(-34f - lz, lz - 56f));
                        float wp = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Sqrt(ox * ox + oz * oz) / 22f);
                        float best = float.MaxValue, ry = 0f;
                        for (int k = 0; k < pts.Count - 1; k++)
                        {
                            Vector3 a = pts[k], b = pts[k + 1];
                            Vector2 a2 = new Vector2(a.x, a.z), ab = new Vector2(b.x - a.x, b.z - a.z), p2 = new Vector2(wx, wz);
                            float tt = Mathf.Clamp01(Vector2.Dot(p2 - a2, ab) / Mathf.Max(ab.sqrMagnitude, 1e-4f));
                            float d = Vector2.Distance(a2 + ab * tt, p2);
                            if (d < best) { best = d; ry = Mathf.Lerp(a.y, b.y, tt); }
                        }
                        // Not under the main road's first stretch: there the main road decides.
                        float wr = best < SideWidth / 2f + 2.5f ? 1f : 1f - Mathf.SmoothStep(0f, 1f, (best - SideWidth / 2f - 2.5f) / 12f);
                        if (Vector2.Distance(new Vector2(wx, wz), new Vector2(J.x, J.z)) < road.width) wr = 0f;
                        if (wp <= 0f && wr <= 0f) continue;
                        float target = wr > wp ? ry - 0.08f : musaY - 0.02f, wgt = Mathf.Max(wp, wr);
                        float cur = h[r, c] * ts.y + tp.y;
                        h[r, c] = Mathf.Clamp01((Mathf.Lerp(cur, target, wgt) - tp.y) / ts.y); changed = true;
                    }
                }
                if (changed) td.SetHeights(0, 0, h);
            }
            Debug.Log($"[Rihla] Nabi Musa: small road {pts.Count * 4f:0} m long, from {J.y:0.0} to {musaY:0.0} m (game heights)");
        }

        // ------------------------------------------------------------------ mesh helpers

        /// <summary>A quad turned to face <paramref name="outward"/>.</summary>
        static void QuadOut(MB mb, Material m, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 outward, Vector2? uv = null)
        {
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), outward) < 0f) mb.Quad(m, a, d, c, b, uv); else mb.Quad(m, a, b, c, d, uv);
        }

        /// <summary>A dome over a circle of radius r at c, h high (a hemisphere when h = r).</summary>
        static void Dome(MB mb, Material m, Vector3 c, float r, float h, int seg = 18, int rings = 6)
        {
            Vector3 P(int i, int j)
            {
                float th = i * Mathf.PI * 2f / seg, ph = j * Mathf.PI / 2f / rings;
                return c + new Vector3(Mathf.Cos(th) * Mathf.Cos(ph) * r, Mathf.Sin(ph) * h, Mathf.Sin(th) * Mathf.Cos(ph) * r);
            }
            for (int i = 0; i < seg; i++)
                for (int j = 0; j < rings; j++)
                {
                    Vector3 a = P(i, j), b = P(i + 1, j), cc = P(i + 1, j + 1), d = P(i, j + 1);
                    QuadOut(mb, m, a, b, cc, d, (a + cc) / 2f - c, new Vector2(2f * Mathf.PI * r / seg / 2f, h / rings / 2f));
                }
        }

        /// <summary>An upright prism (a drum, a shaft) with <paramref name="sides"/> sides, from radius r0 at the
        /// bottom to r1 at the top, with a flat top.</summary>
        static void Prism(MB mb, Material m, Vector3 b, float r0, float r1, float h, int sides, float twist = 0f)
        {
            Vector3 Q(int i, float r, float y) { float a = twist + i * Mathf.PI * 2f / sides; return b + new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r); }
            float side = 2f * Mathf.PI * Mathf.Max(r0, r1) / sides;
            for (int i = 0; i < sides; i++)
            {
                Vector3 a = Q(i, r0, 0f), bb = Q(i + 1, r0, 0f), c = Q(i + 1, r1, h), d = Q(i, r1, h);
                QuadOut(mb, m, a, bb, c, d, (a + c) / 2f - (b + Vector3.up * h / 2f), new Vector2(side / 2f, h / 2f));
                QuadOut(mb, m, b + Vector3.up * h, d, c, b + Vector3.up * h, Vector3.up);
            }
        }

        /// <summary>A pointed arch outline (a doorway), flat, just in front of a wall: base centre, width, the height
        /// of its sides and of its point, facing <paramref name="normal"/>.</summary>
        static void ArchShape(MB mb, Material m, Vector3 baseC, Vector3 along, Vector3 normal, float w, float side, float top)
        {
            baseC += normal * 0.03f;
            Vector3 X = along.normalized * (w / 2f), Y = Vector3.up;
            QuadOut(mb, m, baseC - X, baseC + X, baseC + X + Y * side, baseC - X + Y * side, normal);
            // Two arcs meeting at the point.
            const int k = 6; Vector3 apex = baseC + Y * top;
            for (int s = -1; s <= 1; s += 2)
                for (int i = 0; i < k; i++)
                {
                    float t0 = i / (float)k, t1 = (i + 1) / (float)k;
                    Vector3 Arc(float t) { float a = t * Mathf.PI / 2f; return baseC + X * s * Mathf.Cos(a) + Y * (side + Mathf.Sin(a) * (top - side)); }
                    Vector3 p0 = Arc(t0), p1 = Arc(t1), c0 = new Vector3(baseC.x, p0.y, baseC.z) + X * 0f, c1 = new Vector3(baseC.x, p1.y, baseC.z);
                    c0 = baseC + Y * (p0.y - baseC.y); c1 = baseC + Y * (p1.y - baseC.y);
                    QuadOut(mb, m, c0, p0, p1, c1, normal);
                }
            _ = apex;
        }

        /// <summary>A pointed arch frame of stone around a doorway, standing out from the wall.</summary>
        static void ArchFrame(MB mb, Material m, Vector3 baseC, Vector3 along, Vector3 normal, float w, float side, float top, float thick = 0.32f)
        {
            Vector3 X = along.normalized * (w / 2f + thick / 2f);
            mb.Box(m, baseC - X + Vector3.up * side / 2f + normal * 0.12f, normal, new Vector3(thick, side, 0.3f));
            mb.Box(m, baseC + X + Vector3.up * side / 2f + normal * 0.12f, normal, new Vector3(thick, side, 0.3f));
            const int k = 6;
            for (int s = -1; s <= 1; s += 2)
                for (int i = 0; i < k; i++)
                {
                    float a0 = i / (float)k * Mathf.PI / 2f, a1 = (i + 1) / (float)k * Mathf.PI / 2f;
                    Vector3 p0 = baseC + X * s * Mathf.Cos(a0) + Vector3.up * (side + Mathf.Sin(a0) * (top - side)) + normal * 0.12f;
                    Vector3 p1 = baseC + X * s * Mathf.Cos(a1) + Vector3.up * (side + Mathf.Sin(a1) * (top - side)) + normal * 0.12f;
                    var seg = p1 - p0;
                    mb.Box(m, (p0 + p1) / 2f, seg, new Vector3(0.3f, thick, seg.magnitude + 0.06f), false);
                }
        }

        // ------------------------------------------------------------------ the site

        static NabiMusaVisit NabiMusaSite(RoadPath road)
        {
            if (musaRoad == null) return null;
            var root = new GameObject("Maqam al-Nabi Musa").transform;
            var stone = TexMat("musa_stone", "white_sandstone_blocks_02", 3f, new Color(1.08f, 1.04f, 0.96f), 0.08f);
            var plaster = TexMat("musa_plaster", "white_plaster_rough_01", 2.5f, new Color(1.45f, 1.42f, 1.36f), 0.12f);
            var paving = TexMat("musa_paving", "cobblestone_large_01", 3f, new Color(1.08f, 1.02f, 0.92f), 0.1f);
            var gravel = TexMat("musa_gravel", "rihla_shoulder", 3f, new Color(1.05f, 1.02f, 0.98f), 0.05f);
            var asphalt = TexMat("musa_road", "asphalt_02", 5f, new Color(0.92f, 0.92f, 0.92f), 0.15f);
            var dark = Mat("musa_doorway", new Color(0.09f, 0.075f, 0.065f), 0.05f);
            var door = TexMat("musa_door_wood", "brown_planks_05", 1.2f, new Color(0.55f, 0.36f, 0.22f), 0.2f);
            var panel = TexMat("musa_panel", "white_sandstone_blocks_02", 1.2f, new Color(0.82f, 0.76f, 0.64f), 0.1f);
            var carve = Mat("musa_carving", new Color(0.62f, 0.55f, 0.44f), 0.1f);
            var green = Mat("musa_green", new Color(0.12f, 0.36f, 0.2f), 0.3f);
            var brass = Mat("musa_brass", new Color(0.78f, 0.62f, 0.3f), 0.75f); brass.SetFloat("_Metallic", 0.8f);

            // The small road: a sheet of asphalt along the line levelled in ShapeNabiMusa.
            var mb = new MB();
            {
                var pts = musaRoad; int b0 = mb.v.Count; float run = 0f;
                for (int k = 0; k < pts.Count; k++)
                {
                    Vector3 dir = pts[Mathf.Min(k + 1, pts.Count - 1)] - pts[Mathf.Max(k - 1, 0)]; dir.y = 0f; dir.Normalize();
                    Vector3 across = Vector3.Cross(Vector3.up, dir) * SideWidth / 2f;
                    if (k > 0) run += Vector3.Distance(pts[k], pts[k - 1]);
                    float lift = k < 3 ? 0.02f : 0.05f;
                    mb.v.Add(pts[k] - across + Vector3.up * lift); mb.uv.Add(new Vector2(0f, run));
                    mb.v.Add(pts[k] + across + Vector3.up * lift); mb.uv.Add(new Vector2(SideWidth, run));
                }
                var t = mb.T(asphalt);
                for (int k = 0; k < pts.Count - 1; k++)
                {
                    int a = b0 + k * 2, b = a + 1, c = a + 3, d = a + 2;
                    if (Vector3.Cross(mb.v[b] - mb.v[a], mb.v[c] - mb.v[a]).y > 0f) t.AddRange(new[] { a, b, c, a, c, d });
                    else t.AddRange(new[] { a, c, b, a, d, c });
                }
                var rg = mb.Make("Road to Maqam al-Nabi Musa", root, false, true, "musa_road");
                rg.layer = 0;
            }
            var sideGo = new GameObject("Small road (path)");
            sideGo.transform.SetParent(root, false);
            var side = sideGo.AddComponent<RoadPath>();
            side.points = musaRoad.ToArray(); side.width = SideWidth; side.places = new RoadPath.Place[0]; side.ComputeDistances();

            // The ground of the car park and the courtyard.
            mb = new MB();
            void Sheet(Material m, float x0, float x1, float z0, float z1, float y, float step)
            {
                int nx = Mathf.Max(1, Mathf.CeilToInt((x1 - x0) / step)), nz = Mathf.Max(1, Mathf.CeilToInt((z1 - z0) / step));
                int b0 = mb.v.Count;
                for (int j = 0; j <= nz; j++) for (int i = 0; i <= nx; i++)
                {
                    float x = Mathf.Lerp(x0, x1, i / (float)nx), z = Mathf.Lerp(z0, z1, j / (float)nz);
                    var q = ML(x, z); q.y = Mathf.Max(Ground(q.x, q.z), musaY) + y; mb.v.Add(q); mb.uv.Add(new Vector2(x, z));
                }
                var t = mb.T(m); bool flip = Vector3.Cross(mb.v[b0 + 1] - mb.v[b0], mb.v[b0 + nx + 2] - mb.v[b0]).y < 0f;
                for (int j = 0; j < nz; j++) for (int i = 0; i < nx; i++)
                {
                    int a = b0 + j * (nx + 1) + i, bb = a + 1, c = a + nx + 2, d = a + nx + 1;
                    if (flip) t.AddRange(new[] { a, c, bb, a, d, c }); else t.AddRange(new[] { a, bb, c, a, c, d });
                }
            }
            Sheet(gravel, -16f, 16f, 29f, 49f, 0.06f, 4f);
            Sheet(paving, -24f, 24f, -19f, 20f, 0.05f, 4f);
            Sheet(paving, -2.6f, 2.6f, 20f, 29f, 0.05f, 2f);
            mb.Make("Maqam ground", root, false, false, "musa_ground");

            // The buildings: ranges of rooms around the courtyard (solid), the mosque, the gate.
            mb = new MB();
            float y0 = musaY;
            void Range(float x0, float x1, float z0, float z1, float h, Material m)
            {
                mb.Box(m, ML((x0 + x1) / 2f, (z0 + z1) / 2f, h / 2f - 1f), musaN, new Vector3(x1 - x0, h + 2f, z1 - z0));
                // A low parapet around the roof.
                float py = h + 0.3f;
                mb.Box(m, ML((x0 + x1) / 2f, z0 + 0.15f, py), musaN, new Vector3(x1 - x0, 0.6f, 0.3f));
                mb.Box(m, ML((x0 + x1) / 2f, z1 - 0.15f, py), musaN, new Vector3(x1 - x0, 0.6f, 0.3f));
                mb.Box(m, ML(x0 + 0.15f, (z0 + z1) / 2f, py), musaN, new Vector3(0.3f, 0.6f, z1 - z0));
                mb.Box(m, ML(x1 - 0.15f, (z0 + z1) / 2f, py), musaN, new Vector3(0.3f, 0.6f, z1 - z0));
            }
            const float H1 = 5f, H2 = 8.6f, HM = 8.2f;
            Range(-32f, -2.8f, 20f, 28f, H2, stone); Range(2.8f, 32f, 20f, 28f, H2, stone);
            mb.Box(stone, ML(0f, 24f, (H2 + 5.2f) / 2f), musaN, new Vector3(5.6f, H2 - 5.2f, 8f));
            Range(-32f, -24f, -20f, 20f, H1, stone); Range(24f, 32f, -20f, 20f, H1, stone);
            Range(-32f, -11f, -28f, -20f, H1, stone); Range(11f, 32f, -28f, -20f, H1, stone);
            Range(-11f, 11f, -28f, -19f, HM, stone);
            // A moulded band along the outer walls, and buttresses at the corners.
            foreach (var (x0, x1, z, hh) in new[] { (-32f, 32f, 28f, H2), (-32f, 32f, -28f, H1) })
                mb.Box(stone, ML(0f, z + Mathf.Sign(z) * 0.12f, hh - 1.1f), musaN, new Vector3(x1 - x0 + 0.5f, 0.28f, 0.24f));
            foreach (float x in new[] { -32f, 32f })
                mb.Box(stone, ML(x + Mathf.Sign(x) * 0.12f, 0f, H1 - 1.1f), musaN, new Vector3(0.24f, 0.28f, 56.5f));
            foreach (var (x, z) in new[] { (-32f, 28f), (32f, 28f), (-32f, -28f), (32f, -28f) })
                mb.Box(stone, ML(x, z, (z > 0f ? H2 : H1) / 2f), musaN, new Vector3(1.6f, z > 0f ? H2 + 0.4f : H1 + 0.4f, 1.6f));
            mb.Make("Maqam buildings", root, true, true, "musa_buildings");

            // Doorways of the rooms facing the courtyard, the arched gate, and the mosque's front.
            mb = new MB();
            for (float z = -17f; z <= 17.1f; z += 4f)
            {
                ArchShape(mb, dark, ML(-24f, z), musaN, musaR, 1.3f, 1.7f, 2.5f);
                ArchShape(mb, dark, ML(24f, z), musaN, -musaR, 1.3f, 1.7f, 2.5f);
            }
            for (float x = -22f; x <= 22.1f; x += 4f)
            {
                if (Mathf.Abs(x) < 4f) continue;
                ArchShape(mb, dark, ML(x, 20f), musaR, -musaN, 1.3f, 1.7f, 2.5f);
                ArchShape(mb, dark, ML(x, 20f, 4.6f), musaR, -musaN, 0.9f, 1.1f, 1.7f); // the upper rooms' windows
                if (Mathf.Abs(x) > 12f) ArchShape(mb, dark, ML(x, -20f), musaR, musaN, 1.3f, 1.7f, 2.5f);
            }
            // Small high windows in the outer walls.
            for (float x = -28f; x <= 28.1f; x += 6f)
            {
                if (Mathf.Abs(x) < 5f) continue;
                ArchShape(mb, dark, ML(x, 28f, 5.4f), musaR, musaN, 0.55f, 0.7f, 1.1f);
                ArchShape(mb, dark, ML(x, -28f, 2.4f), musaR, -musaN, 0.5f, 0.6f, 0.95f);
            }
            for (float z = -22f; z <= 22.1f; z += 6f)
            {
                ArchShape(mb, dark, ML(32f, z, 2.4f), musaN, musaR, 0.5f, 0.6f, 0.95f);
                ArchShape(mb, dark, ML(-32f, z, 2.4f), musaN, -musaR, 0.5f, 0.6f, 0.95f);
            }
            // The gate: deep stone frames on both faces, and the inscription panel over it on the outside.
            ArchFrame(mb, stone, ML(0f, 28f), musaR, musaN, 5.6f, 3.6f, 5.2f, 0.45f);
            ArchFrame(mb, stone, ML(0f, 20f), musaR, -musaN, 5.6f, 3.6f, 5.2f, 0.45f);
            Vector3 insc = ML(0f, 28.12f, 6.4f);
            mb.Box(panel, insc, musaN, new Vector3(4.2f, 1.25f, 0.18f));
            var rng = new System.Random(668);
            for (int line = 0; line < 3; line++)
                for (float x = -1.85f; x < 1.85f;)
                {
                    float w = 0.08f + (float)rng.NextDouble() * 0.32f;
                    float hgt = 0.05f + (float)rng.NextDouble() * 0.16f;
                    mb.Box(carve, insc + musaR * (x + w / 2f) + Vector3.up * (0.36f - line * 0.34f + hgt / 2f - 0.08f) + musaN * 0.1f, musaN, new Vector3(w, hgt, 0.05f));
                    x += w + 0.05f + (float)rng.NextDouble() * 0.12f;
                }
            // The mosque front: its door (shoes are left on the rack beside it), windows, and a stone frame.
            ArchFrame(mb, stone, ML(0f, -19f), musaR, musaN, 2.4f, 2.6f, 3.8f, 0.4f);
            ArchShape(mb, door, ML(0f, -19f), musaR, musaN, 2.2f, 2.5f, 3.6f);
            foreach (float x in new[] { -7f, -4f, 4f, 7f }) { ArchShape(mb, dark, ML(x, -19f, 1.6f), musaR, musaN, 0.9f, 1.2f, 1.9f); }
            mb.Box(door, ML(-2.4f, -18.5f, 0.35f), musaR, new Vector3(1.2f, 0.7f, 0.35f));
            var shoes = Mat("musa_shoes", new Color(0.18f, 0.13f, 0.1f), 0.3f);
            for (int k = 0; k < 4; k++) mb.Box(shoes, ML(-2.85f + k * 0.28f, -18.5f, 0.76f), musaN, new Vector3(0.11f, 0.08f, 0.26f));
            mb.Make("Maqam doors and windows", root, false, false, "musa_doors");

            // Roofs: a small dome over every room, a large dome over the mosque, and the minaret.
            mb = new MB();
            for (float z = -18f; z <= 18.1f; z += 4f)
            {
                foreach (float x in new[] { -28f, 28f }) { Prism(mb, plaster, ML(x, z, H1), 1.55f, 1.55f, 0.35f, 12); Dome(mb, plaster, ML(x, z, H1 + 0.35f), 1.55f, 1.25f, 14, 5); }
            }
            for (float x = -30f; x <= 30.1f; x += 4f)
            {
                if (Mathf.Abs(x) < 4f) continue;
                Prism(mb, plaster, ML(x, 24f, H2), 1.55f, 1.55f, 0.35f, 12); Dome(mb, plaster, ML(x, 24f, H2 + 0.35f), 1.55f, 1.25f, 14, 5);
                if (Mathf.Abs(x) > 12f) { Prism(mb, plaster, ML(x, -24f, H1), 1.55f, 1.55f, 0.35f, 12); Dome(mb, plaster, ML(x, -24f, H1 + 0.35f), 1.55f, 1.25f, 14, 5); }
            }
            // The mosque: an octagonal drum with small windows, and its dome; a smaller dome beside it.
            Prism(mb, stone, ML(0f, -23.5f, HM), 4.6f, 4.4f, 1.6f, 8, Mathf.PI / 8f);
            Dome(mb, plaster, ML(0f, -23.5f, HM + 1.6f), 4.4f, 4.1f, 24, 8);
            Prism(mb, brass, ML(0f, -23.5f, HM + 5.7f), 0.08f, 0.05f, 1.1f, 6);
            Dome(mb, brass, ML(0f, -23.5f, HM + 6.6f), 0.22f, 0.22f, 8, 3);
            Prism(mb, plaster, ML(-7f, -23.5f, HM), 2.3f, 2.3f, 0.5f, 12); Dome(mb, plaster, ML(-7f, -23.5f, HM + 0.5f), 2.3f, 2.0f, 16, 6);
            Prism(mb, plaster, ML(7f, -23.5f, HM), 2.3f, 2.3f, 0.5f, 12); Dome(mb, plaster, ML(7f, -23.5f, HM + 0.5f), 2.3f, 2.0f, 16, 6);
            // The minaret: a square base, an eight-sided shaft, a balcony, a slimmer top and a pointed cap.
            Vector3 mn = ML(-13.6f, -17.2f);
            mb.Box(stone, mn + Vector3.up * 4.4f, musaN, new Vector3(3.2f, 9.8f, 3.2f));
            Prism(mb, stone, mn + Vector3.up * 9.3f, 1.35f, 1.25f, 8.2f, 8, Mathf.PI / 8f);
            Prism(mb, stone, mn + Vector3.up * 17.5f, 2.0f, 2.0f, 0.3f, 8, Mathf.PI / 8f);
            Prism(mb, stone, mn + Vector3.up * 17.8f, 2.0f, 1.95f, 0.85f, 8, Mathf.PI / 8f);
            Prism(mb, stone, mn + Vector3.up * 17.8f, 1.0f, 0.95f, 3.3f, 8, Mathf.PI / 8f);
            Prism(mb, plaster, mn + Vector3.up * 21.1f, 1.15f, 0.04f, 2.6f, 8, Mathf.PI / 8f);
            Prism(mb, brass, mn + Vector3.up * 23.6f, 0.05f, 0.04f, 0.9f, 6);
            foreach (float a in new[] { 0f, 90f, 180f, 270f })
            {
                Vector3 o = Quaternion.Euler(0f, a, 0f) * musaN;
                ArchShape(mb, dark, mn + Vector3.up * 18.75f + o * 0.98f, Vector3.Cross(Vector3.up, o), o, 0.42f, 0.9f, 1.4f);
            }
            mb.Make("Maqam domes and minaret", root, true, false, "musa_domes");
            var minaretBase = new GameObject("Minaret (solid)"); minaretBase.transform.SetParent(root, false);
            minaretBase.transform.SetPositionAndRotation(mn + Vector3.up * 4.4f, Quaternion.LookRotation(musaN));
            minaretBase.AddComponent<BoxCollider>().size = new Vector3(3.2f, 9.8f, 3.2f);

            // Strings of little flags across the courtyard, ready for the festival; they flutter in the breeze.
            var flagMats = new[] { Cloth("musa_flag_green", new Color(0.1f, 0.42f, 0.22f)), Cloth("musa_flag_red", new Color(0.66f, 0.12f, 0.12f)),
                                   Cloth("musa_flag_white", new Color(0.93f, 0.91f, 0.86f)), Cloth("musa_flag_black", new Color(0.08f, 0.08f, 0.08f)) };
            var lineMat = Mat("musa_flag_string", new Color(0.3f, 0.28f, 0.25f), 0.2f);
            var strings = new MB();
            int flagN = 0;
            foreach (float x in new[] { -19f, -5f, 9f })
            {
                Vector3 a = ML(x, 20f, H2 - 0.2f), b = ML(x, -19f, Mathf.Abs(x) < 11f ? HM - 0.4f : H1 - 0.2f);
                Vector3 prev = a;
                for (int k = 1; k <= 26; k++)
                {
                    float t = k / 26f; Vector3 q = Vector3.Lerp(a, b, t) - Vector3.up * Mathf.Sin(t * Mathf.PI) * 1.6f;
                    var sg = q - prev; strings.Box(lineMat, (q + prev) / 2f, sg, new Vector3(0.02f, 0.02f, sg.magnitude), false);
                    if (k % 1 == 0 && k < 26)
                    {
                        var f = new GameObject("Flag"); f.transform.SetParent(root, false);
                        f.transform.SetPositionAndRotation(q - musaR * 0.15f, Quaternion.LookRotation(musaR));
                        var bn = f.AddComponent<Banner>(); bn.pennant = true; bn.width = 0.32f; bn.height = 0.42f; bn.columns = 4; bn.rows = 4; bn.flutter = 1.6f;
                        var mr = f.GetComponent<MeshRenderer>(); mr.sharedMaterial = flagMats[flagN++ % flagMats.Length]; mr.shadowCastingMode = ShadowCastingMode.Off;
                    }
                    prev = q;
                }
            }
            strings.Make("Flag strings", root, false, false, "musa_flag_strings");

            // Hajj Mahmoud's three banners, blown down about the courtyard; and where they will fly by the gate.
            var bannerMats = new[] { Cloth("musa_banner_green", new Color(0.08f, 0.38f, 0.2f)), Cloth("musa_banner_red", new Color(0.62f, 0.1f, 0.1f)), Cloth("musa_banner_white", new Color(0.94f, 0.92f, 0.86f)) };
            var poleMat = TexMat("musa_pole", "brown_planks_05", 1f, new Color(0.6f, 0.42f, 0.26f), 0.25f);
            var banners = new List<Transform>(); var stands = new List<Transform>();
            // Outside the walls: the brief keeps anything to collect out of sacred precincts (section 5).
            var lying = new[] { ML(-36.5f, 17f), ML(36.5f, -15f), ML(-13f, -32.5f) };
            for (int k = 0; k < 3; k++)
            {
                var g = new GameObject("Banner " + (k + 1)); g.transform.SetParent(root, false);
                Vector3 at = lying[k]; at.y = musaY + 0.06f; g.transform.position = at;
                var bm = new MB();
                Vector3 dir = Quaternion.Euler(0f, 35f + k * 70f, 0f) * musaN;
                bm.Box(poleMat, at + Vector3.up * 0.05f, dir, new Vector3(0.07f, 0.07f, 3f), false);
                bm.Box(bannerMats[k], at + Vector3.up * 0.09f + dir * 0.7f + Vector3.Cross(Vector3.up, dir) * 0.35f, dir, new Vector3(0.75f, 0.06f, 1.3f));
                bm.Box(brass, at + Vector3.up * 0.05f + dir * 1.55f, dir, new Vector3(0.12f, 0.12f, 0.12f), false);
                var go = bm.Make("Fallen banner", g.transform, true, false, "musa_banner_fallen_" + k);
                go.transform.position = Vector3.zero; go.transform.SetParent(g.transform, true);
                banners.Add(g.transform);

                var s = new GameObject("Banner flying " + (k + 1)); s.transform.SetParent(root, false);
                Vector3 sp = ML(-5.5f + k * 5.5f, 30.2f); if (k == 1) sp = ML(5.5f, 30.2f); if (k == 2) sp = ML(-8.5f, 30.2f);
                sp.y = musaY; s.transform.position = sp;
                var pm = new MB();
                pm.Box(poleMat, sp + Vector3.up * 2.2f, musaN, new Vector3(0.08f, 4.4f, 0.08f));
                Dome(pm, brass, sp + Vector3.up * 4.4f, 0.12f, 0.16f, 8, 3);
                pm.Box(stone, sp + Vector3.up * 0.2f, musaN, new Vector3(0.5f, 0.4f, 0.5f));
                var pg = pm.Make("Pole", s.transform, true, false, "musa_banner_pole_" + k);
                pg.transform.position = Vector3.zero; pg.transform.SetParent(s.transform, true);
                var cloth = new GameObject("Cloth"); cloth.transform.SetParent(s.transform, false);
                cloth.transform.SetPositionAndRotation(sp + Vector3.up * 4.25f + musaR * 0.04f, Quaternion.LookRotation(-musaN));
                var bn = cloth.AddComponent<Banner>(); bn.width = 1.3f; bn.height = 1.6f;
                cloth.GetComponent<MeshRenderer>().sharedMaterial = bannerMats[k];
                stands.Add(s.transform);
            }

            // Two stalls outside the gate: tea and sweets, and dried fruit and nuts.
            var stallRng = new System.Random(1269);
            Stall(root, ML(-11f, 37f), musaR, 1, stallRng, out var teaSeller);
            Stall(root, ML(11f, 37f), -musaR, 2, stallRng, out var nutSeller);
            Person(root, "Tea seller", FigureBody.Look.Man, 4, teaSeller, -musaR);
            Person(root, "Nut seller", FigureBody.Look.Woman, 6, nutSeller, musaR);

            // Visitors: a family in the courtyard, two men talking, a woman resting in the shade.
            Person(root, "Visitor (father)", FigureBody.Look.Man, 11, ML(-12f, 6f), musaR);
            Person(root, "Visitor (mother)", FigureBody.Look.Woman, 12, ML(-11f, 7.1f), musaR);
            Person(root, "Visitor (girl)", FigureBody.Look.Girl, 13, ML(-10.6f, 5.4f), -musaN);
            Person(root, "Visitor (boy)", FigureBody.Look.Boy, 14, ML(-9.8f, 6.6f), musaR);
            Person(root, "Visitor 5", FigureBody.Look.ElderMan, 15, ML(14f, -4f), -musaR);
            Person(root, "Visitor 6", FigureBody.Look.ManModern, 16, ML(12.9f, -4.5f), musaR);
            Person(root, "Visitor 7", FigureBody.Look.WomanElder, 17, ML(21.5f, 12f), -musaR);

            // Hajj Mahmoud by the mosque door, and Jiddo.
            var keeper = Person(root, "Hajj Mahmoud", FigureBody.Look.ElderMan, 2, ML(3f, -17.4f), musaN);
            var jiddo = new GameObject("Jiddo (Nabi Musa)"); jiddo.SetActive(false); jiddo.transform.SetParent(root, false);
            jiddo.AddComponent<FigureBody>().look = FigureBody.Look.Jiddo; jiddo.SetActive(true);

            // The clue boxes.
            var cInsc = RihlaWalkBuilder.Marker("inscription", Spot(ML(3.6f, 31.8f))); cInsc.SetParent(root, true);
            var cMin = RihlaWalkBuilder.Marker("minaret", Spot(ML(-37f, -11f))); cMin.SetParent(root, true);
            var cDom = RihlaWalkBuilder.Marker("domes", Spot(ML(37f, 7f))); cDom.SetParent(root, true);

            Transform Point(string n, Vector3 at, Vector3? fwd = null)
            {
                var t = new GameObject(n).transform; t.SetParent(root, false); t.position = Spot(at);
                if (fwd.HasValue) t.rotation = Quaternion.LookRotation(fwd.Value);
                return t;
            }
            var visit = new GameObject("Nabi Musa visit").AddComponent<NabiMusaVisit>();
            visit.side = side; visit.road = road;
            visit.parking = Point("Nabi Musa parking", ML(0f, 40f));
            visit.keeper = keeper; visit.keeperSpot = Point("Where to stand by Hajj Mahmoud", ML(3f, -14.8f));
            visit.jiddo = jiddo.transform;
            visit.siteCentre = Point("Maqam centre", ML(0f, 0f), musaN);
            visit.clueSpots = new[] { cInsc, cMin, cDom };
            visit.banners = banners.ToArray(); visit.bannerStands = stands.ToArray();
            visit.parkDist = side.distances[side.Nearest(visit.parking.position)];
            visit.startDist = 12f;
            Debug.Log($"[Rihla] Nabi Musa site built: small road {side.Length:0} m, parking at {visit.parkDist:0} m");
            return visit;
        }

        static Vector3 Spot(Vector3 q) { q.y = Ground(q.x, q.z); return q; }

        /// <summary>Cloth that shows from both sides (banners, flags, awnings).</summary>
        static Material Cloth(string name, Color c)
        {
            var m = TexMat(name, "cotton_jersey", 0.6f, c, 0.05f);
            m.SetFloat("_Cull", 0f); m.doubleSidedGI = true; m.SetFloat("_BumpScale", 0.6f);
            EditorUtility.SetDirty(m);
            return m;
        }

        static Transform Person(Transform root, string name, FigureBody.Look look, int seed, Vector3 at, Vector3 facing)
        {
            var g = new GameObject(name); g.SetActive(false); g.transform.SetParent(root, false);
            var f = g.AddComponent<FigureBody>(); f.look = look; f.seed = seed; f.cullDistance = 120f;
            g.transform.SetPositionAndRotation(Spot(at), Quaternion.LookRotation(facing));
            g.SetActive(true);
            return g.transform;
        }

        // ------------------------------------------------------------------ a market stall

        /// <summary>
        /// A stall (the owner: "the shops"): a plank table on trestles with a cloth skirt, crates and trays of goods,
        /// a striped awning on four poles with a scalloped edge, a hanging scale, a stool for the seller. Kind 0: fruit
        /// (dates, oranges, bananas, pomegranates); 1: tea and sweets (a brass urn, glasses, trays of sweets);
        /// 2: dried fruit and nuts in open sacks. <paramref name="front"/> points to where customers stand;
        /// <paramref name="sellerAt"/> is where the seller stands, behind it.
        /// </summary>
        static void Stall(Transform root, Vector3 at, Vector3 front, int kind, System.Random rng, out Vector3 sellerAt)
        {
            at = Spot(at); front.y = 0f; front.Normalize();
            Vector3 X = Vector3.Cross(Vector3.up, front); // along the table
            float R() => (float)rng.NextDouble();
            var mb = new MB();
            var wood = TexMat("stall_planks", "brown_planks_05", 1.2f, new Color(0.95f, 0.85f, 0.75f), 0.2f);
            var darkWood = TexMat("stall_planks_dark", "brown_planks_05", 1.2f, new Color(0.55f, 0.42f, 0.32f), 0.2f);
            var skirt = Cloth("stall_skirt_" + kind, kind == 1 ? new Color(0.55f, 0.12f, 0.12f) : kind == 2 ? new Color(0.18f, 0.3f, 0.45f) : new Color(0.2f, 0.4f, 0.25f));
            var awnA = Cloth("stall_awning_a" + kind, kind == 1 ? new Color(0.85f, 0.2f, 0.15f) : kind == 2 ? new Color(0.2f, 0.45f, 0.65f) : new Color(0.15f, 0.5f, 0.3f));
            var awnB = Cloth("stall_awning_b", new Color(0.95f, 0.92f, 0.84f));
            var metal = Mat("stall_metal", new Color(0.5f, 0.5f, 0.52f), 0.6f); metal.SetFloat("_Metallic", 0.7f);
            var sphere = Resources.GetBuiltinResource<Mesh>("New-Sphere.fbx");
            var capsule = Resources.GetBuiltinResource<Mesh>("New-Capsule.fbx");
            var cyl = Resources.GetBuiltinResource<Mesh>("New-Cylinder.fbx");
            const float L = 3f, D = 1.0f, TH = 0.86f;

            // The table: planks, trestles, a cloth skirt on the customers' side.
            mb.Box(wood, at + Vector3.up * TH, front, new Vector3(L, 0.06f, D));
            foreach (float x in new[] { -1.25f, 1.25f })
                foreach (float f in new[] { -0.38f, 0.38f })
                    mb.Box(darkWood, at + X * x + front * f + Vector3.up * TH / 2f, front, new Vector3(0.07f, TH, 0.07f));
            mb.Box(skirt, at + front * (D / 2f + 0.01f) + Vector3.up * (TH - 0.32f), front, new Vector3(L, 0.6f, 0.02f));
            // A shelf behind with more stock.
            mb.Box(darkWood, at - front * 1.3f + Vector3.up * 1.05f, front, new Vector3(L, 0.05f, 0.4f));
            foreach (float x in new[] { -1.4f, 1.4f }) mb.Box(darkWood, at - front * 1.3f + X * x + Vector3.up * 0.52f, front, new Vector3(0.06f, 1.05f, 0.4f));

            // The awning: poles and a striped canvas sloping down to the front, with a scalloped edge.
            foreach (float x in new[] { -1.6f, 1.6f })
            {
                mb.Box(darkWood, at + X * x + front * 0.75f + Vector3.up * 1.15f, front, new Vector3(0.07f, 2.3f, 0.07f));
                mb.Box(darkWood, at + X * x - front * 1.2f + Vector3.up * 1.35f, front, new Vector3(0.07f, 2.7f, 0.07f));
            }
            int strips = 8;
            for (int s = 0; s < strips; s++)
            {
                float x0 = -1.75f + s * 3.5f / strips, x1 = x0 + 3.5f / strips;
                Vector3 a = at + X * x0 + front * 1.0f + Vector3.up * 2.28f, b = at + X * x1 + front * 1.0f + Vector3.up * 2.28f;
                Vector3 c = at + X * x1 - front * 1.3f + Vector3.up * 2.72f, d = at + X * x0 - front * 1.3f + Vector3.up * 2.72f;
                // A little sag in the middle of the canvas.
                Vector3 sag = Vector3.down * 0.05f;
                var m = s % 2 == 0 ? awnA : awnB;
                QuadOut(mb, m, a, b, (b + c) / 2f + sag, (a + d) / 2f + sag, Vector3.up);
                QuadOut(mb, m, (a + d) / 2f + sag, (b + c) / 2f + sag, c, d, Vector3.up);
                // The scalloped valance.
                Vector3 mid = (a + b) / 2f + Vector3.down * 0.26f;
                QuadOut(mb, m, a, b, (b + mid) / 2f + Vector3.down * 0.06f, mid, front);
                QuadOut(mb, m, a, mid, mid, a + Vector3.down * 0.14f, front);
            }

            // Goods.
            var crateMat = TexMat("stall_crate", "brown_planks_05", 0.6f, new Color(0.9f, 0.75f, 0.55f), 0.2f);
            void Crate(Vector3 c, float w, float d)
            {
                mb.Box(crateMat, c + Vector3.up * 0.02f, front, new Vector3(w, 0.04f, d));
                mb.Box(crateMat, c + front * (d / 2f) + Vector3.up * 0.09f, front, new Vector3(w, 0.18f, 0.03f));
                mb.Box(crateMat, c - front * (d / 2f) + Vector3.up * 0.09f, front, new Vector3(w, 0.18f, 0.03f));
                mb.Box(crateMat, c + X * (w / 2f) + Vector3.up * 0.09f, front, new Vector3(0.03f, 0.18f, d));
                mb.Box(crateMat, c - X * (w / 2f) + Vector3.up * 0.09f, front, new Vector3(0.03f, 0.18f, d));
            }
            void Pile(Vector3 c, float w, float d, Material m, Mesh mesh, Vector3 size, int count, bool lie = false)
            {
                for (int k = 0; k < count; k++)
                {
                    float u = R(), v = R(), hgt = (1f - Mathf.Abs(u - 0.5f) * 2f) * (1f - Mathf.Abs(v - 0.5f) * 2f);
                    Vector3 q = c + X * (u - 0.5f) * w * 0.85f + front * (v - 0.5f) * d * 0.85f + Vector3.up * (0.06f + hgt * 0.12f + R() * 0.03f);
                    var rot = lie ? Quaternion.LookRotation(X) * Quaternion.Euler(90f, R() * 50f - 25f, R() * 360f) : Quaternion.Euler(R() * 30f, R() * 360f, R() * 30f);
                    mb.Mesh(m, mesh, Matrix4x4.TRS(q, rot, size * (0.88f + R() * 0.24f)));
                }
            }
            Vector3 top = at + Vector3.up * (TH + 0.03f);
            if (kind == 0)
            {
                var ripe = Mat("dates_ripe", new Color(0.35f, 0.17f, 0.08f), 0.5f);
                var yellow = Mat("dates_yellow", new Color(0.9f, 0.68f, 0.18f), 0.4f);
                var orange = Mat("fruit_orange", new Color(0.95f, 0.5f, 0.08f), 0.45f);
                var banana = Mat("fruit_banana", new Color(0.95f, 0.85f, 0.3f), 0.35f);
                var pom = Mat("fruit_pomegranate", new Color(0.62f, 0.1f, 0.12f), 0.45f);
                var goods = new (Material m, Mesh mesh, Vector3 s, int n, bool lie)[]
                { (ripe, sphere, new Vector3(0.045f, 0.07f, 0.045f), 70, false), (yellow, sphere, new Vector3(0.045f, 0.07f, 0.045f), 60, false),
                  (orange, sphere, Vector3.one * 0.085f, 22, false), (pom, sphere, Vector3.one * 0.095f, 16, false), (banana, capsule, new Vector3(0.04f, 0.1f, 0.04f), 22, true) };
                for (int k = 0; k < 5; k++)
                {
                    Vector3 c = top + X * (-1.2f + k * 0.6f) + front * 0.1f; Crate(c, 0.52f, 0.6f);
                    Pile(c, 0.52f, 0.6f, goods[k].m, goods[k].mesh, goods[k].s, goods[k].n, goods[k].lie);
                }
                for (int k = 0; k < 3; k++) { Vector3 c = at - front * 1.3f + Vector3.up * 1.08f + X * (-0.9f + k * 0.9f); Crate(c, 0.6f, 0.36f); Pile(c, 0.6f, 0.36f, k == 1 ? orange : ripe, sphere, k == 1 ? Vector3.one * 0.085f : new Vector3(0.045f, 0.07f, 0.045f), 30); }
            }
            else if (kind == 1)
            {
                // A brass tea urn, glasses on a tray, and round trays of sweets.
                var glass = Mat("stall_tea_glass", new Color(0.55f, 0.25f, 0.08f), 0.9f);
                var trayMat = Mat("stall_tray", new Color(0.82f, 0.66f, 0.32f), 0.8f); trayMat.SetFloat("_Metallic", 0.8f);
                var urn = Mat("stall_urn", new Color(0.78f, 0.55f, 0.25f), 0.8f); urn.SetFloat("_Metallic", 0.9f);
                Vector3 u0 = top + X * 1.05f - front * 0.15f;
                mb.Mesh(urn, cyl, Matrix4x4.TRS(u0 + Vector3.up * 0.22f, Quaternion.identity, new Vector3(0.32f, 0.22f, 0.32f)));
                mb.Mesh(urn, sphere, Matrix4x4.TRS(u0 + Vector3.up * 0.46f, Quaternion.identity, new Vector3(0.3f, 0.18f, 0.3f)));
                mb.Mesh(urn, cyl, Matrix4x4.TRS(u0 + front * 0.18f + Vector3.up * 0.1f, Quaternion.Euler(90f, 0f, 0f) * Quaternion.LookRotation(front), new Vector3(0.03f, 0.06f, 0.03f)));
                Vector3 tray = top + X * 0.45f + front * 0.15f;
                mb.Mesh(trayMat, cyl, Matrix4x4.TRS(tray + Vector3.up * 0.01f, Quaternion.identity, new Vector3(0.42f, 0.008f, 0.42f)));
                for (int k = 0; k < 7; k++) { float a = k * 0.9f; mb.Mesh(glass, cyl, Matrix4x4.TRS(tray + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 0.13f + Vector3.up * 0.05f, Quaternion.identity, new Vector3(0.05f, 0.045f, 0.05f))); }
                var sweets = new[] { Mat("sweet_knafeh", new Color(0.86f, 0.45f, 0.12f), 0.5f), Mat("sweet_pistachio", new Color(0.45f, 0.62f, 0.25f), 0.4f), Mat("sweet_ghraybeh", new Color(0.93f, 0.86f, 0.7f), 0.3f), Mat("sweet_ma3moul", new Color(0.8f, 0.62f, 0.38f), 0.3f) };
                for (int k = 0; k < 4; k++)
                {
                    Vector3 c = top + X * (-1.15f + k * 0.42f) + front * (k % 2 == 0 ? 0.18f : -0.18f);
                    mb.Mesh(trayMat, cyl, Matrix4x4.TRS(c + Vector3.up * 0.01f, Quaternion.identity, new Vector3(0.38f, 0.008f, 0.38f)));
                    if (k == 0) mb.Mesh(sweets[0], cyl, Matrix4x4.TRS(c + Vector3.up * 0.03f, Quaternion.identity, new Vector3(0.34f, 0.02f, 0.34f)));
                    else for (int j = 0; j < 14; j++) { float a = j * 2.4f, rr = 0.04f + (j % 5) * 0.03f; mb.Mesh(sweets[k], k == 2 ? sphere : cyl, Matrix4x4.TRS(c + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * rr + Vector3.up * 0.03f, Quaternion.identity, new Vector3(0.05f, 0.02f, 0.05f))); }
                }
            }
            else
            {
                // Open sacks with rolled tops, full of almonds, pistachios, raisins and dried apricots.
                var sack = TexMat("stall_sack", "cotton_jersey", 0.3f, new Color(0.78f, 0.68f, 0.5f), 0.05f);
                var nuts = new[] { Mat("nut_almond", new Color(0.62f, 0.42f, 0.25f), 0.3f), Mat("nut_pistachio", new Color(0.62f, 0.66f, 0.38f), 0.3f), Mat("dried_raisin", new Color(0.2f, 0.1f, 0.08f), 0.4f), Mat("dried_apricot", new Color(0.92f, 0.55f, 0.18f), 0.4f) };
                for (int k = 0; k < 4; k++)
                {
                    Vector3 c = top + X * (-1.1f + k * 0.73f);
                    mb.Mesh(sack, cyl, Matrix4x4.TRS(c + Vector3.up * 0.14f, Quaternion.identity, new Vector3(0.5f, 0.14f, 0.5f)));
                    mb.Mesh(sack, cyl, Matrix4x4.TRS(c + Vector3.up * 0.29f, Quaternion.identity, new Vector3(0.56f, 0.03f, 0.56f)));
                    for (int j = 0; j < 40; j++) { float a = R() * 6.28f, rr = Mathf.Sqrt(R()) * 0.22f; mb.Mesh(nuts[k], sphere, Matrix4x4.TRS(c + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * rr + Vector3.up * (0.3f + (0.22f - rr) * 0.3f), Quaternion.Euler(R() * 360f, R() * 360f, 0f), new Vector3(0.035f, 0.025f, 0.05f))); }
                }
            }
            // A hanging scale from the awning, and the seller's stool.
            Vector3 hook = at + X * -1.0f + front * 0.3f + Vector3.up * 2.2f;
            mb.Box(metal, hook + Vector3.down * 0.25f, front, new Vector3(0.02f, 0.5f, 0.02f));
            mb.Box(metal, hook + Vector3.down * 0.52f, front, new Vector3(0.06f, 0.12f, 0.04f));
            mb.Mesh(metal, cyl, Matrix4x4.TRS(hook + Vector3.down * 0.75f, Quaternion.identity, new Vector3(0.28f, 0.01f, 0.28f)));
            Vector3 stool = at - front * 0.8f + X * 1.0f;
            mb.Mesh(darkWood, cyl, Matrix4x4.TRS(stool + Vector3.up * 0.42f, Quaternion.identity, new Vector3(0.34f, 0.02f, 0.34f)));
            foreach (int k in new[] { 0, 1, 2 }) { float a = k * 2.09f; mb.Box(darkWood, stool + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 0.12f + Vector3.up * 0.21f, front, new Vector3(0.03f, 0.42f, 0.03f)); }

            var go = mb.Make("Stall (" + (kind == 0 ? "fruit" : kind == 1 ? "tea and sweets" : "dried fruit and nuts") + ")", root, true, false, "stall_" + kind + "_" + Mathf.RoundToInt(at.x) + "_" + Mathf.RoundToInt(at.z));
            go.layer = DetailCulling.Small;
            var block = new GameObject("Stall table (solid)"); block.transform.SetParent(root, false);
            block.transform.SetPositionAndRotation(at + Vector3.up * 0.6f, Quaternion.LookRotation(front));
            block.AddComponent<BoxCollider>().size = new Vector3(3.2f, 1.2f, 1.2f);
            sellerAt = at - front * 0.8f - X * 0.3f;
        }
    }
}

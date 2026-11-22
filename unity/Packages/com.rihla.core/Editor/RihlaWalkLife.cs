using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Rihla.EditorTools
{
    /// <summary>
    /// R4, "as if visiting": the small things that make the lanes of the Old City feel lived in, built cheaply
    /// enough for integrated graphics. Awnings over shops, potted plants by doors, open wooden shutters, arches
    /// across narrow lanes, and leafy trees in both eras; street lamps, overhead cables and black water tanks on
    /// the roofs only in today's view. Which buildings get what is decided by a hash of their position, so every
    /// build is the same. All of it is set dressing, not a record of any particular house (NEEDS_REVIEW.md H17).
    /// </summary>
    public static partial class RihlaWalkBuilder
    {
        struct Opening { public Vector2 a, d, outN; public float u, w, top, base0; public bool shop; }
        struct WindowAt { public Vector2 a, d, outN; public float u, y, base0; }
        static readonly List<Opening> openings = new List<Opening>();
        static readonly List<WindowAt> windowsAt = new List<WindowAt>();
        static readonly List<(Vector2 a, Vector2 d, Vector2 outN, float len, float base0, float h)> streetWalls = new List<(Vector2, Vector2, Vector2, float, float, float)>();

        /// <summary>Builds the lane details; returns the parent of what exists only today (for the Time Lens).</summary>
        static GameObject LaneLife(List<Street> streets)
        {
            var today = new GameObject("Today only (lamps, cables, water tanks)");

            // Every mesh of this pass is created first, then filled (Mesh() writes any mesh already filled).
            var awnings = new[]
            {
                Mesh("Awnings (cream)", Mat("awning1", new Color(0.86f, 0.8f, 0.66f), 0.05f), false),
                Mesh("Awnings (red)", Mat("awning2", new Color(0.6f, 0.25f, 0.19f), 0.05f), false),
                Mesh("Awnings (green)", Mat("awning3", new Color(0.28f, 0.4f, 0.31f), 0.05f), false),
            };
            var pots = Mesh("Plant pots", Mat("pot", new Color(0.64f, 0.38f, 0.25f), 0.1f), false);
            var shutters = Mesh("Window shutters", Mat("woodshutter", new Color(0.2f, 0.34f, 0.31f), 0.2f), false);
            var arches = Mesh("Arches across lanes", Mat("gate", Color.white), false);
            var iron = Mesh("Lamp brackets", Mat("iron", new Color(0.07f, 0.07f, 0.07f), 0.35f, 0.4f), false, today.transform);
            var glass = Mesh("Lamp glass", Mat("lampglass", new Color(1f, 0.9f, 0.7f), 0.6f, 0f, new Color(1f, 0.72f, 0.4f) * 0.9f), false, today.transform);
            var cables = Mesh("Overhead cables", Mat("iron", new Color(0.07f, 0.07f, 0.07f), 0.35f, 0.4f), false, today.transform);
            var tanks = Mesh("Roof water tanks", Mat("tank", new Color(0.06f, 0.06f, 0.065f), 0.45f), false, today.transform);
            var plantLeaves = Mesh("Potted plants", Foliage("plantleaf", new Color(0.55f, 0.75f, 0.45f)), false);

            int nAwn = 0, nPot = 0, nShut = 0, nLamp = 0, nArch = 0, nCable = 0, nTank = 0;
            foreach (var o in openings)
            {
                Vector2 mid = o.a + o.d * o.u;
                float h = Hash(mid * 2.31f);
                if (o.shop && h < 0.6f)
                {
                    // A cloth awning sloping out over the shop, with a short flap at its front edge.
                    float half = o.w / 2f + 0.35f, y0 = o.base0 + o.top + 0.5f, y1 = o.base0 + o.top - 0.05f, out1 = 1.25f;
                    Vector3 W(float u, float outD, float y) { Vector2 g = o.a + o.d * u + o.outN * outD; return new Vector3(g.x, y, g.y); }
                    var mb = awnings[Mathf.FloorToInt(Hash(mid * 5.7f) * 2.999f)];
                    Quad(mb, W(o.u - half, 0.02f, y0), W(o.u + half, 0.02f, y0), W(o.u + half, out1, y1), W(o.u - half, out1, y1));
                    Quad(mb, W(o.u - half, out1, y1), W(o.u + half, out1, y1), W(o.u + half, out1, y1 - 0.28f), W(o.u - half, out1, y1 - 0.28f));
                    nAwn++;
                }
                else if (!o.shop && h < 0.35f)
                {
                    // A clay pot with a plant beside the door.
                    float side = Hash(mid * 3.3f) < 0.5f ? -1f : 1f;
                    Vector2 p = o.a + o.d * (o.u + side * (o.w / 2f + 0.5f)) + o.outN * 0.35f;
                    Cylinder(pots, new Vector3(p.x, o.base0, p.y), 0.21f, 0.4f, 10);
                    LeafCrown(plantLeaves, new Vector3(p.x, o.base0 + 0.68f, p.y), new Vector3(0.32f, 0.3f, 0.32f), 14, 0.42f, Hash(p));
                    nPot++;
                }
            }

            foreach (var w in windowsAt)
            {
                Vector2 mid = w.a + w.d * w.u;
                if (Hash(mid * 1.93f + Vector2.one * w.y) > 0.45f) continue;
                // Wooden shutters folded back against the wall either side of the window.
                Panel(shutters, w.a, w.d, w.outN, w.u - 0.6f, 0.42f, w.y - 0.05f, w.y + 1.2f, w.base0, 0.07f, false);
                Panel(shutters, w.a, w.d, w.outN, w.u + 0.6f, 0.42f, w.y - 0.05f, w.y + 1.2f, w.base0, 0.07f, false);
                nShut++;
            }

            // Street lamps on brackets, today only (when the first electric lamps came is not checked).
            foreach (var (a, d, outN, len, base0, h) in streetWalls)
                for (float u = 4f; u < len - 2f; u += 19f)
                {
                    Vector2 at = a + d * u;
                    if (Hash(at * 0.77f) > 0.55f) continue;
                    float y = base0 + 3.6f;
                    Box(iron, at, at + outN * 0.62f, 0.05f, y, y + 0.05f);
                    Vector2 c = at + outN * 0.58f;
                    Box(iron, c - d * 0.14f, c + d * 0.14f, 0.28f, y - 0.06f, y + 0.0f);
                    Box(glass, c - d * 0.11f, c + d * 0.11f, 0.22f, y - 0.42f, y - 0.06f);
                    Box(iron, c - d * 0.13f, c + d * 0.13f, 0.26f, y - 0.46f, y - 0.42f);
                    nLamp++;
                }

            // Along each lane: an arch springing from wall to wall where both sides are built, and otherwise,
            // today, a cable or two across.
            foreach (var st in streets)
            {
                if (st.passage || st.width > 8f) continue;
                float carry = 9f;
                for (int i = 0; i < st.line.Count - 1; i++)
                {
                    Vector2 a = st.line[i], b = st.line[i + 1]; float len = (b - a).magnitude; if (len < 0.5f) continue;
                    Vector2 d = (b - a) / len, n = new Vector2(-d.y, d.x);
                    for (float t = carry; t < len; t += 16f)
                    {
                        Vector2 m = a + d * t;
                        float dl = WallDistance(m, -n, st.width), dr = WallDistance(m, n, st.width);
                        carry = 0f;
                        if (dl < 0f || dr < 0f) continue;
                        float hsh = Hash(m * 0.53f);
                        if (hsh < 0.22f)
                        {
                            Vector2 c = m + n * (dr - dl) / 2f; float half = (dl + dr) / 2f + 0.5f;
                            Extrude(arches, ArchProfile(half, 3.9f), c, n, d, -0.55f, 0.55f, 0f);
                            nArch++;
                        }
                        else if (hsh < 0.5f)
                        {
                            float y = 5.3f + Hash(m * 1.7f) * 0.8f, sag = 0.25f + 0.04f * (dl + dr);
                            Vector2 l = m - n * dl, r = m + n * dr, mid = (l + r) / 2f + d * 0.3f;
                            Box(cables, l, mid, 0.025f, y - sag, y - sag + 0.025f);
                            Box(cables, mid, r, 0.025f, y - sag, y - sag + 0.025f);
                            Box(cables, l, mid + d * 0.6f, 0.02f, y + 0.4f - sag * 0.8f, y + 0.42f - sag * 0.8f);
                            Box(cables, mid + d * 0.6f, r, 0.02f, y + 0.4f - sag * 0.8f, y + 0.42f - sag * 0.8f);
                            nCable++;
                        }
                    }
                    carry = Mathf.Max(0f, carry);
                }
            }

            // Black water tanks on many roofs, today only.
            foreach (var (ring, base0, h, id) in houses)
            {
                Vector2 c = new Vector2(ring.Average(p => p.x), ring.Average(p => p.y));
                if (Hash(c * 2.3f) > 0.5f) continue;
                var corner = ring[(int)(id % ring.Count)];
                Vector2 p = corner + (c - corner).normalized * 1.3f;
                if (!Inside(ring, p)) continue;
                float roof = base0 + h;
                Box(tanks, p - new Vector2(0.5f, 0f), p + new Vector2(0.5f, 0f), 0.9f, roof, roof + 0.25f);
                Cylinder(tanks, new Vector3(p.x, roof + 0.25f, p.y), 0.52f, 1.05f, 12);
                if (Hash(c * 4.1f) < 0.35f) { Vector2 q = p + (c - p).normalized * 1.25f; if (Inside(ring, q)) Cylinder(tanks, new Vector3(q.x, roof + 0.25f, q.y), 0.48f, 0.95f, 12); }
                nTank++;
            }

            Debug.Log($"[Rihla] lane life: {nAwn} awnings, {nPot} plants, {nShut} pairs of shutters, {nArch} arches across lanes; today only: {nLamp} lamps, {nCable} cables, {nTank} roofs with water tanks");
            return today;
        }

        /// <summary>Small details flat on a wall or high on a roof: their shadows cannot be seen, so they cast none
        /// (saves shadow-map work on integrated graphics).</summary>
        static readonly HashSet<string> NoShadow = new HashSet<string>
        {
            "Doors", "Shop shutters", "Windows", "Stone frames", "Window shutters", "Plant pots", "Potted plants",
            "Lamp brackets", "Lamp glass", "Overhead cables", "Roof water tanks", "Roof parapets",
            "Window frames (Dome of the Rock)", "Windows (Dome of the Rock)",
        };

        /// <summary>How far from m, along dir, the first house wall is (stepping 0.25 m), or -1 if none close.</summary>
        static float WallDistance(Vector2 m, Vector2 dir, float width)
        {
            for (float s = 0.5f; s < width / 2f + 3f; s += 0.25f)
            {
                Vector2 p = m + dir * s;
                foreach (var (ring, base0, h, id) in houses)
                    if (base0 == 0f && h > 6.5f && Inside(ring, p)) return s;
            }
            return -1f;
        }

        /// <summary>The solid part of an arch across a passage: a pointed arch springing at "spring", solid above.</summary>
        static List<Vector2> ArchProfile(float half, float spring)
        {
            float r = half * 1.3f, off = half * 0.3f;
            float apex = spring + Mathf.Sqrt(r * r - off * off), top = apex + 0.7f;
            var prof = new List<Vector2> { new Vector2(-half, spring) };
            for (int k = 1; k <= 8; k++) { float x = -half + half * k / 8f; prof.Add(new Vector2(x, spring + Mathf.Sqrt(Mathf.Max(0f, r * r - (x - off) * (x - off))))); }
            for (int k = 7; k >= 0; k--) { float x = half - half * k / 8f; prof.Add(new Vector2(x, spring + Mathf.Sqrt(Mathf.Max(0f, r * r - (x + off) * (x + off))))); }
            prof.Add(new Vector2(half, top)); prof.Add(new Vector2(-half, top));
            return prof;
        }

        /// <summary>A quad seen from both sides, each side with its own vertices; uv in metres.</summary>
        static void Quad(MB mb, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            float w = (p1 - p0).magnitude, h = (p3 - p0).magnitude;
            for (int side = 0; side < 2; side++)
            {
                int s = mb.v.Count;
                mb.v.AddRange(new[] { p0, p1, p2, p3 });
                mb.uv.AddRange(new[] { new Vector2(0, 0), new Vector2(w, 0), new Vector2(w, h), new Vector2(0, h) });
                if (side == 0) mb.t.AddRange(new[] { s, s + 2, s + 1, s, s + 3, s + 2 });
                else mb.t.AddRange(new[] { s, s + 1, s + 2, s, s + 2, s + 3 });
            }
        }

        /// <summary>A closed box along a to b (a Segment with its underside), for things seen from below.</summary>
        static void Box(MB mb, Vector2 a, Vector2 b, float thick, float y0, float y1)
        {
            Vector2 d = b - a; if (d.sqrMagnitude < 1e-4f) return; d.Normalize();
            Vector2 n = new Vector2(-d.y, d.x) * thick * 0.5f;
            var ring = Ccw(new List<Vector2> { a - n, b - n, b + n, a + n });
            Prism(mb, ring, y0, y1);
            int s = mb.v.Count;
            foreach (var p in ring) { mb.v.Add(new Vector3(p.x, y0, p.y)); mb.uv.Add(p); }
            mb.t.AddRange(new[] { s, s + 1, s + 2, s, s + 2, s + 3 });
        }

        /// <summary>
        /// A crown of leaf cards: small double-sided squares of the leaf texture scattered through an ellipsoid,
        /// each lit as if its normal pointed out from the centre, so the crown shades like a soft round mass.
        /// </summary>
        static void LeafCrown(MB mb, Vector3 c, Vector3 radii, int cards, float size, float seed)
        {
            var rnd = new System.Random(Mathf.FloorToInt(seed * 1e6f) + cards);
            float R() => (float)rnd.NextDouble();
            for (int k = 0; k < cards; k++)
            {
                Vector3 dir = new Vector3(R() * 2f - 1f, R() * 2f - 1f, R() * 2f - 1f);
                if (dir.sqrMagnitude > 1f || dir.sqrMagnitude < 1e-3f) { k--; continue; }
                Vector3 p = c + Vector3.Scale(dir.normalized * Mathf.Pow(dir.magnitude, 0.4f) * 0.88f, radii);
                Vector3 facing = new Vector3(R() * 2f - 1f, R() * 2f - 1f, R() * 2f - 1f).normalized;
                Vector3 t1 = Vector3.Cross(facing, Vector3.up); if (t1.sqrMagnitude < 1e-3f) t1 = Vector3.right; t1.Normalize();
                Vector3 t2 = Vector3.Cross(facing, t1).normalized;
                float s = size * (0.75f + R() * 0.5f) * 0.5f;
                Vector3 nrm = (Vector3.Scale(p - c, new Vector3(1f / radii.x, 1f / radii.y, 1f / radii.z)).normalized + Vector3.up * 0.35f).normalized;
                // A quarter of the texture per card, so neighbouring cards differ.
                Vector2 uv0 = new Vector2(R() < 0.5f ? 0f : 0.5f, R() < 0.5f ? 0f : 0.5f);
                // One face only: the leaf material draws both sides (Cull Off), and both use the outward normal.
                int s0 = mb.v.Count;
                mb.v.AddRange(new[] { p - t1 * s - t2 * s, p + t1 * s - t2 * s, p + t1 * s + t2 * s, p - t1 * s + t2 * s });
                mb.uv.AddRange(new[] { uv0, uv0 + new Vector2(0.5f, 0f), uv0 + new Vector2(0.5f, 0.5f), uv0 + new Vector2(0f, 0.5f) });
                for (int q = 0; q < 4; q++) mb.n.Add(nrm);
                mb.t.AddRange(new[] { s0, s0 + 2, s0 + 1, s0, s0 + 3, s0 + 2 });
            }
        }

        /// <summary>An alpha-tested, double-sided leaf material over the generated leaf texture.</summary>
        static Material Foliage(string name, Color tint)
        {
            var m = Mat(name, tint, 0.12f);
            m.SetTexture("_BaseMap", LeafTexture());
            m.mainTextureScale = Vector2.one;
            m.SetFloat("_AlphaClip", 1f); m.EnableKeyword("_ALPHATEST_ON"); m.SetFloat("_Cutoff", 0.42f);
            m.SetFloat("_Cull", 0f); m.SetOverrideTag("RenderType", "TransparentCutout");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
            EditorUtility.SetDirty(m);
            return m;
        }

        static string GenTex => CoreConfig.Textures + "Generated/";

        /// <summary>A 512 x 512 texture of small leaves in varied greens on transparency, made once by code.</summary>
        static Texture2D LeafTexture()
        {
            string path = GenTex + "leaves.png";
            if (!File.Exists(path))
            {
                Directory.CreateDirectory(GenTex);
                const int S = 512;
                var px = new Color[S * S];
                var rnd = new System.Random(7);
                float R() => (float)rnd.NextDouble();
                // Leaves are drawn within each quarter separately (cards use one quarter each), away from its edges.
                for (int quad = 0; quad < 4; quad++)
                {
                    int ox = (quad % 2) * S / 2, oy = (quad / 2) * S / 2;
                    for (int k = 0; k < 150; k++)
                    {
                        float cx = ox + 22 + R() * (S / 2 - 44), cy = oy + 22 + R() * (S / 2 - 44);
                        float ang = R() * Mathf.PI, len = 11f + R() * 13f, wid = 3f + R() * 3.5f;
                        float shade = 0.55f + R() * 0.45f;
                        var col = new Color(0.62f * shade, (0.72f + R() * 0.1f) * shade, 0.5f * shade, 1f);
                        float ca = Mathf.Cos(ang), sa = Mathf.Sin(ang);
                        for (int y = (int)(cy - len); y <= cy + len; y++)
                            for (int x = (int)(cx - len); x <= cx + len; x++)
                            {
                                float dx = x - cx, dy = y - cy, u = dx * ca + dy * sa, v = -dx * sa + dy * ca;
                                if ((u * u) / (len * len) + (v * v) / (wid * wid) > 1f) continue;
                                float vein = Mathf.Abs(v) < 0.6f ? 0.85f : 1f;
                                px[y * S + x] = new Color(col.r * vein, col.g * vein, col.b * vein, 1f);
                            }
                    }
                }
                // Transparent pixels take the colour of the leaves, so mipmaps do not darken the edges.
                for (int i = 0; i < px.Length; i++) if (px[i].a == 0f) px[i] = new Color(0.4f, 0.48f, 0.32f, 0f);
                var t = new Texture2D(S, S, TextureFormat.RGBA32, false);
                t.SetPixels(px); t.Apply();
                File.WriteAllBytes(path, t.EncodeToPNG());
                Object.DestroyImmediate(t);
                AssetDatabase.ImportAsset(path);
                var imp = (TextureImporter)AssetImporter.GetAtPath(path);
                imp.alphaIsTransparency = true; imp.mipmapEnabled = true; imp.mipMapsPreserveCoverage = true;
                imp.alphaTestReferenceValue = 0.42f; imp.wrapMode = TextureWrapMode.Clamp;
                imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// <summary>
        /// A tileable 256 x 256 noise texture for the weathered stone shader, made once by code: R large patches,
        /// G medium, B for rain streaks, A fine grain (fractal value noise, normalised per channel, linear).
        /// </summary>
        static Texture2D NoiseTexture()
        {
            string path = GenTex + "RihlaNoise.png";
            if (!File.Exists(path))
            {
                Directory.CreateDirectory(GenTex);
                const int S = 256;
                var ch = new float[4][];
                int[] periods = { 4, 8, 16, 32 };
                for (int c = 0; c < 4; c++)
                {
                    ch[c] = new float[S * S];
                    for (int y = 0; y < S; y++)
                        for (int x = 0; x < S; x++)
                        {
                            float sum = 0f, amp = 0.5f;
                            for (int o = 0; o < 4; o++)
                            {
                                int per = periods[c] << o;
                                sum += amp * ValueNoise(x * per / (float)S, y * per / (float)S, per, c * 7 + o);
                                amp *= 0.5f;
                            }
                            ch[c][y * S + x] = sum;
                        }
                    float lo = ch[c].Min(), hi = ch[c].Max();
                    for (int i = 0; i < ch[c].Length; i++) ch[c][i] = (ch[c][i] - lo) / (hi - lo);
                }
                var t = new Texture2D(S, S, TextureFormat.RGBA32, false, true);
                var px = new Color[S * S];
                for (int i = 0; i < px.Length; i++) px[i] = new Color(ch[0][i], ch[1][i], ch[2][i], ch[3][i]);
                t.SetPixels(px); t.Apply();
                File.WriteAllBytes(path, t.EncodeToPNG());
                Object.DestroyImmediate(t);
                AssetDatabase.ImportAsset(path);
                var imp = (TextureImporter)AssetImporter.GetAtPath(path);
                imp.sRGBTexture = false; imp.wrapMode = TextureWrapMode.Repeat; imp.mipmapEnabled = true;
                imp.textureCompression = TextureImporterCompression.Uncompressed; imp.alphaSource = TextureImporterAlphaSource.FromInput;
                imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static float ValueNoise(float x, float y, int period, int seed)
        {
            int ix = Mathf.FloorToInt(x), iy = Mathf.FloorToInt(y);
            float fx = x - ix, fy = y - iy;
            fx = fx * fx * (3f - 2f * fx); fy = fy * fy * (3f - 2f * fy);
            float L(int i, int j) => Lattice(((i % period) + period) % period, ((j % period) + period) % period, seed);
            return Mathf.Lerp(Mathf.Lerp(L(ix, iy), L(ix + 1, iy), fx), Mathf.Lerp(L(ix, iy + 1), L(ix + 1, iy + 1), fx), fy);
        }

        static float Lattice(int x, int y, int seed)
        {
            uint h = (uint)(x * 374761393 + y * 668265263 + seed * 1442695041);
            h = (h ^ (h >> 13)) * 1274126177u; h ^= h >> 16;
            return h / 4294967295f;
        }
    }
}

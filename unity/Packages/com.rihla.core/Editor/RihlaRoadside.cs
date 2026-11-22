using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Rihla.EditorTools
{
    /// <summary>
    /// Realism on the road down to Ariha (the owner, 19 November 2026: "make the car, roads, around the roads more
    /// realistic"): textured land (Poly Haven CC0, recoloured to the pale Judean wilderness), asphalt with edge lines,
    /// white reflector posts, guard rails where the ground falls away, a line of power poles, desert shrubs and
    /// stones, palm groves and low houses near Ariha, and a car with a shaped body, glass, lights and real wheels.
    /// Everything small is merged into a few meshes per kilometre, so it costs little.
    /// </summary>
    public static partial class RihlaSceneBuilder
    {
        static string PH => CoreConfig.Textures + "PolyHaven/";

        static Texture2D PHTex(string name, bool normal)
        {
            string path = PH + name + (normal ? "_nor_gl_1k.jpg" : "_diff_1k.jpg");
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp && normal && imp.textureType != TextureImporterType.NormalMap) { imp.textureType = TextureImporterType.NormalMap; imp.SaveAndReimport(); }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static TerrainLayer TexLayer(string name, string tex, float tile)
        {
            string layerPath = Built + name + ".terrainlayer";
            var layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(layerPath);
            if (!layer) { layer = new TerrainLayer(); AssetDatabase.CreateAsset(layer, layerPath); }
            layer.diffuseTexture = PHTex(tex, false); layer.normalMapTexture = PHTex(tex, true); layer.normalScale = 1f;
            layer.tileSize = new Vector2(tile, tile); layer.smoothness = 0f; layer.metallic = 0f;
            EditorUtility.SetDirty(layer);
            return layer;
        }

        /// <summary>A lit material with a Poly Haven colour and normal map, repeating every <paramref name="metres"/>.</summary>
        static Material TexMat(string name, string tex, float metres, Color tint, float smooth = 0.1f)
        {
            var m = Mat(name, tint, smooth);
            m.SetTexture("_BaseMap", PHTex(tex, false));
            m.SetTexture("_BumpMap", PHTex(tex, true)); m.EnableKeyword("_NORMALMAP"); m.SetFloat("_BumpScale", 1f);
            m.mainTextureScale = Vector2.one / metres;
            EditorUtility.SetDirty(m);
            return m;
        }

        static Material Glow(string name, Color c) => Unlit(name, c);

        // ------------------------------------------------------------------ mesh building

        /// <summary>Gathers triangles per material, then makes one object with submeshes.</summary>
        class MB
        {
            public readonly List<Vector3> v = new List<Vector3>(); public readonly List<Vector2> uv = new List<Vector2>();
            public readonly Dictionary<Material, List<int>> tris = new Dictionary<Material, List<int>>();
            public List<int> T(Material m) { if (!tris.TryGetValue(m, out var t)) tris[m] = t = new List<int>(); return t; }

            public void Quad(Material m, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2? uvSize = null)
            {
                int i = v.Count; v.Add(a); v.Add(b); v.Add(c); v.Add(d);
                var s = uvSize ?? Vector2.one;
                uv.Add(Vector2.zero); uv.Add(new Vector2(s.x, 0f)); uv.Add(s); uv.Add(new Vector2(0f, s.y));
                T(m).AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3 });
            }

            /// <summary>A box at <paramref name="c"/>, its length along <paramref name="fwd"/> (flattened or not).</summary>
            public void Box(Material m, Vector3 c, Vector3 fwd, Vector3 size, bool upright = true)
            {
                fwd = fwd.sqrMagnitude < 1e-6f ? Vector3.forward : fwd.normalized;
                Vector3 up = upright ? Vector3.up : Vector3.Cross(fwd, Vector3.Cross(Vector3.up, fwd)).normalized;
                if (upright) { fwd.y = 0f; fwd = fwd.sqrMagnitude < 1e-6f ? Vector3.forward : fwd.normalized; }
                Vector3 right = Vector3.Cross(up, fwd).normalized;
                Vector3 X = right * size.x / 2f, Y = up * size.y / 2f, Z = fwd * size.z / 2f;
                Vector3 P(int sx, int sy, int sz) => c + X * sx + Y * sy + Z * sz;
                // Texture coordinates in metres, so a textured material repeats at its own scale on any size of box.
                Vector2 fb = new Vector2(size.x, size.y), sd = new Vector2(size.z, size.y), tb = new Vector2(size.x, size.z);
                Quad(m, P(-1, -1, 1), P(1, -1, 1), P(1, 1, 1), P(-1, 1, 1), fb);    // front
                Quad(m, P(1, -1, -1), P(-1, -1, -1), P(-1, 1, -1), P(1, 1, -1), fb); // back
                Quad(m, P(1, -1, 1), P(1, -1, -1), P(1, 1, -1), P(1, 1, 1), sd);     // right
                Quad(m, P(-1, -1, -1), P(-1, -1, 1), P(-1, 1, 1), P(-1, 1, -1), sd); // left
                Quad(m, P(-1, 1, 1), P(1, 1, 1), P(1, 1, -1), P(-1, 1, -1), tb);     // top
                Quad(m, P(-1, -1, -1), P(1, -1, -1), P(1, -1, 1), P(-1, -1, 1), tb); // bottom
            }

            public void Mesh(Material m, Mesh src, Matrix4x4 at)
            {
                int i = v.Count; var sv = src.vertices; var suv = src.uv;
                for (int k = 0; k < sv.Length; k++) { v.Add(at.MultiplyPoint3x4(sv[k])); uv.Add(suv.Length > k ? suv[k] : Vector2.zero); }
                foreach (int t in src.triangles) T(m).Add(i + t);
            }

            public GameObject Make(string name, Transform parent, bool shadows, bool collider = false, string asset = null)
            {
                if (v.Count == 0) return null;
                var mesh = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
                mesh.SetVertices(v); mesh.SetUVs(0, uv);
                var mats = new List<Material>(tris.Keys);
                mesh.subMeshCount = mats.Count;
                for (int k = 0; k < mats.Count; k++) mesh.SetTriangles(tris[mats[k]], k);
                mesh.RecalculateNormals(); mesh.RecalculateBounds(); mesh.RecalculateTangents();
                AssetDatabase.CreateAsset(mesh, Built + (asset ?? name.Replace(' ', '_').Replace("(", "").Replace(")", "")) + ".asset");
                var go = new GameObject(name); go.transform.SetParent(parent, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var r = go.AddComponent<MeshRenderer>(); r.sharedMaterials = mats.ToArray();
                r.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
                if (collider) go.AddComponent<MeshCollider>().sharedMesh = mesh;
                return go;
            }
        }

        // ------------------------------------------------------------------ along the road

        /// <summary>Reflector posts, guard rails where the ground drops beside the road, and power poles with wires.</summary>
        static void RoadFurniture(RoadPath road)
        {
            var parent = new GameObject("Road furniture").transform;
            var white = Mat("post_white", new Color(0.9f, 0.9f, 0.88f), 0.4f);
            var black = Mat("post_black", new Color(0.08f, 0.08f, 0.08f), 0.3f);
            var reflector = Glow("post_reflector", new Color(1f, 0.55f, 0.1f) * 0.9f);
            var rail = Mat("guard_rail", new Color(0.62f, 0.64f, 0.66f), 0.55f); rail.SetFloat("_Metallic", 0.7f);
            var pole = Mat("power_pole", new Color(0.62f, 0.6f, 0.56f), 0.2f);
            var wire = Mat("power_wire", new Color(0.1f, 0.1f, 0.1f), 0.3f);
            float hw = road.width / 2f;
            int n = road.points.Length;
            Vector3 Dir(int i) { var d = road.points[Mathf.Min(n - 1, i + 1)] - road.points[Mathf.Max(0, i - 1)]; d.y = 0f; return d.normalized; }
            Vector3 Right(int i) => Vector3.Cross(Vector3.up, Dir(i));

            MB mb = null; float chunkStart = 0f; int chunk = 0;
            void Flush() { var g = mb?.Make("Road furniture " + chunk, parent, false, false, "road_furniture_" + chunk); if (g) g.layer = DetailCulling.Small; chunk++; mb = new MB(); }
            Flush();

            // Guard rails: on each side, where the ground 3 m out lies more than 2.5 m below the road.
            var railOn = new bool[2, n];
            for (int i = 0; i < n; i++)
                for (int s = 0; s < 2; s++)
                {
                    Vector3 p = road.points[i] + Right(i) * (s == 0 ? 1f : -1f) * (hw + 3f);
                    railOn[s, i] = road.distances[i] > 250f && Ground(p.x, p.z) < road.points[i].y - 2.5f;
                }
            // Fill short gaps and drop very short runs, so rails look deliberate.
            for (int s = 0; s < 2; s++)
            {
                for (int i = 1; i < n - 4; i++) if (!railOn[s, i] && railOn[s, i - 1]) for (int k = 1; k <= 4; k++) if (railOn[s, i + k]) { for (int j = i; j < i + k; j++) railOn[s, j] = true; break; }
                int run = 0;
                for (int i = 0; i <= n; i++)
                {
                    if (i < n && railOn[s, i]) { run++; continue; }
                    if (run > 0 && run < 6) for (int j = i - run; j < i; j++) railOn[s, j] = false;
                    run = 0;
                }
            }

            float nextPost = 20f, nextPole = 80f; Vector3? lastPoleTop = null; Vector3 lastPoleRight = Vector3.zero;
            for (int i = 1; i < n; i++)
            {
                float d = road.distances[i];
                if (d - chunkStart > 250f) { Flush(); chunkStart = d; }
                for (int s = 0; s < 2; s++)
                {
                    if (!railOn[s, i] || !railOn[s, i - 1]) continue;
                    float sign = s == 0 ? 1f : -1f;
                    Vector3 a = road.points[i - 1] + Right(i - 1) * sign * (hw + 0.7f), b = road.points[i] + Right(i) * sign * (hw + 0.7f);
                    Vector3 mid = (a + b) / 2f, along = b - a;
                    mb.Box(rail, mid + Vector3.up * 0.62f, along, new Vector3(0.05f, 0.32f, along.magnitude + 0.05f), false);
                    if (i % 2 == 0) mb.Box(black, a + Vector3.up * 0.3f + Right(i) * sign * 0.06f, Dir(i), new Vector3(0.1f, 0.85f, 0.1f));
                }
                // Reflector posts every 50 m on both sides, where there is no rail.
                if (d >= nextPost)
                {
                    nextPost += 50f;
                    for (int s = 0; s < 2; s++)
                    {
                        if (railOn[s, i]) continue;
                        float sign = s == 0 ? 1f : -1f;
                        Vector3 p = road.points[i] + Right(i) * sign * (hw + 1.1f);
                        if (!Clear(road, p, hw + 0.9f)) continue;
                        p.y = Ground(p.x, p.z);
                        mb.Box(white, p + Vector3.up * 0.5f, Dir(i), new Vector3(0.12f, 1.0f, 0.12f));
                        mb.Box(black, p + Vector3.up * 0.86f, Dir(i), new Vector3(0.125f, 0.16f, 0.125f));
                        mb.Box(reflector, p + Vector3.up * 0.86f - Dir(i) * 0.065f * (s == 0 ? 1f : 1f), Dir(i), new Vector3(0.06f, 0.1f, 0.01f));
                    }
                }
                // Power poles every 80 m on the left, 8 m tall, with three wires sagging between them.
                if (d >= nextPole && d > 400f)
                {
                    nextPole += 80f;
                    Vector3 p = road.points[i] - Right(i) * (hw + 7f);
                    if (!Clear(road, p, hw + 3f)) { lastPoleTop = null; continue; }
                    p.y = Ground(p.x, p.z);
                    mb.Box(pole, p + Vector3.up * 4f, Dir(i), new Vector3(0.24f, 8f, 0.24f));
                    mb.Box(pole, p + Vector3.up * 7.6f, Right(i), new Vector3(0.12f, 0.12f, 1.8f));
                    Vector3 top = p + Vector3.up * 7.7f;
                    if (lastPoleTop.HasValue && Vector3.Distance(top, lastPoleTop.Value) < 140f)
                        foreach (float off in new[] { -0.75f, 0f, 0.75f })
                        {
                            Vector3 a = lastPoleTop.Value + lastPoleRight * off, b = top + Right(i) * off;
                            const int segs = 8; Vector3 prev = a;
                            for (int k = 1; k <= segs; k++)
                            {
                                float t = k / (float)segs;
                                Vector3 q = Vector3.Lerp(a, b, t) + Vector3.down * (4f * t * (1f - t) * 0.9f);
                                Vector3 dd = q - prev;
                                mb.Box(wire, (prev + q) / 2f, dd, new Vector3(0.025f, 0.025f, dd.magnitude + 0.01f), false);
                                prev = q;
                            }
                        }
                    lastPoleTop = top; lastPoleRight = Right(i);
                }
            }
            Flush();
        }

        /// <summary>A low rounded bush or a stone: a squashed, dented sphere.</summary>
        static Mesh Lump(string name, int seed, float dent)
        {
            var rng = new System.Random(seed);
            const int seg = 12, rings = 8;
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
            for (int r = 0; r <= rings; r++)
            {
                float phi = Mathf.PI * r / rings;
                for (int s = 0; s <= seg; s++)
                {
                    float th = 2f * Mathf.PI * s / seg;
                    float k = 1f + ((float)rng.NextDouble() - 0.5f) * dent;
                    if (s == seg) k = 1f;
                    var p = new Vector3(Mathf.Sin(phi) * Mathf.Cos(th), Mathf.Cos(phi), Mathf.Sin(phi) * Mathf.Sin(th)) * k;
                    v.Add(p); uv.Add(new Vector2(s / (float)seg, r / (float)rings));
                }
            }
            for (int r = 0; r < rings; r++) for (int s = 0; s < seg; s++)
            {
                int a = r * (seg + 1) + s, b = a + seg + 1;
                t.AddRange(new[] { a, a + 1, b, a + 1, b + 1, b });
            }
            // Close the seam so the dents match.
            for (int r = 0; r <= rings; r++) v[r * (seg + 1) + seg] = v[r * (seg + 1)];
            var m = new Mesh { name = name }; m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(t, 0); m.RecalculateNormals();
            return m;
        }

        /// <summary>Desert shrubs and stones in a band beside the road (none in Ariha's groves, which have palms).</summary>
        static void Scatter(RoadPath road, float oasisFrom, float oasisTo)
        {
            var parent = new GameObject("Roadside shrubs and stones").transform;
            var shrubMat = Mat("desert_shrub", new Color(0.5f, 0.51f, 0.39f), 0.05f);
            var shrubDry = Mat("desert_shrub_dry", new Color(0.6f, 0.55f, 0.4f), 0.05f);
            var stoneMat = TexMat("roadside_stone", "rihla_desert_rock", 2f, new Color(0.95f, 0.93f, 0.9f), 0.1f);
            var shrubs = new[] { Lump("shrub a", 3, 0.5f), Lump("shrub b", 5, 0.6f) };
            var stones = new[] { Lump("stone a", 7, 0.35f), Lump("stone b", 11, 0.45f), Lump("stone c", 13, 0.3f) };
            var rng = new System.Random(2026);
            float R() => (float)rng.NextDouble();
            float hw = road.width / 2f; int n = road.points.Length;
            MB mb = new MB(); float chunkStart = 0f; int chunk = 0;
            for (int i = 2; i < n; i += 2)
            {
                float d = road.distances[i];
                if (d - chunkStart > 250f) { var g = mb.Make("Roadside " + chunk, parent, false, false, "roadside_" + chunk); if (g) g.layer = DetailCulling.Small; chunk++; mb = new MB(); chunkStart = d; }
                if (d < 300f || (d > oasisFrom && d < oasisTo)) continue;
                Vector3 dir = road.points[i] - road.points[i - 2]; dir.y = 0f; Vector3 right = Vector3.Cross(Vector3.up, dir.normalized);
                for (int s = -1; s <= 1; s += 2)
                    for (int k = 0; k < 3; k++)
                    {
                        float off = hw + 2.2f + R() * R() * 45f;
                        Vector3 p = road.points[i] + right * s * off + dir.normalized * (R() - 0.5f) * 6f;
                        if (!Clear(road, p, hw + 1.8f)) continue;
                        float g = Ground(p.x, p.z); if (g > road.points[i].y + 12f || g < road.points[i].y - 25f) continue;
                        p.y = g;
                        float roll = R();
                        if (roll < 0.4f)
                        {
                            float sz = 0.22f + R() * R() * 0.6f;
                            var at = Matrix4x4.TRS(p + Vector3.up * sz * 0.25f, Quaternion.Euler(0f, R() * 360f, 0f), new Vector3(sz * 1.3f, sz * 0.75f, sz * 1.2f));
                            mb.Mesh(R() < 0.7f ? shrubMat : shrubDry, shrubs[rng.Next(2)], at);
                        }
                        else if (roll < 0.62f)
                        {
                            float sz = 0.2f + R() * R() * 1.1f;
                            var at = Matrix4x4.TRS(p + Vector3.up * sz * 0.15f, Quaternion.Euler(R() * 20f, R() * 360f, R() * 20f), new Vector3(sz * 1.4f, sz * 0.8f, sz));
                            mb.Mesh(stoneMat, stones[rng.Next(3)], at);
                        }
                    }
            }
            var last = mb.Make("Roadside " + chunk, parent, false, false, "roadside_" + chunk); if (last) last.layer = DetailCulling.Small;
        }

        // ------------------------------------------------------------------ Ariha's palms and houses

        /// <summary>
        /// A date palm, made once and shared: a trunk of bark (Poly Haven, CC0) with the stubs of old leaf bases near
        /// the top, and a crown of 26 fronds in three tiers, the lowest older and yellowing. The crown is its own mesh,
        /// so it can sway in the breeze (PalmSway); its pivot is the top of the trunk.
        /// </summary>
        static (Mesh trunk, Material[] trunkMats, Mesh crown, Material[] crownMats) PalmAsset()
        {
            var trunkMat = TexMat("palm_trunk", "bark_brown_02", 1.1f, new Color(0.95f, 0.86f, 0.72f), 0.05f);
            var frondMat = Mat("palm_frond", new Color(0.62f, 0.72f, 0.42f), 0.15f);
            var oldMat = Mat("palm_frond_old", new Color(0.85f, 0.78f, 0.42f), 0.1f);
            foreach (var m in new[] { frondMat, oldMat })
            {
                m.SetTexture("_BaseMap", FrondTexture());
                m.SetFloat("_AlphaClip", 1f); m.SetFloat("_Cutoff", 0.45f); m.EnableKeyword("_ALPHATEST_ON");
                m.SetFloat("_Cull", 0f); m.renderQueue = (int)RenderQueue.AlphaTest; m.enableInstancing = true;
                EditorUtility.SetDirty(m);
            }
            trunkMat.enableInstancing = true;
            const float H = 9f; const int sides = 10;
            Vector3 Centre(float t) => new Vector3(0.35f * t * t, t * H, 0f);

            // The trunk, its texture wrapped once around (about 1.4 m) and running up in metres.
            var tb = new MB();
            int rings = 36; int b0 = tb.v.Count;
            for (int r = 0; r <= rings; r++)
            {
                float t = r / (float)rings;
                float rad = Mathf.Lerp(0.27f, 0.2f, t) * (1f + 0.04f * Mathf.Sin(r * 2.1f));
                for (int k = 0; k <= sides; k++)
                {
                    float a = k * Mathf.PI * 2f / sides;
                    tb.v.Add(Centre(t) + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * rad);
                    tb.uv.Add(new Vector2(k / (float)sides * 1.4f, t * H));
                }
            }
            var tt = tb.T(trunkMat);
            for (int r = 0; r < rings; r++)
                for (int k = 0; k < sides; k++)
                {
                    int a = b0 + r * (sides + 1) + k, b = a + 1, c = a + sides + 1, d = c + 1;
                    tt.AddRange(new[] { a, c, b, b, c, d });
                }
            // Stubs of old leaf bases just under the crown.
            var rng = new System.Random(9);
            for (int k = 0; k < 22; k++)
            {
                float t = 0.88f + (float)rng.NextDouble() * 0.1f, az = k * 137.5f * Mathf.Deg2Rad;
                Vector3 o = new Vector3(Mathf.Cos(az), 0f, Mathf.Sin(az));
                Vector3 c = Centre(t) + o * 0.24f + Vector3.up * 0.1f;
                tb.Box(trunkMat, c, o + Vector3.up * 1.4f, new Vector3(0.12f, 0.05f, 0.42f), false);
            }
            var trunk = new Mesh { name = "Date palm trunk" }; trunk.SetVertices(tb.v); trunk.SetUVs(0, tb.uv);
            var tmats = new List<Material>(tb.tris.Keys); trunk.subMeshCount = tmats.Count;
            for (int k = 0; k < tmats.Count; k++) trunk.SetTriangles(tb.tris[tmats[k]], k);
            trunk.RecalculateNormals(); trunk.RecalculateBounds();
            AssetDatabase.CreateAsset(trunk, Built + "date_palm_trunk.asset");

            // The crown, around its own pivot at the top of the trunk.
            var mb = new MB();
            Vector3 top = Vector3.zero;
            for (int f = 0; f < 26; f++)
            {
                float az = f * 137.5f * Mathf.Deg2Rad;
                int tier = f < 9 ? 2 : f < 19 ? 1 : 0;
                var mat = tier == 0 ? oldMat : frondMat;
                float rise = (tier == 2 ? 50f : tier == 1 ? 18f : -18f) + (float)rng.NextDouble() * 14f;
                float len = (tier == 2 ? 3.0f : 3.9f) + (float)rng.NextDouble() * 0.8f;
                float bend = tier == 0 ? 16f : 11f;
                Vector3 outDir = new Vector3(Mathf.Cos(az), 0f, Mathf.Sin(az)), side = Vector3.Cross(Vector3.up, outDir);
                const int segs = 9; Vector3 p = top; float ang = rise;
                Vector3 prevL = p, prevC = p, prevR = p;
                for (int k = 1; k <= segs; k++)
                {
                    float t = k / (float)segs;
                    ang -= bend;
                    Vector3 step = (outDir * Mathf.Cos(ang * Mathf.Deg2Rad) + Vector3.up * Mathf.Sin(ang * Mathf.Deg2Rad)) * (len / segs);
                    p += step;
                    float w = Mathf.Sin(Mathf.Min(1f, t * 1.4f) * Mathf.PI * 0.5f) * 0.6f * (1f - t * 0.6f);
                    // The leaflets fold up in a V along the rib.
                    Vector3 L = p - side * w + Vector3.up * w * 0.18f, R = p + side * w + Vector3.up * w * 0.18f;
                    int i0 = mb.v.Count;
                    mb.v.AddRange(new[] { prevL, prevC, p, L, prevC, prevR, R, p });
                    float v0 = (k - 1) / (float)segs, v1 = t;
                    mb.uv.AddRange(new[] { new Vector2(0f, v0), new Vector2(0.5f, v0), new Vector2(0.5f, v1), new Vector2(0f, v1), new Vector2(0.5f, v0), new Vector2(1f, v0), new Vector2(1f, v1), new Vector2(0.5f, v1) });
                    mb.T(mat).AddRange(new[] { i0, i0 + 2, i0 + 1, i0, i0 + 3, i0 + 2, i0 + 4, i0 + 6, i0 + 5, i0 + 4, i0 + 7, i0 + 6 });
                    prevL = L; prevC = p; prevR = R;
                }
            }
            var crown = new Mesh { name = "Date palm crown" }; crown.SetVertices(mb.v); crown.SetUVs(0, mb.uv);
            var cmats = new List<Material>(mb.tris.Keys); crown.subMeshCount = cmats.Count;
            for (int k = 0; k < cmats.Count; k++) crown.SetTriangles(mb.tris[cmats[k]], k);
            crown.RecalculateNormals(); crown.RecalculateBounds();
            AssetDatabase.CreateAsset(crown, Built + "date_palm_crown.asset");
            return (trunk, tmats.ToArray(), crown, cmats.ToArray());
        }

        /// <summary>A whole palm: the trunk, and its crown as a child named "Crown" (which PalmSway moves).</summary>
        static GameObject MakePalm(Transform parent, Vector3 at, Quaternion rot, float scale, bool shadows)
        {
            var (trunk, tmats, crown, cmats) = palmCache ??= PalmAsset();
            var go = new GameObject("Date palm") { layer = DetailCulling.Palms }; go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(at, rot); go.transform.localScale = Vector3.one * scale;
            go.AddComponent<MeshFilter>().sharedMesh = trunk;
            var r = go.AddComponent<MeshRenderer>(); r.sharedMaterials = tmats;
            var c = new GameObject("Crown") { layer = DetailCulling.Palms }; c.transform.SetParent(go.transform, false);
            c.transform.localPosition = new Vector3(0.35f, 9f, 0f);
            c.AddComponent<MeshFilter>().sharedMesh = crown;
            var cr = c.AddComponent<MeshRenderer>(); cr.sharedMaterials = cmats;
            if (!shadows) { r.shadowCastingMode = ShadowCastingMode.Off; cr.shadowCastingMode = ShadowCastingMode.Off; }
            return go;
        }

        /// <summary>A palm frond seen from above: a rib with stiff leaflets on both sides, pale green, see-through between.</summary>
        static Texture2D FrondTexture()
        {
            string path = Built + "palm_frond.png";
            const int W = 128, Hh = 256;
            var tex = new Texture2D(W, Hh, TextureFormat.RGBA32, false);
            var px = new Color[W * Hh];
            var leaf = new Color(0.36f, 0.46f, 0.24f, 1f);
            for (int y = 0; y < Hh; y++)
                for (int x = 0; x < W; x++)
                {
                    float u = (x + 0.5f) / W, v = (y + 0.5f) / Hh, du = Mathf.Abs(u - 0.5f) * 2f;
                    float a = 0f; Color c = leaf;
                    if (du < 0.05f) { a = 1f; c = new Color(0.55f, 0.5f, 0.3f); }
                    else
                    {
                        // Leaflets angle forward from the rib; one every 1/38 of the length.
                        float phase = (v * 38f - du * 2.2f) % 1f; if (phase < 0f) phase += 1f;
                        if (phase < 0.42f && v > 0.04f && du < 0.98f - v * 0.3f) { a = 1f; c = leaf * (0.85f + 0.3f * phase); c.a = 1f; }
                    }
                    px[y * W + x] = new Color(c.r, c.g, c.b, a);
                }
            tex.SetPixels(px); tex.Apply();
            System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(path);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.alphaIsTransparency = true; imp.mipmapEnabled = true; imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// <summary>
        /// Palm groves on both sides of the road as it comes into Ariha, and low flat-roofed houses among them. The town
        /// is suggested, not mapped (H23).
        /// </summary>
        static void Oasis(RoadPath road, float from, float to, Vector3 keepClear, float clearRadius)
        {
            var parent = new GameObject("Ariha: palms and houses").transform;
            var plaster = TexMat("ariha_plaster", "white_plaster_rough_01", 3f, new Color(1.7f, 1.62f, 1.48f), 0.1f);
            var dark = Mat("ariha_window", new Color(0.12f, 0.11f, 0.1f), 0.3f);
            var tank = Mat("ariha_tank", new Color(0.1f, 0.1f, 0.11f), 0.4f);
            var rng = new System.Random(77); float R() => (float)rng.NextDouble();
            float hw = road.width / 2f;
            int palms = 0;
            var houses = new MB();
            var placedHouses = new List<Vector3>();
            for (float d = from; d < to; d += 9f)
            {
                Vector3 c = road.At(d), dir = road.At(d + 4f) - road.At(d - 4f); dir.y = 0f; dir.Normalize();
                Vector3 right = Vector3.Cross(Vector3.up, dir);
                float fade = Mathf.Clamp01((d - from) / 300f);
                for (int s = -1; s <= 1; s += 2)
                    for (int row = 0; row < 7; row++)
                    {
                        if (R() > fade * 0.85f) continue;
                        float off = hw + 6f + row * 8f + R() * 2f;
                        Vector3 p = c + right * s * off + dir * (R() - 0.5f) * 3f;
                        if (Flat2(p, keepClear) < clearRadius || !Clear(road, p, hw + 4f)) continue;
                        p.y = Ground(p.x, p.z);
                        if (Mathf.Abs(p.y - c.y) > 8f) continue;
                        if (row >= 4 && R() < 0.12f && placedHouses.TrueForAll(h => Flat2(h, p) > 14f) && Clear(road, p, hw + 12f))
                        {
                            // A house: one or two storeys, a parapet, windows and a water tank on the roof.
                            float w = 7f + R() * 4f, dep = 6f + R() * 3f, hgt = R() < 0.6f ? 3.4f : 6.6f;
                            Vector3 f = Quaternion.Euler(0f, R() < 0.5f ? 0f : 90f, 0f) * dir;
                            houses.Box(plaster, p + Vector3.up * (hgt / 2f - 0.5f), f, new Vector3(w, hgt + 1f, dep));
                            houses.Box(plaster, p + Vector3.up * (hgt + 0.25f), f, new Vector3(w + 0.1f, 0.5f, dep + 0.1f));
                            Vector3 fr = Vector3.Cross(Vector3.up, f);
                            for (int k = -1; k <= 1; k += 2)
                                for (float y = 1.6f; y < hgt; y += 3.2f)
                                {
                                    houses.Box(dark, p + Vector3.up * y + f * (dep / 2f + 0.02f) + fr * k * w * 0.25f, f, new Vector3(1.1f, 1.3f, 0.05f));
                                    houses.Box(dark, p + Vector3.up * y - f * (dep / 2f + 0.02f) + fr * k * w * 0.25f, f, new Vector3(1.1f, 1.3f, 0.05f));
                                }
                            houses.Box(tank, p + Vector3.up * (hgt + 0.9f) + fr * w * 0.25f, f, new Vector3(1f, 1.1f, 1f));
                            placedHouses.Add(p);
                            continue;
                        }
                        // Only the palms at the spring cast shadows (cost).
                        MakePalm(parent, p, Quaternion.Euler(0f, R() * 360f, 0f), 0.75f + R() * 0.45f, false);
                        palms++;
                    }
            }
            houses.Make("Ariha houses", parent, true, true, "ariha_houses");
            Debug.Log($"[Rihla] Ariha: {palms} palms, {placedHouses.Count} houses");
        }

        /// <summary>Puts a palm (the shared mesh) at a place; used by Ariha's spring as well.</summary>
        static GameObject Palm(Transform parent, Vector3 at, float scale, int seed)
        {
            return MakePalm(parent, at, Quaternion.Euler(0f, seed * 97f % 360f, 0f), scale, true);
        }
        static (Mesh, Material[], Mesh, Material[])? palmCache;

        /// <summary>True when <paramref name="p"/> is at least <paramref name="min"/> metres from every part of the road
        /// (where the road doubles back, a point beside one stretch can lie on another).</summary>
        static bool Clear(RoadPath road, Vector3 p, float min) => Flat2(road.points[road.Nearest(p)], p) >= min;

        static float Flat2(Vector3 a, Vector3 b) => Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));

        // ------------------------------------------------------------------ the car

        /// <summary>
        /// Jiddo's car: a four-door saloon of no real make, its body lofted from cross-sections (with arches over the
        /// wheels), a glass cabin with a painted roof and pillars, bumpers, grille, lamps, mirrors and plain plates.
        /// The physics (collider, WheelColliders) are unchanged.
        /// </summary>
        static void CarBody(Transform car)
        {
            var paint = Mat("car_paint", new Color(0.42f, 0.05f, 0.07f), 0.82f); paint.SetFloat("_Metallic", 0.35f);
            var glass = Mat("car_glass", new Color(0.06f, 0.08f, 0.1f), 0.95f);
            var trim = Mat("car_trim", new Color(0.07f, 0.07f, 0.075f), 0.35f);
            var chrome = Mat("car_chrome", new Color(0.8f, 0.8f, 0.82f), 0.9f); chrome.SetFloat("_Metallic", 1f);
            var plate = Mat("car_plate", new Color(0.92f, 0.92f, 0.9f), 0.4f);
            var head = Glow("car_headlamp", new Color(1f, 0.97f, 0.88f) * 1.3f);
            var tail = Glow("car_taillamp", new Color(0.8f, 0.05f, 0.04f) * 1.4f);
            var amber = Glow("car_indicator", new Color(1f, 0.55f, 0.1f) * 1.1f);
            var mb = new MB();

            // Body profile along the car (z): bottom, top, half width; arches lift the bottom over the wheels.
            float[] kz = { -2.22f, -2.1f, -1.7f, -1.1f, 0.9f, 1.6f, 2.08f, 2.24f };
            float[] kTop = { 0.78f, 0.96f, 1.0f, 1.0f, 0.98f, 0.92f, 0.84f, 0.7f };
            float[] kBot = { 0.38f, 0.3f, 0.28f, 0.28f, 0.28f, 0.29f, 0.32f, 0.4f };
            float[] kW = { 0.8f, 0.87f, 0.9f, 0.9f, 0.9f, 0.89f, 0.85f, 0.78f };
            float Key(float[] k, float z) { for (int i = 0; i < kz.Length - 1; i++) if (z <= kz[i + 1]) return Mathf.Lerp(k[i], k[i + 1], Mathf.InverseLerp(kz[i], kz[i + 1], z)); return k[k.Length - 1]; }
            float Arch(float z) { float a = 0f; foreach (float wz in new[] { -1.35f, 1.35f }) { float u = (z - wz) / 0.47f; if (Mathf.Abs(u) < 1f) a = Mathf.Max(a, 0.5f * Mathf.Sqrt(1f - u * u)); } return a; }
            const int ringN = 20;
            Vector3[] Ring(float z, float bot, float top, float w, float roundness)
            {
                var r = new Vector3[ringN + 1]; float mid = (bot + top) / 2f, h = (top - bot) / 2f;
                for (int i = 0; i <= ringN; i++)
                {
                    float a = i * Mathf.PI * 2f / ringN, c = Mathf.Cos(a), s = Mathf.Sin(a);
                    float e = 2f / roundness;
                    r[i] = new Vector3(Mathf.Sign(c) * Mathf.Pow(Mathf.Abs(c), e) * w, mid + Mathf.Sign(s) * Mathf.Pow(Mathf.Abs(s), e) * h, z);
                }
                return r;
            }
            void Loft(Material m, List<Vector3[]> rings)
            {
                for (int k = 0; k < rings.Count - 1; k++) for (int i = 0; i < ringN; i++)
                    mb.Quad(m, rings[k][i], rings[k][i + 1], rings[k + 1][i + 1], rings[k + 1][i]);
                // Caps.
                foreach (int k in new[] { 0, rings.Count - 1 })
                {
                    Vector3 c = Vector3.zero; foreach (var p in rings[k]) c += p; c /= rings[k].Length;
                    for (int i = 0; i < ringN; i++)
                        if (k == 0) mb.Quad(m, c, rings[k][i + 1], rings[k][i], c); else mb.Quad(m, c, rings[k][i], rings[k][i + 1], c);
                }
            }
            var body = new List<Vector3[]>();
            for (float z = kz[0]; z <= kz[kz.Length - 1] + 1e-3f; z += 0.04f)
                body.Add(Ring(z, Key(kBot, z) + Arch(z), Key(kTop, z), Key(kW, z), 5f));
            Loft(paint, body);

            // The cabin: glass from the belt line to the roof, the windscreen and rear window sloping.
            var cabin = new List<Vector3[]>();
            float[] cz = { -1.2f, -0.72f, 0.32f, 0.92f }; float[] ch = { 0.02f, 0.47f, 0.47f, 0.02f };
            for (float z = cz[0]; z <= cz[3] + 1e-3f; z += 0.04f)
            {
                float h = z < cz[1] ? Mathf.Lerp(ch[0], ch[1], Mathf.InverseLerp(cz[0], cz[1], z)) : z < cz[2] ? ch[1] : Mathf.Lerp(ch[2], ch[3], Mathf.InverseLerp(cz[2], cz[3], z));
                float wb = 0.84f, wt = Mathf.Lerp(0.84f, 0.7f, h / 0.47f);
                var r = new Vector3[ringN + 1];
                // A trapezoid with rounded top corners: bottom at the belt (0.98), top at 0.98 + h.
                for (int i = 0; i <= ringN; i++)
                {
                    float a = i * Mathf.PI * 2f / ringN, c = Mathf.Cos(a), s = Mathf.Sin(a);
                    float y = 0.97f + (s > 0f ? Mathf.Pow(s, 0.4f) * h : 0f);
                    float w = Mathf.Lerp(wb, wt, Mathf.Clamp01((y - 0.97f) / Mathf.Max(0.01f, h)));
                    r[i] = new Vector3(Mathf.Sign(c) * Mathf.Pow(Mathf.Abs(c), 0.35f) * w, y, z);
                }
                cabin.Add(r);
            }
            Loft(glass, cabin);
            // Painted roof and pillars over the glass.
            mb.Box(paint, new Vector3(0f, 1.43f, -0.2f), Vector3.forward, new Vector3(1.22f, 0.04f, 0.94f), false);
            foreach (float x in new[] { -0.79f, 0.79f })
            {
                mb.Box(trim, new Vector3(x * 0.885f, 1.2f, -0.2f), Vector3.forward, new Vector3(0.03f, 0.42f, 0.09f));        // B pillar
                mb.Box(trim, new Vector3(x * 1.12f, 1.02f, 0.78f), Vector3.forward, new Vector3(0.12f, 0.1f, 0.16f));      // mirror
                mb.Box(trim, new Vector3(x * 1.06f, 0.99f, 0.82f), Vector3.right, new Vector3(0.1f, 0.03f, 0.04f));        // mirror arm
                mb.Box(chrome, new Vector3(x * 1.135f, 0.82f, 0.25f), Vector3.forward, new Vector3(0.02f, 0.03f, 0.18f));  // door handles
                mb.Box(chrome, new Vector3(x * 1.135f, 0.82f, -0.62f), Vector3.forward, new Vector3(0.02f, 0.03f, 0.18f));
                mb.Box(trim, new Vector3(x * 1.13f, 0.5f, -0.1f), Vector3.forward, new Vector3(0.02f, 0.08f, 1.6f));       // sill
            }
            // Door lines.
            foreach (float x in new[] { -1f, 1f }) foreach (float z in new[] { 0.9f, -0.22f, -1.2f })
                mb.Box(trim, new Vector3(x * 0.905f, 0.66f, z), Vector3.forward, new Vector3(0.012f, 0.62f, 0.012f));
            // Bumpers, grille, lamps, plates.
            mb.Box(trim, new Vector3(0f, 0.45f, 2.24f), Vector3.forward, new Vector3(1.66f, 0.2f, 0.12f));
            mb.Box(trim, new Vector3(0f, 0.45f, -2.24f), Vector3.forward, new Vector3(1.66f, 0.2f, 0.12f));
            mb.Box(trim, new Vector3(0f, 0.66f, 2.22f), Vector3.forward, new Vector3(0.8f, 0.16f, 0.06f));
            for (int k = 0; k < 4; k++) mb.Box(chrome, new Vector3(0f, 0.61f + k * 0.035f, 2.255f), Vector3.forward, new Vector3(0.78f, 0.008f, 0.01f));
            foreach (float x in new[] { -0.58f, 0.58f })
            {
                mb.Box(head, new Vector3(x, 0.68f, 2.2f), Vector3.forward, new Vector3(0.36f, 0.13f, 0.08f));
                mb.Box(amber, new Vector3(x * 1.33f, 0.68f, 2.15f), Vector3.forward, new Vector3(0.1f, 0.08f, 0.08f));
                mb.Box(tail, new Vector3(x * 1.1f, 0.8f, -2.18f), Vector3.forward, new Vector3(0.32f, 0.14f, 0.08f));
            }
            mb.Box(plate, new Vector3(0f, 0.46f, 2.31f), Vector3.forward, new Vector3(0.52f, 0.11f, 0.01f));
            mb.Box(plate, new Vector3(0f, 0.66f, -2.2f), Vector3.forward, new Vector3(0.52f, 0.11f, 0.05f));
            var go = mb.Make("Car body", car, true, false, "car_body");
            go.transform.localPosition = Vector3.zero;

            // Wheels: tyres with a rounded tread and five-spoke silver rims; the axle is the mesh's y axis, as the
            // cylinders they replace (CarController turns them by the WheelColliders).
            var tyreMat = Mat("tyre", new Color(0.06f, 0.06f, 0.06f), 0.15f);
            var rimMat = Mat("car_rim", new Color(0.72f, 0.73f, 0.75f), 0.8f); rimMat.SetFloat("_Metallic", 0.9f);
            var wheel = WheelMesh(tyreMat, rimMat);
            foreach (Transform t in car)
                if (t.name.EndsWith(" mesh"))
                {
                    t.localScale = Vector3.one;
                    t.GetComponent<MeshFilter>().sharedMesh = wheel;
                    t.GetComponent<MeshRenderer>().sharedMaterials = new[] { tyreMat, rimMat };
                }
            foreach (var n in new[] { "body", "cabin" }) { var old = car.Find(n); if (old) Object.DestroyImmediate(old.gameObject); }
        }

        static Mesh WheelMesh(Material tyre, Material rim)
        {
            var mb = new MB();
            const int seg = 24; const float R = 0.36f, W = 0.22f, rimR = 0.24f;
            // Tyre profile (radius, y) around the axle (y), rounded at the shoulders.
            var prof = new[] { (rimR, -W / 2f), (R - 0.04f, -W / 2f), (R - 0.01f, -W / 2f + 0.02f), (R, -W / 4f), (R, W / 4f), (R - 0.01f, W / 2f - 0.02f), (R - 0.04f, W / 2f), (rimR, W / 2f) };
            for (int i = 0; i < seg; i++)
            {
                float a0 = i * Mathf.PI * 2f / seg, a1 = (i + 1) * Mathf.PI * 2f / seg;
                Vector3 P(float r, float y, float a) => new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r);
                for (int k = 0; k < prof.Length - 1; k++)
                    mb.Quad(tyre, P(prof[k].Item1, prof[k].Item2, a1), P(prof[k].Item1, prof[k].Item2, a0), P(prof[k + 1].Item1, prof[k + 1].Item2, a0), P(prof[k + 1].Item1, prof[k + 1].Item2, a1));
                // Rim faces (both sides), slightly dished, with a hub.
                foreach (float side in new[] { -1f, 1f })
                {
                    float y = side * (W / 2f - 0.03f);
                    var c = new Vector3(0f, y - side * 0.02f, 0f);
                    if (side > 0f) mb.Quad(rim, c, P(rimR, y, a1), P(rimR, y, a0), c); else mb.Quad(rim, c, P(rimR, y, a0), P(rimR, y, a1), c);
                }
            }
            // Five spokes standing out of the rim face on both sides.
            foreach (float side in new[] { -1f, 1f })
                for (int k = 0; k < 5; k++)
                {
                    float a = k * Mathf.PI * 2f / 5f;
                    var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                    mb.Box(rim, dir * 0.12f + Vector3.up * side * (W / 2f - 0.015f), dir, new Vector3(0.05f, 0.02f, 0.2f), false);
                }
            var mesh = new Mesh { name = "Wheel" }; mesh.SetVertices(mb.v); mesh.SetUVs(0, mb.uv);
            var mats = new List<Material>(mb.tris.Keys); mesh.subMeshCount = 2;
            mesh.SetTriangles(mb.tris[tyre], 0); mesh.SetTriangles(mb.tris[rim], 1);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, Built + "car_wheel.asset");
            return mesh;
        }
    }
}

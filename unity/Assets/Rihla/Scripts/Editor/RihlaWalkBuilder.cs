using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Rihla.EditorTools
{
    /// <summary>
    /// Builds Assets/Scenes/E2_AlQuds_Walk.unity, the walk from Bab al-Asbat to al-Masjid al-Aqsa, at true scale
    /// from Generated/walk.json (OpenStreetMap, made by tools/build-walk.ts). Everything is a block-out: outlines
    /// are real, heights missing from OSM and the shapes of the domes are estimates (NEEDS_REVIEW.md H17).
    /// </summary>
    public static partial class RihlaWalkBuilder
    {
        public const string ScenePath = "Assets/Scenes/E2_AlQuds_Walk.unity";
        const string Built = "Assets/Rihla/Built/Walk/";
        /// <summary>Height of the raised platform of the Dome of the Rock above the courtyard (estimate, H17).</summary>
        const float Platform = 4f;
        const float WallHeight = 12f, WallThickness = 3f;
        /// <summary>A gate or covered passage: 4.4 m wide, the pointed arch springing at 3.5 m, solid above 6.4 m (estimates, H17).</summary>
        const float ArchHalf = 2.2f, ArchSpring = 3.5f, ArchTop = 6.4f;

        [MenuItem("Rihla/Build E2 walk (Al-Quds)")]
        public static void Build()
        {
            var json = JObject.Parse(File.ReadAllText("Assets/Rihla/Generated/walk.json"));
            Directory.CreateDirectory(Built);
            pending.Clear();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Lighting();
            var streets = json["streets"].Select(s => new Street
            {
                passage = (bool)s["passage"], width = (float)s["width"],
                line = s["line"].Select(V2).ToList(),
            }).ToList();
            var platformWall = json["walls"].First(w => (string)w["name"] == "rock-platform");
            var platform = Ccw(platformWall["line"].Select(V2).ToList());
            if ((platform[0] - platform[platform.Count - 1]).sqrMagnitude < 0.01f) platform.RemoveAt(platform.Count - 1);
            var compound = Ccw(json["compound"].Select(V2).ToList());

            Ground(compound, streets);
            Prism(Mesh("Rock platform", Mat("platform", new Color(0.83f, 0.79f, 0.7f)), true), platform, 0f, Platform);
            Walls(json, streets);
            var points = json["points"];
            var dome = V2(points["domeOfTheRock"]); var qibli = V2(points["alQibli"]); var gateC = V2(points["babAlAsbat"]);
            JToken domeB = null, qibliB = null, chainB = null, gateB = null;
            Buildings(json, streets, platform, ref domeB, ref qibliB, ref chainB, ref gateB);
            Trees(json, platform);
            Facades(streets);
            Roofs();
            var todayOnly = LaneLife(streets);

            var domeRenderer = DomeOfTheRock(Ring(domeB), out float domeR);
            DomeOfTheChain(Ring(chainB));
            var qibliRing = Ring(qibliB);
            Qibli(qibliRing, out Vector3 qibliFront);
            var gateRing = Ring(gateB);
            float gateEast = gateRing.Max(p => p.x);
            Lions(gateEast, gateC.y);

            // The raised platform: where the line north-south through the Dome crosses its edge, put stairs.
            float north = Cross(platform, dome.x, true), south = Cross(platform, dome.x, false);
            Stairs(new Vector3(dome.x, 0f, north + 8f), new Vector3(dome.x, Platform, north - 0.5f));
            Stairs(new Vector3(dome.x, 0f, south - 8f), new Vector3(dome.x, Platform, south + 0.5f));

            // Jiddo's route and the steps of content/walk.json along it.
            var path = new List<Vector3>
            {
                new Vector3(gateEast + 7f, 0f, gateC.y + 1.5f),                                  // 0 outsideGate
                P(3.6f, 0.4f), P(-7.6f, -1.3f),                                                   // 2 insideGate
                P(-6.2f, -8f), P(-2.2f, -13.9f), P(-1.1f, -27.2f), P(-0.4f, -42.4f), P(0.1f, -61.9f), P(0.7f, -70f),
                P(1.2f, -77.7f),                                                                  // 9 haramGate
                P(-35.8f, -142.9f), P(-39.9f, -150.1f),
                new Vector3(dome.x, 0f, north + 9f),
                new Vector3(dome.x, Platform, north - 6f),                                        // 13 platformTop
                new Vector3(dome.x, Platform, dome.y + domeR + 4f),                               // 14 domeOfTheRock
                new Vector3(dome.x + domeR + 6f, Platform, dome.y + domeR + 4f),                 // around the octagon,
                new Vector3(dome.x + domeR + 6f, Platform, dome.y - domeR - 4f),                 // not through it
                new Vector3(dome.x, Platform, south + 6f),
                new Vector3(dome.x, 0f, south - 9f),
                qibliFront + new Vector3(0f, 0f, 4f),                                             // 19 alQibli
                qibliFront + new Vector3(6f, 0f, 7f),                                             // 20 end
            };
            int[] stepAt = { 0, 2, 9, 13, 14, 19, 20 };

            // Clues: the lions on the gate, the tiles of the Dome of the Rock, the dome of the Qibli Mosque.
            var lionsSpot = Marker("Clue: lions", new Vector3(gateEast + 5f, 0f, gateC.y));
            var tilesSpot = Marker("Clue: tiles", new Vector3(dome.x, Platform, dome.y + domeR + 2f));
            var qibliSpot = Marker("Clue: Qibli dome", qibliFront);

            RihlaUIBuilder.AddContent();
            RihlaUIBuilder.AddUI();
            var (child, cam) = Player(path[0] + new Vector3(10f, 0f, 2f));
            var jiddo = Jiddo(path[0]);
            Townspeople(streets, platform, compound, path, child.transform, new[] { Ring(domeB), qibliRing, Ring(chainB) });

            var lens = new GameObject("Time Lens").AddComponent<TimeLens>();
            lens.thenLook = ThenLook(lens.gameObject);
            lens.changing = new[] { domeRenderer };
            lens.nowColors = new[] { new Color(0.93f, 0.72f, 0.28f) }; lens.nowMetal = new[] { 0.9f };
            lens.thenColors = new[] { new Color(0.27f, 0.28f, 0.3f) }; lens.thenMetal = new[] { 0.35f };
            lens.onlyThen = new GameObject[0]; lens.onlyNow = new[] { todayOnly };

            var sound = cam.gameObject.AddComponent<Soundscape>();
            sound.child = child; sound.jiddo = jiddo; sound.lens = lens;
            var dir = new GameObject("Walk").AddComponent<WalkDirector>();
            dir.child = child; dir.cam = cam; dir.jiddo = jiddo; dir.lens = lens;
            dir.path = path.ToArray(); dir.stepAt = stepAt;
            dir.clueSpots = new[] { lionsSpot, tilesSpot, qibliSpot };
            dir.compound = compound.ToArray();
            dir.domeOfTheRock = new Vector3(dome.x, Platform, dome.y); dir.alQibli = new Vector3(qibli.x, 0f, qibli.y);
            dir.babAlAsbat = new Vector3(gateC.x, 0f, gateC.y);

            Flush();
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ScenePath);
            RihlaScenes.UpdateBuildSettings();
            AssetDatabase.SaveAssets();
            Debug.Log($"[Rihla] E2 walk built: {json["buildings"].Count()} buildings, route {path.Count} points, platform {north:0}..{south:0} m");

            Vector3 P(float x, float z) => new Vector3(x, 0f, z);
        }

        class Street { public bool passage; public float width; public List<Vector2> line; }

        static Vector2 V2(JToken t) => new Vector2((float)t[0], (float)t[1]);
        static List<Vector2> Ring(JToken b) => Ccw(b["footprint"].Select(V2).ToList());

        // ------------------------------------------------------------------ scene pieces

        static void Lighting()
        {
            // Early morning: the sun low in the east, light travelling west.
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional; sun.color = new Color(1f, 0.87f, 0.72f); sun.intensity = 1.4f;
            // 38 degrees up, to match the sun in the photographed sky below.
            sun.shadows = LightShadows.Soft; sun.shadowStrength = 0.8f; sun.transform.rotation = Quaternion.Euler(38f, -72f, 0f);
            RenderSettings.sun = sun;
            // A photographed morning sky with clouds (Poly Haven, CC0), turned so its sun sits where the scene's
            // sun is: in the file the sun is at u = 0.5996 (35.9 degrees from the panorama's centre), the scene's
            // sun at -18 degrees, so the sky turns by -17.9 degrees.
            const string hdr = Tex + "Sky/kloofendal_38d_partly_cloudy_puresky_2k.hdr";
            var hdrImp = AssetImporter.GetAtPath(hdr) as TextureImporter;
            if (hdrImp && (hdrImp.mipmapEnabled || hdrImp.wrapModeV != TextureWrapMode.Clamp))
            {
                hdrImp.mipmapEnabled = false; hdrImp.wrapModeU = TextureWrapMode.Repeat; hdrImp.wrapModeV = TextureWrapMode.Clamp;
                hdrImp.maxTextureSize = 2048; hdrImp.textureCompression = TextureImporterCompression.CompressedHQ;
                hdrImp.SaveAndReimport();
            }
            string skyPath = Built + "Sky.mat";
            var sky = AssetDatabase.LoadAssetAtPath<Material>(skyPath);
            if (!sky) { sky = new Material(Shader.Find("Skybox/Panoramic")); AssetDatabase.CreateAsset(sky, skyPath); }
            sky.shader = Shader.Find("Skybox/Panoramic");
            sky.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>(hdr));
            sky.SetFloat("_Mapping", 1f); sky.SetFloat("_ImageType", 0f); sky.SetFloat("_Rotation", 342.1f);
            sky.SetFloat("_Exposure", 0.95f); sky.SetColor("_Tint", new Color(0.5f, 0.5f, 0.5f));
            EditorUtility.SetDirty(sky);
            RenderSettings.skybox = sky;
            // Light bouncing off the pale stone fills the shadows of the lanes with warmth: sky above, warm stone around.
            RenderSettings.ambientMode = AmbientMode.Trilight;
            // R4: a cooler, brighter sky fill (shade in the lanes is lit mostly by the blue sky), warm from below.
            RenderSettings.ambientSkyColor = new Color(0.74f, 0.81f, 0.95f);
            RenderSettings.ambientEquatorColor = new Color(0.76f, 0.74f, 0.7f);
            RenderSettings.ambientGroundColor = new Color(0.66f, 0.57f, 0.46f);
            // The stored ambient probe only changes when lighting is baked, so the game sets it from these colours.
            var fill = sun.gameObject.AddComponent<SkyFill>();
            fill.sky = RenderSettings.ambientSkyColor; fill.horizon = RenderSettings.ambientEquatorColor; fill.ground = RenderSettings.ambientGroundColor;
            Look();
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.86f, 0.81f, 0.72f); RenderSettings.fogStartDistance = 180f; RenderSettings.fogEndDistance = 1100f;
        }

        /// <summary>The everyday look ("now"): filmic tone mapping, a little glow, warm morning colour.</summary>
        static void Look()
        {
            string path = Built + "Look.asset";
            AssetDatabase.DeleteAsset(path);
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);
            var tm = profile.Add<Tonemapping>(true); tm.mode.Override(TonemappingMode.ACES);
            var bl = profile.Add<Bloom>(true); bl.intensity.Override(0.3f); bl.threshold.Override(1.1f);
            var ca = profile.Add<ColorAdjustments>(true); ca.postExposure.Override(0.35f); ca.contrast.Override(8f); ca.saturation.Override(-4f);
            var wb = profile.Add<WhiteBalance>(true); wb.temperature.Override(5f);
            var vg = profile.Add<Vignette>(true); vg.intensity.Override(0.18f); vg.smoothness.Override(0.6f);
            foreach (var c in profile.components) AssetDatabase.AddObjectToAsset(c, profile);
            EditorUtility.SetDirty(profile);
            var go = new GameObject("Look (now)");
            var vol = go.AddComponent<Volume>(); vol.isGlobal = true; vol.priority = 0f; vol.sharedProfile = profile;
        }

        static void Ground(List<Vector2> compound, List<Street> streets)
        {
            // The ground as a mesh in metres, so its stone paving repeats at the right size.
            var ground = Mesh("Ground", Mat("ground", new Color(0.66f, 0.6f, 0.5f)), true);
            Cap(ground, new List<Vector2> { new Vector2(-950f, -1050f), new Vector2(650f, -1050f), new Vector2(650f, 550f), new Vector2(-950f, 550f) }, 0f);
            var court = Mesh("Courtyard of al-Masjid al-Aqsa", Mat("courtyard", new Color(0.86f, 0.82f, 0.74f)), false);
            Cap(court, compound, 0.02f);
            var paving = Mesh("Streets", Mat("street", new Color(0.78f, 0.74f, 0.66f)), false);
            foreach (var s in streets)
                for (int i = 0; i < s.line.Count - 1; i++)
                {
                    Vector2 a = s.line[i], b = s.line[i + 1], d = (b - a).normalized, n = new Vector2(-d.y, d.x) * s.width * 0.5f;
                    Cap(paving, Ccw(new List<Vector2> { a - n, b - n, b + n, a + n }), 0.04f);
                }
        }

        static void Walls(JObject json, List<Street> streets)
        {
            var mb = Mesh("City walls", Mat("wall", new Color(0.8f, 0.74f, 0.62f)), true);
            var low = Mesh("Retaining walls", Mat("wall", new Color(0.8f, 0.74f, 0.62f)), true);
            var merlons = Mesh("Battlements", Mat("wall", new Color(0.8f, 0.74f, 0.62f)), false);
            foreach (var w in json["walls"])
            {
                if ((string)w["name"] == "rock-platform") continue;
                bool city = (string)w["kind"] == "city";
                var line = w["line"].Select(V2).ToList();
                for (int i = 0; i < line.Count - 1; i++)
                    foreach (var (a, b) in Gaps(line[i], line[i + 1], streets))
                    {
                        Segment(city ? mb : low, a, b, city ? WallThickness : 1f, 0f, city ? WallHeight : 1.2f);
                        if (city) Merlons(merlons, a, b);
                    }
            }
        }

        /// <summary>Battlements along the top of a city wall, on the side away from the Old City (outside).</summary>
        static void Merlons(MB mb, Vector2 a, Vector2 b)
        {
            Vector2 d = b - a; float len = d.magnitude; if (len < 1.5f) return; d /= len;
            Vector2 n = new Vector2(-d.y, d.x);
            Vector2 mid = (a + b) / 2f, city = new Vector2(-450f, -350f); // roughly the middle of the Old City
            if (((mid + n) - city).sqrMagnitude < ((mid - n) - city).sqrMagnitude) n = -n;
            for (float t = 0.6f; t + 1f < len; t += 2.2f)
            {
                Vector2 p = a + d * t + n * (WallThickness / 2f - 0.4f);
                Segment(mb, p, p + d * 1f, 0.8f, WallHeight, WallHeight + 1.2f);
            }
        }

        /// <summary>Splits a wall segment so that every street crossing it passes through a 5 m opening.</summary>
        static IEnumerable<(Vector2, Vector2)> Gaps(Vector2 a, Vector2 b, List<Street> streets)
        {
            var cuts = new List<float>();
            foreach (var s in streets)
                for (int i = 0; i < s.line.Count - 1; i++)
                    if (Intersect(a, b, s.line[i], s.line[i + 1], out float t)) cuts.Add(t);
            float len = (b - a).magnitude; if (len < 0.01f) yield break;
            float half = 2.5f / len, from = 0f;
            foreach (float t in cuts.OrderBy(v => v))
            {
                if (t - half > from) yield return (Vector2.Lerp(a, b, from), Vector2.Lerp(a, b, t - half));
                from = Mathf.Max(from, t + half);
            }
            if (from < 1f) yield return (Vector2.Lerp(a, b, from), b);
        }

        static bool Intersect(Vector2 p, Vector2 p2, Vector2 q, Vector2 q2, out float t)
        {
            Vector2 r = p2 - p, s = q2 - q; float den = r.x * s.y - r.y * s.x; t = 0f;
            if (Mathf.Abs(den) < 1e-6f) return false;
            Vector2 qp = q - p; t = (qp.x * s.y - qp.y * s.x) / den; float u = (qp.x * r.y - qp.y * r.x) / den;
            return t >= 0f && t <= 1f && u >= 0f && u <= 1f;
        }

        static readonly List<(List<Vector2> ring, float base0, float h, long id)> houses = new List<(List<Vector2>, float, float, long)>();

        static void Buildings(JObject json, List<Street> streets, List<Vector2> platform, ref JToken dome, ref JToken qibli, ref JToken chain, ref JToken gate)
        {
            houses.Clear();
            var tones = new[]
            {
                Mesh("Buildings (limestone 1)", Mat("lime1", new Color(0.86f, 0.8f, 0.68f)), true),
                Mesh("Buildings (limestone 2)", Mat("lime2", new Color(0.8f, 0.73f, 0.6f)), true),
                Mesh("Buildings (limestone 3)", Mat("lime3", new Color(0.9f, 0.86f, 0.77f)), true),
            };
            var gates = Mesh("Gates", Mat("gate", new Color(0.76f, 0.69f, 0.56f)), true);
            foreach (var b in json["buildings"])
            {
                string lm = (string)b["landmark"];
                if (lm == "dome-of-the-rock") { dome = b; continue; }
                if (lm == "al-qibli") { qibli = b; continue; }
                if (lm == "dome-of-the-chain") { chain = b; continue; }
                if (lm == "bab-al-asbat") gate = b;
                var ring = Ring(b);
                if (ring.Count < 3) continue;
                long id = (long)b["id"];
                Vector2 c = new Vector2(ring.Average(p => p.x), ring.Average(p => p.y));
                float base0 = Inside(platform, c) ? Platform : 0f;
                float h = b["height"]?.Type == JTokenType.Float || b["height"]?.Type == JTokenType.Integer ? (float)b["height"] : 6.5f + (id % 5);
                bool arch = (bool)b["gate"] || streets.Any(s => s.passage && Crosses(ring, s.line));
                if (arch) ArchedPassage(gates, ring, c, streets, base0, Mathf.Max(h, ArchTop + 3f));
                else { Prism(tones[id % 3], ring, base0, base0 + h); houses.Add((ring, base0, h, id)); }
            }
        }


        /// <summary>
        /// A gate (or a building over a street) with a pointed-arch passage along the street: solid piers either
        /// side, solid above the arch, and the arch itself, so the child walks through it.
        /// </summary>
        static void ArchedPassage(MB mb, List<Vector2> ring, Vector2 centre, List<Street> streets, float base0, float h)
        {
            // The street that runs through (or nearest to) the building gives the passage's line.
            Vector2 a0 = default, b0 = default; float best = float.MaxValue;
            foreach (var st in streets)
                for (int i = 0; i < st.line.Count - 1; i++)
                {
                    Vector2 a = st.line[i], b = st.line[i + 1];
                    float d = DistToSeg(centre, a, b) - (st.passage ? 50f : 0f);
                    if (d < best) { best = d; a0 = a; b0 = b; }
                }
            Vector2 dir = (b0 - a0).normalized, n = new Vector2(-dir.y, dir.x);
            Vector2 c = ClosestOnSeg(centre, a0, b0);
            var left = Clip(ring, c, -n, ArchHalf); var right = Clip(ring, c, n, ArchHalf);
            var mid = Clip(Clip(ring, c, n, -ArchHalf), c, -n, -ArchHalf);
            if (left.Count >= 3) Prism(mb, Ccw(left), base0, base0 + h);
            if (right.Count >= 3) Prism(mb, Ccw(right), base0, base0 + h);
            if (mid.Count < 3) return;
            Prism(mb, Ccw(mid), base0 + ArchTop, base0 + h);
            float t0 = mid.Min(p => Vector2.Dot(p - c, dir)), t1 = mid.Max(p => Vector2.Dot(p - c, dir));
            // The arch: the space between a pointed arch and the solid above, extruded along the passage.
            var prof = new List<Vector2> { new Vector2(-ArchHalf, ArchSpring) };
            float r = ArchHalf * 1.3f, off = ArchHalf * 0.3f;
            for (int k = 1; k <= 8; k++) { float x = -ArchHalf + ArchHalf * k / 8f; prof.Add(new Vector2(x, ArchSpring + Mathf.Sqrt(Mathf.Max(0f, r * r - (x - off) * (x - off))))); }
            for (int k = 7; k >= 0; k--) { float x = ArchHalf - ArchHalf * k / 8f; prof.Add(new Vector2(x, ArchSpring + Mathf.Sqrt(Mathf.Max(0f, r * r - (x + off) * (x + off))))); }
            prof.Add(new Vector2(ArchHalf, ArchTop)); prof.Add(new Vector2(-ArchHalf, ArchTop));
            for (int i = 0; i < prof.Count; i++) prof[i] = new Vector2(prof[i].x, Mathf.Min(prof[i].y, ArchTop));
            Extrude(mb, prof, c, n, dir, t0, t1, base0);
        }

        /// <summary>
        /// Trees where OpenStreetMap maps one, and its grass and garden areas. OSM does not say the species, so
        /// they are drawn as a mix of olive (wide, grey-green) and cypress (tall, dark) shapes (estimate, H17).
        /// </summary>
        static void Trees(JObject json, List<Vector2> platform)
        {
            var grass = Mesh("Grass", Mat("grass", new Color(0.42f, 0.46f, 0.28f)), false);
            foreach (var g in json["greens"] ?? new JArray())
            {
                var ring = Ccw(g.Select(V2).ToList());
                if (ring.Count < 3) continue;
                Vector2 c = new Vector2(ring.Average(p => p.x), ring.Average(p => p.y));
                Cap(grass, ring, (Inside(platform, c) ? Platform : 0f) + 0.06f);
            }
            var trunks = Mesh("Tree trunks", Mat("bark", new Color(0.33f, 0.27f, 0.21f)), true);
            var olive = Mesh("Olive crowns (core)", Mat("olive", new Color(0.3f, 0.34f, 0.24f), 0.05f), false);
            var cypress = Mesh("Cypress crowns (core)", Mat("cypress", new Color(0.12f, 0.19f, 0.11f), 0.05f), false);
            var oliveLeaves = Mesh("Olive leaves", Foliage("oliveleaf", new Color(0.86f, 0.92f, 0.78f)), false);
            var cypressLeaves = Mesh("Cypress leaves", Foliage("cypressleaf", new Color(0.36f, 0.5f, 0.33f)), false);
            int i = 0;
            foreach (var t in json["trees"] ?? new JArray())
            {
                Vector2 p = V2(t); i++;
                float y = Inside(platform, p) ? Platform : 0f;
                float r = Hash(p) ; // 0..1, the same each build
                var at = new Vector3(p.x, y, p.y);
                if (r < 0.3f)
                {
                    float h = 8f + r * 10f;
                    Cylinder(trunks, at, 0.18f, 1.2f, 6);
                    Ellipsoid(cypress, at + Vector3.up * (1f + h / 2f), new Vector3(0.75f, h / 2f * 0.9f, 0.75f), 8, 7);
                    LeafCrown(cypressLeaves, at + Vector3.up * (1f + h / 2f), new Vector3(1.15f, h / 2f, 1.15f), Mathf.RoundToInt(h * 9f), 0.8f, r);
                }
                else
                {
                    float s = 0.8f + r * 0.6f;
                    Cylinder(trunks, at, 0.22f * s, 1.7f * s, 6);
                    Ellipsoid(olive, at + new Vector3(0f, 2.6f * s, 0f), new Vector3(1.7f, 0.9f, 1.7f) * s, 8, 6);
                    LeafCrown(oliveLeaves, at + new Vector3(0f, 2.6f * s, 0f), new Vector3(2.5f, 1.35f, 2.5f) * s, 60, 1.1f, r);
                    LeafCrown(oliveLeaves, at + new Vector3(0.9f, 2.2f * s, 0.5f) * s, new Vector3(1.6f, 1.05f, 1.6f) * s, 22, 0.9f, r + 0.31f);
                    LeafCrown(oliveLeaves, at + new Vector3(-0.8f, 2.3f * s, -0.6f) * s, new Vector3(1.5f, 0.95f, 1.5f) * s, 20, 0.9f, r + 0.67f);
                }
            }
        }

        static float Hash(Vector2 p) { float h = Mathf.Sin(p.x * 12.9898f + p.y * 78.233f) * 43758.5453f; return h - Mathf.Floor(h); }

        static void Cylinder(MB mb, Vector3 at, float r, float h, int segs)
        {
            var ring = new List<Vector2>();
            for (int k = 0; k < segs; k++) { float a = k * Mathf.PI * 2f / segs; ring.Add(new Vector2(at.x + Mathf.Cos(a) * r, at.z + Mathf.Sin(a) * r)); }
            Prism(mb, Ccw(ring), at.y, at.y + h);
        }

        static void Ellipsoid(MB mb, Vector3 c, Vector3 radii, int segs, int rings)
        {
            int s0 = mb.v.Count;
            for (int j = 0; j <= rings; j++)
            {
                float v = Mathf.PI * j / rings, y = Mathf.Cos(v), rr = Mathf.Sin(v);
                for (int k = 0; k <= segs; k++)
                {
                    float u = 2f * Mathf.PI * k / segs;
                    var n = new Vector3(Mathf.Cos(u) * rr, y, Mathf.Sin(u) * rr);
                    mb.v.Add(c + Vector3.Scale(n, radii)); mb.uv.Add(new Vector2(k / (float)segs, j / (float)rings));
                }
            }
            for (int j = 0; j < rings; j++)
                for (int k = 0; k < segs; k++)
                {
                    int a = s0 + j * (segs + 1) + k, b = a + segs + 1;
                    mb.t.AddRange(new[] { a, a + 1, b, a + 1, b + 1, b });
                }
        }

        // ------------------------------------------------------------------ building detail (E3)

        /// <summary>
        /// Doors and windows on the walls of houses that face a street or lane: an arched doorway at street level
        /// and small windows above, each set in a lighter stone frame. Sizes and spacing are estimates (H17).
        /// </summary>
        static void Facades(List<Street> streets)
        {
            var doors = Mesh("Doors", Mat("door", new Color(0.32f, 0.22f, 0.14f), 0.25f), false);
            var shutters = Mesh("Shop shutters", Mat("shutter", new Color(0.22f, 0.36f, 0.33f), 0.35f, 0.3f), false);
            var windows = Mesh("Windows", Mat("window", new Color(0.06f, 0.06f, 0.07f), 0.7f), false);
            var frames = Mesh("Stone frames", Mat("frame", new Color(0.93f, 0.88f, 0.78f), 0.2f), false);
            var segs = new List<(Vector2 a, Vector2 b)>();
            foreach (var st in streets) for (int i = 0; i < st.line.Count - 1; i++) segs.Add((st.line[i], st.line[i + 1]));
            int n = 0;
            openings.Clear(); windowsAt.Clear(); streetWalls.Clear();
            foreach (var (ring, base0, h, id) in houses)
            {
                for (int i = 0; i < ring.Count; i++)
                {
                    Vector2 a = ring[i], b = ring[(i + 1) % ring.Count];
                    float len = (b - a).magnitude; if (len < 2.5f) continue;
                    Vector2 d = (b - a) / len, outN = new Vector2(d.y, -d.x), mid = (a + b) / 2f;
                    // Does this wall face a street close by?
                    bool faces = false;
                    foreach (var (sa, sb) in segs)
                    {
                        Vector2 q = ClosestOnSeg(mid, sa, sb), to = q - mid;
                        if (to.sqrMagnitude < 36f && Vector2.Dot(to, outN) > 0.3f) { faces = true; break; }
                    }
                    if (!faces) continue;
                    n++;
                    streetWalls.Add((a, d, outN, len, base0, h));
                    // Ground floor: a doorway, or a shop with a painted shutter, every 6-9 m.
                    int bays = Mathf.Max(1, Mathf.FloorToInt(len / 7f));
                    for (int k = 0; k < bays; k++)
                    {
                        float u = len * (k + 0.5f) / bays;
                        bool shop = Hash(mid + d * u) < 0.35f;
                        float w = shop ? 2.2f : 1.3f, top = shop ? 2.6f : 2.3f;
                        if (u - w / 2f < 0.4f || u + w / 2f > len - 0.4f) continue;
                        Panel(frames, a, d, outN, u, w + 0.5f, 0f, top + 0.35f, base0, 0.03f, true);
                        Panel(shop ? shutters : doors, a, d, outN, u, w, 0f, top, base0, 0.06f, true);
                        openings.Add(new Opening { a = a, d = d, outN = outN, u = u, w = w, top = top, base0 = base0, shop = shop });
                    }
                    // Upper floors: windows every 3.2 m, from 4 m up, if the house is tall enough.
                    for (float y = 4f; y + 1.3f < h - 0.6f; y += 3.4f)
                        for (float u = 1.6f; u + 1.2f < len; u += 3.2f)
                        {
                            Panel(frames, a, d, outN, u, 1.1f, y - 0.15f, y + 1.35f, base0, 0.03f, false);
                            Panel(windows, a, d, outN, u, 0.75f, y, y + 1.15f, base0, 0.06f, false);
                            windowsAt.Add(new WindowAt { a = a, d = d, outN = outN, u = u, y = y, base0 = base0 });
                        }
                }
            }
            Debug.Log($"[Rihla] facades on {n} street-facing walls");
        }

        /// <summary>
        /// A flat panel on a wall, centred at u along it, from y0 to y1, standing out by "off"; with a pointed top
        /// when arched. Drawn on both sides so its facing never matters.
        /// </summary>
        static void Panel(MB mb, Vector2 a, Vector2 d, Vector2 outN, float u, float w, float y0, float y1, float base0, float off, bool arched)
        {
            var prof = new List<Vector2> { new Vector2(-w / 2f, y0), new Vector2(w / 2f, y0) };
            if (arched)
            {
                float spring = y1 - w * 0.45f;
                prof.Add(new Vector2(w / 2f, spring));
                for (int k = 1; k < 6; k++) { float t = k / 6f; float x = Mathf.Lerp(w / 2f, 0f, t); prof.Add(new Vector2(x, spring + (y1 - spring) * Mathf.Sin(t * Mathf.PI / 2f))); }
                prof.Add(new Vector2(0f, y1));
                for (int k = 5; k > 0; k--) { float t = k / 6f; float x = Mathf.Lerp(-w / 2f, 0f, t); prof.Add(new Vector2(x, spring + (y1 - spring) * Mathf.Sin(t * Mathf.PI / 2f))); }
                prof.Add(new Vector2(-w / 2f, spring));
            }
            else { prof.Add(new Vector2(w / 2f, y1)); prof.Add(new Vector2(-w / 2f, y1)); }
            var tris = EarClip(prof);
            Vector2 c = a + d * u + outN * off;
            // Each side gets its own vertices, so their lighting directions do not cancel out.
            for (int side = 0; side < 2; side++)
            {
                int s0 = mb.v.Count;
                foreach (var q in prof) { Vector2 g = c + d * q.x; mb.v.Add(new Vector3(g.x, base0 + q.y, g.y)); mb.uv.Add(q); }
                for (int i = 0; i < tris.Count; i += 3)
                    if (side == 0) mb.t.AddRange(new[] { s0 + tris[i], s0 + tris[i + 1], s0 + tris[i + 2] });
                    else mb.t.AddRange(new[] { s0 + tris[i], s0 + tris[i + 2], s0 + tris[i + 1] });
            }
        }

        /// <summary>
        /// Roofs: a low parapet around every flat roof, and on some houses a small plastered dome, as on many
        /// old houses of Al-Quds (which houses have them is not known from the map: an estimate, H17).
        /// </summary>
        static void Roofs()
        {
            var parapets = Mesh("Roof parapets", Mat("lime3", Color.white), false);
            var domes = Mesh("Roof domes", Mat("roofdome", new Color(0.92f, 0.9f, 0.85f), 0.1f), false);
            int domeCount = 0;
            foreach (var (ring, base0, h, id) in houses)
            {
                for (int i = 0; i < ring.Count; i++)
                {
                    Vector2 a = ring[i], b = ring[(i + 1) % ring.Count], d = b - a;
                    if (d.sqrMagnitude < 0.25f) continue;
                    Vector2 inN = new Vector2(-d.y, d.x).normalized * 0.15f;
                    Segment(parapets, a + inN, b + inN, 0.3f, base0 + h, base0 + h + 0.7f);
                }
                float area = 0f; for (int i = 0; i < ring.Count; i++) area += Cross2(ring[i], ring[(i + 1) % ring.Count]) / 2f;
                Vector2 c = new Vector2(ring.Average(p => p.x), ring.Average(p => p.y));
                if (area > 35f && area < 400f && Inside(ring, c) && Hash(c * 1.7f) < 0.4f)
                {
                    float r = Mathf.Clamp(Mathf.Sqrt(area) * 0.22f, 1.5f, 3.2f);
                    float inner = float.MaxValue; for (int i = 0; i < ring.Count; i++) inner = Mathf.Min(inner, DistToSeg(c, ring[i], ring[(i + 1) % ring.Count]));
                    r = Mathf.Min(r, inner - 0.4f);
                    if (r < 1.2f) continue;
                    Cylinder(domes, new Vector3(c.x, base0 + h, c.y), r, 0.5f, 12);
                    Ellipsoid(domes, new Vector3(c.x, base0 + h + 0.5f, c.y), new Vector3(r, r * 0.75f, r), 12, 6);
                    domeCount++;
                }
            }
            Debug.Log($"[Rihla] roofs: parapets on {houses.Count} houses, {domeCount} small domes");
        }

        static float DistToSeg(Vector2 p, Vector2 a, Vector2 b) => (p - ClosestOnSeg(p, a, b)).magnitude;
        static Vector2 ClosestOnSeg(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a; float t = ab.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return a + ab * t;
        }

        /// <summary>Keeps the part of a ring where dot(p - c, n) >= d (Sutherland-Hodgman).</summary>
        static List<Vector2> Clip(List<Vector2> ring, Vector2 c, Vector2 n, float d)
        {
            var o = new List<Vector2>();
            for (int i = 0; i < ring.Count; i++)
            {
                Vector2 a = ring[i], b = ring[(i + 1) % ring.Count];
                float da = Vector2.Dot(a - c, n) - d, db = Vector2.Dot(b - c, n) - d;
                if (da >= 0f) o.Add(a);
                if ((da >= 0f) != (db >= 0f)) o.Add(Vector2.Lerp(a, b, da / (da - db)));
            }
            return o;
        }

        /// <summary>Extrudes a profile (s across, y up) along a direction; faces are made on both sides.</summary>
        static void Extrude(MB mb, List<Vector2> prof, Vector2 c, Vector2 n, Vector2 dir, float t0, float t1, float base0)
        {
            Vector3 P(Vector2 q, float t) { Vector2 g = c + n * q.x + dir * t; return new Vector3(g.x, base0 + q.y, g.y); }
            var ccw = Ccw(new List<Vector2>(prof));
            var tris = EarClip(ccw);
            // Both sides of every face, each with its own vertices so the lighting directions do not cancel out.
            foreach (float t in new[] { t0, t1 })
                for (int side = 0; side < 2; side++)
                {
                    int s0 = mb.v.Count;
                    foreach (var q in ccw) { mb.v.Add(P(q, t)); mb.uv.Add(new Vector2(q.x, q.y)); }
                    for (int i = 0; i < tris.Count; i += 3)
                        if (side == 0) mb.t.AddRange(new[] { s0 + tris[i], s0 + tris[i + 1], s0 + tris[i + 2] });
                        else mb.t.AddRange(new[] { s0 + tris[i], s0 + tris[i + 2], s0 + tris[i + 1] });
                }
            for (int i = 0; i < ccw.Count; i++)
            {
                Vector2 a = ccw[i], b = ccw[(i + 1) % ccw.Count];
                float len = (b - a).magnitude;
                for (int side = 0; side < 2; side++)
                {
                    int s0 = mb.v.Count;
                    mb.v.Add(P(a, t0)); mb.v.Add(P(b, t0)); mb.v.Add(P(b, t1)); mb.v.Add(P(a, t1));
                    mb.uv.Add(new Vector2(0, 0)); mb.uv.Add(new Vector2(len, 0)); mb.uv.Add(new Vector2(len, t1 - t0)); mb.uv.Add(new Vector2(0, t1 - t0));
                    if (side == 0) mb.t.AddRange(new[] { s0, s0 + 1, s0 + 2, s0, s0 + 2, s0 + 3 });
                    else mb.t.AddRange(new[] { s0, s0 + 2, s0 + 1, s0, s0 + 3, s0 + 2 });
                }
            }
        }

        static bool Crosses(List<Vector2> ring, List<Vector2> line)
        {
            foreach (var p in line) if (Inside(ring, p)) return true;
            for (int i = 0; i < line.Count - 1; i++)
                for (int j = 0; j < ring.Count; j++)
                    if (Intersect(line[i], line[i + 1], ring[j], ring[(j + 1) % ring.Count], out _)) return true;
            return false;
        }

        /// <summary>The Dome of the Rock: the real octagon from OSM; marble below, tiles above, a drum and the dome.</summary>
        static MeshRenderer DomeOfTheRock(List<Vector2> ring, out float radius)
        {
            Vector2 c = new Vector2(ring.Average(p => p.x), ring.Average(p => p.y));
            radius = ring.Average(p => (p - c).magnitude);
            var root = new GameObject("Dome of the Rock").transform;
            Prism(Mesh("Marble", Mat("marble", new Color(0.93f, 0.92f, 0.89f), 0.4f), true, root), ring, Platform, Platform + 5.5f);
            Prism(Mesh("Tiles", TileMat(), true, root), ring, Platform + 5.5f, Platform + 11f);
            // R4: tall arched windows set into the tiled walls, evenly along each side, in light frames. How many
            // per side, and their size, are estimates (NEEDS_REVIEW.md H17); the real tile patterns are not copied (R5).
            var frames = Mesh("Window frames (Dome of the Rock)", Mat("domeframe", new Color(0.86f, 0.88f, 0.86f), 0.3f), false, root);
            var glass = Mesh("Windows (Dome of the Rock)", Mat("domeglass", new Color(0.05f, 0.07f, 0.1f), 0.8f), false, root);
            for (int i = 0; i < ring.Count; i++)
            {
                Vector2 a = ring[i], b = ring[(i + 1) % ring.Count]; float len = (b - a).magnitude;
                if (len < 5f) continue;
                Vector2 dd = (b - a) / len, outN = new Vector2(dd.y, -dd.x);
                int k = Mathf.Max(3, Mathf.FloorToInt(len / 3.2f));
                for (int w = 0; w < k; w++)
                {
                    float u = len * (w + 0.5f) / k;
                    Panel(frames, a, dd, outN, u, 1.75f, Platform + 6.1f, Platform + 10.3f, 0f, 0.04f, true);
                    Panel(glass, a, dd, outN, u, 1.25f, Platform + 6.4f, Platform + 9.9f, 0f, 0.07f, true);
                }
            }
            float drumR = radius * 0.42f, top = Platform + 11f;
            var drum = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            drum.name = "Drum"; drum.transform.SetParent(root); drum.transform.position = new Vector3(c.x, top + 3f, c.y);
            drum.transform.localScale = new Vector3(drumR * 2f, 3f, drumR * 2f);
            drum.GetComponent<MeshRenderer>().sharedMaterial = TileMat();
            var d = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            d.name = "Dome"; d.transform.SetParent(root); Object.DestroyImmediate(d.GetComponent<Collider>());
            d.transform.position = new Vector3(c.x, top + 6f, c.y);
            d.transform.localScale = new Vector3(drumR * 2.1f, drumR * 2.3f, drumR * 2.1f);
            var r = d.GetComponent<MeshRenderer>(); r.sharedMaterial = Mat("dome", new Color(0.93f, 0.72f, 0.28f), 0.75f, 0.9f);
            return r;
        }

        /// <summary>
        /// A plain, generic tile grid for the Dome of the Rock's tiled walls: blue tiles, light joints and a turquoise
        /// square in each. It does not copy the real patterns or the inscription, which must not be imitated (R5).
        /// </summary>
        static Material TileMat()
        {
            string texPath = Built + "tiles_generic2.png";
            if (!File.Exists(texPath))
            {
                const int N = 256; var t = new Texture2D(N, N, TextureFormat.RGB24, true);
                Color blue = new Color(0.12f, 0.33f, 0.58f), joint = new Color(0.82f, 0.85f, 0.86f), turq = new Color(0.2f, 0.62f, 0.66f);
                for (int y = 0; y < N; y++)
                    for (int x = 0; x < N; x++)
                    {
                        int u = x % 32, v = y % 32;
                        bool j = u < 2 || v < 2;
                        bool sq = Mathf.Abs(u - 16) + Mathf.Abs(v - 16) < 7;
                        // R4: each tile a little lighter or darker, as glazed tiles are.
                        float vary = 0.88f + 0.24f * Hash(new Vector2(x / 32, y / 32) * 3.7f);
                        t.SetPixel(x, y, j ? joint : (sq ? turq : blue) * vary);
                    }
                t.Apply(); File.WriteAllBytes(texPath, t.EncodeToPNG()); Object.DestroyImmediate(t);
                AssetDatabase.ImportAsset(texPath);
            }
            var m = Mat("tiles", Color.white, 0.55f);
            m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texPath));
            m.mainTextureScale = new Vector2(1f / 2.4f, 1f / 2.4f); // tiles about 30 cm across
            EditorUtility.SetDirty(m);
            return m;
        }

        static void DomeOfTheChain(List<Vector2> ring)
        {
            Vector2 c = new Vector2(ring.Average(p => p.x), ring.Average(p => p.y));
            float r = ring.Average(p => (p - c).magnitude);
            var root = new GameObject("Dome of the Chain").transform;
            Prism(Mesh("Arcade", Mat("marble", new Color(0.93f, 0.92f, 0.89f), 0.4f), true, root), ring, Platform, Platform + 4.5f);
            var d = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            d.name = "Dome"; d.transform.SetParent(root); Object.DestroyImmediate(d.GetComponent<Collider>());
            d.transform.position = new Vector3(c.x, Platform + 4.5f, c.y); d.transform.localScale = Vector3.one * r * 1.2f;
            d.GetComponent<MeshRenderer>().sharedMaterial = Mat("lead", new Color(0.55f, 0.57f, 0.6f), 0.5f, 0.4f);
        }

        /// <summary>The Qibli Mosque: its OSM outline, and a dome over the south part, near the mihrab (estimate, H17).</summary>
        static void Qibli(List<Vector2> ring, out Vector3 front)
        {
            var root = new GameObject("Al-Qibli Mosque").transform;
            Prism(Mesh("Hall", Mat("lime3", new Color(0.9f, 0.86f, 0.77f)), true, root), ring, 0f, 11f);
            float minZ = ring.Min(p => p.y), maxZ = ring.Max(p => p.y), cx = ring.Average(p => p.x);
            float width = ring.Max(p => p.x) - ring.Min(p => p.x);
            float r = Mathf.Min(9f, width * 0.22f);
            var d = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            d.name = "Dome"; d.transform.SetParent(root); Object.DestroyImmediate(d.GetComponent<Collider>());
            d.transform.position = new Vector3(cx, 11f + r * 0.25f, minZ + (maxZ - minZ) * 0.2f);
            d.transform.localScale = new Vector3(r * 2f, r * 1.6f, r * 2f);
            d.GetComponent<MeshRenderer>().sharedMaterial = Mat("lead", new Color(0.55f, 0.57f, 0.6f), 0.5f, 0.4f);
            front = new Vector3(cx, 0f, maxZ + 7f);
        }

        /// <summary>Two carved lions on each side of Bab al-Asbat's outer face (positions approximate, H17).</summary>
        static void Lions(float east, float z)
        {
            var mat = Mat("lion", new Color(0.72f, 0.64f, 0.5f));
            foreach (float side in new[] { -1f, 1f })
                foreach (float off in new[] { 0f, 1.1f })
                {
                    var l = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    l.name = "Lion relief"; Object.DestroyImmediate(l.GetComponent<Collider>());
                    l.transform.position = new Vector3(east + 0.15f, 6.2f, z + side * (2.8f + off));
                    l.transform.localScale = new Vector3(0.3f, 0.6f, 0.95f);
                    l.GetComponent<MeshRenderer>().sharedMaterial = mat;
                }
        }

        static void Stairs(Vector3 foot, Vector3 top)
        {
            var root = new GameObject("Stairs").transform;
            var mat = Mat("platform", new Color(0.83f, 0.79f, 0.7f));
            Vector3 d = top - foot; float run = new Vector2(d.x, d.z).magnitude;
            var ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ramp.name = "Ramp (collider)"; ramp.transform.SetParent(root);
            ramp.transform.position = (foot + top) / 2f - Vector3.up * 0.15f;
            ramp.transform.rotation = Quaternion.LookRotation(d);
            ramp.transform.localScale = new Vector3(10f, 0.3f, d.magnitude);
            Object.DestroyImmediate(ramp.GetComponent<MeshRenderer>());
            int n = Mathf.RoundToInt(d.y / 0.25f);
            for (int i = 0; i < n; i++)
            {
                var s = GameObject.CreatePrimitive(PrimitiveType.Cube);
                s.name = "Step"; s.transform.SetParent(root); Object.DestroyImmediate(s.GetComponent<Collider>());
                float t = (i + 0.5f) / n;
                Vector3 p = Vector3.Lerp(foot, top, t);
                s.transform.position = new Vector3(p.x, (i + 0.5f) * d.y / n / 2f, p.z);
                s.transform.rotation = Quaternion.LookRotation(new Vector3(d.x, 0f, d.z));
                s.transform.localScale = new Vector3(10f, (i + 0.5f) * d.y / n, run / n + 0.02f);
                s.GetComponent<MeshRenderer>().sharedMaterial = mat;
            }
        }

        static Transform Marker(string name, Vector3 at)
        {
            var root = new GameObject(name).transform; root.position = at;
            var mat = Mat("marker", new Color(0.95f, 0.8f, 0.4f), 0.5f, 0f, new Color(0.9f, 0.65f, 0.2f) * 2f);
            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "Glow"; ring.transform.SetParent(root, false); Object.DestroyImmediate(ring.GetComponent<Collider>());
            ring.transform.localPosition = new Vector3(0f, 0.03f, 0f); ring.transform.localScale = new Vector3(1.6f, 0.02f, 1.6f);
            ring.GetComponent<MeshRenderer>().sharedMaterial = mat;
            var beam = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            beam.name = "Beam"; beam.transform.SetParent(root, false); Object.DestroyImmediate(beam.GetComponent<Collider>());
            beam.transform.localPosition = new Vector3(0f, 1.6f, 0f); beam.transform.localScale = new Vector3(0.12f, 1.6f, 0.12f);
            beam.GetComponent<MeshRenderer>().sharedMaterial = mat;
            return root;
        }

        static (Walker, FollowCamera) Player(Vector3 at)
        {
            var child = new GameObject("Child");
            child.transform.SetPositionAndRotation(at + Vector3.up * 0.1f, Quaternion.LookRotation(Vector3.left));
            var cc = child.AddComponent<CharacterController>(); cc.height = 1.35f; cc.radius = 0.25f; cc.center = new Vector3(0f, 0.675f, 0f); cc.stepOffset = 0.4f;
            var walker = child.AddComponent<Walker>();
            child.AddComponent<FigureBody>().hero = true; // Yusuf or Maryam, built when the walk starts

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>(); cam.nearClipPlane = 0.1f; cam.farClipPlane = 3000f; cam.fieldOfView = 60f;
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;
            var follow = camGo.AddComponent<FollowCamera>();
            follow.target = child.transform; follow.distance = 3.6f; follow.height = 1.7f; follow.lookHeight = 1.25f; follow.stiffness = 6f;
            return (walker, follow);
        }

        static Transform Jiddo(Vector3 at)
        {
            var root = new GameObject("Jiddo Ibrahim").transform;
            root.SetPositionAndRotation(at, Quaternion.LookRotation(Vector3.right));
            root.gameObject.AddComponent<FigureBody>().look = FigureBody.Look.Jiddo; // thobe, kufiya and agal, grey beard
            return root;
        }

        /// <summary>
        /// People of the town on a morning walk: along the lanes near the child's route, across the courtyard of
        /// al-Aqsa, and a few standing in pairs, talking. Their dress is modest and an estimate (H18).
        /// </summary>
        static void Townspeople(List<Street> streets, List<Vector2> platform, List<Vector2> compound, List<Vector3> path, Transform child, List<Vector2>[] landmarks)
        {
            var parent = new GameObject("Townspeople").transform;
            var rng = new System.Random(1187);
            float R() => (float)rng.NextDouble();
            var looks = new[] { FigureBody.Look.Man, FigureBody.Look.Man, FigureBody.Look.ManModern, FigureBody.Look.ManModern,
                                FigureBody.Look.Woman, FigureBody.Look.Woman, FigureBody.Look.Woman, FigureBody.Look.WomanElder,
                                FigureBody.Look.ElderMan };
            var walkers = new List<Townsfolk>();
            int n = 0;
            bool Blocked(Vector2 p)
            {
                if (Inside(platform, p) || EdgeDistance(platform, p) < 2.5f) return true;
                foreach (var l in landmarks) if (Inside(l, p) || EdgeDistance(l, p) < 1.5f) return true;
                foreach (var h in houses) if (Inside(h.ring, p)) return true;
                return false;
            }
            float NearRoute(Vector2 p) => path.Min(q => (new Vector2(q.x, q.z) - p).magnitude);
            void Person(Vector3[] route, Vector3 faceTo, float speed)
            {
                var go = new GameObject("Person " + (++n));
                go.transform.SetParent(parent); go.transform.position = route[0];
                var f = go.AddComponent<FigureBody>(); f.look = looks[rng.Next(looks.Length)]; f.seed = n; f.cullDistance = 85f;
                // Elders walk slowly, with a stick.
                bool elder = f.look == FigureBody.Look.ElderMan || f.look == FigureBody.Look.WomanElder;
                var t = go.AddComponent<Townsfolk>(); t.route = route; t.faceTo = faceTo; t.speed = elder ? 0.5f + R() * 0.15f : speed; t.child = child;
                if (route.Length > 1 && !elder) walkers.Add(t);
            }

            // Along the lanes: up to 60 m of a street near the route, on one side of it so there is room to pass.
            foreach (var st in streets.OrderBy(_ => R()).ToList())
            {
                if (n >= 22) break;
                if (st.line.Count < 2 || NearRoute(st.line[st.line.Count / 2]) > 70f) continue;
                var pts = new List<Vector3>(); float len = 0f, side = (R() < 0.5f ? -1f : 1f) * Mathf.Max(0f, st.width * 0.5f - 0.7f);
                for (int i = 0; i < st.line.Count && len < 60f; i++)
                {
                    Vector2 a = st.line[i], dir = i + 1 < st.line.Count ? st.line[i + 1] - a : a - st.line[i - 1];
                    Vector2 q = a + new Vector2(-dir.normalized.y, dir.normalized.x) * side;
                    if (i > 0) len += (a - st.line[i - 1]).magnitude;
                    if (Blocked(q)) break;
                    pts.Add(new Vector3(q.x, 0f, q.y));
                }
                if (pts.Count < 2 || len < 15f) continue;
                Person(pts.ToArray(), default, 0.9f + R() * 0.4f);
            }

            // Across the courtyard: straight walks that keep off the platform and the buildings.
            var bmin = new Vector2(compound.Min(p => p.x), compound.Min(p => p.y)); var bmax = new Vector2(compound.Max(p => p.x), compound.Max(p => p.y));
            Vector2 InCompound() => new Vector2(Mathf.Lerp(bmin.x, bmax.x, R()), Mathf.Lerp(bmin.y, bmax.y, R()));
            int courtyard = 0;
            for (int tries = 0; tries < 4000 && courtyard < 14; tries++)
            {
                Vector2 a = InCompound(), b = a + Random2(R) * (20f + R() * 40f);
                bool ok = true;
                for (float u = 0f; u <= 1.001f && ok; u += 0.02f) { var q = Vector2.Lerp(a, b, u); ok = Inside(compound, q) && EdgeDistance(compound, q) > 2f && !Blocked(q); }
                if (!ok) continue;
                Person(new[] { new Vector3(a.x, 0f, a.y), new Vector3(b.x, 0f, b.y) }, default, 0.8f + R() * 0.4f);
                courtyard++;
            }

            // Standing in pairs, talking: in the courtyard near the route.
            int pairs = 0;
            for (int tries = 0; tries < 4000 && pairs < 5; tries++)
            {
                Vector2 a = InCompound(), d = Random2(R) * 0.75f;
                if (NearRoute(a) > 45f || NearRoute(a) < 5f || !Inside(compound, a) || Blocked(a - d) || Blocked(a + d)) continue;
                Vector3 p1 = new Vector3(a.x - d.x, 0f, a.y - d.y), p2 = new Vector3(a.x + d.x, 0f, a.y + d.y);
                Person(new[] { p1 }, p2, 0f); Person(new[] { p2 }, p1, 0f);
                pairs++;
            }
            // Children walking beside a parent, along the lanes and across the courtyard.
            int kids = 0;
            foreach (var w in walkers.OrderBy(_ => R()).ToList())
            {
                if (kids >= 9) break;
                var go = new GameObject("Person " + (++n) + " (child)");
                go.transform.SetParent(parent);
                float side = R() < 0.5f ? -0.7f : 0.7f;
                go.transform.position = w.route[0] + new Vector3(side, 0f, 0f);
                var f = go.AddComponent<FigureBody>(); f.look = R() < 0.5f ? FigureBody.Look.Boy : FigureBody.Look.Girl; f.seed = n; f.cullDistance = 85f;
                var t = go.AddComponent<Townsfolk>(); t.leader = w; t.beside = side; t.child = child;
                w.speed = Mathf.Min(w.speed, 0.95f); // a grown-up keeps to a child's pace
                kids++;
            }
            int elders = parent.GetComponentsInChildren<FigureBody>().Count(f => f.look == FigureBody.Look.ElderMan || f.look == FigureBody.Look.WomanElder);
            Debug.Log($"[Rihla] townspeople: {n} ({courtyard} crossing the courtyard, {pairs} pairs talking, {kids} children with a parent, {elders} elders)");
        }

        static Vector2 Random2(System.Func<float> r) { float a = r() * Mathf.PI * 2f; return new Vector2(Mathf.Cos(a), Mathf.Sin(a)); }

        static float EdgeDistance(List<Vector2> ring, Vector2 p)
        {
            float best = float.MaxValue;
            for (int i = 0; i < ring.Count; i++)
            {
                Vector2 a = ring[i], b = ring[(i + 1) % ring.Count], ab = b - a;
                float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(1e-6f, ab.sqrMagnitude));
                best = Mathf.Min(best, (a + ab * t - p).magnitude);
            }
            return best;
        }

        /// <summary>The old-photograph look of the Time Lens: faded warm colour, vignette and grain.</summary>
        static Volume ThenLook(GameObject on)
        {
            string path = Built + "ThenLook.asset";
            AssetDatabase.DeleteAsset(path);
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);
            var ca = profile.Add<ColorAdjustments>(true);
            ca.saturation.Override(-70f); ca.contrast.Override(14f); ca.postExposure.Override(0.15f);
            ca.colorFilter.Override(new Color(1f, 0.87f, 0.7f));
            var v = profile.Add<Vignette>(true); v.intensity.Override(0.38f); v.smoothness.Override(0.5f);
            var g = profile.Add<FilmGrain>(true); g.intensity.Override(0.35f); g.type.Override(FilmGrainLookup.Medium3);
            foreach (var c in profile.components) AssetDatabase.AddObjectToAsset(c, profile);
            EditorUtility.SetDirty(profile);
            var vol = on.AddComponent<Volume>(); vol.isGlobal = true; vol.priority = 10f; vol.sharedProfile = profile; vol.weight = 1f;
            return vol;
        }

        // ------------------------------------------------------------------ meshes

        class MB
        {
            public readonly List<Vector3> v = new List<Vector3>(); public readonly List<int> t = new List<int>();
            public readonly List<Vector2> uv = new List<Vector2>();
            /// <summary>Optional normals, one per vertex; otherwise they are calculated.</summary>
            public readonly List<Vector3> n = new List<Vector3>();
            public string name; public Material mat; public bool collider; public Transform parent;
        }

        static readonly List<MB> pending = new List<MB>();

        static MB Mesh(string name, Material mat, bool collider, Transform parent = null)
        {
            var mb = new MB { name = name, mat = mat, collider = collider, parent = parent };
            pending.Add(mb);
            Flush(); // meshes already filled are written before a new one starts
            return mb;
        }

        /// <summary>Writes meshes collected so far (called before each new mesh and at the end of a build).</summary>
        static void Flush()
        {
            foreach (var mb in pending.ToList())
            {
                if (mb.v.Count == 0) continue;
                pending.Remove(mb);
                Write(mb);
            }
        }

        static void Write(MB mb)
        {
            var m = new UnityEngine.Mesh { name = mb.name, indexFormat = IndexFormat.UInt32 };
            m.SetVertices(mb.v); m.SetTriangles(mb.t, 0); m.SetUVs(0, mb.uv);
            if (mb.n.Count == mb.v.Count) m.SetNormals(mb.n); else m.RecalculateNormals();
            m.RecalculateBounds();
            string p = Built + Safe(mb.name) + ".asset";
            AssetDatabase.DeleteAsset(p); AssetDatabase.CreateAsset(m, p);
            var go = new GameObject(mb.name); if (mb.parent) go.transform.SetParent(mb.parent);
            go.AddComponent<MeshFilter>().sharedMesh = m;
            var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = mb.mat;
            if (NoShadow.Contains(mb.name)) mr.shadowCastingMode = ShadowCastingMode.Off;
            if (mb.collider) go.AddComponent<MeshCollider>().sharedMesh = m;
            go.isStatic = true;
        }

        static string Safe(string s) => new string(s.Select(ch => char.IsLetterOrDigit(ch) ? ch : '_').ToArray());

        static void Prism(MB mb, List<Vector2> ring, float y0, float y1)
        {
            for (int i = 0; i < ring.Count; i++)
            {
                Vector2 a = ring[i], b = ring[(i + 1) % ring.Count];
                int s = mb.v.Count; float len = (b - a).magnitude;
                mb.v.Add(new Vector3(a.x, y0, a.y)); mb.v.Add(new Vector3(a.x, y1, a.y)); mb.v.Add(new Vector3(b.x, y1, b.y)); mb.v.Add(new Vector3(b.x, y0, b.y));
                mb.uv.Add(new Vector2(0, y0)); mb.uv.Add(new Vector2(0, y1)); mb.uv.Add(new Vector2(len, y1)); mb.uv.Add(new Vector2(len, y0));
                mb.t.AddRange(new[] { s, s + 1, s + 2, s, s + 2, s + 3 });
            }
            Cap(mb, ring, y1);
        }

        static void Segment(MB mb, Vector2 a, Vector2 b, float thick, float y0, float y1)
        {
            Vector2 d = (b - a); if (d.sqrMagnitude < 0.01f) return; d.Normalize();
            Vector2 n = new Vector2(-d.y, d.x) * thick * 0.5f;
            Prism(mb, Ccw(new List<Vector2> { a - n, b - n, b + n, a + n }), y0, y1);
        }

        /// <summary>A flat cap facing up over a counter-clockwise ring (ear clipping, so concave outlines work).</summary>
        static void Cap(MB mb, List<Vector2> ring, float y)
        {
            int s = mb.v.Count;
            foreach (var p in ring) { mb.v.Add(new Vector3(p.x, y, p.y)); mb.uv.Add(p); }
            var tris = EarClip(ring);
            for (int i = 0; i < tris.Count; i += 3) mb.t.AddRange(new[] { s + tris[i], s + tris[i + 2], s + tris[i + 1] });
        }

        static List<int> EarClip(List<Vector2> ring)
        {
            var idx = Enumerable.Range(0, ring.Count).ToList(); var tris = new List<int>();
            int guard = 0;
            while (idx.Count > 3 && guard++ < 10000)
            {
                bool cut = false;
                for (int i = 0; i < idx.Count; i++)
                {
                    int ia = idx[(i + idx.Count - 1) % idx.Count], ib = idx[i], ic = idx[(i + 1) % idx.Count];
                    Vector2 a = ring[ia], b = ring[ib], c = ring[ic];
                    if (Cross2(b - a, c - b) <= 1e-7f) continue;
                    bool inside = false;
                    foreach (int j in idx) if (j != ia && j != ib && j != ic && InTri(ring[j], a, b, c)) { inside = true; break; }
                    if (inside) continue;
                    tris.AddRange(new[] { ia, ib, ic }); idx.RemoveAt(i); cut = true; break;
                }
                if (!cut) break;
            }
            if (idx.Count >= 3) for (int i = 1; i < idx.Count - 1; i++) tris.AddRange(new[] { idx[0], idx[i], idx[i + 1] });
            return tris;
        }

        static float Cross2(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;
        static bool InTri(Vector2 p, Vector2 a, Vector2 b, Vector2 c) => Cross2(b - a, p - a) >= 0 && Cross2(c - b, p - b) >= 0 && Cross2(a - c, p - c) >= 0;

        static List<Vector2> Ccw(List<Vector2> ring)
        {
            if (ring.Count > 1 && (ring[0] - ring[ring.Count - 1]).sqrMagnitude < 1e-4f) ring = ring.Take(ring.Count - 1).ToList();
            float a = 0f; for (int i = 0; i < ring.Count; i++) a += Cross2(ring[i], ring[(i + 1) % ring.Count]);
            if (a < 0f) ring.Reverse();
            return ring;
        }

        static bool Inside(List<Vector2> ring, Vector2 p)
        {
            bool inside = false;
            for (int i = 0, j = ring.Count - 1; i < ring.Count; j = i++)
                if ((ring[i].y > p.y) != (ring[j].y > p.y) && p.x < (ring[j].x - ring[i].x) * (p.y - ring[i].y) / (ring[j].y - ring[i].y) + ring[i].x) inside = !inside;
            return inside;
        }

        /// <summary>Where the north-south line at x crosses the ring: its northern or southern crossing.</summary>
        static float Cross(List<Vector2> ring, float x, bool northern)
        {
            var zs = new List<float>();
            for (int i = 0; i < ring.Count; i++)
            {
                Vector2 a = ring[i], b = ring[(i + 1) % ring.Count];
                if ((a.x - x) * (b.x - x) <= 0f && Mathf.Abs(a.x - b.x) > 1e-4f) zs.Add(Mathf.Lerp(a.y, b.y, (x - a.x) / (b.x - a.x)));
            }
            return northern ? zs.Max() : zs.Min();
        }

        const string Tex = "Assets/Rihla/Textures/";
        /// <summary>Material name: texture (colour, normal), metres per repeat, and the tint over it.</summary>
        static readonly Dictionary<string, (string col, string nor, float metres, Color tint)> Textured = new Dictionary<string, (string, string, float, Color)>
        {
            ["ground"] = ("PolyHaven/rock_tile_floor_diff_1k.jpg", "PolyHaven/rock_tile_floor_nor_gl_1k.jpg", 4f, new Color(1.06f, 0.98f, 0.86f)),
            ["courtyard"] = ("PolyHaven/rock_tile_floor_diff_1k.jpg", "PolyHaven/rock_tile_floor_nor_gl_1k.jpg", 4.5f, new Color(1.1f, 1.04f, 0.94f)),
            ["street"] = ("PolyHaven/rock_tile_floor_diff_1k.jpg", "PolyHaven/rock_tile_floor_nor_gl_1k.jpg", 2.2f, new Color(1.08f, 1f, 0.88f)),
            ["platform"] = ("PolyHaven/rock_tile_floor_diff_1k.jpg", "PolyHaven/rock_tile_floor_nor_gl_1k.jpg", 5f, new Color(1.08f, 1.06f, 1.02f)),
            ["wall"] = ("PolyHaven/sandstone_blocks_08_diff_1k.jpg", "PolyHaven/sandstone_blocks_08_nor_gl_1k.jpg", 4.5f, new Color(0.97f, 0.93f, 0.86f)),
            ["gate"] = ("PolyHaven/sandstone_blocks_08_diff_1k.jpg", "PolyHaven/sandstone_blocks_08_nor_gl_1k.jpg", 3f, new Color(0.97f, 0.93f, 0.85f)),
            ["lion"] = ("PolyHaven/sandstone_blocks_08_diff_1k.jpg", "PolyHaven/sandstone_blocks_08_nor_gl_1k.jpg", 1f, new Color(0.85f, 0.78f, 0.65f)),
            ["lime1"] = ("PolyHaven/sandstone_blocks_08_diff_1k.jpg", "PolyHaven/sandstone_blocks_08_nor_gl_1k.jpg", 3f, new Color(1f, 0.98f, 0.94f)),
            ["lime2"] = ("PolyHaven/white_sandstone_blocks_02_diff_1k.jpg", "PolyHaven/white_sandstone_blocks_02_nor_gl_1k.jpg", 3f, new Color(1f, 0.96f, 0.88f)),
            ["lime3"] = ("PolyHaven/white_plaster_rough_01_diff_1k.jpg", "PolyHaven/white_plaster_rough_01_nor_gl_1k.jpg", 3f, new Color(0.98f, 0.96f, 0.91f)),
            ["marble"] = ("PolyHaven/marble_01_diff_1k.jpg", "PolyHaven/marble_01_nor_gl_1k.jpg", 2f, Color.white),
        };

        /// <summary>Materials drawn with the weathered stone shader (R4): walls and houses, and paving.</summary>
        static readonly HashSet<string> WeatheredWall = new HashSet<string> { "wall", "gate", "lime1", "lime2", "lime3", "frame", "roofdome" };
        static readonly HashSet<string> WeatheredGround = new HashSet<string> { "ground", "courtyard", "street", "platform" };

        static Texture2D LoadTex(string path, bool normal)
        {
            string full = Tex + path;
            var imp = AssetImporter.GetAtPath(full) as TextureImporter;
            if (imp && normal && imp.textureType != TextureImporterType.NormalMap) { imp.textureType = TextureImporterType.NormalMap; imp.SaveAndReimport(); }
            if (imp && imp.maxTextureSize > 1024) { imp.maxTextureSize = 1024; imp.SaveAndReimport(); }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(full);
        }

        static Material Mat(string name, Color c, float smooth = 0.15f, float metal = 0f, Color? emission = null)
        {
            string p = Built + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (!m) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, p); }
            bool ground = WeatheredGround.Contains(name), weathered = ground || WeatheredWall.Contains(name);
            var shader = Shader.Find(weathered ? "Rihla/Weathered Stone" : "Universal Render Pipeline/Lit");
            if (shader && m.shader != shader) m.shader = shader;
            if (weathered)
            {
                m.SetTexture("_RihlaNoise", NoiseTexture());
                m.SetFloat("_RihlaGround", ground ? 1f : 0f);
                if (ground) m.EnableKeyword("_RIHLA_GROUND"); else m.DisableKeyword("_RIHLA_GROUND");
            }
            if (Textured.TryGetValue(name, out var tx))
            {
                c = tx.tint;
                m.SetTexture("_BaseMap", LoadTex(tx.col, false));
                m.SetTexture("_BumpMap", LoadTex(tx.nor, true)); m.EnableKeyword("_NORMALMAP"); m.SetFloat("_BumpScale", 1f);
                m.mainTextureScale = Vector2.one / tx.metres;
            }
            m.SetColor("_BaseColor", c); m.SetFloat("_Smoothness", smooth); m.SetFloat("_Metallic", metal);
            if (emission.HasValue) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", emission.Value); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None; }
            EditorUtility.SetDirty(m);
            return m;
        }
    }

    /// <summary>Keeps the build's scene list in one place: the road first, then the other scenes that exist.</summary>
    public static class RihlaScenes
    {
        public static void UpdateBuildSettings()
        {
            var list = new[] { RihlaSceneBuilder.ScenePath, RihlaCesiumBuilder.ScenePath, RihlaWalkBuilder.ScenePath }
                .Where(File.Exists).Select(p => new EditorBuildSettingsScene(p, true)).ToArray();
            EditorBuildSettings.scenes = list;
        }
    }
}

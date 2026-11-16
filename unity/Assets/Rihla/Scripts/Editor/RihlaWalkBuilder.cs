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
    public static class RihlaWalkBuilder
    {
        public const string ScenePath = "Assets/Scenes/E2_AlQuds_Walk.unity";
        const string Built = "Assets/Rihla/Built/Walk/";
        /// <summary>Height of the raised platform of the Dome of the Rock above the courtyard (estimate, H17).</summary>
        const float Platform = 4f;
        const float WallHeight = 12f, WallThickness = 3f, ArchBase = 4.5f;

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

            var lens = new GameObject("Time Lens").AddComponent<TimeLens>();
            lens.thenLook = ThenLook(lens.gameObject);
            lens.changing = new[] { domeRenderer };
            lens.nowColors = new[] { new Color(0.93f, 0.72f, 0.28f) }; lens.nowMetal = new[] { 0.9f };
            lens.thenColors = new[] { new Color(0.27f, 0.28f, 0.3f) }; lens.thenMetal = new[] { 0.35f };
            lens.onlyThen = new GameObject[0]; lens.onlyNow = new GameObject[0];

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
            sun.shadows = LightShadows.Hard; sun.transform.rotation = Quaternion.Euler(24f, -78f, 0f);
            RenderSettings.sun = sun;
            RenderSettings.skybox = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Skybox.mat");
            RenderSettings.ambientMode = AmbientMode.Skybox;
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.84f, 0.8f, 0.74f); RenderSettings.fogStartDistance = 250f; RenderSettings.fogEndDistance = 1400f;
        }

        static void Ground(List<Vector2> compound, List<Street> streets)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Plane);
            g.name = "Ground"; g.transform.position = new Vector3(-150f, 0f, -250f); g.transform.localScale = new Vector3(160f, 1f, 160f);
            g.GetComponent<MeshRenderer>().sharedMaterial = Mat("ground", new Color(0.66f, 0.6f, 0.5f));
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
            foreach (var w in json["walls"])
            {
                if ((string)w["name"] == "rock-platform") continue;
                bool city = (string)w["kind"] == "city";
                var line = w["line"].Select(V2).ToList();
                for (int i = 0; i < line.Count - 1; i++)
                    foreach (var (a, b) in Gaps(line[i], line[i + 1], streets))
                        Segment(city ? mb : low, a, b, city ? WallThickness : 1f, 0f, city ? WallHeight : 1.2f);
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

        static void Buildings(JObject json, List<Street> streets, List<Vector2> platform, ref JToken dome, ref JToken qibli, ref JToken chain, ref JToken gate)
        {
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
                if (arch) Prism(gates, ring, base0 + ArchBase, base0 + Mathf.Max(h, ArchBase + 3f));
                else Prism(tones[id % 3], ring, base0, base0 + h);
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
            Prism(Mesh("Tiles", Mat("tiles", new Color(0.16f, 0.42f, 0.62f), 0.55f), true, root), ring, Platform + 5.5f, Platform + 11f);
            float drumR = radius * 0.42f, top = Platform + 11f;
            var drum = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            drum.name = "Drum"; drum.transform.SetParent(root); drum.transform.position = new Vector3(c.x, top + 3f, c.y);
            drum.transform.localScale = new Vector3(drumR * 2f, 3f, drumR * 2f);
            drum.GetComponent<MeshRenderer>().sharedMaterial = Mat("tiles", new Color(0.16f, 0.42f, 0.62f), 0.55f);
            var d = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            d.name = "Dome"; d.transform.SetParent(root); Object.DestroyImmediate(d.GetComponent<Collider>());
            d.transform.position = new Vector3(c.x, top + 6f, c.y);
            d.transform.localScale = new Vector3(drumR * 2.1f, drumR * 2.3f, drumR * 2.1f);
            var r = d.GetComponent<MeshRenderer>(); r.sharedMaterial = Mat("dome", new Color(0.93f, 0.72f, 0.28f), 0.75f, 0.9f);
            return r;
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
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body"; body.transform.SetParent(child.transform, false); Object.DestroyImmediate(body.GetComponent<Collider>());
            body.transform.localPosition = new Vector3(0f, 0.675f, 0f); body.transform.localScale = new Vector3(0.5f, 0.675f, 0.5f);
            body.GetComponent<MeshRenderer>().sharedMaterial = Mat("child", new Color(0.2f, 0.4f, 0.75f));

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
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body"; body.transform.SetParent(root, false); Object.DestroyImmediate(body.GetComponent<Collider>());
            body.transform.localPosition = new Vector3(0f, 0.85f, 0f); body.transform.localScale = new Vector3(0.6f, 0.85f, 0.6f);
            body.GetComponent<MeshRenderer>().sharedMaterial = Mat("jiddo", new Color(0.45f, 0.36f, 0.27f));
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head"; head.transform.SetParent(root, false); Object.DestroyImmediate(head.GetComponent<Collider>());
            head.transform.localPosition = new Vector3(0f, 1.62f, 0f); head.transform.localScale = Vector3.one * 0.3f;
            head.GetComponent<MeshRenderer>().sharedMaterial = Mat("kufiya", new Color(0.92f, 0.9f, 0.86f));
            return root;
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
            m.RecalculateNormals(); m.RecalculateBounds();
            string p = Built + Safe(mb.name) + ".asset";
            AssetDatabase.DeleteAsset(p); AssetDatabase.CreateAsset(m, p);
            var go = new GameObject(mb.name); if (mb.parent) go.transform.SetParent(mb.parent);
            go.AddComponent<MeshFilter>().sharedMesh = m;
            go.AddComponent<MeshRenderer>().sharedMaterial = mb.mat;
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

        static Material Mat(string name, Color c, float smooth = 0.15f, float metal = 0f, Color? emission = null)
        {
            string p = Built + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (!m) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, p); }
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

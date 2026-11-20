using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Rihla.EditorTools
{
    /// <summary>
    /// Builds the Phase E0 scene from Assets/Rihla/Generated/scene.json (written by tools/export-unity.ts):
    /// the terrain tiles, the Jericho road, the Dead Sea, the Old City walls, place markers, a placeholder
    /// car, the walking child, a chase camera and the HUD. Menu: Rihla > Build E0 scene, or in batch mode
    /// -executeMethod Rihla.EditorTools.RihlaSceneBuilder.Build
    /// </summary>
    public static partial class RihlaSceneBuilder
    {
        const string Gen = "Assets/Rihla/Generated/";
        const string Built = "Assets/Rihla/Built/";
        const string Content = "Assets/Rihla/Content/";
        public const string ScenePath = "Assets/Scenes/E0_JerichoRoad.unity";

        [MenuItem("Rihla/Build E0 scene")]
        public static void Build()
        {
            palmCache = null;
            var json = JObject.Parse(File.ReadAllText(Gen + "scene.json"));
            Directory.CreateDirectory(Built);
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Lighting();
            var terrains = Terrains(json);
            var road = Road(json);
            Physics.SyncTransforms(); // so the raycasts below see the new terrain and road colliders
            Water((float)json["waterY"], terrains);
            Walls(json);
            var (car, walker, cam) = Player(road);
            CarBody(car.transform);

            RihlaUIBuilder.AddContent();
            RihlaUIBuilder.AddUI();

            var sw = new GameObject("Player").AddComponent<PlayerSwitch>();
            sw.car = car; sw.walker = walker; sw.cam = cam;
            var hud = new GameObject("HUD").AddComponent<Hud>();
            hud.car = car; hud.player = sw; hud.road = road;
            var teach = new GameObject("Teaching").AddComponent<Teaching>();
            teach.car = car; teach.player = sw; teach.road = road; teach.hud = hud;
            teach.pageMarker = PageMarker(road, json);
            // C1b: Jiddo's remarks, the stop at Khan al-Ahmar and the shepherd's flock.
            var story = new GameObject("Drive story").AddComponent<DriveStory>();
            story.car = car; story.player = sw; story.road = road;
            story.khan = KhanAlAhmar(road);
            story.flock = FlockCrossing(road);
            // C1c: Ariha's spring and Tell al-Sultan, and the visit on foot.
            var visit = ArihaSite(road, out var siteCentre);
            visit.car = car; visit.player = sw; visit.road = road; story.visit = visit;
            visit.guideMaterial = Unlit("guide_unlit_walk", new Color(1f, 0.62f, 0.12f) * 2.2f);
            // Realism along the road (the owner, 19 November 2026).
            float arihaDist = System.Array.Find(road.places, x => x.id == "ariha").roadDist;
            RoadFurniture(road);
            Scatter(road, arihaDist - 700f, arihaDist + 600f);
            Oasis(road, arihaDist - 700f, arihaDist + 600f, siteCentre, 95f);
            cam.gameObject.AddComponent<DetailCulling>();
            var sound = cam.gameObject.AddComponent<Soundscape>(); sound.child = walker; sound.birdiness = 0.2f;
            // The goal box and one soft golden arrow in the air over the road ahead.
            var guide = new GameObject("Drive guidance").AddComponent<DriveGuide>();
            guide.car = car; guide.player = sw; guide.road = road; guide.pageMarker = teach.pageMarker; guide.story = story; guide.visit = visit;
            guide.material = Additive("guide_light", Color.white);

            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ScenePath);
            RihlaScenes.UpdateBuildSettings();
            AssetDatabase.SaveAssets();
            Debug.Log($"[Rihla] E0 scene built: {terrains.Count} terrains, road {road.points.Length} points ({road.Length:0} m), {road.places.Length} places");
        }

        /// <summary>
        /// The first page of the Rihla, a golden scroll on a stone beside the road at its place (Khan al-Hatruri),
        /// on the khan's side, as in the browser prototype.
        /// </summary>
        static Transform PageMarker(RoadPath road, JObject json)
        {
            var journal = JObject.Parse(File.ReadAllText(Content + "journal.json"));
            string placeId = (string)journal["pages"]?[0]?["place"];
            int pi = System.Array.FindIndex(road.places, p => p.id == placeId);
            if (pi < 0) { Debug.LogWarning("[Rihla] page place not found: " + placeId); return null; }
            var place = road.places[pi];
            int i = 0; while (i < road.points.Length - 2 && road.distances[i] < place.roadDist) i++;
            Vector3 r = road.points[i], d = road.points[i + 1] - r; d.y = 0f; d.Normalize();
            Vector3 side = new Vector3(d.z, 0f, -d.x); // right of the direction of travel
            if (Vector3.Dot(side, place.position - r) < 0f) side = -side;
            Vector3 at = r + side * (road.width / 2f + 3.2f); at.y = Ground(at.x, at.z);

            var root = new GameObject("Rihla page (" + placeId + ")").transform;
            root.position = at;
            var plinth = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            plinth.name = "stone"; plinth.transform.SetParent(root, false);
            plinth.transform.localPosition = new Vector3(0f, 0.5f, 0f); plinth.transform.localScale = new Vector3(1.5f, 0.5f, 1.5f);
            plinth.GetComponent<MeshRenderer>().sharedMaterial = Mat("page_stone", new Color(0.81f, 0.76f, 0.64f));
            // Unlit, so it glows (URP Lit's emission setting is not kept on materials made by code).
            var scrollMat = Unlit("page_scroll_unlit", new Color(1f, 0.78f, 0.3f) * 1.8f);
            var scroll = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            scroll.name = "scroll"; scroll.transform.SetParent(root, false);
            Object.DestroyImmediate(scroll.GetComponent<Collider>());
            scroll.transform.localPosition = new Vector3(0f, 1.75f, 0f); scroll.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            scroll.transform.localScale = new Vector3(0.32f, 0.5f, 0.32f);
            scroll.GetComponent<MeshRenderer>().sharedMaterial = scrollMat;
            var beam = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            beam.name = "beam"; beam.transform.SetParent(root, false);
            Object.DestroyImmediate(beam.GetComponent<Collider>());
            beam.transform.localPosition = new Vector3(0f, 14f, 0f); beam.transform.localScale = new Vector3(0.25f, 12f, 0.25f);
            beam.GetComponent<MeshRenderer>().sharedMaterial = scrollMat;
            return root;
        }

        static Material Unlit(string name, Color c)
        {
            string p = Built + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (!m) { m = new Material(Shader.Find("Universal Render Pipeline/Unlit")); AssetDatabase.CreateAsset(m, p); }
            m.SetColor("_BaseColor", c); m.SetFloat("_Cull", 0f);
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>A glowing see-through unlit material: what is drawn adds light to what is behind it.</summary>
        static Material Additive(string name, Color c)
        {
            string p = Built + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (!m) { m = new Material(Shader.Find("Universal Render Pipeline/Unlit")); AssetDatabase.CreateAsset(m, p); }
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 2f); m.SetFloat("_Cull", 0f);
            m.SetFloat("_SrcBlend", (float)BlendMode.One); m.SetFloat("_DstBlend", (float)BlendMode.One);
            m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One); m.SetFloat("_DstBlendAlpha", (float)BlendMode.One);
            m.SetFloat("_ZWrite", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>
        /// The ruin of Khan al-Ahmar beside the road (C1b): the stone walls of a courtyard inn, partly fallen, with its
        /// gateway towards the road and a row of vaulted rooms inside. The shape is an estimate, not a survey of the
        /// real remains (NEEDS_REVIEW H22).
        /// </summary>
        static Transform KhanAlAhmar(RoadPath road)
        {
            int pi = System.Array.FindIndex(road.places, p => p.id == "khan-al-ahmar");
            if (pi < 0) return null;
            var place = road.places[pi];
            int i = 0; while (i < road.points.Length - 2 && road.distances[i] < place.roadDist) i++;
            Vector3 r = road.points[i], d = road.points[Mathf.Min(road.points.Length - 1, i + 3)] - road.points[Mathf.Max(0, i - 3)]; d.y = 0f; d.Normalize();
            Vector3 side = new Vector3(d.z, 0f, -d.x);
            if (Vector3.Dot(side, place.position - r) < 0f) side = -side;
            const float half = 14f, wallH = 4.2f, thick = 1.1f;
            Vector3 c = r + side * (road.width / 2f + 6f + half); c.y = Ground(c.x, c.z);
            var root = new GameObject("Khan al-Ahmar (ruin)").transform;
            root.SetPositionAndRotation(c, Quaternion.LookRotation(-side)); // +z faces the road
            var stone = Mat("khan_stone", new Color(0.86f, 0.7f, 0.56f), 0.12f);
            var dark = Mat("khan_shadow", new Color(0.2f, 0.16f, 0.13f), 0.05f);
            var rng = new System.Random(17);
            void Block(string n, Vector3 local, Vector3 size, Material m, bool collide = true)
            {
                Vector3 w = root.TransformPoint(local);
                float g = Ground(w.x, w.z) - c.y;
                var b = GameObject.CreatePrimitive(PrimitiveType.Cube);
                b.name = n; b.transform.SetParent(root, false);
                b.transform.localPosition = new Vector3(local.x, g + local.y + size.y / 2f - 0.75f, local.z);
                b.transform.localScale = new Vector3(size.x, size.y + 1.5f, size.z);
                b.GetComponent<MeshRenderer>().sharedMaterial = m;
                if (!collide) Object.DestroyImmediate(b.GetComponent<Collider>());
            }
            // The four walls in 4 m stretches, some fallen low; a gap for the gate in the front wall.
            for (int wall = 0; wall < 4; wall++)
            {
                for (float t = -half; t < half - 0.1f; t += 4f)
                {
                    float mid = t + 2f;
                    if (wall == 0 && Mathf.Abs(mid) < 2.5f) continue; // the gateway
                    double roll = rng.NextDouble();
                    float h = roll < 0.25 ? 1.2f + (float)rng.NextDouble() : roll < 0.4 ? 2.4f : wallH + (float)rng.NextDouble() * 0.5f;
                    Vector3 at, size;
                    switch (wall)
                    {
                        case 0: at = new Vector3(mid, 0f, half); size = new Vector3(4.02f, h, thick); break;
                        case 1: at = new Vector3(mid, 0f, -half); size = new Vector3(4.02f, h, thick); break;
                        case 2: at = new Vector3(half, 0f, mid); size = new Vector3(thick, h, 4.02f); break;
                        default: at = new Vector3(-half, 0f, mid); size = new Vector3(thick, h, 4.02f); break;
                    }
                    Block("wall", at, size, stone);
                }
            }
            // The gateway: two tall piers, a lintel and a raised block over it, and the dark of the passage.
            Block("gate pier", new Vector3(-2.6f, 0f, half + 0.3f), new Vector3(1.6f, 6f, 1.8f), stone);
            Block("gate pier", new Vector3(2.6f, 0f, half + 0.3f), new Vector3(1.6f, 6f, 1.8f), stone);
            Block("gate lintel", new Vector3(0f, 4.3f, half + 0.3f), new Vector3(3.8f, 1.7f, 1.8f), stone);
            // A row of vaulted rooms along the back wall, their doorways dark.
            for (int k = 0; k < 5; k++)
            {
                float x = -half + 3.4f + k * 5.3f;
                if (k == 3) { Block("fallen room", new Vector3(x, 0f, -half + 2.6f), new Vector3(4.6f, 1.1f, 4f), stone); continue; }
                Block("room", new Vector3(x, 0f, -half + 2.6f), new Vector3(4.8f, 3.4f, 4.2f), stone);
                Block("doorway", new Vector3(x, 0f, -half + 4.72f), new Vector3(1.4f, 2.2f, 0.08f), dark, false);
            }
            // Fallen stones in the courtyard.
            for (int k = 0; k < 14; k++)
            {
                float sz = 0.4f + (float)rng.NextDouble() * 0.6f;
                Block("stone", new Vector3(((float)rng.NextDouble() - 0.5f) * 22f, 0f, ((float)rng.NextDouble() - 0.5f) * 18f), new Vector3(sz * 1.4f, sz * 0.7f, sz), stone, false);
            }
            return root;
        }

        /// <summary>
        /// Where the shepherd crosses: near the distance down the road given in content/drive.json, where the ground on
        /// both sides is closest to the height of the road, so the flock does not climb a cliff.
        /// </summary>
        static Flock FlockCrossing(RoadPath road)
        {
            var drive = JObject.Parse(File.ReadAllText(Content + "drive.json"));
            float at = (float?)drive["flock"]?["at"] ?? 4900f;
            float best = float.MaxValue; Vector3 bf = default, bt = default; float bestAt = at;
            for (float a = at - 250f; a <= at + 250f; a += 10f)
            {
                Vector3 c = road.At(a), d = road.At(a + 4f) - road.At(a - 4f); d.y = 0f; d.Normalize();
                Vector3 across = Vector3.Cross(Vector3.up, d);
                Vector3 f = c - across * 15f, t = c + across * 17f;
                f.y = Ground(f.x, f.z); t.y = Ground(t.x, t.z);
                float mf = Ground((c.x + f.x) / 2f, (c.z + f.z) / 2f), mt = Ground((c.x + t.x) / 2f, (c.z + t.z) / 2f);
                // The road should come straight and level towards it, so the child sees the flock from afar.
                Vector3 back = road.At(a - 70f); Vector3 b0 = c - back; b0.y = 0f;
                float bend = Vector3.Angle(b0, d), dip = Mathf.Max(0f, back.y - c.y - 4f);
                for (float k = 10f; k < 70f; k += 10f)
                {
                    Vector3 q = road.At(a - k), line = Vector3.Lerp(c, back, k / 70f);
                    bend = Mathf.Max(bend, Vector2.Distance(new Vector2(q.x, q.z), new Vector2(line.x, line.z)) * 3f);
                }
                float cost = Mathf.Abs(f.y - c.y) + Mathf.Abs(t.y - c.y) + Mathf.Abs(mf - c.y) + Mathf.Abs(mt - c.y) + Mathf.Abs(a - at) * 0.01f + bend * 0.25f + dip;
                if (cost < best) { best = cost; bf = f; bt = t; bestAt = a; }
            }
            var go = new GameObject("Shepherd and flock");
            var flock = go.AddComponent<Flock>(); flock.from = bf; flock.to = bt;
            go.transform.position = bf;
            Debug.Log($"[Rihla] flock crosses at {bestAt:0} m along the road (ground off the road within {best:0.0} m in all)");
            return flock;
        }

        static Material Mat(string name, Color c, float smooth = 0.2f)
        {
            string p = Built + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (!m) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, p); }
            m.SetColor("_BaseColor", c); m.SetFloat("_Smoothness", smooth);
            EditorUtility.SetDirty(m);
            return m;
        }

        static void Lighting()
        {
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional; sun.intensity = 1.3f; sun.color = new Color(1f, 0.95f, 0.86f);
            sun.shadows = LightShadows.Hard; // hard shadows are cheaper on integrated graphics
            sun.transform.rotation = Quaternion.Euler(38f, -60f, 0f);
            RenderSettings.sun = sun;
            RenderSettings.skybox = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Skybox.mat");
            RenderSettings.ambientMode = AmbientMode.Skybox;
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 1500f; RenderSettings.fogEndDistance = 9000f;
            RenderSettings.fogColor = new Color(0.78f, 0.80f, 0.82f);
        }

        static TerrainLayer Layer(string name, Color c)
        {
            string texPath = Built + name + ".png", layerPath = Built + name + ".terrainlayer";
            var tex = new Texture2D(4, 4);
            var px = new Color[16]; for (int i = 0; i < 16; i++) px[i] = c;
            tex.SetPixels(px); tex.Apply();
            File.WriteAllBytes(texPath, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(texPath);
            var layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(layerPath);
            if (!layer) { layer = new TerrainLayer(); AssetDatabase.CreateAsset(layer, layerPath); }
            layer.diffuseTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            layer.tileSize = new Vector2(10f, 10f);
            EditorUtility.SetDirty(layer);
            return layer;
        }

        static List<Terrain> Terrains(JObject json)
        {
            var parent = new GameObject("Terrain").transform;
            // Poly Haven ground and rock (CC0), recoloured to the pale stone and soil of the wilderness.
            var soil = TexLayer("layer_soil", "rihla_desert_soil", 9f);
            var rock = TexLayer("layer_rock", "rihla_desert_rock", 16f);
            // Watered ground in Ariha's oasis, darker and greener, under the palms.
            var oasis = TexLayer("layer_oasis", "rihla_oasis_ground", 7f);
            Vector2 ariha = Vector2.zero;
            foreach (var p in json["places"]) if ((string)p["id"] == "ariha") ariha = new Vector2((float)p["position"][0], (float)p["position"][2]);
            var mat = new Material(Shader.Find("Universal Render Pipeline/Terrain/Lit"));
            AssetDatabase.CreateAsset(mat, Built + "terrain.mat");
            var list = new List<Terrain>();
            foreach (JObject t in json["terrains"])
            {
                int res = (int)t["resolution"];
                var size = t["size"].ToObject<float[]>(); var pos = t["position"].ToObject<float[]>();
                byte[] raw = File.ReadAllBytes(Gen + (string)t["file"]);
                var h = new float[res, res];
                for (int r = 0; r < res; r++) for (int c = 0; c < res; c++) h[r, c] = (raw[(r * res + c) * 2] | raw[(r * res + c) * 2 + 1] << 8) / 65535f;

                var td = new TerrainData { heightmapResolution = res };
                td.size = new Vector3(size[0], size[1], size[2]);
                td.SetHeights(0, 0, h);
                if (t["holes"] != null)
                {
                    byte[] hb = File.ReadAllBytes(Gen + (string)t["holes"]); int n = res - 1;
                    var holes = new bool[n, n];
                    for (int r = 0; r < n; r++) for (int c = 0; c < n; c++) holes[r, c] = hb[r * n + c] != 0;
                    td.SetHoles(0, 0, holes);
                }
                // Rock on steep ground, soil elsewhere.
                td.terrainLayers = new[] { soil, rock, oasis };
                td.alphamapResolution = 256;
                var alpha = new float[256, 256, 3];
                for (int r = 0; r < 256; r++) for (int c = 0; c < 256; c++)
                {
                    float steep = td.GetSteepness((c + 0.5f) / 256f, (r + 0.5f) / 256f);
                    float k = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(18f, 32f, steep));
                    float wx = pos[0] + (c + 0.5f) / 256f * size[0], wz = pos[2] + (r + 0.5f) / 256f * size[2];
                    float d = Vector2.Distance(new Vector2(wx, wz), ariha) + (Mathf.PerlinNoise(wx * 0.01f, wz * 0.01f) - 0.5f) * 260f;
                    float o = (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(560f, 900f, d))) * (1f - k);
                    alpha[r, c, 0] = (1f - k) * (1f - o); alpha[r, c, 1] = k; alpha[r, c, 2] = o;
                }
                td.SetAlphamaps(0, 0, alpha);
                AssetDatabase.CreateAsset(td, Built + (string)t["name"] + ".asset");
                // The splat textures are sub-assets; without this they are lost on save and reload.
                foreach (var tex in td.alphamapTextures) if (!AssetDatabase.Contains(tex)) AssetDatabase.AddObjectToAsset(tex, td);

                var go = Terrain.CreateTerrainGameObject(td);
                go.name = (string)t["name"]; go.transform.SetParent(parent);
                go.transform.position = new Vector3(pos[0], pos[1], pos[2]);
                var terrain = go.GetComponent<Terrain>();
                terrain.materialTemplate = mat;
                bool isFar = (string)t["kind"] == "far";
                terrain.heightmapPixelError = isFar ? 24f : 10f; // low settings for integrated graphics
                terrain.basemapDistance = 250f; // beyond this the terrain is drawn from one baked colour map (cheaper)
                terrain.drawInstanced = true;
                if (isFar) terrain.groupingID = 1;
                list.Add(terrain);
            }
            return list;
        }

        static RoadPath Road(JObject json)
        {
            var go = new GameObject("Jericho Road");
            var road = go.AddComponent<RoadPath>();
            var pts = new List<Vector3>();
            foreach (var p in json["road"]["points"]) pts.Add(new Vector3((float)p[0], (float)p[1], (float)p[2]));
            road.points = pts.ToArray(); road.width = (float)json["road"]["width"];
            road.ComputeDistances();
            var places = new List<RoadPath.Place>();
            foreach (var p in json["places"])
                places.Add(new RoadPath.Place
                {
                    id = (string)p["id"], kind = (string)p["kind"], nameEn = (string)p["name"]["en"], nameAr = (string)p["name"]["ar"],
                    position = new Vector3((float)p["position"][0], (float)p["position"][1], (float)p["position"][2]), roadDist = (float)p["roadDist"],
                });
            road.places = places.ToArray();

            // Surface with skirts that drop into the carved road bed; dashed centre line.
            int n = road.points.Length; float hw = road.width / 2f;
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>();
            var lv = new List<Vector3>(); var lt = new List<int>();
            for (int i = 0; i < n; i++)
            {
                Vector3 p = road.points[i], d = road.points[Mathf.Min(n - 1, i + 1)] - road.points[Mathf.Max(0, i - 1)];
                Vector3 side = Vector3.Cross(Vector3.up, new Vector3(d.x, 0f, d.z)).normalized; // points right
                float vCoord = road.distances[i] / road.width;
                v.Add(p - side * (hw + 0.6f) + Vector3.down * 0.9f); v.Add(p - side * hw + Vector3.up * 0.02f);
                v.Add(p + side * hw + Vector3.up * 0.02f); v.Add(p + side * (hw + 0.6f) + Vector3.down * 0.9f);
                uv.Add(new Vector2(-0.1f, vCoord)); uv.Add(new Vector2(0f, vCoord)); uv.Add(new Vector2(1f, vCoord)); uv.Add(new Vector2(1.1f, vCoord));
                if (i > 0)
                {
                    int a = (i - 1) * 4, b = i * 4;
                    for (int s = 0; s < 3; s++) { tri.AddRange(new[] { a + s, b + s, a + s + 1, a + s + 1, b + s, b + s + 1 }); }
                }
                // Solid white lines along both edges.
                if (i > 0)
                {
                    Vector3 q = road.points[i - 1]; int k = lv.Count;
                    foreach (float e in new[] { -(hw - 0.3f), hw - 0.3f })
                    {
                        k = lv.Count;
                        lv.Add(q + side * (e - 0.07f) + Vector3.up * 0.04f); lv.Add(q + side * (e + 0.07f) + Vector3.up * 0.04f);
                        lv.Add(p + side * (e - 0.07f) + Vector3.up * 0.04f); lv.Add(p + side * (e + 0.07f) + Vector3.up * 0.04f);
                        lt.AddRange(new[] { k, k + 2, k + 1, k + 1, k + 2, k + 3 });
                    }
                }
                // Dashes: 3 m paint, 6 m gap.
                if (i > 0 && (road.distances[i] % 9f) < 3f)
                {
                    Vector3 q = road.points[i - 1]; int k = lv.Count;
                    lv.Add(q - side * 0.08f + Vector3.up * 0.04f); lv.Add(q + side * 0.08f + Vector3.up * 0.04f);
                    lv.Add(p - side * 0.08f + Vector3.up * 0.04f); lv.Add(p + side * 0.08f + Vector3.up * 0.04f);
                    lt.AddRange(new[] { k, k + 2, k + 1, k + 1, k + 2, k + 3 });
                }
            }
            var mesh = new Mesh { name = "road", indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(v); mesh.SetUVs(0, uv); mesh.SetTriangles(tri, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, Built + "road.asset");
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = TexMat("asphalt", "asphalt_02", 0.5f, new Color(0.95f, 0.95f, 0.97f), 0.12f);
            go.AddComponent<MeshCollider>().sharedMesh = mesh;

            var lines = new Mesh { name = "road_lines", indexFormat = IndexFormat.UInt32 };
            lines.SetVertices(lv); lines.SetTriangles(lt, 0); lines.RecalculateNormals();
            AssetDatabase.CreateAsset(lines, Built + "road_lines.asset");
            var lgo = new GameObject("Centre line"); lgo.transform.SetParent(go.transform);
            lgo.AddComponent<MeshFilter>().sharedMesh = lines;
            var lr = lgo.AddComponent<MeshRenderer>(); lr.sharedMaterial = Mat("paint", new Color(0.92f, 0.9f, 0.82f), 0.3f);
            lr.shadowCastingMode = ShadowCastingMode.Off;
            return road;
        }

        static void Water(float y, List<Terrain> terrains)
        {
            var far = terrains.Find(t => t.name == "far");
            var go = GameObject.CreatePrimitive(PrimitiveType.Plane);
            go.name = "Dead Sea"; Object.DestroyImmediate(go.GetComponent<Collider>());
            Vector3 c = far.transform.position + far.terrainData.size * 0.5f;
            go.transform.position = new Vector3(c.x, y, c.z);
            go.transform.localScale = new Vector3(far.terrainData.size.x / 10f, 1f, far.terrainData.size.z / 10f);
            go.GetComponent<MeshRenderer>().sharedMaterial = Mat("water", new Color(0.18f, 0.38f, 0.48f), 0.92f);
        }

        static float Ground(float x, float z)
        {
            if (Physics.Raycast(new Vector3(x, 2000f, z), Vector3.down, out var hit, 4000f)) return hit.point.y;
            return 0f;
        }

        static void Walls(JObject json)
        {
            // Block-out only: the Old City wall line from places.json, 10 m high (heights are not compressed for buildings).
            var parent = new GameObject("Old City walls").transform;
            var mat = Mat("limestone", new Color(0.83f, 0.76f, 0.62f));
            var w = new List<Vector2>();
            foreach (var p in json["walls"]) w.Add(new Vector2((float)p[0], (float)p[1]));
            for (int i = 0; i < w.Count; i++)
            {
                Vector2 a = w[i], b = w[(i + 1) % w.Count], m = (a + b) / 2f;
                float len = Vector2.Distance(a, b); if (len < 0.5f) continue;
                var seg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                seg.name = "wall"; seg.transform.SetParent(parent);
                float ga = Ground(a.x, a.y), gb = Ground(b.x, b.y), g = Mathf.Min(ga, gb);
                float hgt = 10f + Mathf.Abs(ga - gb);
                seg.transform.position = new Vector3(m.x, g + hgt / 2f - 1f, m.y);
                seg.transform.rotation = Quaternion.LookRotation(new Vector3(b.x - a.x, 0f, b.y - a.y));
                seg.transform.localScale = new Vector3(3f, hgt, len + 3f);
                seg.GetComponent<MeshRenderer>().sharedMaterial = mat;
            }
        }

        static void Markers(RoadPath road)
        {
            var parent = new GameObject("Place markers").transform;
            var mat = Mat("marker", new Color(0.85f, 0.65f, 0.2f), 0.5f);
            foreach (var p in road.places)
            {
                if (p.id == "khan-al-ahmar") continue; // the ruin of the khan stands there instead
                var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                go.name = p.id; go.transform.SetParent(parent);
                Object.DestroyImmediate(go.GetComponent<Collider>());
                float g = Ground(p.position.x, p.position.z);
                go.transform.position = new Vector3(p.position.x, g + 15f, p.position.z);
                go.transform.localScale = new Vector3(1.2f, 15f, 1.2f);
                go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            }
        }

        static (CarController, Walker, FollowCamera) Player(RoadPath road)
        {
            Vector3 p0 = road.points[0], dir = road.points[3] - p0; dir.y = 0f;
            var car = new GameObject("Car (placeholder)");
            car.transform.SetPositionAndRotation(p0 + Vector3.up * 0.8f, Quaternion.LookRotation(dir.normalized));
            var rb = car.AddComponent<Rigidbody>(); rb.mass = 1500f; rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            var col = car.AddComponent<BoxCollider>(); col.center = new Vector3(0f, 0.85f, 0f); col.size = new Vector3(1.8f, 0.8f, 4.4f);

            var paint = Mat("car_paint", new Color(0.55f, 0.08f, 0.1f), 0.7f);
            var glass = Mat("car_glass", new Color(0.12f, 0.16f, 0.2f), 0.95f);
            var tyre = Mat("tyre", new Color(0.06f, 0.06f, 0.06f), 0.1f);
            Box(car.transform, "body", new Vector3(0f, 0.8f, 0f), new Vector3(1.8f, 0.6f, 4.4f), paint);
            Box(car.transform, "cabin", new Vector3(0f, 1.35f, -0.2f), new Vector3(1.6f, 0.55f, 2.4f), glass);

            var cc = car.AddComponent<CarController>(); cc.road = road;
            WheelCollider W(string n, float x, float z, out Transform mesh)
            {
                var wgo = new GameObject(n); wgo.transform.SetParent(car.transform, false);
                wgo.transform.localPosition = new Vector3(x, 0.55f, z);
                var wc = wgo.AddComponent<WheelCollider>();
                wc.mass = 20f; wc.radius = 0.36f; wc.suspensionDistance = 0.25f;
                wc.suspensionSpring = new JointSpring { spring = 35000f, damper = 4500f, targetPosition = 0.5f };
                var fwd = wc.forwardFriction; fwd.stiffness = 1.5f; wc.forwardFriction = fwd;
                var sid = wc.sidewaysFriction; sid.stiffness = 1.8f; wc.sidewaysFriction = sid;
                var m = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Object.DestroyImmediate(m.GetComponent<Collider>());
                m.name = n + " mesh"; m.transform.SetParent(car.transform, false);
                m.transform.localScale = new Vector3(0.72f, 0.12f, 0.72f);
                m.GetComponent<MeshRenderer>().sharedMaterial = tyre;
                mesh = m.transform;
                return wc;
            }
            cc.frontLeft = W("wheel FL", -0.82f, 1.35f, out cc.frontLeftMesh);
            cc.frontRight = W("wheel FR", 0.82f, 1.35f, out cc.frontRightMesh);
            cc.rearLeft = W("wheel RL", -0.82f, -1.35f, out cc.rearLeftMesh);
            cc.rearRight = W("wheel RR", 0.82f, -1.35f, out cc.rearRightMesh);

            var child = new GameObject("Child (walking)");
            var ch = child.AddComponent<CharacterController>(); ch.height = 1.35f; ch.radius = 0.25f; ch.center = new Vector3(0f, 0.7f, 0f);
            child.AddComponent<FigureBody>().hero = true; // Yusuf or Maryam, as in the walk
            var walker = child.AddComponent<Walker>();
            child.SetActive(false);

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>(); cam.farClipPlane = 12000f; cam.nearClipPlane = 0.3f; cam.fieldOfView = 60f;
            camGo.AddComponent<AudioListener>();
            var follow = camGo.AddComponent<FollowCamera>(); follow.target = car.transform; follow.Snap();
            return (cc, walker, follow);
        }

        static void Box(Transform parent, string name, Vector3 pos, Vector3 size, Material m)
        {
            var b = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.DestroyImmediate(b.GetComponent<Collider>());
            b.name = name; b.transform.SetParent(parent, false);
            b.transform.localPosition = pos; b.transform.localScale = size;
            b.GetComponent<MeshRenderer>().sharedMaterial = m;
        }
    }
}

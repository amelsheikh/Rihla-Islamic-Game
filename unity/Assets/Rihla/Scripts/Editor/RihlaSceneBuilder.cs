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
    public static class RihlaSceneBuilder
    {
        const string Gen = "Assets/Rihla/Generated/";
        const string Built = "Assets/Rihla/Built/";
        const string Content = "Assets/Rihla/Content/";
        public const string ScenePath = "Assets/Scenes/E0_JerichoRoad.unity";

        [MenuItem("Rihla/Build E0 scene")]
        public static void Build()
        {
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
            Markers(road);
            var (car, walker, cam) = Player(road);

            var content = new GameObject("Content").AddComponent<ContentLibrary>();
            content.places = Load(Content + "places.json");
            content.history = Load(Content + "history.json");
            content.journal = Load(Content + "journal.json");
            content.religiousTexts = Load(Content + "religious_texts.json");
            content.english = Load(Content + "i18n/en.json");
            content.arabic = Load(Content + "i18n/ar.json");

            var sw = new GameObject("Player").AddComponent<PlayerSwitch>();
            sw.car = car; sw.walker = walker; sw.cam = cam;
            var hud = new GameObject("HUD").AddComponent<Hud>();
            hud.car = car; hud.player = sw; hud.road = road;

            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log($"[Rihla] E0 scene built: {terrains.Count} terrains, road {road.points.Length} points ({road.Length:0} m), {road.places.Length} places");
        }

        static TextAsset Load(string p) => AssetDatabase.LoadAssetAtPath<TextAsset>(p);

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
            sun.shadows = LightShadows.Soft;
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
            var soil = Layer("layer_soil", new Color(0.72f, 0.62f, 0.47f));
            var rock = Layer("layer_rock", new Color(0.56f, 0.52f, 0.47f));
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
                td.terrainLayers = new[] { soil, rock };
                td.alphamapResolution = 256;
                var alpha = new float[256, 256, 2];
                for (int r = 0; r < 256; r++) for (int c = 0; c < 256; c++)
                {
                    float steep = td.GetSteepness((c + 0.5f) / 256f, (r + 0.5f) / 256f);
                    float k = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(18f, 32f, steep));
                    alpha[r, c, 0] = 1f - k; alpha[r, c, 1] = k;
                }
                td.SetAlphamaps(0, 0, alpha);
                AssetDatabase.CreateAsset(td, Built + (string)t["name"] + ".asset");

                var go = Terrain.CreateTerrainGameObject(td);
                go.name = (string)t["name"]; go.transform.SetParent(parent);
                go.transform.position = new Vector3(pos[0], pos[1], pos[2]);
                var terrain = go.GetComponent<Terrain>();
                terrain.materialTemplate = mat;
                bool isFar = (string)t["kind"] == "far";
                terrain.heightmapPixelError = isFar ? 12f : 6f; // low settings for integrated graphics
                terrain.basemapDistance = 600f;
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
            go.AddComponent<MeshRenderer>().sharedMaterial = Mat("asphalt", new Color(0.22f, 0.22f, 0.23f), 0.15f);
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
            var bodyMesh = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            Object.DestroyImmediate(bodyMesh.GetComponent<Collider>());
            bodyMesh.transform.SetParent(child.transform, false);
            bodyMesh.transform.localPosition = new Vector3(0f, 0.68f, 0f); bodyMesh.transform.localScale = new Vector3(0.45f, 0.68f, 0.45f);
            bodyMesh.GetComponent<MeshRenderer>().sharedMaterial = Mat("child", new Color(0.2f, 0.42f, 0.7f));
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

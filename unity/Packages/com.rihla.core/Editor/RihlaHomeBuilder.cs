using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Rihla.EditorTools
{
    /// <summary>
    /// Builds Assets/Scenes/C1_Home.unity, the opening of the game: an evening in Jiddo's house in the Old City of
    /// Al-Quds. A vaulted stone room with cement-tile floor, floor seating (majlis) along the walls, a rug, a brass
    /// lantern, a low table with tea, a bookshelf, the family chest, and a window onto the evening sky with the Dome
    /// of the Rock far off. Jiddo sits with his old book; Teta beside him; Yusuf and Maryam on cushions before them.
    /// The room is an imagined family home, not a particular house (NEEDS_REVIEW.md H19).
    /// </summary>
    public static class RihlaHomeBuilder
    {
        public static string ScenePath => CoreConfig.Scenes + "C1_Home.unity";
        static string Built => CoreConfig.Built + "Home/";
        const float W = 3.6f, D = 3.1f, H = 2.9f, Rise = 0.9f; // half width (x), half depth (z), wall height, vault rise

        [MenuItem("Rihla/Build C1 home (the opening)", true)] static bool BuildOk() => RihlaMenus.Enabled;
        [MenuItem("Rihla/Build C1 home (the opening)")]
        public static void Build()
        {
            Directory.CreateDirectory(Built);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var room = new GameObject("Room").transform;
            var plaster = Mat("plaster", new Color(0.93f, 0.88f, 0.8f), 0.08f, "white_plaster_rough_01", 2f);
            var floorMat = Mat("tiles", Color.white, 0.35f); floorMat.SetTexture("_BaseMap", TileTexture()); floorMat.mainTextureScale = Vector2.one / 0.4f;

            // Floor, walls and a shallow vault (the stone houses of the Old City are roofed with vaults).
            Quad(room, "Floor", floorMat, new Vector3(-W, 0, -D), new Vector3(W, 0, -D), new Vector3(W, 0, D), new Vector3(-W, 0, D), true, Vector3.up * 5f);
            Wall(room, "Back wall", plaster, new Vector3(-W, 0, D), new Vector3(W, 0, D), window: true);
            Wall(room, "Front wall", plaster, new Vector3(W, 0, -D), new Vector3(-W, 0, -D), window: false);
            EndWall(room, "Left wall", plaster, -W, false);
            EndWall(room, "Right wall", plaster, W, true);
            Vault(room, plaster);

            // The window in the back wall: an arch onto the evening sky, with the Dome of the Rock far off.
            var sky = Mat("evening sky", Color.black, 0f, null, 1f, Color.white * 1.6f); var skyTex = SkyTexture(); sky.SetTexture("_BaseMap", skyTex);
            var winC = new Vector3(-2.1f, 1.55f, D - 0.02f);
            var pane = Quad(room, "Window view", sky, winC + new Vector3(0.55f, -0.75f, 0.2f), winC + new Vector3(-0.55f, -0.75f, 0.2f), winC + new Vector3(-0.55f, 0.75f, 0.2f), winC + new Vector3(0.55f, 0.75f, 0.2f), false);
            pane.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            var wood = Mat("wood", new Color(0.32f, 0.2f, 0.12f), 0.35f);
            var darkWood = Mat("dark wood", new Color(0.2f, 0.12f, 0.08f), 0.4f);
            Box(room, "Window sill", wood, winC + new Vector3(0, -0.78f, -0.08f), new Vector3(1.3f, 0.06f, 0.3f));
            Box(room, "Shutter L", wood, winC + new Vector3(-0.85f, 0, -0.06f), new Vector3(0.55f, 1.5f, 0.04f));
            Box(room, "Shutter R", wood, winC + new Vector3(0.85f, 0, -0.06f), new Vector3(0.55f, 1.5f, 0.04f));
            Box(room, "Window bar", darkWood, winC + new Vector3(0, 0, 0.12f), new Vector3(0.03f, 1.5f, 0.03f));
            Box(room, "Window bar", darkWood, winC + new Vector3(0, 0.2f, 0.12f), new Vector3(1.1f, 0.03f, 0.03f));

            // Floor seating along the back and left walls, in a red woven cloth, with back cushions.
            var cloth = Mat("majlis cloth", Color.white, 0.1f); cloth.SetTexture("_BaseMap", ClothTexture()); cloth.mainTextureScale = new Vector2(1.5f, 1f);
            var plain = Mat("cushion", new Color(0.52f, 0.12f, 0.1f), 0.1f);
            for (float x = -1.2f; x <= 2.8f; x += 0.8f)
            {
                Box(room, "Majlis seat", cloth, new Vector3(x, 0.17f, D - 0.4f), new Vector3(0.78f, 0.34f, 0.72f));
                Box(room, "Majlis back", plain, new Vector3(x, 0.55f, D - 0.1f), new Vector3(0.74f, 0.45f, 0.18f)).localRotation = Quaternion.Euler(-10f, 0, 0);
            }
            for (float z = -1.8f; z <= 1.6f; z += 0.8f)
            {
                Box(room, "Majlis seat", cloth, new Vector3(-W + 0.4f, 0.17f, z), new Vector3(0.72f, 0.34f, 0.78f));
                Box(room, "Majlis back", plain, new Vector3(-W + 0.1f, 0.55f, z), new Vector3(0.18f, 0.45f, 0.74f)).localRotation = Quaternion.Euler(0, 0, 10f);
            }

            // The rug and the low round table with tea.
            var rug = Mat("rug", Color.white, 0.05f); rug.SetTexture("_BaseMap", RugTexture());
            Quad(room, "Rug", rug, new Vector3(-1.6f, 0.012f, -1.4f), new Vector3(1.9f, 0.012f, -1.4f), new Vector3(1.9f, 0.012f, 2.1f), new Vector3(-1.6f, 0.012f, 2.1f), false, Vector3.up * 5f);
            var brass = Mat("brass", new Color(0.78f, 0.58f, 0.28f), 0.7f, null, 1f, null, 0.85f);
            var table = new GameObject("Low table").transform; table.SetParent(room); table.position = new Vector3(1.55f, 0f, 1.25f);
            Prim(PrimitiveType.Cylinder, table, "Tray", brass, new Vector3(0, 0.36f, 0), new Vector3(0.75f, 0.015f, 0.75f));
            Prim(PrimitiveType.Cylinder, table, "Stand", darkWood, new Vector3(0, 0.18f, 0), new Vector3(0.45f, 0.18f, 0.45f));
            var glass = Mat("tea glass", new Color(0.55f, 0.2f, 0.08f), 0.9f);
            for (int i = 0; i < 4; i++)
            {
                float a = i * Mathf.PI / 2f + 0.4f;
                Prim(PrimitiveType.Cylinder, table, "Tea glass", glass, new Vector3(Mathf.Cos(a) * 0.22f, 0.41f, Mathf.Sin(a) * 0.22f), new Vector3(0.05f, 0.04f, 0.05f));
            }
            Prim(PrimitiveType.Sphere, table, "Teapot", brass, new Vector3(0, 0.45f, 0), new Vector3(0.17f, 0.15f, 0.17f));
            Prim(PrimitiveType.Cylinder, table, "Teapot lid", brass, new Vector3(0, 0.53f, 0), new Vector3(0.07f, 0.02f, 0.07f));
            var bowl = Prim(PrimitiveType.Sphere, table, "Bowl of dates", Mat("dates", new Color(0.32f, 0.14f, 0.07f), 0.5f), new Vector3(-0.2f, 0.4f, -0.12f), new Vector3(0.16f, 0.06f, 0.16f));

            // A bookshelf on the right wall, and the old family chest beside it.
            var shelf = new GameObject("Bookshelf").transform; shelf.SetParent(room); shelf.position = new Vector3(W - 0.2f, 0, -0.6f);
            Box(shelf, "Shelf frame", darkWood, new Vector3(0, 0.95f, 0), new Vector3(0.36f, 1.9f, 1.2f));
            var bookCols = new[] { new Color(0.45f, 0.12f, 0.1f), new Color(0.12f, 0.25f, 0.18f), new Color(0.55f, 0.42f, 0.2f), new Color(0.15f, 0.18f, 0.32f), new Color(0.6f, 0.55f, 0.45f) };
            var rnd = new System.Random(3);
            for (int row = 0; row < 4; row++)
            {
                float y = 0.15f + row * 0.45f, z = -0.52f;
                Box(shelf, "Shelf", wood, new Vector3(-0.19f, y - 0.02f, 0), new Vector3(0.05f, 0.03f, 1.12f)).localPosition += new Vector3(0.0f, 0, 0);
                while (z < 0.48f)
                {
                    float w = 0.03f + (float)rnd.NextDouble() * 0.04f, h = 0.24f + (float)rnd.NextDouble() * 0.12f;
                    Box(shelf, "Book", Mat("book " + rnd.Next(5), bookCols[rnd.Next(bookCols.Length)], 0.3f), new Vector3(-0.2f, y + h / 2f, z + w / 2f), new Vector3(0.24f, h, w));
                    z += w + 0.004f;
                }
            }
            var chest = new GameObject("The family chest").transform; chest.SetParent(room); chest.position = new Vector3(W - 0.45f, 0, 1.3f);
            chest.rotation = Quaternion.Euler(0, -90f, 0);
            Box(chest, "Chest", Mat("chest wood", new Color(0.36f, 0.22f, 0.12f), 0.3f), new Vector3(0, 0.27f, 0), new Vector3(0.95f, 0.5f, 0.55f));
            // C1a: the lid opens on a hinge along the back; inside lies Zaynab's first page, in a warm light.
            var lidHinge = new GameObject("Chest lid hinge").transform; lidHinge.SetParent(chest, false); lidHinge.localPosition = new Vector3(0, 0.52f, -0.29f);
            Box(lidHinge, "Chest lid", Mat("chest wood", new Color(0.36f, 0.22f, 0.12f), 0.3f), new Vector3(0, 0.04f, 0.29f), new Vector3(0.98f, 0.08f, 0.58f));
            Box(lidHinge, "Lid band", brass, new Vector3(0, 0.081f, 0.29f), new Vector3(0.05f, 0.004f, 0.6f));
            Box(lidHinge, "Clasp", brass, new Vector3(0, 0.0f, 0.585f), new Vector3(0.08f, 0.1f, 0.02f));
            foreach (float bx in new[] { -0.35f, 0.35f }) Box(chest, "Chest band", brass, new Vector3(bx, 0.27f, 0), new Vector3(0.05f, 0.52f, 0.57f));
            var tatreez = Mat("tatreez cloth", Color.white, 0.1f); tatreez.SetTexture("_BaseMap", RihlaWalkBuilder.TatreezTexture()); EditorUtility.SetDirty(tatreez);
            Box(chest, "Tatreez panel", tatreez, new Vector3(0, 0.27f, 0.277f), new Vector3(0.5f, 0.34f, 0.004f));
            var inner = Mat("chest light", Color.black, 0f, null, 1f, new Color(1f, 0.7f, 0.35f) * 1.4f);
            Box(chest, "Inner glow", inner, new Vector3(0, 0.524f, 0), new Vector3(0.86f, 0.006f, 0.46f)).GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            var zpage = Box(chest, "Zaynab's page", Mat("old page", new Color(0.88f, 0.78f, 0.56f), 0.1f), new Vector3(0, 0.45f, 0.02f), new Vector3(0.34f, 0.012f, 0.26f));
            Box(zpage, "Seal", Mat("seal", new Color(0.6f, 0.1f, 0.08f), 0.4f), new Vector3(0.3f, 0.9f, 0.3f), new Vector3(0.15f, 0.6f, 0.2f));

            // A picture of the Dome of the Rock on the left wall (a view from this game), in a wooden frame.
            var pic = Mat("dome picture", Color.white, 0.2f); pic.SetTexture("_BaseMap", Picture());
            var frame = new GameObject("Picture").transform; frame.SetParent(room); frame.position = new Vector3(-W + 0.03f, 1.75f, -0.6f); frame.rotation = Quaternion.Euler(0, 90f, 0);
            Box(frame, "Frame", darkWood, Vector3.zero, new Vector3(0.95f, 0.62f, 0.04f));
            Box(frame, "Picture", pic, new Vector3(0f, 0f, 0.021f), new Vector3(0.84f, 0.52f, 0.004f)).GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;

            // A brass lantern hanging from the vault: the warm light of the room.
            var lantern = new GameObject("Lantern").transform; lantern.SetParent(room); lantern.position = new Vector3(0.4f, 2.25f, 0.9f);
            var lampGlass = Mat("lantern glass", new Color(1f, 0.8f, 0.5f), 0.5f, null, 1f, new Color(1f, 0.62f, 0.3f) * 4f);
            Prim(PrimitiveType.Cylinder, lantern, "Lantern glass", lampGlass, Vector3.zero, new Vector3(0.2f, 0.16f, 0.2f)).GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            Prim(PrimitiveType.Sphere, lantern, "Lantern cap", brass, new Vector3(0, 0.18f, 0), new Vector3(0.26f, 0.12f, 0.26f)).GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            Prim(PrimitiveType.Cylinder, lantern, "Lantern chain", darkWood, new Vector3(0, 0.6f, 0), new Vector3(0.015f, 0.4f, 0.015f)).GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            var light = new GameObject("Lantern light").AddComponent<Light>(); light.transform.SetParent(lantern, false); light.transform.localPosition = new Vector3(0, -0.2f, 0);
            light.type = LightType.Point; light.color = new Color(1f, 0.71f, 0.42f); light.intensity = 7f; light.range = 10f;
            light.shadows = LightShadows.Soft; light.shadowStrength = 0.75f;
            var lamp2 = new GameObject("Table lamp light").AddComponent<Light>(); lamp2.transform.SetParent(room);
            lamp2.transform.position = new Vector3(-2.8f, 1.0f, 2.2f); lamp2.type = LightType.Point; lamp2.color = new Color(1f, 0.66f, 0.38f); lamp2.intensity = 2.5f; lamp2.range = 5f;
            var lamp2Glass = Prim(PrimitiveType.Sphere, room, "Corner lamp", lampGlass, new Vector3(-2.8f, 0.9f, 2.4f), new Vector3(0.22f, 0.26f, 0.22f));
            Prim(PrimitiveType.Cylinder, room, "Corner lamp stand", darkWood, new Vector3(-2.8f, 0.4f, 2.4f), new Vector3(0.14f, 0.4f, 0.14f));
            var blue = new GameObject("Window glow").AddComponent<Light>(); blue.transform.SetParent(room);
            blue.transform.position = winC + new Vector3(0, 0, -0.6f); blue.type = LightType.Point; blue.color = new Color(0.45f, 0.55f, 0.85f); blue.intensity = 0.6f; blue.range = 3.5f;

            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(0.34f, 0.28f, 0.23f);
            RenderSettings.skybox = null; RenderSettings.fog = false;
            Look();

            // The family. Jiddo with the book, Teta beside him, the two children on cushions before them.
            float seat = 0.34f;
            var jiddo = Person(room, "Jiddo Ibrahim", FigureBody.Look.Jiddo, new Vector3(0.3f, 0f, D - 0.42f), 180f, seat, 1f, 52f, new Color(0.88f, 0.85f, 0.78f));
            var teta = Person(room, "Teta Amina", FigureBody.Look.WomanElder, new Vector3(1.25f, 0f, D - 0.42f), 195f, seat, 0.92f, 14f, new Color(0.11f, 0.1f, 0.12f));
            var floorCushion = Mat("floor cushion", new Color(0.18f, 0.3f, 0.36f), 0.1f);
            var yusufAt = new Vector3(-0.55f, 0f, 1.05f); var maryamAt = new Vector3(0.95f, 0f, 0.85f);
            Box(room, "Floor cushion", floorCushion, yusufAt + new Vector3(0, 0.07f, 0), new Vector3(0.62f, 0.14f, 0.62f));
            Box(room, "Floor cushion", floorCushion, maryamAt + new Vector3(0, 0.07f, 0), new Vector3(0.62f, 0.14f, 0.62f));
            float Face(Vector3 from) { var d = jiddo.position - from; return Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg; }
            Person(room, "Yusuf", FigureBody.Look.Yusuf, yusufAt, Face(yusufAt), 0.14f, 0.74f, 8f, null);
            Person(room, "Maryam", FigureBody.Look.Maryam, maryamAt, Face(maryamAt), 0.14f, 0.74f, 8f, FigureBody.MaryamDress);

            // Jiddo's book on his lap; its cover opens on a hinge at the spine.
            var book = new GameObject("Jiddo's book").transform; book.SetParent(jiddo, false);
            book.localPosition = new Vector3(0f, seat + 0.22f, 0.45f); book.localRotation = Quaternion.Euler(-28f, 0f, 0f);
            var leather = Mat("book leather", new Color(0.3f, 0.12f, 0.07f), 0.35f);
            var pages = Mat("book pages", new Color(0.93f, 0.88f, 0.74f), 0.1f);
            Box(book, "Back cover", leather, new Vector3(0, -0.012f, 0), new Vector3(0.3f, 0.012f, 0.22f));
            Box(book, "Pages", pages, new Vector3(0, 0.005f, 0), new Vector3(0.285f, 0.025f, 0.21f));
            var hinge = new GameObject("Cover hinge").transform; hinge.SetParent(book, false); hinge.localPosition = new Vector3(-0.15f, 0.02f, 0);
            Box(hinge, "Front cover", leather, new Vector3(0.15f, 0f, 0), new Vector3(0.3f, 0.012f, 0.22f));
            Box(hinge, "Gold mark", brass, new Vector3(0.15f, 0.007f, 0), new Vector3(0.12f, 0.003f, 0.12f));

            // Camera shots, the interface, and the opening's director.
            RihlaUIBuilder.AddContent();
            RihlaUIBuilder.AddUI();
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>(); cam.nearClipPlane = 0.05f; cam.farClipPlane = 60f; cam.fieldOfView = 50f;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black;
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;
            camGo.AddComponent<Soundscape>().evening = true; // crickets outside, a soft breeze at the window
            Transform Shot(string name, Vector3 pos, Vector3 look)
            {
                var t = new GameObject("Shot: " + name).transform; t.position = pos; t.rotation = Quaternion.LookRotation(look - pos); return t;
            }
            var bookWorld = book.position;
            var dir = new GameObject("Opening").AddComponent<StoryOpening>();
            dir.cam = cam; dir.bookCover = hinge;
            dir.wide = Shot("wide", new Vector3(2.9f, 1.75f, -2.7f), new Vector3(0.1f, 0.75f, 1.6f));
            dir.close = Shot("close", new Vector3(0.25f, 1.15f, -0.25f), new Vector3(0.3f, 0.75f, 2.2f));
            dir.onBook = Shot("on the book", bookWorld + new Vector3(0f, 0.55f, -0.32f), bookWorld);
            dir.chestShot = Shot("on the chest", chest.position + new Vector3(-1.25f, 1.2f, -0.45f), chest.position + new Vector3(0f, 0.5f, 0f));
            dir.chestLid = lidHinge; dir.chestPage = zpage;
            camGo.transform.SetPositionAndRotation(dir.wide.position, dir.wide.rotation);

            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ScenePath);
            RihlaScenes.UpdateBuildSettings();
            AssetDatabase.SaveAssets();
            Debug.Log("[Rihla] C1 home built: the room, the family, the book");
        }

        /// <summary>
        /// A seated figure: hips at <paramref name="seat"/>, legs forward (trousers) or hidden in the seat (robes),
        /// with a lap of the robe's cloth over the knees so it reads as sitting.
        /// </summary>
        static Transform Person(Transform parent, string name, FigureBody.Look look, Vector3 at, float yaw, float seat, float scale, float armHold, Color? lap)
        {
            var root = new GameObject(name).transform; root.SetParent(parent);
            root.SetPositionAndRotation(at, Quaternion.Euler(0f, yaw, 0f));
            var fig = new GameObject("Figure").transform; fig.SetParent(root, false);
            fig.localPosition = new Vector3(0f, seat - 0.86f * scale + 0.03f, 0f);
            var f = fig.gameObject.AddComponent<FigureBody>(); f.look = look; f.seated = true; f.armHold = armHold; f.seed = name.Length;
            if (lap.HasValue)
            {
                // Knees and shins under the robe: rounded shapes in its cloth, one for each leg.
                var m = Mat("lap " + name, lap.Value, 0.12f);
                var shoe = Mat("shoes", new Color(0.2f, 0.14f, 0.1f), 0.3f);
                for (int side = -1; side <= 1; side += 2)
                {
                    float x = side * 0.105f * scale, r = 0.1f * scale, len = 0.46f * scale;
                    var thigh = Prim(PrimitiveType.Capsule, root, "Thigh", m, new Vector3(x, seat + r * 0.75f, len / 2f), new Vector3(r * 2f, len / 2f, r * 2.1f));
                    thigh.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    if (seat > 0.2f)
                    {
                        Prim(PrimitiveType.Capsule, root, "Shin", m, new Vector3(x, (seat + r) / 2f, len), new Vector3(r * 1.8f, (seat + r) / 2f, r * 1.8f));
                        Prim(PrimitiveType.Sphere, root, "Shoe", shoe, new Vector3(x, 0.035f, len + 0.06f), new Vector3(0.1f, 0.07f, 0.24f));
                    }
                }
            }
            return root;
        }

        // ------------------------------------------------------------------ the room's shell

        static void Wall(Transform parent, string name, Material m, Vector3 a, Vector3 b, bool window)
        {
            var mb = new MeshBuilder();
            Vector3 up = Vector3.up * H;
            if (!window) mb.Quad(a, b, b + up, a + up, 1f);
            else
            {
                // The window opening: x from -2.65 to -1.55, y from 0.8 to 2.3 (the back wall runs from -W to W).
                float x0 = -2.65f, x1 = -1.55f, y0 = 0.8f, y1 = 2.3f; float z = a.z;
                Vector3? hole = null;
                mb.Quad(new Vector3(-W, 0, z), new Vector3(x0, 0, z), new Vector3(x0, H, z), new Vector3(-W, H, z), 1f, hole);
                mb.Quad(new Vector3(x1, 0, z), new Vector3(W, 0, z), new Vector3(W, H, z), new Vector3(x1, H, z), 1f, hole);
                mb.Quad(new Vector3(x0, 0, z), new Vector3(x1, 0, z), new Vector3(x1, y0, z), new Vector3(x0, y0, z), 1f, hole);
                mb.Quad(new Vector3(x0, y1, z), new Vector3(x1, y1, z), new Vector3(x1, H, z), new Vector3(x0, H, z), 1f, hole);
                // The thickness of the stone wall around the opening (its faces look into the opening).
                float t = 0.4f; hole = new Vector3((x0 + x1) / 2f, (y0 + y1) / 2f, z + t / 2f);
                mb.Quad(new Vector3(x0, y0, z), new Vector3(x0, y0, z + t), new Vector3(x0, y1, z + t), new Vector3(x0, y1, z), 1f, hole);
                mb.Quad(new Vector3(x1, y0, z + t), new Vector3(x1, y0, z), new Vector3(x1, y1, z), new Vector3(x1, y1, z + t), 1f, hole);
                mb.Quad(new Vector3(x0, y1, z), new Vector3(x0, y1, z + t), new Vector3(x1, y1, z + t), new Vector3(x1, y1, z), 1f, hole);
                mb.Quad(new Vector3(x1, y0, z), new Vector3(x1, y0, z + t), new Vector3(x0, y0, z + t), new Vector3(x0, y0, z), 1f, hole);
            }
            mb.Make(name, parent, m);
        }

        static float VaultY(float z) => H + Rise * (1f - (z / D) * (z / D));

        static void EndWall(Transform parent, string name, Material m, float x, bool right)
        {
            var mb = new MeshBuilder();
            const int n = 12;
            for (int i = 0; i < n; i++)
            {
                float z0 = -D + 2f * D * i / n, z1 = -D + 2f * D * (i + 1) / n;
                Vector3 a = new Vector3(x, 0, z0), b = new Vector3(x, 0, z1), c = new Vector3(x, VaultY(z1), z1), d = new Vector3(x, VaultY(z0), z0);
                if (right) mb.Quad(b, a, d, c, 1f); else mb.Quad(a, b, c, d, 1f);
            }
            mb.Make(name, parent, m);
        }

        static void Vault(Transform parent, Material m)
        {
            var mb = new MeshBuilder();
            const int n = 14;
            for (int i = 0; i < n; i++)
            {
                float z0 = -D + 2f * D * i / n, z1 = -D + 2f * D * (i + 1) / n;
                mb.Quad(new Vector3(W, VaultY(z0), z0), new Vector3(W, VaultY(z1), z1), new Vector3(-W, VaultY(z1), z1), new Vector3(-W, VaultY(z0), z0), 1f);
            }
            mb.Make("Vault", parent, m);
        }

        /// <summary>Builds meshes from quads, UVs in metres; faces point toward the room (counter-clockwise from inside).</summary>
        class MeshBuilder
        {
            readonly List<Vector3> v = new List<Vector3>(); readonly List<Vector2> uv = new List<Vector2>(); readonly List<int> t = new List<int>();
            public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float scale, Vector3? toward = null)
            {
                bool flip = Vector3.Dot(Vector3.Cross(c - a, b - a), (toward ?? Centre) - a) <= 0f;
                int i = v.Count; v.Add(a); v.Add(b); v.Add(c); v.Add(d);
                Vector3 n = Vector3.Cross(b - a, d - a).normalized;
                foreach (var p in new[] { a, b, c, d })
                {
                    Vector2 q = Mathf.Abs(n.y) > 0.7f ? new Vector2(p.x, p.z) : Mathf.Abs(n.x) > 0.7f ? new Vector2(p.z, p.y) : new Vector2(p.x, p.y);
                    uv.Add(q * scale);
                }
                if (flip) t.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3 }); else t.AddRange(new[] { i, i + 2, i + 1, i, i + 3, i + 2 });
            }
            public void Make(string name, Transform parent, Material m)
            {
                var mesh = new Mesh { name = name }; mesh.SetVertices(v); mesh.SetUVs(0, uv); mesh.SetTriangles(t, 0);
                mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds();
                AssetDatabase.CreateAsset(mesh, Built + name.Replace(' ', '_') + ".asset");
                var go = new GameObject(name); go.transform.SetParent(parent, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh; go.AddComponent<MeshRenderer>().sharedMaterial = m;
            }
        }

        static readonly Vector3 Centre = new Vector3(0f, 1.4f, 0f);

        /// <summary>A quad whose front faces <paramref name="toward"/> (the room's centre unless given).</summary>
        static GameObject Quad(Transform parent, string name, Material m, Vector3 a, Vector3 b, Vector3 c, Vector3 d, bool shadows, Vector3? toward = null)
        {
            var mesh = new Mesh { name = name };
            mesh.vertices = new[] { a, b, c, d };
            mesh.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
            if (name == "Floor") mesh.uv = new[] { new Vector2(a.x, a.z), new Vector2(b.x, b.z), new Vector2(c.x, c.z), new Vector2(d.x, d.z) };
            bool front = Vector3.Dot(Vector3.Cross(c - a, b - a), (toward ?? Centre) - a) > 0f;
            mesh.triangles = front ? new[] { 0, 2, 1, 0, 3, 2 } : new[] { 0, 1, 2, 0, 2, 3 };
            mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds();
            var dir = Built + name.Replace(' ', '_') + "_" + parent.name.Replace(' ', '_') + ".asset";
            AssetDatabase.CreateAsset(mesh, dir);
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = m; if (!shadows) r.shadowCastingMode = ShadowCastingMode.Off;
            return go;
        }

        static Transform Box(Transform parent, string name, Material m, Vector3 at, Vector3 size) =>
            Prim(PrimitiveType.Cube, parent, name, m, at, size);

        static Transform Prim(PrimitiveType type, Transform parent, string name, Material m, Vector3 at, Vector3 size)
        {
            var go = GameObject.CreatePrimitive(type); go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false); go.transform.localPosition = at; go.transform.localScale = size;
            go.GetComponent<MeshRenderer>().sharedMaterial = m;
            return go.transform;
        }

        // ------------------------------------------------------------------ materials and textures

        static Material Mat(string name, Color c, float smooth, string polyHaven = null, float metres = 1f, Color? emission = null, float metal = 0f)
        {
            string p = Built + name.Replace(' ', '_') + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (emission.HasValue)
            {
                // Things that give light (the lantern's glass, the sky in the window) are unlit, at a brightness above
                // 1 so they glow (bloom); Lit's emission keyword was not kept on these materials.
                if (!m) { m = new Material(Shader.Find("Universal Render Pipeline/Unlit")); AssetDatabase.CreateAsset(m, p); }
                m.shader = Shader.Find("Universal Render Pipeline/Unlit");
                m.SetColor("_BaseColor", emission.Value);
                EditorUtility.SetDirty(m);
                return m;
            }
            if (!m) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, p); }
            m.SetColor("_BaseColor", c); m.SetFloat("_Smoothness", smooth); m.SetFloat("_Metallic", metal);
            if (polyHaven != null)
            {
                m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{CoreConfig.Textures}PolyHaven/{polyHaven}_diff_1k.jpg"));
                var nor = AssetDatabase.LoadAssetAtPath<Texture2D>($"{CoreConfig.Textures}PolyHaven/{polyHaven}_nor_gl_1k.jpg");
                if (nor) { m.SetTexture("_BumpMap", nor); m.EnableKeyword("_NORMALMAP"); }
                m.mainTextureScale = Vector2.one / metres;
            }
            if (emission.HasValue) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", emission.Value); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None; }
            EditorUtility.SetDirty(m);
            return m;
        }

        static Texture2D Save(Texture2D t, string name, bool repeat = true)
        {
            string path = Built + name + ".png";
            File.WriteAllBytes(path, t.EncodeToPNG()); Object.DestroyImmediate(t);
            AssetDatabase.ImportAsset(path);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp; imp.anisoLevel = 4; imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// <summary>
        /// Patterned cement tiles, as in many old houses of the Levant: 2 x 2 tiles of 20 cm, each a four-fold
        /// motif of a star and petals in terracotta, green and black on cream (an imagined pattern, not copied).
        /// </summary>
        static Texture2D TileTexture()
        {
            const int S = 256; var t = new Texture2D(S, S, TextureFormat.RGB24, true);
            Color cream = new Color(0.86f, 0.8f, 0.68f), terra = new Color(0.64f, 0.3f, 0.2f), green = new Color(0.25f, 0.36f, 0.3f), black = new Color(0.16f, 0.14f, 0.13f);
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    // Position inside one tile, folded to a quarter, from 0 (centre) to 1 (edge).
                    float u = (x % 128) / 127f * 2f - 1f, v = (y % 128) / 127f * 2f - 1f;
                    float ax = Mathf.Abs(u), ay = Mathf.Abs(v), r = Mathf.Sqrt(u * u + v * v), dia = ax + ay;
                    Color c = cream;
                    if (Mathf.Max(ax, ay) > 0.94f) c = black * 0.8f + cream * 0.2f;            // grout line
                    else if (Mathf.Max(ax, ay) > 0.86f) c = terra;                              // border band
                    else if (dia < 0.38f) c = terra;                                             // central diamond
                    else if (dia < 0.48f) c = cream;
                    else if (Mathf.Abs(dia - 0.62f) < 0.05f) c = black;                         // ring of the star
                    else if (r > 0.62f && r < 0.84f && Mathf.Abs(ax - ay) < 0.16f) c = green;   // petals toward the corners
                    else if ((ax < 0.08f || ay < 0.08f) && dia > 0.7f) c = green;              // petals toward the edges
                    float n = Mathf.PerlinNoise(x * 0.09f, y * 0.09f) * 0.08f - 0.04f;
                    t.SetPixel(x, y, c * (1f + n));
                }
            t.Apply(); return Save(t, "cement_tiles");
        }

        /// <summary>A woven rug: a deep red field, a dark blue border with a cream band, and rows of small diamonds.</summary>
        static Texture2D RugTexture()
        {
            const int S = 512; var t = new Texture2D(S, S, TextureFormat.RGB24, true);
            Color red = new Color(0.5f, 0.1f, 0.08f), blue = new Color(0.12f, 0.15f, 0.3f), cream = new Color(0.85f, 0.76f, 0.58f), gold = new Color(0.72f, 0.5f, 0.2f);
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float u = x / (float)S, v = y / (float)S, e = Mathf.Min(Mathf.Min(u, 1 - u), Mathf.Min(v, 1 - v));
                    Color c = red;
                    if (e < 0.012f) c = cream;
                    else if (e < 0.075f) { c = blue; float m = Mathf.Abs(((u + v) * 40f) % 2f - 1f); if (e > 0.025f && e < 0.062f && m < 0.35f) c = gold; }
                    else if (e < 0.09f) c = cream;
                    else
                    {
                        float cu = u - 0.5f, cv = v - 0.5f, dia = Mathf.Abs(cu) + Mathf.Abs(cv);
                        if (dia < 0.16f) c = dia < 0.1f ? blue : (dia < 0.12f ? cream : gold);
                        else
                        {
                            float gx = (u * 14f) % 1f - 0.5f, gy = (v * 14f) % 1f - 0.5f;
                            if (Mathf.Abs(gx) + Mathf.Abs(gy) < 0.2f) c = gold * 0.9f;
                        }
                    }
                    float n = Mathf.PerlinNoise(x * 0.4f, y * 0.05f) * 0.12f - 0.06f;
                    t.SetPixel(x, y, c * (1f + n));
                }
            t.Apply(); return Save(t, "rug", false);
        }

        /// <summary>Majlis cloth: red with black and gold stripes and a cross-stitch band (an imagined weave).</summary>
        static Texture2D ClothTexture()
        {
            const int S = 256; var t = new Texture2D(S, S, TextureFormat.RGB24, true);
            Color red = new Color(0.55f, 0.12f, 0.1f), black = new Color(0.12f, 0.1f, 0.1f), gold = new Color(0.75f, 0.55f, 0.22f), green = new Color(0.2f, 0.35f, 0.25f);
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    int band = y % 64; Color c = red;
                    if (band < 6) c = black; else if (band < 9) c = gold;
                    else if (band > 24 && band < 40) { int cx = x % 16, cy = (band - 24); c = Mathf.Abs(cx - 8) + Mathf.Abs(cy - 8) < 5 ? (cx + cy) % 2 == 0 ? black : green : red; }
                    float n = ((x * 7 + y * 13) % 5) * 0.012f;
                    t.SetPixel(x, y, c * (0.97f + n));
                }
            t.Apply(); return Save(t, "majlis_cloth");
        }

        /// <summary>The evening sky through the window: deep blue above, warm at the horizon, the city's roofs, and the Dome far off.</summary>
        static Texture2D SkyTexture()
        {
            const int Wd = 256, Ht = 340; var t = new Texture2D(Wd, Ht, TextureFormat.RGB24, true);
            Color top = new Color(0.14f, 0.22f, 0.48f), low = new Color(0.95f, 0.6f, 0.42f), city = new Color(0.07f, 0.07f, 0.12f), gold = new Color(1f, 0.75f, 0.32f);
            var rnd = new System.Random(5);
            float[] roof = new float[Wd]; float h = 70f;
            for (int x = 0; x < Wd; x++) { if (x % 18 == 0) h = 55f + (float)rnd.NextDouble() * 35f; roof[x] = h; }
            for (int y = 0; y < Ht; y++)
                for (int x = 0; x < Wd; x++)
                {
                    float v = y / (float)Ht; Color c = Color.Lerp(low, top, Mathf.SmoothStep(0.15f, 0.85f, v));
                    // The Dome of the Rock: an octagon below a dome, far off.
                    float dx = x - 150f, baseTop = 112f, domeR = 26f;
                    bool dome = (dx * dx + (y - baseTop) * (y - baseTop) < domeR * domeR && y >= baseTop);
                    bool drum = Mathf.Abs(dx) < domeR + 2f && y < baseTop && y > baseTop - 14f;
                    bool octa = Mathf.Abs(dx) < 46f && y < baseTop - 14f && y > 70f;
                    if (dome) c = Color.Lerp(gold * 0.55f, gold, Mathf.Clamp01(1f - (dx + 10f) / domeR));
                    else if (drum || octa) c = new Color(0.08f, 0.1f, 0.16f);
                    else if (y < roof[x]) c = city;
                    if (y < roof[x] && !dome && !drum && !octa && rnd.NextDouble() < 0.004) c = new Color(1f, 0.75f, 0.4f);
                    if (v > 0.6f && rnd.NextDouble() < 0.0015) c = Color.white * 0.9f;
                    t.SetPixel(x, y, c);
                }
            t.Apply(); return Save(t, "evening_sky", false);
        }

        /// <summary>The picture on the wall: a view of the Dome of the Rock rendered by this game (docs/screenshots).</summary>
        static Texture2D Picture()
        {
            const string src = "../docs/screenshots/r4-dome-today.png";
            var t = new Texture2D(2, 2);
            if (File.Exists(src)) t.LoadImage(File.ReadAllBytes(src)); else { t.SetPixel(0, 0, Color.gray); t.Apply(); }
            return Save(t, "dome_picture", false);
        }

        static void Look()
        {
            string path = Built + "Look.asset";
            AssetDatabase.DeleteAsset(path);
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);
            var tm = profile.Add<Tonemapping>(true); tm.mode.Override(TonemappingMode.ACES);
            var bl = profile.Add<Bloom>(true); bl.intensity.Override(0.7f); bl.threshold.Override(0.9f); bl.scatter.Override(0.7f);
            var ca = profile.Add<ColorAdjustments>(true); ca.postExposure.Override(0.55f); ca.contrast.Override(10f); ca.saturation.Override(4f);
            var wb = profile.Add<WhiteBalance>(true); wb.temperature.Override(12f);
            var vg = profile.Add<Vignette>(true); vg.intensity.Override(0.32f); vg.smoothness.Override(0.55f);
            foreach (var c in profile.components) AssetDatabase.AddObjectToAsset(c, profile);
            EditorUtility.SetDirty(profile);
            var vol = new GameObject("Look (evening at home)").AddComponent<Volume>(); vol.isGlobal = true; vol.sharedProfile = profile;
        }
    }
}

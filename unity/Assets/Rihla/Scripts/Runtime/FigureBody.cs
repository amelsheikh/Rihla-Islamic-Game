using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rihla
{
    /// <summary>
    /// A simple, stylised person made from smooth shapes when the scene starts: a robe or shirt and trousers, a head
    /// with a kufiya, cap, headscarf or hair, arms that swing and feet that step as the person walks. Faces have no
    /// features. Every look is modest (brief section 5): long robes and dresses, headscarves for women, sleeves to the
    /// wrist. Which dress each person wears is an estimate for today's Old City (NEEDS_REVIEW H18).
    /// </summary>
    public class FigureBody : MonoBehaviour
    {
        public enum Look { Jiddo, Yusuf, Maryam, Man, ManModern, Woman, WomanElder }

        public Look look = Look.Man;
        /// <summary>Picks the colours and small differences between people of the same look.</summary>
        public int seed;
        /// <summary>The player's child: Yusuf or Maryam, as chosen (saved), or -maryam on the command line.</summary>
        public bool hero;
        /// <summary>Hidden when farther than this from the camera (0 = always shown).</summary>
        public float cullDistance;

        Transform bodyT, armL, armR, legL, legR, footL, footR;
        Vector3 lastPos, bodyBase, footLBase, footRBase;
        float speed, phase, cullTimer, scale = 1f;
        bool culled;
        Renderer[] renderers;

        enum Head { None, Hair, Kufiya, Taqiyah, Hijab }

        class Spec
        {
            public float scale = 1f, headScale = 1f;
            public bool trousers, wideHem, beard, tatreez;
            public Head head;
            public Color skin, robe, legs, shoes = new Color(0.16f, 0.12f, 0.1f), head1, head2, trim;
        }

        // The children's colours, shared with the card where the player chooses (TeachingPages.ChooseHero).
        public static readonly Color ChildSkin = new Color(0.8f, 0.62f, 0.49f);
        public static readonly Color YusufShirt = new Color(0.24f, 0.44f, 0.72f), YusufHair = new Color(0.17f, 0.11f, 0.08f);
        public static readonly Color MaryamDress = new Color(0.5f, 0.3f, 0.44f), MaryamScarf = new Color(0.94f, 0.87f, 0.72f);

        void Awake()
        {
            if (hero && Array.IndexOf(Environment.GetCommandLineArgs(), "-maryam") >= 0) { SaveData.Current.hero = "maryam"; SaveData.Current.heroChosen = true; }
            Rebuild();
            lastPos = transform.position;
        }

        /// <summary>Builds the body again (for the child, as Yusuf or Maryam from the save).</summary>
        public void Rebuild()
        {
            if (hero) look = SaveData.Current.hero == "maryam" ? Look.Maryam : Look.Yusuf;
            var old = transform.Find("Figure");
            if (old) { old.gameObject.SetActive(false); Destroy(old.gameObject); }
            Build(MakeSpec());
            culled = false;
        }

        // ------------------------------------------------------------------ looks

        static readonly Color[] Skins =
        {
            new Color(0.78f, 0.6f, 0.47f), new Color(0.7f, 0.52f, 0.39f), new Color(0.62f, 0.45f, 0.33f),
            new Color(0.84f, 0.67f, 0.54f), new Color(0.55f, 0.39f, 0.28f),
        };
        static readonly Color[] Thobes =
        {
            new Color(0.9f, 0.88f, 0.83f), new Color(0.74f, 0.71f, 0.64f), new Color(0.55f, 0.5f, 0.42f),
            new Color(0.42f, 0.44f, 0.46f), new Color(0.82f, 0.78f, 0.68f),
        };
        static readonly Color[] Shirts =
        {
            new Color(0.82f, 0.82f, 0.8f), new Color(0.38f, 0.45f, 0.55f), new Color(0.55f, 0.5f, 0.4f),
            new Color(0.3f, 0.33f, 0.3f), new Color(0.65f, 0.6f, 0.52f),
        };
        static readonly Color[] Dresses =
        {
            new Color(0.12f, 0.12f, 0.14f), new Color(0.17f, 0.2f, 0.32f), new Color(0.3f, 0.33f, 0.24f),
            new Color(0.36f, 0.25f, 0.2f), new Color(0.38f, 0.16f, 0.2f), new Color(0.48f, 0.44f, 0.4f),
        };
        static readonly Color[] Scarves =
        {
            new Color(0.93f, 0.91f, 0.86f), new Color(0.82f, 0.74f, 0.62f), new Color(0.12f, 0.12f, 0.13f),
            new Color(0.62f, 0.7f, 0.76f), new Color(0.74f, 0.6f, 0.62f), new Color(0.55f, 0.6f, 0.5f),
        };

        Spec MakeSpec()
        {
            var r = new System.Random(seed * 7919 + (int)look * 104729);
            T Pick<T>(T[] a) => a[r.Next(a.Length)];
            float Range(float a, float b) => a + (float)r.NextDouble() * (b - a);
            var s = new Spec { skin = Pick(Skins) };
            switch (look)
            {
                case Look.Jiddo:
                    s.skin = new Color(0.74f, 0.56f, 0.43f); s.robe = new Color(0.88f, 0.85f, 0.78f);
                    s.head = Head.Kufiya; s.head1 = new Color(0.95f, 0.94f, 0.9f); s.head2 = new Color(0.1f, 0.1f, 0.11f);
                    s.beard = true; s.trim = new Color(0.86f, 0.85f, 0.82f); s.shoes = new Color(0.25f, 0.17f, 0.11f);
                    break;
                case Look.Yusuf:
                    s.scale = 0.74f; s.headScale = 1.28f; s.skin = ChildSkin;
                    s.trousers = true; s.robe = YusufShirt; s.legs = new Color(0.27f, 0.28f, 0.33f);
                    s.head = Head.Hair; s.head1 = YusufHair; s.shoes = new Color(0.92f, 0.92f, 0.9f);
                    break;
                case Look.Maryam:
                    s.scale = 0.74f; s.headScale = 1.28f; s.skin = ChildSkin;
                    s.wideHem = true; s.robe = MaryamDress;
                    s.head = Head.Hijab; s.head1 = MaryamScarf; s.shoes = new Color(0.92f, 0.92f, 0.9f);
                    break;
                case Look.Man:
                    s.scale = Range(0.97f, 1.05f); s.robe = Pick(Thobes);
                    bool kufiya = r.NextDouble() < 0.5;
                    s.head = kufiya ? Head.Kufiya : Head.Taqiyah;
                    s.head1 = kufiya ? new Color(0.95f, 0.94f, 0.9f) : new Color(0.93f, 0.92f, 0.88f);
                    s.head2 = r.NextDouble() < 0.6 ? new Color(0.1f, 0.1f, 0.11f) : new Color(0.62f, 0.12f, 0.12f);
                    s.beard = r.NextDouble() < 0.5; s.trim = r.NextDouble() < 0.5 ? new Color(0.18f, 0.14f, 0.12f) : new Color(0.6f, 0.58f, 0.55f);
                    break;
                case Look.ManModern:
                    s.scale = Range(0.96f, 1.05f); s.trousers = true; s.robe = Pick(Shirts);
                    s.legs = Pick(new[] { new Color(0.2f, 0.21f, 0.24f), new Color(0.35f, 0.32f, 0.28f), new Color(0.28f, 0.3f, 0.36f) });
                    s.head = Head.Hair; s.head1 = r.NextDouble() < 0.75 ? new Color(0.12f, 0.09f, 0.07f) : new Color(0.55f, 0.54f, 0.52f);
                    s.beard = r.NextDouble() < 0.35; s.trim = s.head1;
                    break;
                case Look.Woman:
                    s.scale = Range(0.92f, 0.98f); s.wideHem = true; s.robe = Pick(Dresses);
                    s.head = Head.Hijab; s.head1 = Pick(Scarves);
                    break;
                case Look.WomanElder:
                    // A dark Palestinian thob with red cross-stitch (tatreez) on the chest and hem, and a white veil.
                    s.scale = Range(0.9f, 0.95f); s.wideHem = true; s.robe = new Color(0.11f, 0.1f, 0.12f);
                    s.tatreez = true; s.trim = new Color(0.66f, 0.12f, 0.13f);
                    s.head = Head.Hijab; s.head1 = new Color(0.94f, 0.93f, 0.9f);
                    break;
            }
            return s;
        }

        // ------------------------------------------------------------------ building the body

        void Build(Spec s)
        {
            scale = s.scale;
            var root = new GameObject("Figure").transform;
            root.SetParent(transform, false); root.localScale = Vector3.one * s.scale;
            float hs = s.headScale, hy = 1.61f + (hs - 1f) * 0.07f;

            var body = new Parts();
            Material skin = Mat(s.skin, 0.3f), robe = Mat(s.robe, 0.08f), shoes = Mat(s.shoes, 0.35f);
            if (s.trousers)
                Lathe(body.Of(robe), Shirt, 0.66f, 0f, 360f);
            else
                Lathe(body.Of(robe), s.wideHem ? Dress : Thobe, 0.68f, 0f, 360f);
            if (s.tatreez)
            {
                var trim = body.Of(Mat(s.trim, 0.15f));
                Lathe(trim, new[] { new Vector2(1.04f, 0.198f), new Vector2(1.2f, 0.208f), new Vector2(1.34f, 0.218f) }, 0.68f, -38f, 38f, 1.03f, true);
                Lathe(trim, new[] { new Vector2(0.06f, 0.307f), new Vector2(0.17f, 0.297f) }, 0.68f, 0f, 360f, 1.02f, true);
            }
            Lathe(body.Of(skin), new[] { new Vector2(1.44f, 0f), new Vector2(1.44f, 0.05f), new Vector2(1.58f, 0.045f), new Vector2(1.58f, 0f) }, 1f, 0f, 360f);
            Ellipsoid(body.Of(skin), new Vector3(0f, hy, 0f), new Vector3(0.09f, 0.11f, 0.1f) * hs);
            HeadWear(body, s, hy, hs);
            if (s.beard) Ellipsoid(body.Of(Mat(s.trim, 0.1f)), new Vector3(0f, hy - 0.075f * hs, 0.055f * hs), new Vector3(0.072f, 0.055f, 0.055f) * hs);
            bodyT = body.Make("Body", root, Vector3.zero);
            bodyBase = bodyT.localPosition;

            // Arms: a sleeve to the wrist and a hand, swinging from the shoulder.
            var sleeve = new[] { new Vector2(-0.56f, 0f), new Vector2(-0.56f, 0.058f), new Vector2(-0.28f, 0.056f), new Vector2(0f, 0.066f), new Vector2(0.035f, 0f) };
            for (int side = -1; side <= 1; side += 2)
            {
                var arm = new Parts();
                Lathe(arm.Of(robe), sleeve, 1f, 0f, 360f);
                Ellipsoid(arm.Of(skin), new Vector3(0f, -0.6f, 0.01f), new Vector3(0.038f, 0.055f, 0.034f));
                var t = arm.Make(side < 0 ? "Arm L" : "Arm R", bodyT, new Vector3(0.205f * side, 1.39f, 0f));
                t.localRotation = Quaternion.Euler(0f, 0f, 4f * side);
                if (side < 0) armL = t; else armR = t;
            }

            if (s.trousers)
            {
                var leg = new[] { new Vector2(-0.8f, 0f), new Vector2(-0.8f, 0.058f), new Vector2(-0.4f, 0.064f), new Vector2(0f, 0.085f), new Vector2(0.03f, 0f) };
                for (int side = -1; side <= 1; side += 2)
                {
                    var p = new Parts();
                    Lathe(p.Of(Mat(s.legs, 0.1f)), leg, 1f, 0f, 360f);
                    Ellipsoid(p.Of(shoes), new Vector3(0f, -0.81f, 0.035f), new Vector3(0.048f, 0.036f, 0.1f));
                    var t = p.Make(side < 0 ? "Leg L" : "Leg R", root, new Vector3(0.09f * side, 0.86f, 0f));
                    if (side < 0) legL = t; else legR = t;
                }
            }
            else
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    var p = new Parts();
                    Ellipsoid(p.Of(shoes), Vector3.zero, new Vector3(0.048f, 0.036f, 0.1f));
                    var t = p.Make(side < 0 ? "Foot L" : "Foot R", root, new Vector3(0.085f * side, 0.035f, 0.03f));
                    if (side < 0) { footL = t; footLBase = t.localPosition; } else { footR = t; footRBase = t.localPosition; }
                }
            }
            renderers = GetComponentsInChildren<Renderer>();
            foreach (var rd in renderers) { rd.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; rd.receiveShadows = true; }
        }

        void HeadWear(Parts body, Spec s, float hy, float hs)
        {
            switch (s.head)
            {
                case Head.Hair:
                    Ellipsoid(body.Of(Mat(s.head1, 0.25f)), new Vector3(0f, hy + 0.022f * hs, -0.022f * hs), new Vector3(0.095f, 0.103f, 0.1f) * hs);
                    break;
                case Head.Taqiyah:
                    Ellipsoid(body.Of(Mat(s.head1, 0.1f)), new Vector3(0f, hy + 0.058f * hs, -0.008f * hs), new Vector3(0.096f, 0.066f, 0.105f) * hs);
                    break;
                case Head.Hijab:
                {
                    // A hood a little larger than the head and set back, so the face shows in an oval, then a drape over
                    // the neck and shoulders.
                    var cloth = body.Of(Mat(s.head1, 0.06f));
                    Ellipsoid(cloth, new Vector3(0f, hy + 0.012f * hs, -0.042f * hs), new Vector3(0.113f, 0.13f, 0.13f) * hs);
                    Lathe(cloth, new[] { new Vector2(1.37f, 0.24f), new Vector2(1.44f, 0.195f), new Vector2(1.5f, 0.125f), new Vector2(1.56f, 0.08f) }, 0.72f, 0f, 360f, 1f, true);
                    break;
                }
                case Head.Kufiya:
                {
                    // The kufiya (checked cloth) over the head and falling behind the shoulders, held by a black agal.
                    var cloth = body.Of(Mat(s.head1, 0.05f, Check(s.head2)));
                    Ellipsoid(cloth, new Vector3(0f, hy + 0.024f * hs, -0.038f * hs), new Vector3(0.11f, 0.12f, 0.122f) * hs);
                    Lathe(cloth, new[] { new Vector2(1.32f, 0.24f), new Vector2(1.45f, 0.19f), new Vector2(1.56f, 0.125f), new Vector2(hy + 0.05f * hs, 0.112f * hs) }, 0.8f, 75f, 285f, 1f, true);
                    var agal = new[] { new Vector2(-0.014f, 0.1f), new Vector2(0f, 0.116f), new Vector2(0.014f, 0.1f), new Vector2(0f, 0.086f), new Vector2(-0.014f, 0.1f) };
                    for (int i = 0; i < agal.Length; i++) agal[i] = new Vector2(agal[i].x + hy + 0.075f * hs, agal[i].y * hs);
                    Lathe(body.Of(Mat(new Color(0.06f, 0.06f, 0.07f), 0.3f)), agal, 1.06f, 0f, 360f, 1f, false, new Vector3(0f, 0f, -0.03f * hs));
                    break;
                }
            }
        }

        // Profiles (height, radius) from the hem up, for a person 1.74 m tall before scaling.
        static readonly Vector2[] Thobe =
        {
            new Vector2(0.03f, 0f), new Vector2(0.03f, 0.25f), new Vector2(0.4f, 0.235f), new Vector2(0.8f, 0.205f), new Vector2(1.0f, 0.2f),
            new Vector2(1.2f, 0.215f), new Vector2(1.34f, 0.222f), new Vector2(1.42f, 0.19f), new Vector2(1.47f, 0.1f), new Vector2(1.5f, 0f),
        };
        static readonly Vector2[] Dress =
        {
            new Vector2(0.03f, 0f), new Vector2(0.03f, 0.31f), new Vector2(0.4f, 0.27f), new Vector2(0.8f, 0.215f), new Vector2(1.0f, 0.196f),
            new Vector2(1.2f, 0.208f), new Vector2(1.34f, 0.218f), new Vector2(1.42f, 0.188f), new Vector2(1.47f, 0.1f), new Vector2(1.5f, 0f),
        };
        static readonly Vector2[] Shirt =
        {
            new Vector2(0.8f, 0f), new Vector2(0.8f, 0.19f), new Vector2(1.0f, 0.2f), new Vector2(1.2f, 0.215f),
            new Vector2(1.34f, 0.222f), new Vector2(1.42f, 0.19f), new Vector2(1.47f, 0.1f), new Vector2(1.5f, 0f),
        };

        // ------------------------------------------------------------------ walking

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            Vector3 p = transform.position, d = p - lastPos; d.y = 0f; lastPos = p;
            float v = d.magnitude / dt; if (v > 6f) v = 0f; // a teleport is not a step
            speed = Mathf.Lerp(speed, v, 1f - Mathf.Exp(-10f * dt));

            if (cullDistance > 0f && (cullTimer -= dt) <= 0f)
            {
                cullTimer = 0.4f + UnityEngine.Random.value * 0.2f;
                var cam = Camera.main;
                bool far = cam && (cam.transform.position - p).sqrMagnitude > cullDistance * cullDistance;
                if (far != culled) { culled = far; foreach (var rd in renderers) rd.enabled = !far; }
            }
            if (culled) return;

            float k = Mathf.Clamp01(speed / (1.2f * scale));
            phase += speed * dt / (0.62f * scale) * Mathf.PI;
            float sw = Mathf.Sin(phase);
            if (armL) armL.localRotation = Quaternion.Euler(sw * 26f * k, 0f, -4f);
            if (armR) armR.localRotation = Quaternion.Euler(-sw * 26f * k, 0f, 4f);
            if (legL) legL.localRotation = Quaternion.Euler(-sw * 24f * k, 0f, 0f);
            if (legR) legR.localRotation = Quaternion.Euler(sw * 24f * k, 0f, 0f);
            if (footL) footL.localPosition = footLBase + new Vector3(0f, Mathf.Max(0f, -Mathf.Cos(phase)) * 0.03f * k, sw * 0.14f * k);
            if (footR) footR.localPosition = footRBase + new Vector3(0f, Mathf.Max(0f, Mathf.Cos(phase)) * 0.03f * k, -sw * 0.14f * k);
            float breathe = Mathf.Sin(Time.time * 1.7f + seed) * 0.004f;
            bodyT.localPosition = bodyBase + new Vector3(0f, Mathf.Abs(Mathf.Cos(phase)) * 0.022f * k + breathe, 0f);
        }

        // ------------------------------------------------------------------ meshes

        class Parts
        {
            public readonly List<Vector3> v = new List<Vector3>(); public readonly List<Vector2> uv = new List<Vector2>();
            readonly List<Material> mats = new List<Material>(); readonly List<List<int>> tris = new List<List<int>>();
            public (Parts p, List<int> t) Of(Material m)
            {
                int i = mats.IndexOf(m);
                if (i < 0) { mats.Add(m); tris.Add(new List<int>()); i = mats.Count - 1; }
                return (this, tris[i]);
            }
            public Transform Make(string name, Transform parent, Vector3 at)
            {
                var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.localPosition = at;
                var m = new Mesh { name = name };
                m.SetVertices(v); m.SetUVs(0, uv); m.subMeshCount = tris.Count;
                for (int i = 0; i < tris.Count; i++) m.SetTriangles(tris[i], i);
                m.RecalculateNormals(); m.RecalculateBounds();
                go.AddComponent<MeshFilter>().sharedMesh = m;
                go.AddComponent<MeshRenderer>().sharedMaterials = mats.ToArray();
                return go.transform;
            }
        }

        /// <summary>
        /// A surface turned around the upright axis: profile points are (height, radius), the angle runs from a0 to a1
        /// degrees (0 = in front), depth squashes it front to back. Two-sided for open cloth such as a drape.
        /// </summary>
        static void Lathe((Parts p, List<int> t) at, Vector2[] profile, float depth, float a0, float a1,
                          float grow = 1f, bool twoSided = false, Vector3 offset = default)
        {
            // Split the profile at sharp corners (a hem, a cuff) so each piece gets its own normals and the edge stays
            // crisp instead of shading dark.
            var piece = new List<Vector2> { profile[0] };
            for (int i = 1; i < profile.Length; i++)
            {
                piece.Add(profile[i]);
                if (i < profile.Length - 1 && Vector2.Angle(profile[i] - profile[i - 1], profile[i + 1] - profile[i]) > 50f)
                {
                    LatheStrip(at, piece.ToArray(), depth, a0, a1, grow, twoSided, offset);
                    piece = new List<Vector2> { profile[i] };
                }
            }
            LatheStrip(at, piece.ToArray(), depth, a0, a1, grow, twoSided, offset);
        }

        static void LatheStrip((Parts p, List<int> t) at, Vector2[] profile, float depth, float a0, float a1,
                               float grow, bool twoSided, Vector3 offset)
        {
            var (p, t) = at;
            int segs = Mathf.Max(4, Mathf.RoundToInt(Mathf.Abs(a1 - a0) / 22.5f));
            bool full = Mathf.Abs(a1 - a0) >= 359.9f;
            for (int pass = 0; pass < (twoSided ? 2 : 1); pass++)
            {
                int s0 = p.v.Count, cols = segs + 1;
                for (int j = 0; j < profile.Length; j++)
                    for (int k = 0; k <= segs; k++)
                    {
                        float a = Mathf.Deg2Rad * Mathf.Lerp(a0, a1, k / (float)segs), r = profile[j].y * grow;
                        p.v.Add(offset + new Vector3(Mathf.Sin(a) * r, profile[j].x, Mathf.Cos(a) * r * depth));
                        p.uv.Add(new Vector2(k / (float)segs * (full ? 4f : 2f), profile[j].x * 3f));
                    }
                // Winding chosen so the first quad faces away from the axis (the second pass faces inwards).
                bool flip = pass == 1;
                for (int j = 0; j < profile.Length - 1; j++)
                    for (int k = 0; k < segs; k++)
                    {
                        int a = s0 + j * cols + k, b = a + cols;
                        if (!flip) t.AddRange(new[] { a, a + 1, b, a + 1, b + 1, b });
                        else t.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 });
                    }
            }
        }

        static void Ellipsoid((Parts p, List<int> t) at, Vector3 c, Vector3 radii, int segs = 14, int rings = 9)
        {
            var (p, t) = at;
            int s0 = p.v.Count;
            for (int j = 0; j <= rings; j++)
            {
                float v = Mathf.PI * j / rings, y = Mathf.Cos(v), rr = Mathf.Sin(v);
                for (int k = 0; k <= segs; k++)
                {
                    float u = 2f * Mathf.PI * k / segs;
                    p.v.Add(c + Vector3.Scale(new Vector3(Mathf.Sin(u) * rr, y, Mathf.Cos(u) * rr), radii));
                    p.uv.Add(new Vector2(k / (float)segs * 2f, j / (float)rings * 2f));
                }
            }
            for (int j = 0; j < rings; j++)
                for (int k = 0; k < segs; k++)
                {
                    int a = s0 + j * (segs + 1) + k, b = a + segs + 1;
                    t.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 });
                }
        }

        // ------------------------------------------------------------------ materials

        static readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();
        static readonly Dictionary<Color, Texture2D> checks = new Dictionary<Color, Texture2D>();

        static Material Mat(Color c, float smooth, Texture2D tex = null)
        {
            string key = ColorUtility.ToHtmlStringRGB(c) + smooth.ToString("F2") + (tex ? tex.name : "");
            if (mats.TryGetValue(key, out var m) && m) return m;
            m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "Figure " + key };
            m.SetColor("_BaseColor", c); m.SetFloat("_Smoothness", smooth);
            if (tex) m.SetTexture("_BaseMap", tex);
            mats[key] = m;
            return m;
        }

        /// <summary>The kufiya's woven check: a fine grid of the second colour over white cloth.</summary>
        static Texture2D Check(Color line)
        {
            if (checks.TryGetValue(line, out var t) && t) return t;
            const int n = 64;
            t = new Texture2D(n, n, TextureFormat.RGBA32, true) { name = "check" + ColorUtility.ToHtmlStringRGB(line), wrapMode = TextureWrapMode.Repeat };
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    bool gx = x % 8 < 2, gy = y % 8 < 2, dot = (x + y) % 8 < 1;
                    float w = gx && gy ? 1f : (gx || gy ? 0.75f : dot ? 0.35f : 0f);
                    t.SetPixel(x, y, Color.Lerp(Color.white, line, w));
                }
            t.Apply(true);
            checks[line] = t;
            return t;
        }
    }
}

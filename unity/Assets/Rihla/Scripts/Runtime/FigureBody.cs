using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rihla
{
    /// <summary>
    /// A simple, stylised person made from smooth shapes when the scene starts: a robe or shirt and trousers, a head
    /// with a kufiya, cap, headscarf or hair, arms that swing and feet that step as the person walks. Faces have no
    /// features. Every look is modest (brief section 5): long robes and dresses, headscarves for women, sleeves to the
    /// wrist. Which dress each person wears is an estimate for today's Old City (NEEDS_REVIEW H18). In the Time Lens's
    /// "then" view the townspeople wear the dress of about 1910 instead (see Period below; also an estimate, H18).
    /// </summary>
    public class FigureBody : MonoBehaviour
    {
        public enum Look { Jiddo, Yusuf, Maryam, Man, ManModern, Woman, WomanElder, ElderMan, Boy, Girl }

        public Look look = Look.Man;
        /// <summary>Picks the colours and small differences between people of the same look.</summary>
        public int seed;
        /// <summary>The player's child: Yusuf or Maryam, as chosen (saved), or -maryam on the command line.</summary>
        public bool hero;
        /// <summary>Hidden when farther than this from the camera (0 = always shown).</summary>
        public float cullDistance;

        Transform bodyT, armL, armR, legL, legR, footL, footR;
        Vector3 lastPos, bodyBase, footLBase, footRBase;
        /// <summary>How far the right arm swings: less when it holds a walking stick.</summary>
        float swingR = 1f;
        float speed, phase, cullTimer, scale = 1f;
        bool culled;
        Renderer[] renderers;
        Transform figure;

        /// <summary>A built body, kept so a townsperson can switch between the dress of 1910 and of today without
        /// building it again (building all of them takes about 75 ms, a visible stutter).</summary>
        class Built
        {
            public Transform figure, bodyT, armL, armR, legL, legR, footL, footR;
            public Vector3 bodyBase, footLBase, footRBase;
            public Renderer[] renderers;
            public float scale, swingR;
        }
        Built thenBody, nowBody;

        enum Head { None, Hair, Kufiya, Taqiyah, Hijab, Tarboush }

        class Spec
        {
            public float scale = 1f, headScale = 1f;
            public bool trousers, wideHem, beard, tatreez, turban, cloak, longWrap, sash, stick;
            public Head head;
            public Color skin, robe, legs, shoes = new Color(0.16f, 0.12f, 0.1f), head1, head2, trim, cloak1, cloak2, sashColor;
            /// <summary>Second colour for vertical stripes on the robe (alpha 0 = plain).</summary>
            public Color stripe;
        }

        // ------------------------------------------------------------------ the era (Time Lens)

        static readonly List<FigureBody> all = new List<FigureBody>();
        /// <summary>True while the walk shows "then" (about 1910): townspeople wear the dress of that time.</summary>
        public static bool Then { get; private set; }

        /// <summary>Called by the Time Lens when the view crosses between "then" and "now".</summary>
        public static void SetEra(bool then)
        {
            if (Then == then) return;
            Then = then;
            foreach (var f in all) if (f && f.Townsperson) f.ShowEra();
        }

        /// <summary>Keeps the body of the era being left, and shows the other era's body (built the first time).</summary>
        void ShowEra()
        {
            if (!figure) { Rebuild(); return; }
            var leaving = new Built
            {
                figure = figure, bodyT = bodyT, armL = armL, armR = armR, legL = legL, legR = legR, footL = footL, footR = footR,
                bodyBase = bodyBase, footLBase = footLBase, footRBase = footRBase, renderers = renderers, scale = scale, swingR = swingR,
            };
            if (Then) nowBody = leaving; else thenBody = leaving;
            figure.gameObject.SetActive(false);
            var next = Then ? thenBody : nowBody;
            if (next != null && next.figure)
            {
                figure = next.figure; bodyT = next.bodyT; armL = next.armL; armR = next.armR; legL = next.legL; legR = next.legR;
                footL = next.footL; footR = next.footR; bodyBase = next.bodyBase; footLBase = next.footLBase; footRBase = next.footRBase;
                renderers = next.renderers; scale = next.scale; swingR = next.swingR;
                figure.gameObject.SetActive(true);
                foreach (var rd in renderers) rd.enabled = true;
            }
            else
            {
                armL = armR = legL = legR = footL = footR = null;
                Build(Then ? Period() : MakeSpec());
            }
            culled = false;
        }

        bool Townsperson => !hero && look != Look.Jiddo;

        void OnEnable() { if (!all.Contains(this)) all.Add(this); }
        void OnDestroy() => all.Remove(this);

        // The children's colours, shared with the card where the player chooses (TeachingPages.ChooseHero).
        public static readonly Color ChildSkin = new Color(0.8f, 0.62f, 0.49f);
        public static readonly Color YusufShirt = new Color(0.24f, 0.44f, 0.72f), YusufHair = new Color(0.17f, 0.11f, 0.08f);
        public static readonly Color MaryamDress = new Color(0.5f, 0.3f, 0.44f), MaryamScarf = new Color(0.94f, 0.87f, 0.72f);

        void Awake()
        {
            Rebuild();
            lastPos = transform.position;
        }

        /// <summary>Builds the body again (for the child, as Yusuf or Maryam from the save).</summary>
        public void Rebuild()
        {
            if (hero) look = SaveData.Current.hero == "maryam" ? Look.Maryam : Look.Yusuf;
            var old = figure;
            if (old)
            {
                foreach (var mf in old.GetComponentsInChildren<MeshFilter>(true)) Destroy(mf.sharedMesh);
                old.gameObject.SetActive(false); Destroy(old.gameObject);
            }
            armL = armR = legL = legR = footL = footR = null;
            Build(Then && Townsperson ? Period() : MakeSpec());
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
                    // A dark Palestinian thob with red cross-stitch (tatreez) on the chest and hem, and a white veil;
                    // some walk with a stick.
                    s.scale = Range(0.9f, 0.95f); s.wideHem = true; s.robe = new Color(0.11f, 0.1f, 0.12f);
                    s.tatreez = true; s.trim = new Color(0.66f, 0.12f, 0.13f);
                    s.head = Head.Hijab; s.head1 = new Color(0.94f, 0.93f, 0.9f);
                    s.stick = r.NextDouble() < 0.6;
                    break;
                case Look.ElderMan:
                    // An old man with a white or grey beard and a walking stick, in a thobe with a kufiya or a white cap.
                    ElderMan(s, r, Pick(Thobes), r.NextDouble() < 0.6 ? Head.Kufiya : Head.Taqiyah);
                    break;
                case Look.Boy:
                    s.scale = Range(0.6f, 0.76f); s.headScale = 1.26f; s.skin = Pick(Skins);
                    if (r.NextDouble() < 0.7)
                    {
                        s.trousers = true; s.robe = Pick(Shirts); s.legs = Pick(new[] { new Color(0.22f, 0.24f, 0.3f), new Color(0.3f, 0.28f, 0.25f) });
                        s.head = Head.Hair; s.head1 = new Color(0.13f, 0.09f, 0.07f); s.shoes = Pick(new[] { new Color(0.9f, 0.9f, 0.88f), new Color(0.2f, 0.2f, 0.22f) });
                    }
                    else { s.robe = Pick(Thobes); s.head = Head.Taqiyah; s.head1 = new Color(0.93f, 0.92f, 0.88f); }
                    break;
                case Look.Girl:
                    // Girls as Maryam is dressed: a long dress and a headscarf (the owner's choice for Maryam, H18).
                    s.scale = Range(0.6f, 0.76f); s.headScale = 1.26f; s.skin = Pick(Skins);
                    s.wideHem = true; s.robe = Pick(new[] { new Color(0.45f, 0.28f, 0.4f), new Color(0.24f, 0.36f, 0.5f), new Color(0.32f, 0.42f, 0.32f), new Color(0.55f, 0.35f, 0.28f) });
                    s.head = Head.Hijab; s.head1 = Pick(Scarves); s.shoes = new Color(0.9f, 0.9f, 0.88f);
                    break;
            }
            return s;
        }

        /// <summary>
        /// The dress of about 1910, an estimate from period photographs that needs checking (NEEDS_REVIEW H18). Men of
        /// the town in a red tarboush (some wound with a white turban), a long striped robe and a sash; men from the
        /// villages in a kufiya and agal with a striped abaya over the robe; men in European jackets with a tarboush;
        /// women of the town in a dark wrap from head to below the waist; village women in a long embroidered thob and a
        /// white veil. Faces stay without features, everyone stays covered (brief section 5).
        /// </summary>
        Spec Period()
        {
            var r = new System.Random(seed * 6151 + (int)look * 31337);
            T Pick<T>(T[] a) => a[r.Next(a.Length)];
            float Range(float a, float b) => a + (float)r.NextDouble() * (b - a);
            var s = new Spec { skin = Pick(Skins) };
            Color red = new Color(0.58f, 0.1f, 0.1f), white = new Color(0.93f, 0.92f, 0.88f);
            switch (look)
            {
                case Look.Man:
                    s.scale = Range(0.97f, 1.05f);
                    s.beard = r.NextDouble() < 0.7; s.trim = r.NextDouble() < 0.6 ? new Color(0.18f, 0.14f, 0.12f) : new Color(0.6f, 0.58f, 0.55f);
                    if (r.NextDouble() < 0.45)
                    {
                        // Of the town: tarboush, striped robe (qumbaz) and a sash.
                        s.head = Head.Tarboush; s.head1 = red; s.turban = r.NextDouble() < 0.35; s.head2 = white;
                        s.robe = Pick(new[] { new Color(0.82f, 0.78f, 0.68f), new Color(0.7f, 0.66f, 0.55f), new Color(0.55f, 0.5f, 0.42f) });
                        s.stripe = Pick(new[] { new Color(0.42f, 0.33f, 0.24f), new Color(0.3f, 0.32f, 0.4f), new Color(0.5f, 0.2f, 0.18f) });
                        s.sash = true; s.sashColor = Pick(new[] { new Color(0.3f, 0.22f, 0.15f), new Color(0.8f, 0.75f, 0.6f), new Color(0.4f, 0.12f, 0.12f) });
                    }
                    else
                    {
                        // From the villages: kufiya and agal, a white robe and a striped abaya.
                        s.head = Head.Kufiya; s.head1 = white; s.head2 = r.NextDouble() < 0.5 ? new Color(0.1f, 0.1f, 0.11f) : white;
                        s.robe = new Color(0.88f, 0.86f, 0.8f);
                        s.cloak = true;
                        s.cloak1 = Pick(new[] { new Color(0.34f, 0.24f, 0.16f), new Color(0.15f, 0.13f, 0.12f), new Color(0.45f, 0.35f, 0.24f) });
                        s.cloak2 = Pick(new[] { new Color(0.86f, 0.82f, 0.72f), new Color(0.62f, 0.52f, 0.38f) });
                    }
                    break;
                case Look.ManModern:
                    // A European jacket and trousers with a tarboush.
                    s.scale = Range(0.96f, 1.05f); s.trousers = true;
                    s.robe = Pick(new[] { new Color(0.16f, 0.16f, 0.18f), new Color(0.3f, 0.26f, 0.22f), new Color(0.22f, 0.24f, 0.3f) });
                    s.legs = s.robe * 0.9f; s.legs.a = 1f;
                    s.head = Head.Tarboush; s.head1 = red;
                    s.beard = r.NextDouble() < 0.4; s.trim = new Color(0.14f, 0.11f, 0.09f);
                    break;
                case Look.Woman:
                    s.scale = Range(0.92f, 0.98f); s.wideHem = true; s.head = Head.Hijab;
                    if (r.NextDouble() < 0.5)
                    {
                        // Of the town: a dark wrap from the head to below the waist over a long dress.
                        s.robe = Pick(new[] { new Color(0.1f, 0.1f, 0.12f), new Color(0.14f, 0.15f, 0.22f), new Color(0.25f, 0.2f, 0.18f) });
                        s.head1 = Pick(new[] { new Color(0.08f, 0.08f, 0.09f), new Color(0.12f, 0.12f, 0.16f) });
                        s.longWrap = true;
                    }
                    else
                    {
                        // From the villages: a long thob, white or indigo, with red embroidery, and a white veil.
                        s.robe = r.NextDouble() < 0.5 ? new Color(0.86f, 0.83f, 0.75f) : new Color(0.13f, 0.15f, 0.26f);
                        s.tatreez = true; s.trim = new Color(0.62f, 0.1f, 0.12f);
                        s.head1 = white;
                    }
                    break;
                case Look.ElderMan:
                    ElderMan(s, r, new Color(0.84f, 0.8f, 0.7f), r.NextDouble() < 0.5 ? Head.Kufiya : Head.Tarboush);
                    if (s.head == Head.Tarboush) { s.head1 = red; s.turban = true; s.head2 = white; s.stripe = new Color(0.42f, 0.33f, 0.24f); s.sash = true; s.sashColor = new Color(0.3f, 0.22f, 0.15f); }
                    else { s.head1 = white; s.cloak = true; s.cloak1 = new Color(0.3f, 0.22f, 0.15f); s.cloak2 = new Color(0.86f, 0.82f, 0.72f); }
                    break;
                case Look.Boy:
                    // A long robe with a sash, and a tarboush or a white cap.
                    s.scale = Range(0.6f, 0.76f); s.headScale = 1.26f;
                    s.robe = Pick(new[] { new Color(0.86f, 0.83f, 0.75f), new Color(0.7f, 0.66f, 0.55f) });
                    s.sash = true; s.sashColor = Pick(new[] { new Color(0.4f, 0.12f, 0.12f), new Color(0.3f, 0.22f, 0.15f) });
                    bool tarboush = r.NextDouble() < 0.5;
                    s.head = tarboush ? Head.Tarboush : Head.Taqiyah; s.head1 = tarboush ? red : white;
                    break;
                case Look.Girl:
                    // A long dress, some embroidered, and a white head cloth.
                    s.scale = Range(0.6f, 0.76f); s.headScale = 1.26f; s.wideHem = true;
                    s.robe = Pick(new[] { new Color(0.86f, 0.83f, 0.75f), new Color(0.13f, 0.15f, 0.26f), new Color(0.4f, 0.2f, 0.2f) });
                    s.tatreez = r.NextDouble() < 0.5; s.trim = new Color(0.62f, 0.1f, 0.12f);
                    s.head = Head.Hijab; s.head1 = white;
                    break;
                default:
                    return MakeSpec(); // the older women's dark thob with tatreez and a white veil fits both times
            }
            return s;
        }

        static void ElderMan(Spec s, System.Random r, Color robe, Head head)
        {
            s.scale = 0.93f + (float)r.NextDouble() * 0.06f; s.robe = robe; s.head = head; s.stick = true;
            s.head1 = new Color(0.94f, 0.93f, 0.89f);
            s.head2 = r.NextDouble() < 0.6 ? new Color(0.1f, 0.1f, 0.11f) : new Color(0.62f, 0.12f, 0.12f);
            s.beard = true; s.trim = r.NextDouble() < 0.5 ? new Color(0.9f, 0.89f, 0.86f) : new Color(0.66f, 0.64f, 0.6f);
            s.skin = new Color(0.72f, 0.55f, 0.43f);
        }

        // ------------------------------------------------------------------ building the body

        void Build(Spec s)
        {
            scale = s.scale;
            var root = new GameObject("Figure").transform; figure = root;
            root.SetParent(transform, false); root.localScale = Vector3.one * s.scale;
            float hs = s.headScale, hy = 1.61f + (hs - 1f) * 0.07f;

            var body = new Parts();
            Material skin = Mat(s.skin, 0.3f), robe = s.stripe.a > 0f ? Mat(Color.white, 0.08f, Stripes(s.robe, s.stripe, 3)) : Mat(s.robe, 0.08f), shoes = Mat(s.shoes, 0.35f);
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
            if (s.sash)
                Lathe(body.Of(Mat(s.sashColor, 0.1f)), new[] { new Vector2(0.96f, 0.207f), new Vector2(1.06f, 0.209f) }, 0.68f, 0f, 360f, 1.03f);
            if (s.cloak)
            {
                // The abaya: a wide cloak with broad stripes, open at the front.
                var cloak = new[] { new Vector2(0.2f, 0.29f), new Vector2(0.7f, 0.265f), new Vector2(1.1f, 0.25f), new Vector2(1.34f, 0.255f),
                                    new Vector2(1.42f, 0.215f), new Vector2(1.47f, 0.13f), new Vector2(1.5f, 0.09f) };
                Lathe(body.Of(Mat(Color.white, 0.05f, Stripes(s.cloak1, s.cloak2, 1))), cloak, 0.72f, 32f, 328f, 1f, true);
            }
            if (s.longWrap)
            {
                // The wrap of the town's women falls from the head over the shoulders and arms to below the waist.
                var wrap = new[] { new Vector2(0.86f, 0.3f), new Vector2(1.1f, 0.285f), new Vector2(1.34f, 0.28f), new Vector2(1.43f, 0.23f),
                                   new Vector2(1.5f, 0.15f), new Vector2(1.56f, 0.09f) };
                Lathe(body.Of(Mat(s.head1, 0.06f)), wrap, 0.74f, 0f, 360f, 1f, true);
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
                if (s.stick && side > 0)
                {
                    // A plain wooden walking stick from the hand to the ground, a little forward, with a rounded top.
                    var wood = arm.Of(Mat(new Color(0.3f, 0.2f, 0.12f), 0.25f));
                    Lathe(wood, new[] { new Vector2(-1.39f, 0f), new Vector2(-1.39f, 0.013f), new Vector2(-0.56f, 0.015f), new Vector2(-0.56f, 0f) },
                          1f, 0f, 360f, 1f, false, new Vector3(0f, 0f, 0.06f));
                    Ellipsoid(wood, new Vector3(0f, -0.545f, 0.06f), new Vector3(0.022f, 0.02f, 0.03f), 8, 5);
                }
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
            swingR = s.stick ? 0.35f : 1f;
            renderers = root.GetComponentsInChildren<Renderer>();
            foreach (var rd in renderers) { rd.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; rd.receiveShadows = true; }
        }

        void HeadWear(Parts body, Spec s, float hy, float hs)
        {
            switch (s.head)
            {
                case Head.Hair:
                    Ellipsoid(body.Of(Mat(s.head1, 0.25f)), new Vector3(0f, hy + 0.022f * hs, -0.022f * hs), new Vector3(0.095f, 0.103f, 0.1f) * hs);
                    break;
                case Head.Tarboush:
                {
                    // A red felt tarboush, a cut-off cone, with a short black tassel; some wound with a white turban.
                    float b = hy + 0.045f * hs, top = hy + 0.17f * hs;
                    Lathe(body.Of(Mat(s.head1, 0.15f)), new[] { new Vector2(b, 0f), new Vector2(b, 0.094f * hs), new Vector2(top, 0.074f * hs), new Vector2(top, 0f) },
                          1f, 0f, 360f, 1f, false, new Vector3(0f, 0f, -0.012f * hs));
                    Ellipsoid(body.Of(Mat(new Color(0.05f, 0.05f, 0.06f), 0.2f)), new Vector3(0f, top - 0.035f * hs, -0.085f * hs), new Vector3(0.012f, 0.04f, 0.012f) * hs, 6, 5);
                    if (s.turban)
                    {
                        var ring = new[] { new Vector2(-0.035f, 0.098f), new Vector2(0f, 0.122f), new Vector2(0.035f, 0.098f), new Vector2(0f, 0.08f), new Vector2(-0.035f, 0.098f) };
                        for (int i = 0; i < ring.Length; i++) ring[i] = new Vector2(ring[i].x * hs + hy + 0.08f * hs, ring[i].y * hs);
                        Lathe(body.Of(Mat(s.head2, 0.05f)), ring, 1f, 0f, 360f, 1f, false, new Vector3(0f, 0f, -0.012f * hs));
                    }
                    break;
                }
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
            if (armR) armR.localRotation = Quaternion.Euler(-sw * 26f * k * swingR, 0f, 4f);
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

        static readonly Dictionary<string, Texture2D> stripes = new Dictionary<string, Texture2D>();

        /// <summary>Vertical stripes of the line colour over the cloth colour, for a white material: narrow for a robe
        /// (kind 3), broad for an abaya (kind 1).</summary>
        static Texture2D Stripes(Color cloth, Color line, int kind)
        {
            string key = ColorUtility.ToHtmlStringRGB(cloth) + ColorUtility.ToHtmlStringRGB(line) + kind;
            if (stripes.TryGetValue(key, out var t) && t) return t;
            const int n = 32;
            t = new Texture2D(n, 4, TextureFormat.RGBA32, true) { name = "stripe" + key, wrapMode = TextureWrapMode.Repeat };
            for (int x = 0; x < n; x++)
            {
                bool on = kind == 1 ? (x / 8) % 2 == 1 : x % 16 < 3;
                for (int y = 0; y < 4; y++) t.SetPixel(x, y, on ? line : cloth);
            }
            t.Apply(true);
            stripes[key] = t;
            return t;
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

using System.Collections.Generic;
using UnityEngine;

namespace Rihla
{
    /// <summary>
    /// The game's own skeleton and movement for every person (the owner, 21 November 2026: "can we build those
    /// skeletons and movements, I really want the game to be genuine"). Nothing is bought, downloaded or recorded: the
    /// bones are placed by code inside the body that <see cref="FigureBody"/> builds, every vertex of the body is tied
    /// to the bones nearest it, and each frame the bones are posed by the rules of how people move:
    /// <list type="bullet">
    /// <item>Walking: each foot is planted and rolls from heel to toe while the body passes over it, then lifts and swings
    /// forward (the knee bending to clear the ground); the legs are solved from where the feet must be, so a planted
    /// foot does not slide. The hips rise and fall twice a step, turn with the leg that swings forward and the shoulders
    /// turn against them; the arms swing opposite the legs, the elbows bending as they come forward.</item>
    /// <item>Running: a shorter time on each foot, the knees high, the elbows bent, leaning forward.</item>
    /// <item>The old: shorter, slower steps, a bent back and bent knees, the head held up, the stick hand steady.</item>
    /// <item>Standing: breathing, the weight shifting slowly from one foot to the other, the arms hanging easy.</item>
    /// <item>Talking: the hands rise and move, the head nods.</item>
    /// <item>Sitting (at home): the upper body only (breathing, the arms holding, talking); the lap is part of the
    /// room.</item>
    /// </list>
    /// A robe moves partly with the legs and partly with the hips, so it swings rather than splitting into trousers.
    /// </summary>
    public class FigureRig
    {
        // Lengths for a person 1.74 m tall before the body's own scale.
        const float HipY = 0.9f, Thigh = 0.41f, Shin = 0.41f, AnkleY = 0.08f, HipX = 0.09f, FootX = 0.095f;

        Transform hips, spine, chest, neck, head, shL, shR, uaL, uaR, laL, laR, haL, haR, ulL, ulR, llL, llR, ftL, ftR;
        Quaternion uaRestL, uaRestR;
        readonly Transform figure;
        float phase, time;
        /// <summary>Smoothed amounts of walking, running and talking, so changes blend.</summary>
        float walkK, runK, talkK;
        readonly float seed;

        public SkinnedMeshRenderer Skin { get; private set; }
        public Transform Head => head;

        /// <summary>The pieces of a built body, by what they are.</summary>
        public struct Pieces
        {
            public Transform body, head, armL, armR, legL, legR, footL, footR;
        }

        FigureRig(Transform figure, float seed) { this.figure = figure; this.seed = seed; }

        public static bool Off = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-norig") >= 0;

        // ------------------------------------------------------------------ the skeleton

        Transform Bone(string name, Transform parent, Vector3 at)
        {
            var t = new GameObject(name).transform; t.SetParent(parent, false); t.localPosition = at; return t;
        }

        void MakeBones(Transform armL, Transform armR)
        {
            var root = Bone("Skeleton", figure, Vector3.zero);
            hips = Bone("Hips", root, new Vector3(0f, HipY + 0.02f, 0f));
            spine = Bone("Spine", hips, new Vector3(0f, 0.12f, 0f));
            chest = Bone("Chest", spine, new Vector3(0f, 0.2f, 0f));
            neck = Bone("Neck", chest, new Vector3(0f, 0.23f, 0f));
            head = Bone("Head", neck, new Vector3(0f, 0.05f, 0f));
            // The arms hang from where the body's arms were made (a shoulder 0.205 m out, at 1.39 m), turned a little out.
            Vector3 aL = armL ? figure.InverseTransformPoint(armL.position) : new Vector3(-0.205f, 1.39f, 0f);
            Vector3 aR = armR ? figure.InverseTransformPoint(armR.position) : new Vector3(0.205f, 1.39f, 0f);
            float chestY = HipY + 0.02f + 0.12f + 0.2f;
            shL = Bone("Shoulder L", chest, new Vector3(-0.06f, aL.y - chestY, 0f));
            shR = Bone("Shoulder R", chest, new Vector3(0.06f, aR.y - chestY, 0f));
            uaL = Bone("Upper arm L", shL, new Vector3(aL.x + 0.06f, 0f, 0f));
            uaR = Bone("Upper arm R", shR, new Vector3(aR.x - 0.06f, 0f, 0f));
            uaL.localRotation = uaRestL = armL ? armL.localRotation : Quaternion.Euler(0f, 0f, -4f);
            uaR.localRotation = uaRestR = armR ? armR.localRotation : Quaternion.Euler(0f, 0f, 4f);
            laL = Bone("Forearm L", uaL, new Vector3(0f, -0.29f, 0f));
            laR = Bone("Forearm R", uaR, new Vector3(0f, -0.29f, 0f));
            haL = Bone("Hand L", laL, new Vector3(0f, -0.27f, 0f));
            haR = Bone("Hand R", laR, new Vector3(0f, -0.27f, 0f));
            ulL = Bone("Thigh L", hips, new Vector3(-HipX, -0.02f, 0f));
            ulR = Bone("Thigh R", hips, new Vector3(HipX, -0.02f, 0f));
            llL = Bone("Shin L", ulL, new Vector3(0f, -Thigh, 0f));
            llR = Bone("Shin R", ulR, new Vector3(0f, -Thigh, 0f));
            ftL = Bone("Foot L", llL, new Vector3(-(FootX - HipX), -Shin, 0f));
            ftR = Bone("Foot R", llR, new Vector3(FootX - HipX, -Shin, 0f));
        }

        // ------------------------------------------------------------------ skinning

        struct Seg { public int bone; public Vector3 a, b; }

        /// <summary>Makes the skinned body under <paramref name="figure"/> and removes the old pieces; null when it
        /// cannot (the pieces are then left as they were).</summary>
        public static FigureRig Make(Transform figure, Pieces p, float seed)
        {
            if (Off || !p.body) return null;
            var rig = new FigureRig(figure, seed);
            rig.MakeBones(p.armL, p.armR);
            if (!rig.BuildSkin(p)) { Object.Destroy(figure.Find("Skeleton").gameObject); return null; }
            foreach (var t in new[] { p.armL, p.armR, p.legL, p.legR, p.footL, p.footR, p.head, p.body })
            {
                if (!t) continue;
                foreach (var mf in t.GetComponentsInChildren<MeshFilter>(true)) Object.Destroy(mf.sharedMesh);
                t.gameObject.SetActive(false); Object.Destroy(t.gameObject);
            }
            return rig;
        }

        bool BuildSkin(Pieces p)
        {
            var bones = new List<Transform>();
            int Id(Transform t) { int i = bones.IndexOf(t); if (i < 0) { bones.Add(t); i = bones.Count - 1; } return i; }
            Vector3 L(Transform t) => figure.InverseTransformPoint(t.position);
            Seg S(Transform a, Transform b) => new Seg { bone = Id(a), a = L(a), b = L(b) };
            Seg Tip(Transform a, Vector3 d) => new Seg { bone = Id(a), a = L(a), b = L(a) + d };

            int hipsId = Id(hips);
            var trunk = new List<Seg> { S(hips, spine), S(spine, chest), S(chest, neck), S(neck, head), S(shL, uaL), S(shR, uaR) };
            var legL = new List<Seg> { S(ulL, llL), S(llL, ftL) };
            var legR = new List<Seg> { S(ulR, llR), S(llR, ftR) };
            var legs = new List<Seg>(legL); legs.AddRange(legR);
            var footL = new List<Seg> { Tip(ftL, new Vector3(0f, -0.05f, 0.14f)) };
            var footR = new List<Seg> { Tip(ftR, new Vector3(0f, -0.05f, 0.14f)) };
            List<Seg> Arm(Transform sh, Transform ua, Transform la, Transform ha)
                => new List<Seg> { S(sh, ua), S(ua, la), S(la, ha), Tip(ha, L(ha) - L(la)) };
            var armL = Arm(shL, uaL, laL, haL); var armR = Arm(shR, uaR, laR, haR);
            var headSegs = new List<Seg> { Tip(head, new Vector3(0f, 0.2f, 0f)) };
            var neckSegs = new List<Seg>(trunk) { headSegs[0] };
            float headBase = L(head).y + 0.03f, hipY = L(ulL).y;

            var go = new GameObject("Body (skinned)");
            go.transform.SetParent(figure, false);
            var root = go.transform;
            Matrix4x4 toRoot = root.worldToLocalMatrix;
            var verts = new List<Vector3>(); var norms = new List<Vector3>(); var uvs = new List<Vector2>();
            var weights = new List<BoneWeight>();
            var subs = new List<(Material m, List<int> t)>();

            void Add(Transform part, bool only, System.Func<Vector3, BoneWeight> weigh)
            {
                if (!part) return;
                var kids = new List<Transform>();
                if (only) { foreach (Transform c in part) kids.Add(c); foreach (var c in kids) c.SetParent(null, true); }
                foreach (var mf in part.GetComponentsInChildren<MeshFilter>(true))
                {
                    var mesh = mf.sharedMesh; if (!mesh) continue;
                    var mats = mf.GetComponent<MeshRenderer>().sharedMaterials;
                    var m = mf.transform.localToWorldMatrix;
                    var v = mesh.vertices; var n = mesh.normals; var uv = mesh.uv;
                    int b0 = verts.Count;
                    for (int i = 0; i < v.Length; i++)
                    {
                        Vector3 w = m.MultiplyPoint3x4(v[i]);
                        verts.Add(toRoot.MultiplyPoint3x4(w));
                        norms.Add(toRoot.MultiplyVector(m.MultiplyVector(i < n.Length ? n[i] : Vector3.up)).normalized);
                        uvs.Add(i < uv.Length ? uv[i] : Vector2.zero);
                        weights.Add(weigh(figure.InverseTransformPoint(w)));
                    }
                    for (int s = 0; s < mesh.subMeshCount && s < mats.Length; s++)
                    {
                        int k = subs.FindIndex(x => x.m == mats[s]);
                        if (k < 0) { subs.Add((mats[s], new List<int>())); k = subs.Count - 1; }
                        foreach (int t in mesh.GetTriangles(s)) subs[k].t.Add(t + b0);
                    }
                }
                if (only) foreach (var c in kids) c.SetParent(part, true);
            }

            // The robe or shirt: above the hips with the trunk; below, partly with the legs (more towards the hem) and
            // partly with the hips, so the cloth swings with the stride.
            Add(p.body, true, q =>
            {
                if (q.y >= hipY) return Weigh(q, trunk);
                float share = 0.8f * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(hipY - 0.05f, hipY - 0.55f, q.y));
                return Mix(Weigh(q, legs), hipsId, share);
            });
            Add(p.head, false, q => q.y > headBase ? Weigh(q, headSegs) : Weigh(q, neckSegs));
            Add(p.armL, false, q => Weigh(q, armL));
            Add(p.armR, false, q => Weigh(q, armR));
            Add(p.legL, false, q => Weigh(q, q.y < AnkleY + 0.04f ? footL : legL));
            Add(p.legR, false, q => Weigh(q, q.y < AnkleY + 0.04f ? footR : legR));
            Add(p.footL, false, q => Weigh(q, footL));
            Add(p.footR, false, q => Weigh(q, footR));
            if (verts.Count == 0) { Object.Destroy(go); return false; }

            var mesh2 = new Mesh { name = "Person (skinned)" };
            if (verts.Count > 65000) mesh2.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh2.SetVertices(verts); mesh2.SetNormals(norms); mesh2.SetUVs(0, uvs);
            mesh2.subMeshCount = subs.Count;
            for (int i = 0; i < subs.Count; i++) mesh2.SetTriangles(subs[i].t, i);
            mesh2.boneWeights = weights.ToArray();
            var bind = new Matrix4x4[bones.Count];
            for (int i = 0; i < bones.Count; i++) bind[i] = bones[i].worldToLocalMatrix * root.localToWorldMatrix;
            mesh2.bindposes = bind;
            mesh2.RecalculateBounds();

            var skin = go.AddComponent<SkinnedMeshRenderer>();
            skin.sharedMesh = mesh2; skin.bones = bones.ToArray(); skin.rootBone = hips;
            var ml = new Material[subs.Count]; for (int i = 0; i < subs.Count; i++) ml[i] = subs[i].m;
            skin.sharedMaterials = ml;
            skin.quality = SkinQuality.Bone4;
            skin.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            // Bounds about the hips, room for a stride or a raised hand.
            skin.localBounds = new Bounds(new Vector3(0f, 0.1f, 0f), new Vector3(1.6f, 2.2f, 1.8f));
            Skin = skin;
            return true;
        }

        /// <summary>Ties a point to the nearest bones: the four nearest, nearer ones much stronger, so joints bend
        /// smoothly and a piece far from any joint moves with one bone.</summary>
        static BoneWeight Weigh(Vector3 q, List<Seg> segs)
        {
            var best = new (int bone, float wt)[4]; int filled = 0;
            foreach (var s in segs)
            {
                float d = DistSeg(q, s.a, s.b), wt = 1f / Mathf.Pow(d * d + 0.0006f, 2f);
                int same = -1; for (int k = 0; k < filled; k++) if (best[k].bone == s.bone) same = k;
                if (same >= 0) { if (wt > best[same].wt) best[same].wt = wt; continue; }
                if (filled < 4) { best[filled++] = (s.bone, wt); continue; }
                int lo = 0; for (int k = 1; k < 4; k++) if (best[k].wt < best[lo].wt) lo = k;
                if (wt > best[lo].wt) best[lo] = (s.bone, wt);
            }
            return ToWeight(best, filled);
        }

        static BoneWeight ToWeight((int bone, float wt)[] best, int filled)
        {
            System.Array.Sort(best, 0, filled, Comparer<(int bone, float wt)>.Create((x, y) => y.wt.CompareTo(x.wt)));
            float sum = 0f; for (int k = 0; k < filled; k++) sum += best[k].wt;
            var bw = new BoneWeight();
            if (sum <= 0f) return bw;
            if (filled > 0) { bw.boneIndex0 = best[0].bone; bw.weight0 = best[0].wt / sum; }
            if (filled > 1) { bw.boneIndex1 = best[1].bone; bw.weight1 = best[1].wt / sum; }
            if (filled > 2) { bw.boneIndex2 = best[2].bone; bw.weight2 = best[2].wt / sum; }
            if (filled > 3) { bw.boneIndex3 = best[3].bone; bw.weight3 = best[3].wt / sum; }
            return bw;
        }

        /// <summary>The weights <paramref name="w"/> by <paramref name="share"/>, the rest to one bone.</summary>
        static BoneWeight Mix(BoneWeight w, int bone, float share)
        {
            var list = new (int bone, float wt)[4];
            list[0] = (w.boneIndex0, w.weight0 * share); list[1] = (w.boneIndex1, w.weight1 * share); list[2] = (w.boneIndex2, w.weight2 * share);
            list[3] = (bone, 1f - share);
            return ToWeight(list, 4);
        }

        static float DistSeg(Vector3 p, Vector3 a, Vector3 b)
        {
            Vector3 ab = b - a; float l = ab.sqrMagnitude;
            float t = l > 1e-8f ? Mathf.Clamp01(Vector3.Dot(p - a, ab) / l) : 0f;
            return Vector3.Distance(p, a + ab * t);
        }

        // ------------------------------------------------------------------ movement

        static float Smooth(float x) { x = Mathf.Clamp01(x); return x * x * (3f - 2f * x); }
        static float Noise(float t, float s) => Mathf.PerlinNoise(t, s) * 2f - 1f;

        /// <summary>Poses the bones for this frame.</summary>
        /// <param name="v">Walking speed in m/s, as if for a grown man (a child's speed divided by its size).</param>
        /// <param name="old">Walks with a stick: bent, slower steps.</param>
        /// <param name="seated">Sitting: only the upper body moves.</param>
        /// <param name="armHold">Sitting: how far the arms are raised (holding a book), degrees.</param>
        /// <param name="talking">Speaking: gestures and nods.</param>
        /// <param name="headYaw">How far the head turns (to look at the child), degrees.</param>
        public void Animate(float dt, float v, bool old, bool seated, float armHold, bool talking, float headYaw)
        {
            time += dt;
            float t = time + seed * 3.7f;
            walkK = Mathf.MoveTowards(walkK, seated ? 0f : Smooth(v / 0.9f), dt * 3f);
            runK = Mathf.MoveTowards(runK, seated ? 0f : Smooth((v - 2.3f) / 1.2f), dt * 3f);
            talkK = Mathf.MoveTowards(talkK, talking ? 1f : 0f, dt * 2f);
            float k = walkK, r = runK, oldK = old ? 1f : 0f;

            // The step: its length grows with speed; running spends less of each stride on the ground.
            float pace = Mathf.Max(v, 0f);
            float step = Mathf.Lerp(Mathf.Lerp(0.32f, 0.6f, Mathf.Clamp01(pace / 1.3f)) + Mathf.Clamp01((pace - 1.3f) / 0.9f) * 0.12f, 0.85f, r) * (old ? 0.62f : 1f);
            float stance = Mathf.Lerp(0.6f, 0.34f, r);
            if (k > 0.01f) phase = Mathf.Repeat(phase + pace * dt / (2f * step), 1f);
            float pL = phase, pR = Mathf.Repeat(phase + 0.5f, 1f);
            float travel = 2f * stance * step, front = travel * 0.42f, back = -travel * 0.58f;

            // Standing: the weight moves slowly from one foot to the other.
            float shift = Mathf.Sin(t * 0.85f) * 0.6f + Noise(t * 0.12f, seed) * 0.4f;
            float idle = 1f - k;

            // The hips: lower when walking (more when old or running), rising and falling twice a stride; over the
            // standing foot when still.
            float bob = Mathf.Cos(4f * Mathf.PI * (phase - 0.3f));
            float hy = 0.02f - k * (0.03f + 0.025f * oldK) + r * 0.012f + k * bob * (0.016f - 0.004f * r) + (seated ? 0f : Mathf.Sin(t * 1.7f) * 0.002f) - idle * Mathf.Abs(shift) * 0.008f;
            float hx = seated ? 0f : idle * shift * 0.025f;
            float yaw = seated ? 0f : k * Mathf.Cos(2f * Mathf.PI * pL) * (5f + 3f * r);
            float roll = seated ? 0f : -k * Mathf.Sin(4f * Mathf.PI * pL) * 2.5f + idle * shift * 2.5f;
            hips.localPosition = new Vector3(hx, HipY + hy, 0f);
            hips.localRotation = Quaternion.Euler(0f, yaw, roll);

            // The trunk leans into a run, bends with age, turns against the hips, breathes.
            float breath = Mathf.Sin(t * 1.6f);
            spine.localRotation = Quaternion.Euler(3f * k + 7f * r + 10f * oldK + breath * 0.6f, -yaw * 0.6f, -roll * 0.5f);
            chest.localRotation = Quaternion.Euler(1.5f * k + 6f * oldK + breath * 0.8f, -yaw * 0.9f, -roll * 0.4f);
            float nod = talkK * Noise(t * 1.3f, seed + 5f) * 5f + idle * Noise(t * 0.3f, seed + 9f) * 2f;
            neck.localRotation = Quaternion.Euler(-(4f * k + 8f * r + 14f * oldK) * 0.5f + nod * 0.5f, yaw * 0.6f, 0f);
            head.localRotation = Quaternion.Euler(-(4f * k + 8f * r + 14f * oldK) * 0.5f + nod * 0.5f, headYaw, roll * 0.3f);

            // The legs, solved from where each foot must be.
            if (seated)
            {
                ulL.localRotation = ulR.localRotation = llL.localRotation = llR.localRotation = ftL.localRotation = ftR.localRotation = Quaternion.identity;
            }
            else
            {
                Leg(ulL, llL, ftL, -1f, pL, k, stance, front, back, r, shift, idle);
                Leg(ulR, llR, ftR, 1f, pR, k, stance, front, back, r, -shift, idle);
            }

            // The arms: swinging opposite the legs, the elbows bending as they come forward; raised to talk; held in the
            // lap when sitting.
            float swingAmp = (16f * k + 26f * r) * (old ? 0.5f : 1f);
            Arm(uaL, laL, haL, uaRestL, -1f, Mathf.Cos(2f * Mathf.PI * pR) * swingAmp, k, r, oldK, seated, armHold, t, 1f);
            Arm(uaR, laR, haR, uaRestR, 1f, Mathf.Cos(2f * Mathf.PI * pL) * swingAmp * (old ? 0.35f : 1f), k, r, oldK, seated, armHold, t, old ? 0.3f : 1f);
            shL.localRotation = Quaternion.Euler(0f, 0f, breath * 0.6f - k * Mathf.Cos(2f * Mathf.PI * pR) * 1.5f);
            shR.localRotation = Quaternion.Euler(0f, 0f, -breath * 0.6f + k * Mathf.Cos(2f * Mathf.PI * pL) * 1.5f);
        }

        void Leg(Transform thigh, Transform shin, Transform foot, float side, float p, float k, float stance,
                 float front, float back, float r, float weight, float idle)
        {
            // Where the ankle should be, in the figure's space, for walking and for standing.
            float z, lift = 0f, pitch = 0f;
            if (p < stance)
            {
                float u = p / stance;
                z = Mathf.Lerp(front, back, u);
                float heel = Smooth((u - 0.62f) / 0.38f);
                lift = heel * (0.1f + 0.05f * r);
                pitch = heel * (28f + 10f * r) - (1f - Smooth(u / 0.12f)) * 10f;
            }
            else
            {
                float u = (p - stance) / (1f - stance);
                z = Mathf.Lerp(back, front, Smooth(u));
                lift = Mathf.Sin(Mathf.PI * u) * (0.07f + 0.16f * r) + (1f - Smooth(u / 0.25f)) * 0.1f;
                pitch = Mathf.Lerp(30f, -12f, Smooth(u * 1.3f));
            }
            // Standing: the free leg's knee eases as the weight goes to the other foot.
            float idleLift = idle * Mathf.Max(0f, -weight) * 0.012f;
            var target = new Vector3(side * FootX, AnkleY + lift * k + idleLift, z * k);

            // Solve thigh and shin from the hip joint to the target, in the hips' space.
            Vector3 hipJ = thigh.parent.localPosition + thigh.parent.localRotation * thigh.localPosition;
            Vector3 d = Quaternion.Inverse(thigh.parent.localRotation) * (target - hipJ);
            float lat = Mathf.Atan2(d.x, -d.y) * Mathf.Rad2Deg;
            float down = -Mathf.Sqrt(d.x * d.x + d.y * d.y);
            float dist = Mathf.Clamp(Mathf.Sqrt(down * down + d.z * d.z), 0.25f, Thigh + Shin - 0.002f);
            float knee = 180f - Mathf.Acos(Mathf.Clamp((Thigh * Thigh + Shin * Shin - dist * dist) / (2f * Thigh * Shin), -1f, 1f)) * Mathf.Rad2Deg;
            float baseA = Mathf.Atan2(d.z, -down) * Mathf.Rad2Deg;
            float off = Mathf.Acos(Mathf.Clamp((Thigh * Thigh + dist * dist - Shin * Shin) / (2f * Thigh * dist), -1f, 1f)) * Mathf.Rad2Deg;
            float fwd = baseA + off;
            thigh.localRotation = Quaternion.Euler(0f, 0f, lat) * Quaternion.Euler(-fwd, 0f, 0f);
            shin.localRotation = Quaternion.Euler(knee, 0f, 0f);
            // The foot flat on the ground, then rolled from heel to toe.
            foot.localRotation = Quaternion.Euler(fwd - knee, 0f, 0f) * Quaternion.Euler(0f, 0f, -lat) * Quaternion.Inverse(thigh.parent.localRotation) * Quaternion.Euler(pitch * k, 0f, 0f);
        }

        void Arm(Transform ua, Transform la, Transform ha, Quaternion rest, float side, float swing, float k, float r, float oldK,
                 bool seated, float armHold, float t, float freedom)
        {
            float fwd, bend, abd = 3f, twist = 0f;
            if (seated)
            {
                fwd = armHold * 0.55f; bend = armHold * 0.85f + 8f; abd = 2f;
            }
            else
            {
                fwd = swing; bend = 10f + 6f * k + 70f * r + 12f * oldK + Mathf.Max(0f, swing) * 0.55f;
                abd = 4f + 4f * r;
            }
            // Talking: the hands come up and move as the words do, the right more than the left.
            float burst = 0.55f + 0.45f * Mathf.Clamp01(0.6f + Noise(t * 0.35f, seed + side * 13f) * 1.5f);
            float g = talkK * freedom * (side > 0f ? 1f : 0.45f) * burst;
            if (g > 0.001f)
            {
                float n1 = Noise(t * 0.9f, seed + side * 3f), n2 = Noise(t * 1.4f, seed + side * 7f), n3 = Noise(t * 0.7f, seed + side * 11f);
                float beat = Mathf.Sin(t * 9f + side) * Mathf.Max(0f, Noise(t * 2.1f, seed + side)) * 9f;
                fwd = Mathf.Lerp(fwd, 14f + 10f * n1, g);
                bend = Mathf.Lerp(bend, 74f + 16f * n2 + beat, g);
                abd = Mathf.Lerp(abd, 14f + 8f * n3, g);
                twist = g * (55f + 25f * n3);
            }
            ua.localRotation = rest * Quaternion.Euler(0f, 0f, abd * side) * Quaternion.Euler(-fwd, 0f, 0f);
            la.localRotation = Quaternion.Euler(-bend, -twist * side, 0f);
            ha.localRotation = Quaternion.Euler(-6f - 8f * g, 0f, 0f);
        }
    }
}

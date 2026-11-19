using System.Collections.Generic;
using UnityEngine;

namespace Rihla
{
    /// <summary>
    /// A shepherd and his flock beside the road down to Ariha (C1b). They graze on one side until DriveStory calls
    /// <see cref="Cross"/>; then the shepherd walks across the road with his staff and the sheep and goats trot after
    /// him in a loose bunch, bleating, and graze on the other side. Mostly white sheep with brown faces and broad
    /// tails, as the sheep of the region look, and a few black goats (an estimate, NEEDS_REVIEW H22). Everything is
    /// made here from simple shapes, without colliders, so the car is stopped by DriveStory and not by the sheep.
    /// </summary>
    public class Flock : MonoBehaviour
    {
        /// <summary>Where the crossing starts and ends (on the two sides of the road).</summary>
        public Vector3 from, to;
        public int count = 16;
        public float walkSpeed = 1.4f;

        public enum State { Grazing, Crossing, Done }
        public State Now { get; private set; } = State.Grazing;
        /// <summary>The middle of the flock, for the camera, the car and the sounds.</summary>
        public Vector3 Centre { get; private set; }
        public Transform Shepherd => shepherd;

        class Animal
        {
            public Transform root, neck; public Transform[] legs;
            public float lag, side, phase, wander, size; public Vector3 graze, pos, vel;
        }
        readonly List<Animal> flock = new List<Animal>();
        Transform shepherd; float s, bleatTimer = 2f, maxLag;
        AudioSource voice; AudioClip bleat;

        void Start()
        {
            var rng = new System.Random(4242);
            float R() => (float)rng.NextDouble();
            var wool = Mat(new Color(0.86f, 0.82f, 0.72f)); var face = Mat(new Color(0.42f, 0.27f, 0.17f));
            var goat = Mat(new Color(0.11f, 0.09f, 0.08f)); var goatBrown = Mat(new Color(0.4f, 0.28f, 0.18f));
            var hoof = Mat(new Color(0.17f, 0.13f, 0.11f));
            for (int i = 0; i < count; i++)
            {
                bool isGoat = i % 5 == 3;
                var a = new Animal
                {
                    lag = 2.5f + i * 0.75f + R() * 1.5f, side = (R() - 0.5f) * 6f, phase = R() * 10f, wander = R() * 100f,
                    size = isGoat ? 1.05f : 1.15f + R() * 0.15f,
                };
                maxLag = Mathf.Max(maxLag, a.lag);
                Build(a, i, isGoat ? (i % 2 == 0 ? goat : goatBrown) : wool, isGoat ? (i % 2 == 0 ? goat : goatBrown) : face, hoof, isGoat);
                a.graze = from + Offset(a, 1.6f); a.pos = Ground(a.graze);
                a.root.position = a.pos; a.root.rotation = Quaternion.Euler(0f, R() * 360f, 0f);
                flock.Add(a);
            }

            // The shepherd: made inactive so FigureBody builds him with the right look.
            var sg = new GameObject("Shepherd"); sg.SetActive(false); sg.transform.SetParent(transform, false);
            var fb = sg.AddComponent<FigureBody>(); fb.look = FigureBody.Look.Shepherd; fb.seed = 7;
            sg.transform.position = Ground(from - (to - from).normalized * 1.5f);
            sg.transform.rotation = Quaternion.LookRotation(Flat(to - from));
            sg.SetActive(true);
            shepherd = sg.transform;

            voice = gameObject.AddComponent<AudioSource>();
            voice.spatialBlend = 1f; voice.minDistance = 10f; voice.maxDistance = 140f; voice.rolloffMode = AudioRolloffMode.Linear;
            voice.playOnAwake = false;
            bleat = Bleat();
            Centre = from;
        }

        /// <summary>Starts the crossing (once).</summary>
        public void Cross() { if (Now == State.Grazing) Now = State.Crossing; }

        /// <summary>Puts the flock on the far side at once (when the crossing was seen on an earlier drive).</summary>
        public void AlreadyCrossed()
        {
            Now = State.Done; s = Vector3.Distance(from, to) + maxLag;
            foreach (var a in flock) { a.graze = to + Offset(a, 1.6f); a.pos = Ground(a.graze); a.root.position = a.pos; }
            if (shepherd) shepherd.position = Ground(to + Flat(to - from).normalized * 3f);
        }

        Vector3 Offset(Animal a, float spread)
        {
            Vector3 along = Flat(to - from).normalized, across = Vector3.Cross(Vector3.up, along);
            return across * a.side * spread + along * (-a.lag * 0.35f);
        }

        void Update()
        {
            var cam = Camera.main;
            if (cam && (cam.transform.position - Centre).sqrMagnitude > 450f * 450f) return;
            float dt = Time.deltaTime, length = Vector3.Distance(from, to);
            Vector3 along = Flat(to - from).normalized, across = Vector3.Cross(Vector3.up, along);

            if (Now == State.Crossing)
            {
                s += walkSpeed * dt;
                Vector3 sp = Ground(from + along * Mathf.Min(s, length + 3f));
                if (shepherd) { shepherd.position = sp; shepherd.rotation = Quaternion.Slerp(shepherd.rotation, Quaternion.LookRotation(along), dt * 4f); }
                if (s > length + maxLag + 1f)
                {
                    Now = State.Done;
                    foreach (var a in flock) a.graze = a.pos;
                }
            }

            Vector3 sum = Vector3.zero;
            foreach (var a in flock)
            {
                Vector3 goal;
                if (Now == State.Crossing)
                {
                    float t = Mathf.Clamp(s - a.lag, -2f, length + 2f);
                    // Bunched on the road, spreading out again on either side.
                    float bunch = Mathf.Lerp(0.5f, 1f, Mathf.Abs(t / length - 0.5f) * 2f);
                    goal = from + along * t + across * a.side * bunch;
                }
                else
                {
                    // Grazing: a slow wander about its spot.
                    a.wander += dt * 0.15f;
                    goal = a.graze + new Vector3(Mathf.Sin(a.wander * 1.3f), 0f, Mathf.Cos(a.wander * 0.9f)) * 1.2f;
                }
                Vector3 d = Flat(goal - a.pos);
                float maxSpeed = Now == State.Crossing ? walkSpeed * 1.6f : 0.25f;
                Vector3 want = d.magnitude > 0.15f ? d.normalized * Mathf.Min(maxSpeed, d.magnitude * 1.5f) : Vector3.zero;
                a.vel = Vector3.MoveTowards(a.vel, want, dt * 3f);
                a.pos = Ground(a.pos + a.vel * dt);
                a.root.position = a.pos;
                float sp = a.vel.magnitude;
                if (sp > 0.05f) a.root.rotation = Quaternion.Slerp(a.root.rotation, Quaternion.LookRotation(a.vel), dt * 5f);
                // Legs swing with the pace; the head goes down to graze when standing.
                a.phase += dt * sp * 9f;
                float swing = Mathf.Sin(a.phase) * Mathf.Clamp01(sp * 1.5f) * 32f;
                a.legs[0].localRotation = a.legs[3].localRotation = Quaternion.Euler(swing, 0f, 0f);
                a.legs[1].localRotation = a.legs[2].localRotation = Quaternion.Euler(-swing, 0f, 0f);
                float graze = sp < 0.3f ? 38f + Mathf.Sin(Time.time * 0.7f + a.phase) * 8f : 0f;
                a.neck.localRotation = Quaternion.Slerp(a.neck.localRotation, Quaternion.Euler(graze, 0f, 0f), dt * 2f);
                sum += a.pos;
            }
            Centre = flock.Count > 0 ? sum / flock.Count : from;
            transform.position = Centre;

            // Bleats now and then, more often on the move.
            if (cam && (cam.transform.position - Centre).sqrMagnitude < 160f * 160f && (bleatTimer -= dt) < 0f)
            {
                bleatTimer = Now == State.Crossing ? Random.Range(1.2f, 3.5f) : Random.Range(4f, 10f);
                voice.pitch = Random.Range(0.85f, 1.3f);
                voice.PlayOneShot(bleat, Random.Range(0.45f, 0.75f));
            }
        }

        // ------------------------------------------------------------------ building

        void Build(Animal a, int i, Material body, Material head, Material hoof, bool isGoat)
        {
            var root = new GameObject(isGoat ? "Goat " + i : "Sheep " + i).transform; root.SetParent(transform, false);
            var inner = new GameObject("body").transform; inner.SetParent(root, false); inner.localScale = Vector3.one * a.size;
            Part(PrimitiveType.Sphere, inner, new Vector3(0f, 0.62f, 0f), isGoat ? new Vector3(0.4f, 0.44f, 0.85f) : new Vector3(0.6f, 0.56f, 0.92f), body);
            if (!isGoat) Part(PrimitiveType.Sphere, inner, new Vector3(0f, 0.55f, -0.44f), new Vector3(0.36f, 0.34f, 0.26f), body); // the broad tail
            else Part(PrimitiveType.Capsule, inner, new Vector3(0f, 0.78f, -0.42f), new Vector3(0.06f, 0.09f, 0.06f), body);
            var neck = new GameObject("neck").transform; neck.SetParent(inner, false); neck.localPosition = new Vector3(0f, 0.76f, 0.36f);
            Part(PrimitiveType.Sphere, neck, new Vector3(0f, 0.06f, 0.2f), isGoat ? new Vector3(0.18f, 0.22f, 0.36f) : new Vector3(0.22f, 0.24f, 0.36f), head);
            Part(PrimitiveType.Sphere, neck, new Vector3(-0.13f, 0.08f, 0.14f), new Vector3(0.16f, 0.05f, 0.08f), head).localRotation = Quaternion.Euler(0f, 0f, -30f);
            Part(PrimitiveType.Sphere, neck, new Vector3(0.13f, 0.08f, 0.14f), new Vector3(0.16f, 0.05f, 0.08f), head).localRotation = Quaternion.Euler(0f, 0f, 30f);
            if (isGoat)
            {
                Part(PrimitiveType.Capsule, neck, new Vector3(-0.05f, 0.24f, 0.08f), new Vector3(0.035f, 0.1f, 0.035f), hoof).localRotation = Quaternion.Euler(-35f, 0f, 0f);
                Part(PrimitiveType.Capsule, neck, new Vector3(0.05f, 0.24f, 0.08f), new Vector3(0.035f, 0.1f, 0.035f), hoof).localRotation = Quaternion.Euler(-35f, 0f, 0f);
            }
            a.legs = new Transform[4];
            float[] xs = { -0.17f, 0.17f, -0.17f, 0.17f }, zs = { 0.28f, 0.28f, -0.28f, -0.28f };
            for (int k = 0; k < 4; k++)
            {
                var hip = new GameObject("leg").transform; hip.SetParent(inner, false); hip.localPosition = new Vector3(xs[k], 0.46f, zs[k]);
                Part(PrimitiveType.Cylinder, hip, new Vector3(0f, -0.23f, 0f), new Vector3(0.075f, 0.23f, 0.075f), isGoat ? body : hoof);
                a.legs[k] = hip;
            }
            a.root = root; a.neck = neck;
        }

        static Transform Part(PrimitiveType type, Transform parent, Vector3 at, Vector3 scale, Material m)
        {
            var go = GameObject.CreatePrimitive(type);
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false); go.transform.localPosition = at; go.transform.localScale = scale;
            var r = go.GetComponent<MeshRenderer>(); r.sharedMaterial = m;
            r.shadowCastingMode = scale.x > 0.3f ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
            return go.transform;
        }

        static Material Mat(Color c)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "Flock" };
            m.SetColor("_BaseColor", c); m.SetFloat("_Smoothness", 0.08f);
            return m;
        }

        static Vector3 Flat(Vector3 v) { v.y = 0f; return v.sqrMagnitude < 1e-6f ? Vector3.forward : v; }

        static Vector3 Ground(Vector3 p)
        {
            if (Physics.Raycast(new Vector3(p.x, p.y + 20f, p.z), Vector3.down, out var hit, 60f, ~0, QueryTriggerInteraction.Ignore)) p.y = hit.point.y;
            return p;
        }

        /// <summary>A short "baa": a bleating voice with a quaver, made here (no recording needed).</summary>
        static AudioClip Bleat()
        {
            const int rate = 22050; int n = (int)(rate * 0.75f);
            var d = new float[n]; double ph = 0; var rng = new System.Random(5);
            float peak = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)rate;
                float f0 = 330f * (1f + 0.05f * Mathf.Sin(2f * Mathf.PI * 5.5f * t)) * (1f - 0.12f * t);
                ph += f0 / rate;
                float v = 0f;
                for (int k = 1; k <= 14; k++)
                {
                    float f = k * f0;
                    float w = Mathf.Exp(-Mathf.Pow((f - 700f) / 260f, 2f)) + 0.55f * Mathf.Exp(-Mathf.Pow((f - 1900f) / 450f, 2f)) + 0.25f / k;
                    v += w * Mathf.Sin((float)(2.0 * Mathf.PI * k * ph));
                }
                v += ((float)rng.NextDouble() - 0.5f) * 0.15f;
                float env = Mathf.Clamp01(t / 0.05f) * Mathf.Clamp01((0.75f - t) / 0.3f) * (0.72f + 0.28f * Mathf.Sin(2f * Mathf.PI * 9f * t));
                d[i] = v * env; peak = Mathf.Max(peak, Mathf.Abs(d[i]));
            }
            for (int i = 0; i < n; i++) d[i] = d[i] / peak * 0.6f;
            var clip = AudioClip.Create("bleat", n, 1, rate, false); clip.SetData(d, 0);
            return clip;
        }
    }
}

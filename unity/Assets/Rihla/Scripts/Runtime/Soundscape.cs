using UnityEngine;

namespace Rihla
{
    /// <summary>
    /// The sound of the walk, made by code like the browser prototype's (src/audio/sound.ts), so it needs no
    /// recordings: a soft morning breeze, birdsong (livelier among trees), footsteps on stone for the child and for
    /// Jiddo, a gentle chime when a clue is found, and a rising tone for the Time Lens reveal. In the "then" view the
    /// world sounds slightly muffled, like a memory. The adhan is not made here: it must be a real recording (R4).
    /// </summary>
    /// <remarks>Sits on the camera beside its AudioListener and adds its sound to everything the listener hears.</remarks>
    public class Soundscape : MonoBehaviour
    {
        public Walker child;
        public Transform jiddo;
        public TimeLens lens;
        [Range(0f, 1f)] public float volume = 0.8f;
        /// <summary>How lively the birds are here, 0..1 (set from the number of trees nearby).</summary>
        public float birdiness = 0.3f;
        /// <summary>Evening indoors (Jiddo's house): crickets outside, a softer breeze, no birds (C1a).</summary>
        public bool evening;
        /// <summary>Where the trees stand: near them the leaves rustle when the breeze rises (C1a).</summary>
        public Vector3[] trees;

        /// <summary>The breeze now, 0..1: it rises and falls slowly. Leaves blowing past follow it.</summary>
        public static float Gust { get; private set; }
        /// <summary>How many trees are close to the listener, 0..1.</summary>
        public static float Leafiness { get; private set; }

        public static Soundscape Instance { get; private set; }

        // Shared with the audio thread.
        volatile float stepChild, stepJiddo, chime, reveal, muffle, birdAmount, gustNow, leafNow, eveningNow;
        float rustleLp, rustleHp, rustleEnv, rustleTimer, cricketT, cricketTimer, cricketPhase, cricketPulses, cricketFreq;
        int rate; System.Random rng = new System.Random(7);
        float wind, windLp, windGain, birdTimer, birdPhase, birdFreq, birdTime, birdLen, birdChirps, lp1, lp2;
        float stepT = 10f, stepGain, stepLp, jStepT = 10f, jStepLp, chimeT = 10f, revealT = 10f;
        float childDist, jiddoDist; Vector3 lastChild, lastJiddo;

        void Awake()
        {
            Instance = this;
            rate = AudioSettings.outputSampleRate;
        }

        void Start()
        {
            if (child) lastChild = child.transform.position;
            if (jiddo) lastJiddo = jiddo.position;
        }

        /// <summary>A gentle two-note chime (a clue found).</summary>
        public static void Chime() { if (Instance) Instance.chime = 1f; }
        /// <summary>The slow rising tone of the Time Lens reveal.</summary>
        public static void Reveal() { if (Instance) Instance.reveal = 1f; }

        void Update()
        {
            // Footsteps: one every 0.7 m walked (a child's stride), and every 0.8 m for Jiddo.
            if (child)
            {
                Vector3 p = child.transform.position; float d = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(lastChild.x, lastChild.z));
                lastChild = p; if (d < 2f) childDist += d;
                if (childDist > 0.7f) { childDist = 0f; stepChild = 1f; }
            }
            if (jiddo)
            {
                Vector3 p = jiddo.position; float d = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(lastJiddo.x, lastJiddo.z));
                lastJiddo = p; if (d < 2f) jiddoDist += d;
                if (jiddoDist > 0.8f) { jiddoDist = 0f; stepJiddo = 1f; }
            }
            muffle = lens ? (lens.Then ? 1f : 0f) : 0f;
            // The breeze: two slow waves and a quicker flutter, so gusts come every half minute or so.
            float tt = Time.time;
            Gust = Mathf.Clamp01(0.35f + 0.35f * Mathf.Sin(tt * 0.19f) * Mathf.Sin(tt * 0.071f + 1.3f) + 0.25f * Mathf.Max(0f, Mathf.Sin(tt * 0.33f + 2f)) + 0.08f * Mathf.Sin(tt * 1.7f));
            gustNow = Gust;
            float leaf = 0f;
            if (trees != null)
            {
                Vector3 me = transform.position;
                foreach (var t in trees) { float d = Vector2.Distance(new Vector2(t.x, t.z), new Vector2(me.x, me.z)); if (d < 16f) leaf += 1f - d / 16f; }
            }
            Leafiness = Mathf.Clamp01(leaf / 1.6f); leafNow = Leafiness;
            eveningNow = evening ? 1f : 0f;
            // More birdsong among trees (Ariha's palms): the breeze in the leaves and the birds go together.
            birdAmount = Mathf.Clamp01(birdiness + Leafiness * 0.6f);
            bool paused = RihlaUI.Instance && RihlaUI.Instance.ModalOpen;
            AudioListener.volume = volume * (paused ? 0.5f : 1f);
        }

        float Noise() => (float)(rng.NextDouble() * 2.0 - 1.0);

        void OnAudioFilterRead(float[] data, int channels)
        {
            float dt = 1f / rate;
            for (int i = 0; i < data.Length; i += channels)
            {
                float s = 0f, n = Noise();

                // Breeze: low-passed noise that swells with the gusts (softer through Jiddo's window).
                windGain += (gustNow - windGain) * 0.00004f;
                windLp += (n - windLp) * 0.02f; wind += (windLp - wind) * 0.05f;
                s += wind * (0.04f + 0.12f * windGain) * (eveningNow > 0f ? 0.45f : 1f);

                // Leaves: bright, crackling noise in small bursts, near trees and when the breeze is up.
                if (leafNow > 0.01f)
                {
                    rustleTimer -= dt;
                    if (rustleTimer <= 0f) { rustleTimer = 0.015f + (float)rng.NextDouble() * 0.05f / (0.2f + windGain); rustleEnv = 0.4f + (float)rng.NextDouble() * 0.6f; }
                    rustleEnv *= 0.9985f;
                    rustleLp += (n - rustleLp) * 0.45f; rustleHp = n - rustleLp;
                    s += rustleHp * rustleEnv * leafNow * (0.1f + windGain * 0.9f) * 0.05f;
                }

                // Crickets on an evening: trains of three or four quick chirps, a little apart.
                if (eveningNow > 0f)
                {
                    cricketTimer -= dt;
                    if (cricketTimer <= 0f && cricketPulses <= 0f) { cricketPulses = 3 + rng.Next(2); cricketFreq = 4300f + (float)rng.NextDouble() * 500f; cricketTimer = 0.6f + (float)rng.NextDouble() * 0.9f; }
                    if (cricketPulses > 0f)
                    {
                        cricketT += dt;
                        if (cricketT > 0.045f) { cricketT = 0f; cricketPulses--; }
                        float e = Mathf.Sin(Mathf.Clamp01(cricketT / 0.03f) * Mathf.PI);
                        cricketPhase += cricketFreq * dt;
                        s += Mathf.Sin(cricketPhase * 2f * Mathf.PI) * e * 0.012f;
                    }
                }

                // Birds: short sweeping chirps in little bursts; more of them among trees.
                birdTime += dt;
                if (eveningNow > 0f) { }
                else if (birdLen <= 0f)
                {
                    birdTimer -= dt;
                    if (birdTimer <= 0f)
                    {
                        if (birdChirps <= 0f) birdChirps = 1 + rng.Next(5);
                        birdChirps--;
                        birdLen = 0.05f + (float)rng.NextDouble() * 0.09f;
                        birdFreq = 2600f + (float)rng.NextDouble() * 2400f;
                        birdTimer = birdChirps > 0 ? 0.06f + (float)rng.NextDouble() * 0.1f
                                                   : (1.2f + (float)rng.NextDouble() * 3.5f) / (0.3f + birdAmount);
                    }
                }
                else
                {
                    birdLen -= dt;
                    float env = Mathf.Sin(Mathf.Clamp01(birdLen / 0.14f) * Mathf.PI);
                    birdPhase += (birdFreq + 900f * Mathf.Sin(birdTime * 70f) - birdLen * 6000f) * dt;
                    s += Mathf.Sin(birdPhase * 2f * Mathf.PI) * env * 0.05f * (0.4f + birdAmount);
                }

                // Footsteps on stone: a soft low thump with a short gritty scuff.
                if (stepChild > 0f) { stepT = 0f; stepGain = 0.9f + 0.2f * (float)rng.NextDouble(); stepChild = 0f; }
                if (stepT < 0.16f)
                {
                    stepT += dt; float e = Mathf.Exp(-stepT * 38f);
                    stepLp += (n - stepLp) * 0.12f;
                    s += (stepLp * 0.5f + Mathf.Sin(stepT * 2f * Mathf.PI * 95f) * 0.6f) * e * 0.16f * stepGain;
                }
                if (stepJiddo > 0f) { jStepT = 0f; stepJiddo = 0f; }
                if (jStepT < 0.18f)
                {
                    jStepT += dt; float e = Mathf.Exp(-jStepT * 30f);
                    jStepLp += (n - jStepLp) * 0.08f;
                    s += (jStepLp * 0.5f + Mathf.Sin(jStepT * 2f * Mathf.PI * 80f) * 0.6f) * e * 0.09f;
                }

                // A clue found: two soft bell notes.
                if (chime > 0f) { chimeT = 0f; chime = 0f; }
                if (chimeT < 2.5f)
                {
                    chimeT += dt;
                    float a = Mathf.Exp(-chimeT * 2.2f) * Bell(chimeT, 880f);
                    float b = chimeT > 0.18f ? Mathf.Exp(-(chimeT - 0.18f) * 2.2f) * Bell(chimeT - 0.18f, 1318.5f) : 0f;
                    s += (a + b) * 0.08f;
                }

                // The Time Lens reveal: a warm chord that rises and opens over five seconds.
                if (reveal > 0f) { revealT = 0f; reveal = 0f; }
                if (revealT < 7f)
                {
                    revealT += dt;
                    float env = Mathf.Sin(Mathf.Clamp01(revealT / 7f) * Mathf.PI) * 0.06f;
                    float rise = 1f + 0.5f * Mathf.SmoothStep(0f, 1f, revealT / 5f);
                    s += env * (Mathf.Sin(revealT * 2f * Mathf.PI * 220f * rise) + 0.6f * Mathf.Sin(revealT * 2f * Mathf.PI * 330f * rise)
                                + 0.4f * Mathf.Sin(revealT * 2f * Mathf.PI * 440f * rise));
                }

                // "Then" sounds a little muffled, like a memory.
                float cut = Mathf.Lerp(1f, 0.12f, muffle);
                lp1 += (s - lp1) * cut; lp2 += (lp1 - lp2) * cut; s = lp2;

                for (int c = 0; c < channels; c++) data[i + c] += s;
            }
        }

        static float Bell(float t, float f) => Mathf.Sin(t * 2f * Mathf.PI * f) + 0.3f * Mathf.Sin(t * 2f * Mathf.PI * f * 2.76f);
    }
}

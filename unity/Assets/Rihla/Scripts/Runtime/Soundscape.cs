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

        public static Soundscape Instance { get; private set; }

        // Shared with the audio thread.
        volatile float stepChild, stepJiddo, chime, reveal, muffle, birdAmount;
        int rate; System.Random rng = new System.Random(7);
        float wind, windLp, windGain, birdTimer, birdPhase, birdFreq, birdTime, birdLen, birdChirps, lp1, lp2;
        float stepT = 10f, stepGain, stepLp, jStepT = 10f, jStepLp, chimeT = 10f, revealT = 10f;
        float childDist, jiddoDist; Vector3 lastChild, lastJiddo;
        Transform[] trees;

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
            birdAmount = Mathf.Clamp01(birdiness);
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

                // Breeze: slowly swelling, low-passed noise.
                windGain += (0.5f + 0.5f * Mathf.Sin(birdTime * 0.21f) * Mathf.Sin(birdTime * 0.07f) - windGain) * 0.00002f;
                windLp += (n - windLp) * 0.02f; wind += (windLp - wind) * 0.05f;
                s += wind * (0.05f + 0.07f * windGain);

                // Birds: short sweeping chirps in little bursts; more of them among trees.
                birdTime += dt;
                if (birdLen <= 0f)
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

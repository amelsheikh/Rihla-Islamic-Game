using UnityEngine;

namespace Rihla
{
    /// <summary>
    /// The sound of Jiddo's car (the owner, 20 November 2026: "the sounds, we need to start working on them"), made by
    /// code so it needs no recordings: the engine, a low hum of firing pulses whose pitch rises with the speed and
    /// the accelerator, through four gears; the tyres on the asphalt; the wind past the windows at speed. The engine
    /// is switched off when nobody is in the car.
    /// </summary>
    [RequireComponent(typeof(CarController))]
    public class CarSound : MonoBehaviour
    {
        [Range(0f, 1f)] public float volume = 0.55f;
        CarController car; AudioSource engine, tyres, wind;
        float lastKmh, load, running;

        void Start()
        {
            car = GetComponent<CarController>();
            engine = Source("Engine", EngineClip(), 0.6f);
            tyres = Source("Tyres", NoiseClip("tyre noise", 0.985f, 0.4f), 0f);
            wind = Source("Wind", NoiseClip("wind noise", 0.995f, 0.25f), 0f);
        }

        AudioSource Source(string name, AudioClip clip, float vol)
        {
            var g = new GameObject(name); g.transform.SetParent(transform, false);
            var s = g.AddComponent<AudioSource>();
            s.clip = clip; s.loop = true; s.volume = vol * volume; s.spatialBlend = 0.6f; s.dopplerLevel = 0f;
            s.minDistance = 4f; s.maxDistance = 60f; s.rolloffMode = AudioRolloffMode.Linear;
            s.Play();
            return s;
        }

        void Update()
        {
            if (!engine) return;
            float kmh = car.SpeedKmh, dt = Mathf.Max(Time.deltaTime, 1e-4f);
            float accel = (kmh - lastKmh) / dt; lastKmh = kmh;
            load = Mathf.Lerp(load, Mathf.Clamp01(accel / 8f + 0.15f), 1f - Mathf.Exp(-4f * dt));
            running = Mathf.MoveTowards(running, car.driverPresent ? 1f : 0f, dt * 0.8f);
            // Four gears: within each, the engine climbs from about 1.0 to 1.9 times its idle note.
            float[] top = { 22f, 45f, 70f, 200f };
            float lo = 0f, rpm = 1f;
            for (int i = 0; i < top.Length; i++)
            {
                if (kmh <= top[i] || i == top.Length - 1) { rpm = 1f + 0.9f * Mathf.Clamp01((kmh - lo) / (top[i] - lo)) + i * 0.08f; break; }
                lo = top[i] - 6f;
            }
            engine.pitch = Mathf.Lerp(engine.pitch, rpm * (0.95f + load * 0.15f), 1f - Mathf.Exp(-6f * dt));
            engine.volume = volume * running * (0.32f + 0.35f * load + 0.12f * Mathf.Clamp01(kmh / 80f));
            tyres.volume = volume * Mathf.Clamp01(kmh / 70f) * 0.55f;
            tyres.pitch = 0.8f + Mathf.Clamp01(kmh / 100f) * 0.5f;
            wind.volume = volume * Mathf.Clamp01((kmh - 30f) / 70f) * 0.5f;
            bool paused = Time.timeScale == 0f;
            engine.mute = tyres.mute = wind.mute = paused;
        }

        /// <summary>One second of a four-cylinder engine at idle: pulses at 30 a second and their harmonics, rough and
        /// a little uneven, made to loop without a click.</summary>
        static AudioClip EngineClip()
        {
            int rate = 44100, n = rate;
            var d = new float[n]; var rng = new System.Random(4);
            float f0 = 30f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)rate, s = 0f;
                for (int h = 1; h <= 10; h++)
                {
                    float a = h == 1 ? 1f : h == 2 ? 0.75f : h == 4 ? 0.45f : 0.6f / h;
                    s += a * Mathf.Sin(2f * Mathf.PI * f0 * h * t + h * 1.7f);
                }
                // The firing pulse: each cycle starts with a short knock.
                float ph = (t * f0) % 1f; s += Mathf.Exp(-ph * 18f) * 0.9f;
                s += ((float)rng.NextDouble() * 2f - 1f) * 0.08f;
                d[i] = s * 0.18f;
            }
            var clip = AudioClip.Create("engine", n, 1, rate, false); clip.SetData(d, 0);
            return clip;
        }

        /// <summary>Two seconds of noise, low-passed (smooth = how much: 0.995 is a deep rumble), looped smoothly.</summary>
        internal static AudioClip NoiseClip(string name, float smooth, float gain)
        {
            int rate = 44100, n = rate * 2, fade = rate / 5;
            var d = new float[n + fade]; var rng = new System.Random(name.Length * 31);
            float lp = 0f, lp2 = 0f;
            for (int i = 0; i < d.Length; i++)
            {
                lp = lp * smooth + ((float)rng.NextDouble() * 2f - 1f) * (1f - smooth);
                lp2 = lp2 * 0.6f + lp * 0.4f;
                d[i] = lp2;
            }
            float peak = 1e-4f; foreach (var x in d) peak = Mathf.Max(peak, Mathf.Abs(x));
            var o = new float[n];
            for (int i = 0; i < n; i++)
            {
                float v = d[i];
                if (i < fade) { float k = i / (float)fade; v = d[i] * k + d[n + i] * (1f - k); }
                o[i] = v / peak * gain;
            }
            var clip = AudioClip.Create(name, n, 1, rate, false); clip.SetData(o, 0);
            return clip;
        }
    }
}

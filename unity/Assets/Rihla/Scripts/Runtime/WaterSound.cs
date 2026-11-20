using UnityEngine;

namespace Rihla
{
    /// <summary>
    /// Running water heard near a spring or a channel (Ariha's Ein al-Sultan): a loop of soft rushing noise with little
    /// bubbles in it, made by code, placed in the world so it grows louder as the child walks closer.
    /// </summary>
    public class WaterSound : MonoBehaviour
    {
        [Range(0f, 1f)] public float volume = 0.6f;
        public float reach = 35f;

        void Start()
        {
            var s = gameObject.AddComponent<AudioSource>();
            s.clip = Clip(); s.loop = true; s.volume = volume; s.spatialBlend = 1f; s.dopplerLevel = 0f;
            s.minDistance = 3f; s.maxDistance = reach; s.rolloffMode = AudioRolloffMode.Linear;
            s.Play();
        }

        static AudioClip Clip()
        {
            int rate = 44100, n = rate * 3, fade = rate / 4;
            var d = new float[n + fade]; var rng = new System.Random(17);
            float lp = 0f, hp = 0f, prev = 0f, bub = 0f, bubF = 0f, bubT = 0f;
            for (int i = 0; i < d.Length; i++)
            {
                float w = (float)rng.NextDouble() * 2f - 1f;
                lp = lp * 0.82f + w * 0.18f;            // the rush: bright noise...
                hp = 0.97f * (hp + lp - prev); prev = lp; // ...without the rumble
                // Now and then a small bubble: a quick tone that rises as it fades.
                if (bub <= 0.001f && rng.NextDouble() < 0.0006) { bub = 1f; bubF = 500f + (float)rng.NextDouble() * 900f; bubT = 0f; }
                float b = 0f;
                if (bub > 0.001f) { bubT += 1f / rate; b = Mathf.Sin(2f * Mathf.PI * bubF * (1f + bubT * 6f) * bubT) * bub * 0.35f; bub *= 0.9993f; }
                d[i] = hp * 0.5f + b;
            }
            var o = new float[n];
            for (int i = 0; i < n; i++) { float v = d[i]; if (i < fade) { float k = i / (float)fade; v = d[i] * k + d[n + i] * (1f - k); } o[i] = v * 0.5f; }
            var clip = AudioClip.Create("water", n, 1, rate, false); clip.SetData(o, 0);
            return clip;
        }
    }
}

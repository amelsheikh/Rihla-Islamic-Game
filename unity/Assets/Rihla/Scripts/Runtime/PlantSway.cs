using System.Collections.Generic;
using UnityEngine;

namespace Rihla
{
    /// <summary>
    /// The breeze in the trees (the owner, 20 November 2026: "effects like a breeze, leaves moving"): the crowns of the
    /// palms near the camera rock and turn a little, more when a gust comes (Soundscape.Gust), each at its own pace.
    /// Only crowns within <see cref="distance"/> move, so a grove of a thousand palms costs almost nothing.
    /// </summary>
    public class PlantSway : MonoBehaviour
    {
        public float distance = 70f;
        readonly List<Transform> crowns = new List<Transform>();
        readonly List<Quaternion> rest = new List<Quaternion>();
        readonly List<float> phase = new List<float>();
        float scanTimer;
        int cursor; readonly List<int> near = new List<int>();

        void Start()
        {
            foreach (var t in FindObjectsByType<Transform>(FindObjectsInactive.Include))
                if (t.name == "Crown") { crowns.Add(t); rest.Add(t.localRotation); phase.Add(t.position.x * 0.37f + t.position.z * 0.21f); }
        }

        void Update()
        {
            if (crowns.Count == 0) return;
            Vector3 cam = transform.position;
            // Every half second, find which crowns are near (a quarter of them per frame, so no frame stalls).
            if ((scanTimer -= Time.deltaTime) <= 0f && cursor == 0) { scanTimer = 0.5f; near.Clear(); cursor = 1; }
            if (cursor > 0)
            {
                int n = Mathf.CeilToInt(crowns.Count / 4f), end = Mathf.Min(crowns.Count, cursor - 1 + n);
                float d2 = distance * distance;
                for (int i = cursor - 1; i < end; i++)
                    if (crowns[i] && crowns[i].gameObject.activeInHierarchy && (crowns[i].position - cam).sqrMagnitude < d2) near.Add(i);
                cursor = end >= crowns.Count ? 0 : end + 1;
            }
            float g = Soundscape.Gust, t = Time.time;
            float amp = 1.2f + g * 4.5f;
            foreach (int i in near)
            {
                float ph = phase[i];
                float x = Mathf.Sin(t * 0.9f + ph) * amp + Mathf.Sin(t * 2.3f + ph * 1.7f) * amp * 0.25f;
                float z = Mathf.Cos(t * 0.7f + ph * 1.3f) * amp * 0.6f;
                float y = Mathf.Sin(t * 0.5f + ph) * amp * 0.8f;
                crowns[i].localRotation = rest[i] * Quaternion.Euler(x, y, z);
            }
        }
    }
}

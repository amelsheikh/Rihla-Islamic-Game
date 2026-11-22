using UnityEngine;

namespace Rihla
{
    /// <summary>
    /// A clue of the walk: a small embroidered box floating over a golden ring (built by RihlaWalkBuilder.Marker).
    /// It bobs and turns slowly, with sparkles around it; Open() lifts the lid, lets the light out, and the box
    /// shrinks away. Runs on unscaled time, so it keeps moving while the clue's page is open.
    /// </summary>
    public class ClueBox : MonoBehaviour
    {
        public Transform box, lid, sparkles;
        float opened = -1f, seed;

        void Awake() => seed = transform.position.x * 0.37f;

        public void Open()
        {
            if (opened < 0f) opened = 0f;
        }

        void Update()
        {
            float t = Time.unscaledTime + seed, dt = Time.unscaledDeltaTime;
            if (box)
            {
                box.localPosition = new Vector3(0f, 1.15f + Mathf.Sin(t * 2f) * 0.07f, 0f);
                box.localRotation = Quaternion.Euler(Mathf.Sin(t * 1.3f) * 4f, t * 32f, 0f);
            }
            if (sparkles)
            {
                sparkles.Rotate(0f, -70f * dt, 0f, Space.Self);
                sparkles.localPosition = new Vector3(0f, 1.25f + Mathf.Sin(t * 1.4f) * 0.1f, 0f);
            }
            if (opened < 0f) return;
            opened += dt;
            if (lid) lid.localRotation = Quaternion.Euler(-Mathf.SmoothStep(0f, 1f, opened / 0.7f) * 105f, 0f, 0f);
            if (sparkles) sparkles.localScale = Vector3.one * (1f + opened * 1.5f);
            float shrink = Mathf.Clamp01((opened - 1.6f) / 0.6f);
            transform.localScale = Vector3.one * (1f - shrink);
            if (shrink >= 1f) gameObject.SetActive(false);
        }
    }
}

using UnityEngine;

namespace Rihla
{
    /// <summary>
    /// A person of the town going about the morning: walks a lane back and forth at an easy pace, pauses now and then,
    /// and stops to let the child pass. With a one-point route the person stands, facing <see cref="faceTo"/>, as if
    /// talking with a neighbour.
    /// </summary>
    public class Townsfolk : MonoBehaviour
    {
        public Vector3[] route;
        public float speed = 1.1f;
        public Vector3 faceTo;
        public Transform child;

        int next = 1, dir = 1;
        float wait;

        void Start()
        {
            wait = Random.Range(0f, 3f);
            if (route == null || route.Length == 0) route = new[] { transform.position };
            transform.position = Ground(route[0]);
            if (route.Length == 1) Face(faceTo - transform.position, 1f);
        }

        void Update()
        {
            if (route.Length < 2) return;
            float dt = Time.deltaTime;
            if (wait > 0f) { wait -= dt; return; }
            Vector3 p = transform.position, goal = route[next], d = goal - p; d.y = 0f;
            // Let the child pass: stop when the child is close in front.
            if (child)
            {
                Vector3 c = child.position - p; c.y = 0f;
                if (c.sqrMagnitude < 2.6f && Vector3.Dot(c, d) > 0f) { Face(c, dt * 3f); return; }
            }
            float move = speed * dt;
            if (d.magnitude <= move)
            {
                transform.position = Ground(goal);
                if (next + dir < 0 || next + dir >= route.Length) { dir = -dir; wait = Random.Range(1.5f, 5f); }
                next += dir;
                return;
            }
            transform.position = Ground(p + d.normalized * move);
            Face(d, dt * 5f);
        }

        void Face(Vector3 d, float t)
        {
            d.y = 0f;
            if (d.sqrMagnitude > 1e-4f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), Mathf.Clamp01(t));
        }

        /// <summary>Stands the person on whatever is underneath (lane, steps, courtyard).</summary>
        Vector3 Ground(Vector3 p)
        {
            float from = Mathf.Max(p.y, transform.position.y) + 1.5f;
            if (Physics.Raycast(new Vector3(p.x, from, p.z), Vector3.down, out var hit, 8f, ~0, QueryTriggerInteraction.Ignore))
                p.y = hit.point.y;
            return p;
        }
    }
}

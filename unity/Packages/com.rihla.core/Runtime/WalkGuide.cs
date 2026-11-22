using System.Collections.Generic;
using UnityEngine;

namespace Rihla
{
    /// <summary>
    /// Shows the child where to go: golden arrows on the ground along the road ahead (sliding forward, so the way is
    /// clear at a glance), and a bobbing arrow above the goal (Jiddo or a clue). WalkDirector gives it the road each
    /// frame. Nothing here casts shadows, and the arrows share one material, so it costs almost nothing.
    /// </summary>
    public class WalkGuide : MonoBehaviour
    {
        public Material material;
        public int count = 12;
        public float spacing = 1.9f, start = 1.4f;
        /// <summary>Size of the arrows, how high the goal's arrow floats and its size, and how fast the trail flows
        /// (the drive uses bigger arrows, spaced out along the road).</summary>
        public float arrowScale = 1f, beaconHeight = 2.7f, beaconScale = 1.4f, slideSpeed = 1.1f;
        /// <summary>How far the arrows stand up from the ground (degrees), and their height above it: seen from a car
        /// a flat arrow is only a thin line.</summary>
        public float tilt = 0f, lift = 0.07f;

        readonly List<Transform> arrows = new List<Transform>();
        Transform beacon;

        void Awake()
        {
            var chevron = Chevron(); var down = DownArrow();
            for (int i = 0; i < count; i++) arrows.Add(Part("Guide arrow", chevron, transform));
            beacon = Part("Goal arrow", down, transform);
        }

        Transform Part(string name, Mesh mesh, Transform parent)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = material;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            go.SetActive(false);
            return go.transform;
        }

        /// <summary>Lays the arrows along <paramref name="route"/> (from the child to the goal), or hides them if null.</summary>
        public void Show(List<Vector3> route, Vector3? goal)
        {
            float total = 0f;
            if (route != null) for (int i = 0; i < route.Count - 1; i++) total += Flat(route[i], route[i + 1]);
            float slide = Time.time * slideSpeed % spacing;
            for (int k = 0; k < arrows.Count; k++)
            {
                float d = start + slide + k * spacing;
                bool on = route != null && d < total - 1.2f;
                var a = arrows[k];
                if (!on) { if (a.gameObject.activeSelf) a.gameObject.SetActive(false); continue; }
                At(route, d, out var p, out var dir);
                a.position = Ground(p) + Vector3.up * lift;
                a.rotation = Quaternion.LookRotation(dir) * Quaternion.Euler(-tilt, 0f, 0f);
                // The first and last arrows grow in and fade out, so the trail flows.
                float s = Mathf.Clamp01((d - start) / spacing) * Mathf.Clamp01((total - 1.2f - d) / spacing) * Mathf.Clamp01((start + count * spacing - d) / (spacing * 2f));
                a.localScale = Vector3.one * Mathf.Lerp(0.4f, 1.35f, s) * arrowScale;
                if (!a.gameObject.activeSelf) a.gameObject.SetActive(true);
            }
            if (goal.HasValue)
            {
                beacon.gameObject.SetActive(true);
                beacon.position = goal.Value + Vector3.up * (beaconHeight + Mathf.Sin(Time.time * 3f) * 0.18f * beaconScale);
                beacon.rotation = Quaternion.Euler(0f, Time.time * 90f, 0f); beacon.localScale = Vector3.one * beaconScale;
            }
            else beacon.gameObject.SetActive(false);
        }

        static void At(List<Vector3> route, float d, out Vector3 p, out Vector3 dir)
        {
            for (int i = 0; i < route.Count - 1; i++)
            {
                float len = Flat(route[i], route[i + 1]);
                if (d <= len || i == route.Count - 2)
                {
                    p = Vector3.Lerp(route[i], route[i + 1], len < 1e-4f ? 0f : Mathf.Clamp01(d / len));
                    dir = route[i + 1] - route[i]; dir.y = 0f; if (dir.sqrMagnitude < 1e-6f) dir = Vector3.forward;
                    return;
                }
                d -= len;
            }
            p = route[route.Count - 1]; dir = Vector3.forward;
        }

        static float Flat(Vector3 a, Vector3 b) => Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));

        static Vector3 Ground(Vector3 p)
        {
            if (Physics.Raycast(new Vector3(p.x, p.y + 1.6f, p.z), Vector3.down, out var hit, 8f, ~0, QueryTriggerInteraction.Ignore)) p.y = hit.point.y;
            return p;
        }

        // ------------------------------------------------------------------ meshes

        /// <summary>A flat chevron pointing along +z, 0.84 m wide.</summary>
        static Mesh Chevron()
        {
            var v = new[]
            {
                new Vector3(-0.42f, 0f, -0.14f), new Vector3(0f, 0f, 0.3f), new Vector3(0f, 0f, 0.06f), new Vector3(-0.42f, 0f, -0.38f),
                new Vector3(0f, 0f, 0.3f), new Vector3(0.42f, 0f, -0.14f), new Vector3(0.42f, 0f, -0.38f), new Vector3(0f, 0f, 0.06f),
            };
            var m = new Mesh { name = "Chevron", vertices = v, triangles = new[] { 0, 1, 2, 0, 2, 3, 4, 5, 6, 4, 6, 7 } };
            m.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up, Vector3.up, Vector3.up, Vector3.up, Vector3.up };
            m.RecalculateBounds();
            return m;
        }

        /// <summary>An arrow pointing down: a four-sided point below a square shaft.</summary>
        static Mesh DownArrow()
        {
            var v = new List<Vector3>(); var t = new List<int>();
            void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d) { int i = v.Count; v.Add(a); v.Add(b); v.Add(c); v.Add(d); t.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3 }); }
            void Tri(Vector3 a, Vector3 b, Vector3 c) { int i = v.Count; v.Add(a); v.Add(b); v.Add(c); t.AddRange(new[] { i, i + 1, i + 2 }); }
            float w = 0.3f, s = 0.1f; Vector3 tip = Vector3.zero;
            var b0 = new[] { new Vector3(-w, 0.45f, -w), new Vector3(w, 0.45f, -w), new Vector3(w, 0.45f, w), new Vector3(-w, 0.45f, w) };
            for (int i = 0; i < 4; i++) Tri(tip, b0[(i + 1) % 4], b0[i]);
            Quad(b0[0], b0[1], b0[2], b0[3]);
            var lo = new[] { new Vector3(-s, 0.45f, -s), new Vector3(s, 0.45f, -s), new Vector3(s, 0.45f, s), new Vector3(-s, 0.45f, s) };
            for (int i = 0; i < 4; i++)
            {
                Vector3 a = lo[i], b = lo[(i + 1) % 4], up = Vector3.up * 0.55f;
                Quad(a, b, b + up, a + up);
            }
            Quad(lo[3] + Vector3.up * 0.55f, lo[2] + Vector3.up * 0.55f, lo[1] + Vector3.up * 0.55f, lo[0] + Vector3.up * 0.55f);
            var m = new Mesh { name = "Down arrow" }; m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }
    }
}

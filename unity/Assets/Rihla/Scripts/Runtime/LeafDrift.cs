using UnityEngine;

namespace Rihla
{
    /// <summary>
    /// Leaves blowing past in the breeze (C1a): a few small olive leaves tumble through the air near the camera when
    /// trees are close and a gust comes (Soundscape.Gust and Leafiness), drifting with the wind. One particle system
    /// that follows the camera, so it costs the same anywhere on the walk.
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    public class LeafDrift : MonoBehaviour
    {
        public Transform follow;
        /// <summary>Which way the breeze blows (from the sea in the west, towards the east: an estimate).</summary>
        public Vector3 wind = new Vector3(1f, 0f, 0.3f);
        public float rate = 16f;
        ParticleSystem ps;

        void Awake()
        {
            ps = GetComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true; main.playOnAwake = true; main.duration = 10f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(5f, 9f);
            main.startSpeed = 0.15f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.045f, 0.085f);
            main.startRotation3D = true;
            main.startRotationX = main.startRotationY = main.startRotationZ = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.gravityModifier = 0.035f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 160;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.42f, 0.46f, 0.28f), new Color(0.62f, 0.52f, 0.26f));
            var em = ps.emission; em.rateOverTime = 0f;
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(24f, 5f, 24f);
            var rot = ps.rotationOverLifetime; rot.enabled = true; rot.separateAxes = true;
            rot.x = new ParticleSystem.MinMaxCurve(-4f, 4f); rot.y = new ParticleSystem.MinMaxCurve(-3f, 3f); rot.z = new ParticleSystem.MinMaxCurve(-4f, 4f);
            var noise = ps.noise; noise.enabled = true; noise.strength = 0.5f; noise.frequency = 0.35f; noise.scrollSpeed = 0.3f;
            var r = GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Mesh; r.mesh = Leaf();
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            ps.Play();
        }

        void Update()
        {
            if (follow) transform.position = follow.position + Vector3.up * 2.5f + Vector3.ProjectOnPlane(follow.forward, Vector3.up).normalized * 5f;
            float g = Soundscape.Gust;
            var em = ps.emission; em.rateOverTime = rate * Soundscape.Leafiness * Mathf.Clamp01(g * 1.8f - 0.35f);
            var v = ps.velocityOverLifetime; v.enabled = true; v.space = ParticleSystemSimulationSpace.World;
            Vector3 w = wind.normalized * (0.4f + 2.2f * g);
            v.x = new ParticleSystem.MinMaxCurve(w.x * 0.6f, w.x * 1.2f);
            v.y = new ParticleSystem.MinMaxCurve(-0.25f, 0.15f);
            v.z = new ParticleSystem.MinMaxCurve(w.z * 0.6f, w.z * 1.2f);
        }

        /// <summary>A small pointed leaf, twice as long as it is wide, drawn on both sides.</summary>
        static Mesh Leaf()
        {
            var v = new[] { new Vector3(0f, 0f, -1f), new Vector3(0.45f, 0f, 0f), new Vector3(0f, 0f, 1f), new Vector3(-0.45f, 0f, 0f) };
            var m = new Mesh { name = "Leaf" };
            m.vertices = new[] { v[0], v[1], v[2], v[3], v[0], v[1], v[2], v[3] };
            m.triangles = new[] { 0, 3, 2, 0, 2, 1, 4, 5, 6, 4, 6, 7 };
            m.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up, Vector3.down, Vector3.down, Vector3.down, Vector3.down };
            m.RecalculateBounds();
            return m;
        }
    }
}

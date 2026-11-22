using UnityEngine;
using UnityEngine.Rendering;

namespace Rihla
{
    /// <summary>
    /// Sets the scene's ambient light (the fill that lights everything in shade) from three colours: the sky
    /// above, the horizon, and the ground below. Unity keeps a stored ambient probe that is only recalculated
    /// when lighting is baked, so a gradient set by code would otherwise not show; this projects the gradient
    /// onto the probe directly when the scene starts.
    /// </summary>
    public class SkyFill : MonoBehaviour
    {
        public Color sky = new Color(0.74f, 0.81f, 0.95f), horizon = new Color(0.76f, 0.74f, 0.7f), ground = new Color(0.66f, 0.57f, 0.46f);
        public float intensity = 1f;
        /// <summary>Scale that makes a uniform gradient equal to Unity's own AddAmbientLight (checked in the editor).</summary>
        const float Norm = 1f / 5.918f;

        void OnEnable() => Apply();

        public void Apply()
        {
            RenderSettings.ambientMode = AmbientMode.Custom; // "Custom": the probe below is used as it is
            RenderSettings.ambientProbe = Project(sky * intensity, horizon * intensity, ground * intensity);
        }

        /// <summary>Projects the gradient sky-horizon-ground onto second-order spherical harmonics.</summary>
        public static SphericalHarmonicsL2 Project(Color sky, Color horizon, Color ground)
        {
            var sh = new SphericalHarmonicsL2();
            const int N = 24; // directions per axis on a latitude-longitude grid, weighted by solid angle
            float total = 0f;
            for (int i = 0; i < N; i++)
            {
                float theta = Mathf.PI * (i + 0.5f) / N, y = Mathf.Cos(theta), r = Mathf.Sin(theta);
                float weight = r * (Mathf.PI / N) * (2f * Mathf.PI / (2 * N));
                Color c = y >= 0f ? Color.Lerp(horizon, sky, Mathf.Pow(y, 0.6f)) : Color.Lerp(horizon, ground, Mathf.Pow(-y, 0.6f));
                for (int j = 0; j < 2 * N; j++)
                {
                    float phi = 2f * Mathf.PI * (j + 0.5f) / (2 * N);
                    var d = new Vector3(r * Mathf.Cos(phi), y, r * Mathf.Sin(phi));
                    // Adding a directional light of colour c * weight in direction d builds up the projection of
                    // the gradient (Unity's AddDirectionalLight handles the basis and its constants).
                    sh.AddDirectionalLight(d, c, weight * Norm);
                    total += weight;
                }
            }
            return sh;
        }
    }
}

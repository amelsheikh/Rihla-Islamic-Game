using UnityEngine;

namespace Rihla
{
    /// <summary>
    /// Keeps the road scene fast on the laptop: small things beside the road (shrubs, stones, posts, rails, poles and
    /// wires, layer 9) are drawn only within 350 m of the camera, and palms (layer 8) within 450 m. Far away they are
    /// smaller than a pixel or hidden in the haze, but drawing all of them cost about two thirds of the frame rate.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class DetailCulling : MonoBehaviour
    {
        public const int Palms = 8, Small = 9;
        public float palmDistance = 450f, smallDistance = 300f;

        void Start()
        {
            var cam = GetComponent<Camera>();
            var d = new float[32];
            d[Palms] = palmDistance; d[Small] = smallDistance;
            cam.layerCullDistances = d; cam.layerCullSpherical = true;
        }
    }
}

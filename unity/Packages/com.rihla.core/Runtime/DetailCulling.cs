using UnityEngine;

namespace Rihla
{
    /// <summary>
    /// Keeps the road scene fast: small things beside the road (shrubs, stones, posts, rails, poles and wires,
    /// layer 9) and palms (layer 8) are drawn only near the camera. How near depends on the picture setting
    /// (<see cref="Quality"/>): 200 m and 300 m on Fast, 600 m and 1,200 m on Best. Far away they are smaller than a
    /// pixel or hidden in the haze, but drawing all of them cost about two thirds of the frame rate on the laptop.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class DetailCulling : MonoBehaviour
    {
        public const int Palms = 8, Small = 9;

        void Start() => Apply();

        public void Apply()
        {
            var cam = GetComponent<Camera>();
            var d = new float[32];
            d[Palms] = Quality.PalmDistance; d[Small] = Quality.SmallDistance;
            cam.layerCullDistances = d; cam.layerCullSpherical = true;
        }
    }
}

using UnityEngine;

namespace Rihla
{
    /// <summary>The centre line of the Jericho road, filled by the scene builder from scene.json.</summary>
    public class RoadPath : MonoBehaviour
    {
        public Vector3[] points;
        public float width = 7f;
        public float[] distances;

        [System.Serializable]
        public struct Place { public string id; public string kind; public string nameEn; public string nameAr; public Vector3 position; public float roadDist; }
        public Place[] places;

        public float Length => distances != null && distances.Length > 0 ? distances[distances.Length - 1] : 0f;

        public void ComputeDistances()
        {
            distances = new float[points.Length];
            for (int i = 1; i < points.Length; i++) distances[i] = distances[i - 1] + Vector3.Distance(points[i - 1], points[i]);
        }

        /// <summary>Index of the closest road point, searching near a hint for speed.</summary>
        public int Nearest(Vector3 p, int hint = -1, int window = 80)
        {
            int lo = 0, hi = points.Length - 1;
            if (hint >= 0) { lo = Mathf.Max(0, hint - window); hi = Mathf.Min(points.Length - 1, hint + window); }
            int best = lo; float bd = float.MaxValue;
            for (int i = lo; i <= hi; i++)
            {
                float dx = points[i].x - p.x, dz = points[i].z - p.z, d = dx * dx + dz * dz;
                if (d < bd) { bd = d; best = i; }
            }
            return best;
        }

        /// <summary>The point a given distance further along the road from index i.</summary>
        public Vector3 Ahead(int i, float metres)
        {
            float target = distances[i] + metres;
            while (i < points.Length - 1 && distances[i + 1] < target) i++;
            if (i >= points.Length - 1) return points[points.Length - 1];
            float t = Mathf.InverseLerp(distances[i], distances[i + 1], target);
            return Vector3.Lerp(points[i], points[i + 1], t);
        }

        /// <summary>The point at a distance along the road from its start (clamped to the road).</summary>
        public Vector3 At(float d)
        {
            if (d <= 0f) return points[0];
            if (d >= Length) return points[points.Length - 1];
            int lo = 0, hi = distances.Length - 1;
            while (hi - lo > 1) { int m = (lo + hi) / 2; if (distances[m] <= d) lo = m; else hi = m; }
            return Vector3.Lerp(points[lo], points[hi], Mathf.InverseLerp(distances[lo], distances[hi], d));
        }

        public Place? NearestPlace(Vector3 p, out float metres)
        {
            metres = float.MaxValue; Place? best = null;
            if (places == null) return null;
            foreach (var pl in places)
            {
                float d = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(pl.position.x, pl.position.z));
                if (d < metres) { metres = d; best = pl; }
            }
            return best;
        }
    }
}

using UnityEngine;

namespace Rihla
{
    /// <summary>
    /// A cloth that waves in the breeze: a banner on a pole, or a little pennant on a string. Its mesh is a small grid
    /// moved every frame by travelling waves that grow with the gusts (Soundscape.Gust), more at the free edge than at
    /// the pole. Hidden far from the camera, where it costs nothing.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class Banner : MonoBehaviour
    {
        public float width = 1.2f, height = 0.8f;
        public int columns = 10, rows = 6;
        /// <summary>How strongly it waves (1 = a banner, more for a light pennant).</summary>
        public float flutter = 1f;
        /// <summary>A pennant hangs as a triangle, pointing down.</summary>
        public bool pennant;
        Mesh mesh; Vector3[] rest, now; float seed;

        void Awake()
        {
            seed = transform.position.x * 0.73f + transform.position.z * 0.31f;
            mesh = new Mesh { name = "Banner cloth" };
            int n = (columns + 1) * (rows + 1);
            rest = new Vector3[n]; var uv = new Vector2[n];
            for (int j = 0; j <= rows; j++)
                for (int i = 0; i <= columns; i++)
                {
                    float u = i / (float)columns, v = j / (float)rows;
                    float x = u * width, y = -v * height;
                    if (pennant) { x = (u - 0.5f) * width * (1f - v); y = -v * height; }
                    rest[j * (columns + 1) + i] = new Vector3(x, y, 0f);
                    uv[j * (columns + 1) + i] = new Vector2(u, 1f - v);
                }
            var tris = new int[columns * rows * 6]; int t = 0;
            for (int j = 0; j < rows; j++)
                for (int i = 0; i < columns; i++)
                {
                    int a = j * (columns + 1) + i, b = a + 1, c = a + columns + 1, d = c + 1;
                    tris[t++] = a; tris[t++] = b; tris[t++] = c; tris[t++] = b; tris[t++] = d; tris[t++] = c;
                }
            now = (Vector3[])rest.Clone();
            mesh.vertices = now; mesh.uv = uv; mesh.triangles = tris; mesh.RecalculateNormals();
            mesh.bounds = new Bounds(new Vector3(width / 2f, -height / 2f, 0f), new Vector3(width + 1f, height + 1f, 2f));
            GetComponent<MeshFilter>().sharedMesh = mesh;
        }

        void Update()
        {
            var cam = Camera.main;
            if (cam && (cam.transform.position - transform.position).sqrMagnitude > 90f * 90f) return;
            float g = 0.35f + Soundscape.Gust * 0.9f, tt = Time.time * (2.2f + g * 2f) + seed;
            for (int k = 0; k < rest.Length; k++)
            {
                Vector3 r = rest[k];
                float free = pennant ? Mathf.Clamp01(-r.y / height) : Mathf.Clamp01(r.x / width);
                float wave = Mathf.Sin(r.x * 4.2f - tt) * 0.6f + Mathf.Sin(r.x * 7.1f + r.y * 3f - tt * 1.6f) * 0.25f;
                now[k] = new Vector3(r.x - free * free * 0.05f * g, r.y - free * 0.04f * (1f - g), wave * free * 0.16f * g * flutter);
            }
            mesh.vertices = now; mesh.RecalculateNormals();
        }
    }
}

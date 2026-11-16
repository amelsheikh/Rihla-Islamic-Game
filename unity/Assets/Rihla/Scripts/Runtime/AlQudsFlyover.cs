using System.IO;
using CesiumForUnity;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Rihla
{
    /// <summary>
    /// "Al-Quds today": a slow circle over the Old City on Google Photorealistic 3D Tiles, streamed by Cesium.
    /// The Cesium ion token (or a Google Map Tiles key) is read at run time from git-ignored StreamingAssets files.
    /// Esc or C goes back to the Jericho road.
    /// </summary>
    public class AlQudsFlyover : MonoBehaviour
    {
        public Cesium3DTileset tileset;
        public float radius = 900f, height = 450f, secondsPerCircle = 240f;
        public string backScene = "E0_JerichoRoad";

        float angle; string message = ""; GUIStyle style;

        /// <summary>Google Photorealistic 3D Tiles as served by Cesium ion.</summary>
        public const long IonGoogleTilesAsset = 2275207;

        void Awake()
        {
            // A Cesium ion token (free account) comes first; a Google Map Tiles API key is the alternative.
            string ion = ReadSecret("cesium_ion_token.txt"), key = ReadSecret("google_maps_key.txt");
            if (ion.Length > 0)
            {
                tileset.ionAccessToken = ion;
                tileset.ionAssetID = IonGoogleTilesAsset;
                tileset.tilesetSource = CesiumDataSource.FromCesiumIon;
            }
            else if (key.Length > 0)
            {
                tileset.tilesetSource = CesiumDataSource.FromUrl;
                tileset.url = "https://tile.googleapis.com/v1/3dtiles/root.json?key=" + key;
            }
            else
            {
                message = "No map key: put a Cesium ion token in StreamingAssets/cesium_ion_token.txt";
                Debug.LogWarning("[Rihla] " + message);
            }
        }

        static string ReadSecret(string file)
        {
            string path = Path.Combine(Application.streamingAssetsPath, file);
            return File.Exists(path) ? File.ReadAllText(path).Trim() : "";
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && (kb.escapeKey.wasPressedThisFrame || kb.cKey.wasPressedThisFrame)) SceneManager.LoadScene(backScene);
            if (kb != null && kb.leftArrowKey.isPressed) angle -= 20f * Time.deltaTime;
            if (kb != null && kb.rightArrowKey.isPressed) angle += 20f * Time.deltaTime;
            angle += 360f / secondsPerCircle * Time.deltaTime;
            float a = angle * Mathf.Deg2Rad;
            transform.position = new Vector3(Mathf.Sin(a) * radius, height, -Mathf.Cos(a) * radius);
            transform.LookAt(Vector3.zero);
        }

        void OnGUI()
        {
            style ??= new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, fontSize = 16, richText = true, padding = new RectOffset(10, 10, 8, 8) };
            GUI.Box(new Rect(12, 12, 460, message.Length > 0 ? 78 : 56),
                "<b>Al-Quds today</b>  (Google 3D map)\nArrows: turn · C or Esc: back to the road" + (message.Length > 0 ? "\n" + message : ""), style);
        }
    }
}

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
    /// The History button tells the story of the Old City's places through time, beside the city of today.
    /// Arrows turn; C or Esc goes back to the Jericho road.
    /// </summary>
    public class AlQudsFlyover : MonoBehaviour
    {
        public Cesium3DTileset tileset;
        public float radius = 900f, height = 450f, secondsPerCircle = 240f;
        public string backScene = "E0_JerichoRoad";
        /// <summary>The places of the Old City whose story through time is in content/history.json.</summary>
        public string[] historySites = { "dome-of-the-rock", "al-qibli", "bab-al-asbat" };

        float angle, fps, fpsTimer; int frames; string message = "";

        /// <summary>Google Photorealistic 3D Tiles as served by Cesium ion.</summary>
        public const long IonGoogleTilesAsset = 2275207;

        void Awake()
        {
            // A Cesium ion token (free account) comes first; a Google Map Tiles API key is the alternative.
            string ion = ReadSecret(CoreConfig.Current.cesiumIonTokenFile), key = ReadSecret(CoreConfig.Current.googleMapsKeyFile);
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
                message = "No map key: put a Cesium ion token in StreamingAssets/" + CoreConfig.Current.cesiumIonTokenFile;
                Debug.LogWarning("[Rihla] " + message);
            }
        }

        void Start()
        {
            var ui = RihlaUI.Instance;
            if (ui) ui.AddMenuItem(() => Lang.T("m.backRoad"), () => SceneManager.LoadScene(backScene));
            if (ui == null) return;
            ui.SetHistoryVisible(true);
            ui.OnHistory = () => TeachingPages.ChooseHistory(historySites, Lang.T("u.historyHere"));
            ui.OnJournal = () => TeachingPages.ShowPage(TeachingPages.FirstPage);
            if (TeachingPages.FirstPage != null) ui.SetJournalVisible(SaveData.Current.HasPage((string)TeachingPages.FirstPage["id"]));
        }

        static string ReadSecret(string file)
        {
            string path = Path.Combine(Application.streamingAssetsPath, file);
            return File.Exists(path) ? File.ReadAllText(path).Trim() : "";
        }

        void Update()
        {
            var ui = RihlaUI.Instance;
            bool modal = ui && ui.ModalOpen;
            var kb = Keyboard.current;
            // Keys wait a moment after the scene opens, so the press that opened it cannot also close it.
            if (Time.timeSinceLevelLoad > 1f && !modal && kb != null && kb.cKey.wasPressedThisFrame) { SceneManager.LoadScene(backScene); return; }
            if (!modal && kb != null && kb.leftArrowKey.isPressed) angle -= 20f * Time.deltaTime;
            if (!modal && kb != null && kb.rightArrowKey.isPressed) angle += 20f * Time.deltaTime;
            angle += 360f / secondsPerCircle * Time.deltaTime;
            float a = angle * Mathf.Deg2Rad;
            transform.position = new Vector3(Mathf.Sin(a) * radius, height, -Mathf.Cos(a) * radius);
            transform.LookAt(Vector3.zero);

            frames++; fpsTimer += Time.unscaledDeltaTime;
            if (fpsTimer >= 0.5f) { fps = frames / fpsTimer; frames = 0; fpsTimer = 0f; }
            if (ui == null) return;
            var c = ContentLibrary.Instance;
            ui.SetHud(Lang.T("u.alQudsToday"), Lang.T("u.flyHelp"), message,
                c ? c.PlaceName("al-quds", Lang.Current) : "", c ? c.PlaceName("al-quds", Lang.Arabic ? "en" : "ar") : "", Lang.T("u.flyKeys"));
            ui.SetFps(fps);
        }
    }
}

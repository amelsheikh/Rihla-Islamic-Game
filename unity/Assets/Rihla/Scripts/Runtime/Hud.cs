using UnityEngine;

namespace Rihla
{
    /// <summary>
    /// Frame-rate measuring and the C key to "Al-Quds today". What the player sees is drawn by RihlaUI and filled
    /// by Teaching. "-quitafter 60" on the command line logs the average FPS every 10 s and quits after 60 s.
    /// </summary>
    public class Hud : MonoBehaviour
    {
        public CarController car;
        public PlayerSwitch player;
        public RoadPath road;

        public float Fps { get; private set; }
        float fpsTimer; int frames;
        float quitAfter = -1f, logTimer, totalTime; int logFrames, totalFrames;

        void Start()
        {
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == "-quitafter" && float.TryParse(args[i + 1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float s)) quitAfter = s;
        }

        void Update()
        {
            frames++; fpsTimer += Time.unscaledDeltaTime;
            if (fpsTimer >= 0.5f) { Fps = frames / fpsTimer; frames = 0; fpsTimer = 0f; }
            var kb = UnityEngine.InputSystem.Keyboard.current;
            bool modal = RihlaUI.Instance && RihlaUI.Instance.ModalOpen;
            if (!modal && Time.timeSinceLevelLoad > 1f && kb != null && kb.cKey.wasPressedThisFrame && Application.CanStreamedLevelBeLoaded("E0_AlQuds_Cesium"))
                UnityEngine.SceneManagement.SceneManager.LoadScene("E0_AlQuds_Cesium");
            if (Time.unscaledTime < 5f) return; // skip loading
            logFrames++; logTimer += Time.unscaledDeltaTime; totalFrames++; totalTime += Time.unscaledDeltaTime;
            if (logTimer >= 10f)
            {
                Debug.Log($"[Rihla] FPS {logFrames / logTimer:0.0} at {Screen.width}x{Screen.height}, {car.SpeedKmh:0} km/h, road {car.RoadIndex}");
                logFrames = 0; logTimer = 0f;
            }
            if (quitAfter > 0f && Time.unscaledTime > quitAfter)
            {
                Debug.Log($"[Rihla] average FPS {totalFrames / totalTime:0.0} over {totalTime:0} s");
                Application.Quit();
            }
        }
    }
}

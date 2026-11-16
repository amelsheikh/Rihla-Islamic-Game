using UnityEngine;

namespace Rihla
{
    /// <summary>
    /// A plain placeholder HUD (IMGUI): speed, true elevation, distance to Ariha, nearest place, FPS.
    /// English only for now: IMGUI does not join Arabic letters, so the Arabic HUD waits for the real UI.
    /// </summary>
    public class Hud : MonoBehaviour
    {
        public CarController car;
        public PlayerSwitch player;
        public RoadPath road;
        /// <summary>Game heights are compressed; multiply to get true metres above sea level (src/world/warp.ts).</summary>
        public float verticalScale = 2.5f;

        float fps, fpsTimer; int frames;
        GUIStyle style;
        // Measuring: "-quitafter 60" logs the average FPS every 10 s to the player log and quits after 60 s.
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
            if (fpsTimer >= 0.5f) { fps = frames / fpsTimer; frames = 0; fpsTimer = 0f; }
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.cKey.wasPressedThisFrame && Application.CanStreamedLevelBeLoaded("E0_AlQuds_Cesium"))
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

        void OnGUI()
        {
            style ??= new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, fontSize = 16, richText = true, padding = new RectOffset(10, 10, 8, 8) };
            Transform who = player && player.OnFoot ? player.walker.transform : car.transform;
            float elev = who.position.y * verticalScale;
            string place = "";
            var near = road.NearestPlace(who.position, out float m);
            if (near.HasValue && m < 400f) place = $"\nNear: <b>{near.Value.nameEn}</b>";
            float toGo = road.Length - (car.RoadIndex >= 0 ? road.distances[car.RoadIndex] : 0f);
            string mode = player && player.OnFoot ? (player.NearCar ? "On foot · E to get in" : "On foot") :
                          car.autopilot ? "Jiddo is driving (P to take over)" : "Driving · E to get out when stopped · P autopilot";
            mode += " · C: Al-Quds today";
            GUI.Box(new Rect(12, 12, 400, 120),
                $"<b>{car.SpeedKmh:0} km/h</b>    Elevation {elev:0} m\nTo Ariha: {Mathf.Max(0, toGo) / 1000f:0.0} km (game){place}\n{mode}\n{fps:0} fps", style);
        }
    }
}

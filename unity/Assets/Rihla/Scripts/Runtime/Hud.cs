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

        void Update()
        {
            frames++; fpsTimer += Time.unscaledDeltaTime;
            if (fpsTimer >= 0.5f) { fps = frames / fpsTimer; frames = 0; fpsTimer = 0f; }
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
            GUI.Box(new Rect(12, 12, 400, 120),
                $"<b>{car.SpeedKmh:0} km/h</b>    Elevation {elev:0} m\nTo Ariha: {Mathf.Max(0, toGo) / 1000f:0.0} km (game){place}\n{mode}\n{fps:0} fps", style);
        }
    }
}

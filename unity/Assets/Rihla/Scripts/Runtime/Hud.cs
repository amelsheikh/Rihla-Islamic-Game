using UnityEngine;

namespace Rihla
{
    /// <summary>
    /// The frame rate shown on the road, and the C key to "Al-Quds today". What the player sees is drawn by RihlaUI
    /// and filled by Teaching. Measuring builds ("-quitafter") is done by RihlaUI, in every scene.
    /// </summary>
    public class Hud : MonoBehaviour
    {
        public CarController car;
        public PlayerSwitch player;
        public RoadPath road;

        public float Fps { get; private set; }
        float fpsTimer; int frames;

        // "-walk" and "-drive" (starting a scene directly, for testing) are handled by the opening, StoryOpening.

        void Update()
        {
            frames++; fpsTimer += Time.unscaledDeltaTime;
            if (fpsTimer >= 0.5f) { Fps = frames / fpsTimer; frames = 0; fpsTimer = 0f; }
            var kb = UnityEngine.InputSystem.Keyboard.current;
            bool modal = RihlaUI.Instance && RihlaUI.Instance.ModalOpen;
            if (!modal && Time.timeSinceLevelLoad > 1f && kb != null && kb.cKey.wasPressedThisFrame && Application.CanStreamedLevelBeLoaded("E0_AlQuds_Cesium"))
                UnityEngine.SceneManagement.SceneManager.LoadScene("E0_AlQuds_Cesium");
        }
    }
}

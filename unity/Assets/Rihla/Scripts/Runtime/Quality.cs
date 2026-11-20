using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Rihla
{
    /// <summary>
    /// The picture setting: "Fast" for a laptop without a graphics card, "Best" for a computer with one.
    /// The first time, it is chosen from the graphics chip (Auto); the button at the top (or Q) switches it, and the
    /// choice is saved. "-fast" and "-best" on the command line set it for tests.
    ///
    /// Fast: the screen drawn at 60% and scaled up; the ground's full textures only within 60 m (beyond, one baked
    /// colour map: on the laptop the textured ground cost more than anything else, 52 against 81 FPS); no ambient
    /// shading (SSAO, 52 against 73); shadows within 40 m; palms within 300 m and only every other palm in Ariha's
    /// groves; shrubs, posts and rails within 200 m; a coarser terrain.
    /// Best: the full screen resolution, smoother edges (MSAA x4), ambient shading, textured ground to 600 m,
    /// shadows within 120 m in two steps, every palm within 1,200 m, roadside things within 600 m, a finer terrain.
    /// </summary>
    public static class Quality
    {
        public static bool Best { get; private set; }
        public static event Action Changed;
        static UniversalRenderPipelineAsset copy;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init()
        {
            var args = Environment.GetCommandLineArgs();
            string q = SaveData.Current.quality;
            if (Array.IndexOf(args, "-fast") >= 0) q = "fast";
            if (Array.IndexOf(args, "-best") >= 0) q = "best";
            Best = q == "best" || (q != "fast" && HasGraphicsCard());
            Debug.Log($"[Rihla] picture: {(Best ? "Best" : "Fast")} ({(string.IsNullOrEmpty(q) ? "auto" : q)}; {SystemInfo.graphicsDeviceName}, {SystemInfo.graphicsMemorySize} MB)");
            ApplyPipeline();
            SceneManager.sceneLoaded += (s, m) => ApplyScene();
        }

        /// <summary>A separate graphics card: not Intel's built-in graphics, not AMD's built-in "Radeon Graphics",
        /// and at least 3 GB of its own memory.</summary>
        static bool HasGraphicsCard()
        {
            string n = SystemInfo.graphicsDeviceName ?? "";
            bool builtIn = n.Contains("Intel") || n.Contains("UHD") || n.Contains("Iris") || n.Trim() == "AMD Radeon(TM) Graphics"
                           || n.Contains("Microsoft Basic") || n.Contains("Radeon(TM) Vega");
            return !builtIn && SystemInfo.graphicsMemorySize >= 3000;
        }

        public static void Toggle() => Set(!Best);

        public static void Set(bool best)
        {
            if (best == Best) return;
            Best = best;
            SaveData.Current.quality = best ? "best" : "fast"; SaveData.Save();
            ApplyPipeline(); ApplyScene();
            Debug.Log("[Rihla] picture set to " + (Best ? "Best" : "Fast"));
            Changed?.Invoke();
        }

        /// <summary>
        /// Render scale, MSAA and shadows live on the pipeline asset. A copy is changed, never the asset itself,
        /// so that playing in the editor does not rewrite the project's settings.
        /// </summary>
        static void ApplyPipeline()
        {
            if (!copy)
            {
                var src = QualitySettings.renderPipeline as UniversalRenderPipelineAsset ?? GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
                if (!src) return;
                copy = UnityEngine.Object.Instantiate(src); copy.name = src.name + " (Rihla picture)";
                copy.hideFlags = HideFlags.DontSave;
                QualitySettings.renderPipeline = copy;
                // In the editor, put the project's own asset back when play stops: otherwise the quality level keeps
                // pointing at the copy, which is destroyed, and the next build fails to list its assets.
                if (Application.isEditor)
                    Application.quitting += () => { QualitySettings.renderPipeline = src; copy = null; };
            }
            copy.renderScale = Best ? 1f : 0.6f;
            copy.msaaSampleCount = Best ? 4 : 1;
            copy.shadowDistance = Best ? 120f : 40f;
            copy.shadowCascadeCount = Best ? 2 : 1;
            if (Test("-noshadows")) copy.shadowDistance = 0f;
            // Ambient shading (SSAO) is a feature of the renderer, shared by every quality copy, so it is switched on
            // or off there. Only in the game itself: in the editor that would rewrite the project's renderer.
            if (!Application.isEditor)
            {
                var f = typeof(UniversalRenderPipelineAsset).GetField("m_RendererDataList", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (f?.GetValue(copy) is ScriptableRendererData[] list)
                    foreach (var d in list) if (d) foreach (var feat in d.rendererFeatures)
                        if (feat is ScreenSpaceAmbientOcclusion) feat.SetActive(Best && !Test("-nossao"));
            }
        }

        /// <summary>Switches for measuring what costs the most (frame-rate tests only).</summary>
        static bool Test(string a) => Array.IndexOf(Environment.GetCommandLineArgs(), a) >= 0;

        public static float PalmDistance => Test("-cull0") ? 1f : Best ? 1200f : 300f;
        public static float SmallDistance => Test("-cull0") ? 1f : Best ? 600f : 200f;

        /// <summary>What belongs to the scene: cull distances, the terrain, and Ariha's palm groves.</summary>
        static void ApplyScene()
        {
            foreach (var c in UnityEngine.Object.FindObjectsByType<DetailCulling>(FindObjectsInactive.Include)) c.Apply();
            foreach (var t in Terrain.activeTerrains)
            {
                t.heightmapPixelError = Best ? 3f : 8f;
                t.basemapDistance = Test("-basemap0") ? 0f : Best ? 600f : 60f;
            }
            var groves = GameObject.Find("Ariha: palms and houses");
            if (groves)
            {
                int i = 0;
                foreach (Transform palm in groves.transform)
                    if (palm.name == "Date palm") palm.gameObject.SetActive(Best || i++ % 2 == 0);
            }
        }
    }
}

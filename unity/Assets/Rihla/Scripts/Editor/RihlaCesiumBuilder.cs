using CesiumForUnity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Rihla.EditorTools
{
    /// <summary>
    /// Builds Assets/Scenes/E0_AlQuds_Cesium.unity: a Cesium georeference on the Old City of Al-Quds with
    /// Google Photorealistic 3D Tiles and a circling camera (AlQudsFlyover). The tileset URL is set at run
    /// time from the git-ignored key file, so the scene file holds no key.
    /// </summary>
    public static class RihlaCesiumBuilder
    {
        public const string ScenePath = "Assets/Scenes/E0_AlQuds_Cesium.unity";
        // Old City of Al-Quds, about its centre (listed in NEEDS_REVIEW.md). Google's tiles use heights above
        // the WGS84 ellipsoid; the Old City lies roughly 740-780 m above it, so the origin sits near the ground.
        const double Latitude = 31.7767, Longitude = 35.2345, Height = 780.0;

        [MenuItem("Rihla/Build E0 Cesium scene (Al-Quds today)")]
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional; sun.intensity = 1.2f; sun.shadows = LightShadows.None;
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            RenderSettings.skybox = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Skybox.mat");
            RenderSettings.fog = false;

            var geoGo = new GameObject("CesiumGeoreference (Al-Quds Old City)");
            var geo = geoGo.AddComponent<CesiumGeoreference>();
            geo.SetOriginLongitudeLatitudeHeight(Longitude, Latitude, Height);

            var tilesGo = new GameObject("Google Photorealistic 3D Tiles");
            tilesGo.transform.SetParent(geoGo.transform, false);
            var tiles = tilesGo.AddComponent<Cesium3DTileset>();
            tiles.tilesetSource = CesiumDataSource.FromUrl;
            tiles.url = "";               // source and token are set at run time from git-ignored StreamingAssets files
            tiles.showCreditsOnScreen = true; // Google and Cesium ion require their credits on screen
            tiles.maximumScreenSpaceError = 24f; // coarser than the default 16, for integrated graphics
            tiles.updateInEditor = false;

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>(); cam.nearClipPlane = 1f; cam.farClipPlane = 50000f; cam.fieldOfView = 55f;
            camGo.AddComponent<AudioListener>();
            var fly = camGo.AddComponent<Rihla.AlQudsFlyover>(); fly.tileset = tiles;
            camGo.transform.position = new Vector3(0f, fly.height, -fly.radius); camGo.transform.LookAt(Vector3.zero);

            RihlaUIBuilder.AddContent();
            RihlaUIBuilder.AddUI();

            EditorSceneManager.SaveScene(scene, ScenePath);
            RihlaScenes.UpdateBuildSettings();
            Debug.Log("[Rihla] E0 Cesium scene built: " + ScenePath);
        }
    }
}

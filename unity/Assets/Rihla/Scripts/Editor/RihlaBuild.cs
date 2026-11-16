using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Rihla.EditorTools
{
    /// <summary>Windows build and screenshots. Batch mode: -executeMethod Rihla.EditorTools.RihlaBuild.Windows (or .Screenshots).</summary>
    public static class RihlaBuild
    {
        [MenuItem("Rihla/Build for Windows")]
        public static void Windows()
        {
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = File.Exists(RihlaCesiumBuilder.ScenePath)
                    ? new[] { RihlaSceneBuilder.ScenePath, RihlaCesiumBuilder.ScenePath }
                    : new[] { RihlaSceneBuilder.ScenePath },
                locationPathName = "../Builds/Windows/Rihla.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            });
            Debug.Log($"[Rihla] Windows build: {report.summary.result}, {report.summary.totalSize / 1e6:0.0} MB, {report.summary.totalTime}");
            if (Application.isBatchMode) EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
        }

        /// <summary>Renders views along the road to docs/screenshots/e0-*.png from the E0 scene's camera.</summary>
        [MenuItem("Rihla/Take E0 screenshots")]
        public static void Screenshots()
        {
            EditorSceneManager.OpenScene(RihlaSceneBuilder.ScenePath);
            var road = Object.FindAnyObjectByType<RoadPath>();
            var cam = Camera.main;
            var follow = cam.GetComponent<FollowCamera>(); if (follow) follow.enabled = false;
            Directory.CreateDirectory("../docs/screenshots");
            var rt = new RenderTexture(1600, 900, 24);
            var tex = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            float[] at = { 0f, 0.25f, 0.5f, 0.75f, 0.98f };
            foreach (float f in at)
            {
                int i = 0; while (i < road.points.Length - 2 && road.distances[i] < f * road.Length) i++;
                Vector3 p = road.points[i], d = (road.points[Mathf.Min(i + 5, road.points.Length - 1)] - p).normalized;
                cam.transform.position = p - d * 25f + Vector3.up * 12f;
                cam.transform.LookAt(p + d * 60f);
                Shot(cam, rt, tex, $"../docs/screenshots/e0-road-{Mathf.RoundToInt(f * 100):00}.png");
            }
            // An overview from high above Al-Quds looking east down to the Dead Sea.
            cam.transform.position = road.points[0] + new Vector3(-300f, 400f, -200f);
            cam.transform.LookAt(road.points[road.points.Length - 1]);
            Shot(cam, rt, tex, "../docs/screenshots/e0-overview.png");
            cam.targetTexture = null; Object.DestroyImmediate(rt);
            Debug.Log("[Rihla] screenshots written to docs/screenshots/e0-*.png");
        }

        static void Shot(Camera cam, RenderTexture rt, Texture2D tex, string path)
        {
            cam.targetTexture = rt; cam.Render();
            RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); tex.Apply();
            RenderTexture.active = null;
            File.WriteAllBytes(path, tex.EncodeToPNG());
        }
    }
}

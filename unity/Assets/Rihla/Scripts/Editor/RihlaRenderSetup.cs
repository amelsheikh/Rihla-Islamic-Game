using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Rihla.EditorTools
{
    /// <summary>Project-wide rendering settings that make the stone read well: screen-space ambient occlusion.</summary>
    public static class RihlaRenderSetup
    {
        const string RendererPath = "Assets/Settings/PC_Renderer.asset";

        /// <summary>Adds (or updates) SSAO on the Windows renderer, at half resolution to stay cheap on integrated graphics.</summary>
        [MenuItem("Rihla/Ambient occlusion (SSAO) on")]
        public static string AddSsao()
        {
            var data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            var f = data.rendererFeatures.OfType<ScreenSpaceAmbientOcclusion>().FirstOrDefault();
            string result = "updated";
            if (!f)
            {
                f = ScriptableObject.CreateInstance<ScreenSpaceAmbientOcclusion>(); f.name = "SSAO";
                AssetDatabase.AddObjectToAsset(f, data);
                data.rendererFeatures.Add(f);
                var so = new SerializedObject(data); var map = so.FindProperty("m_RendererFeatureMap");
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(f, out string _, out long id);
                map.arraySize++; map.GetArrayElementAtIndex(map.arraySize - 1).longValue = id;
                so.ApplyModifiedProperties();
                result = "added";
            }
            var fs = new SerializedObject(f);
            void Set(string name, System.Action<SerializedProperty> act) { var p = fs.FindProperty("m_Settings." + name); if (p != null) act(p); else result += " (no " + name + ")"; }
            Set("Intensity", p => p.floatValue = 1.4f);
            Set("Radius", p => p.floatValue = 0.4f);
            Set("DirectLightingStrength", p => p.floatValue = 0.3f);
            Set("Downsample", p => p.boolValue = true);
            Set("Falloff", p => p.floatValue = 60f);
            fs.ApplyModifiedProperties();
            f.SetActive(true);
            data.SetDirty(); EditorUtility.SetDirty(data); AssetDatabase.SaveAssets();
            return "SSAO " + result;
        }

        [MenuItem("Rihla/Ambient occlusion (SSAO) off")]
        public static string RemoveSsao()
        {
            var data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            var f = data.rendererFeatures.OfType<ScreenSpaceAmbientOcclusion>().FirstOrDefault();
            if (!f) return "none";
            f.SetActive(false); data.SetDirty(); EditorUtility.SetDirty(data); AssetDatabase.SaveAssets();
            return "SSAO off";
        }
    }
}

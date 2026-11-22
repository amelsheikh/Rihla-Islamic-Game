using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Rihla.EditorTools
{
    /// <summary>Adds the content library and the bilingual interface (RihlaUI) to a scene being built.</summary>
    public static class RihlaUIBuilder
    {
        static string UiDir => CoreConfig.UI;
        static string Content => CoreConfig.Content;

        public static ContentLibrary AddContent()
        {
            var content = new GameObject("Content").AddComponent<ContentLibrary>();
            content.places = Load(Content + "places.json");
            content.history = Load(Content + "history.json");
            content.journal = Load(Content + "journal.json");
            content.religiousTexts = Load(Content + "religious_texts.json");
            content.english = Load(Content + "i18n/en.json");
            content.arabic = Load(Content + "i18n/ar.json");
            content.walk = Load(Content + "walk.json");
            content.storybook = Load(Content + "storybook.json");
            content.drive = Load(Content + "drive.json");
            content.ariha = Load(Content + "ariha.json");
            content.nabiMusa = Load(Content + "nabimusa.json");
            return content;
        }

        public static RihlaUI AddUI()
        {
            var ui = new GameObject("UI").AddComponent<RihlaUI>();
            ui.panelSettings = Panel();
            ui.regular = AssetDatabase.LoadAssetAtPath<Font>(CoreConfig.Fonts + "Amiri-Regular.ttf");
            ui.bold = AssetDatabase.LoadAssetAtPath<Font>(CoreConfig.Fonts + "Amiri-Bold.ttf");
            return ui;
        }

        static TextAsset Load(string p) => AssetDatabase.LoadAssetAtPath<TextAsset>(p);

        /// <summary>Panel settings scaled from 1280 x 720, with Unity's default runtime theme.</summary>
        static PanelSettings Panel()
        {
            Directory.CreateDirectory(UiDir);
            string themePath = UiDir + "RihlaTheme.tss";
            if (!File.Exists(themePath))
            {
                File.WriteAllText(themePath, "@import url(\"unity-theme://default\");\n");
                AssetDatabase.ImportAsset(themePath);
            }
            string path = UiDir + "RihlaPanel.asset";
            var ps = AssetDatabase.LoadAssetAtPath<PanelSettings>(path);
            if (!ps) { ps = ScriptableObject.CreateInstance<PanelSettings>(); AssetDatabase.CreateAsset(ps, path); }
            ps.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(themePath);
            ps.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            ps.referenceResolution = new Vector2Int(1280, 720);
            ps.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight; ps.match = 0.5f;
            EditorUtility.SetDirty(ps);
            AssetDatabase.SaveAssets();
            return ps;
        }
    }
}

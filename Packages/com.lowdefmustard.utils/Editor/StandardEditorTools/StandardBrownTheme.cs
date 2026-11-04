using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace LowDefMustard.Utils.Editor
{
    public static class StandardBrownTheme
    {
        // Applies StandardBrownTheme.uss (brown palette + restyled built-in controls) to an editor window's root
        //  - Windows hold the stylesheet in a serialized field as their script's default reference

        // Classes
        public const string themeClass = "standard-brown-theme";
        public const string darkClass = "standard-brown-theme--dark";
        public const string canvasClass = "standard-brown-theme__canvas";
        public const string panelClass = "standard-brown-theme__panel";
        public const string panelHeaderClass = "standard-brown-theme__panel-header";
        public const string groupHeaderClass = "standard-brown-theme__group-header";
        public const string dimClass = "standard-brown-theme__dim";

        // Palette as colours, for code that can't read the USS variables
        //  - Note:  must manually keep aligned w/ corresponding --theme-* variables in StandardBrownTheme.uss
        public static readonly Color canvas = new Color32(0x22, 0x1C, 0x17, 0xFF);
        public static readonly Color cardBorder = new Color32(0x5E, 0x4B, 0x39, 0xFF);
        public static readonly Color accent = new Color32(0xC9, 0xA4, 0x7A, 0xFF);
        public static readonly Color text = new Color32(0xE2, 0xD6, 0xC6, 0xFF);
        public static readonly Color dimText = new Color32(0xA8, 0x97, 0x7F, 0xFF);
        public static readonly Color inputBorderHover = new Color32(0x8B, 0x6E, 0x4E, 0xFF);
        public static readonly Color darkAccent = new Color32(0xD2, 0xA3, 0x3A, 0xFF);
        public static readonly Color darkCardBorder = new Color32(0x8A, 0x6D, 0x2B, 0xFF);

        #region PublicMethods
        public static void Apply(VisualElement root, params StyleSheet[] styleSheets)
        {
            foreach (StyleSheet styleSheet in styleSheets)
            {
                if (styleSheet != null) { root.styleSheets.Add(styleSheet); }
            }
            root.AddToClassList(themeClass);
        }

        public static StyleSheet GetDefaultReference(ScriptableObject owner, string fieldName)
        {
            // For windows serialized (in the editor layout) before their default reference was set, or style holders that aren't windows
            var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(owner))) as MonoImporter;
            return importer != null ? importer.GetDefaultReference(fieldName) as StyleSheet : null;
        }
        #endregion
    }
}

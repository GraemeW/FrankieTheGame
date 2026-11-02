using UnityEditor;
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

        #region PublicMethods
        public static void Apply(VisualElement root, params StyleSheet[] styleSheets)
        {
            foreach (StyleSheet styleSheet in styleSheets)
            {
                if (styleSheet != null) { root.styleSheets.Add(styleSheet); }
            }
            root.AddToClassList(themeClass);
        }

        public static StyleSheet GetDefaultReference(EditorWindow window, string fieldName)
        {
            // For windows serialized (in the editor layout) before their default reference was set
            var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(window))) as MonoImporter;
            return importer != null ? importer.GetDefaultReference(fieldName) as StyleSheet : null;
        }
        #endregion
    }
}

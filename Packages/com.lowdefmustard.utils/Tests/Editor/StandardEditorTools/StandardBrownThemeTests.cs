using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using LowDefMustard.Utils.Editor;

namespace LowDefMustard.Utils.Tests.Editor
{
    public class StandardBrownThemeTests
    {
        // State
        private EditorWindow window;

        // Setup
        [TearDown]
        public void TearDown()
        {
            if (window != null) { window.Close(); }
        }

        // Private Methods
        private static StyleSheet LoadThemeStyleSheet()
        {
            string guid = AssetDatabase.FindAssets($"{nameof(StandardBrownTheme)} t:{nameof(StyleSheet)}").FirstOrDefault();
            return guid == null ? null : AssetDatabase.LoadAssetAtPath<StyleSheet>(AssetDatabase.GUIDToAssetPath(guid));
        }

        #region Tests
        [Test]
        public void ThemeStyleSheet_ShipsBesideTheScript()
        {
            Assert.IsNotNull(LoadThemeStyleSheet(), "StandardBrownTheme.uss should import as a StyleSheet");
        }

        [Test]
        public void Apply_AddsThemeClass()
        {
            var root = new VisualElement();

            StandardBrownTheme.Apply(root);

            Assert.IsTrue(root.ClassListContains(StandardBrownTheme.themeClass));
        }

        [Test]
        public void Apply_AddsGivenStyleSheets_SkippingNulls()
        {
            var root = new VisualElement();
            StyleSheet theme = LoadThemeStyleSheet();
            StyleSheet other = ScriptableObject.CreateInstance<StyleSheet>();

            StandardBrownTheme.Apply(root, theme, null, other);

            Assert.AreEqual(2, root.styleSheets.count);
            Assert.IsTrue(root.styleSheets.Contains(theme));
            Assert.IsTrue(root.styleSheets.Contains(other));
            Object.DestroyImmediate(other);
        }

        [Test]
        public void GetDefaultReference_WindowWithoutOne_ReturnsNull()
        {
            window = HeadlessEditorWindowTestHelper.CreateOffscreenWindow();

            Assert.IsNull(StandardBrownTheme.GetDefaultReference(window, "styleSheet"));
        }
        #endregion
    }
}

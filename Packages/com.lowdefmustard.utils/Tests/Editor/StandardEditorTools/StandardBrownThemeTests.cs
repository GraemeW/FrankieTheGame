using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using LowDefMustard.Utils.Editor;

namespace LowDefMustard.Utils.Tests.Editor
{
    public class StandardBrownThemeTests
    {
        // State
        private readonly TestObjectTracker tracker = new();
        private EditorWindow window;

        // Setup
        [TearDown]
        public void TearDown()
        {
            HeadlessEditorWindow.Close(window);
            tracker.DestroyAll();
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
            StyleSheet other = tracker.Create<StyleSheet>();

            StandardBrownTheme.Apply(root, theme, null, other);

            Assert.AreEqual(2, root.styleSheets.count);
            Assert.IsTrue(root.styleSheets.Contains(theme));
            Assert.IsTrue(root.styleSheets.Contains(other));
        }

        [UnityTest]
        public IEnumerator Placeholder_UsesThemePlaceholderColour()
        {
            window = HeadlessEditorWindow.CreateOffscreenWindow();
            StandardBrownTheme.Apply(window.rootVisualElement, LoadThemeStyleSheet());
            var textField = new TextField();
            textField.textEdition.placeholder = "placeholder";
            window.rootVisualElement.Add(textField);
            yield return HeadlessEditorWindow.WaitUntilSettled(window);
            yield return HeadlessEditorWindow.Settle();

            var textElement = textField.Q<TextElement>();
            Assert.AreEqual((Color)new Color32(0x7A, 0x61, 0x48, 0xFF), textElement.resolvedStyle.color);
        }

        [Test]
        public void GetDefaultReference_WindowWithoutOne_ReturnsNull()
        {
            window = HeadlessEditorWindow.CreateOffscreenWindow();

            Assert.IsNull(StandardBrownTheme.GetDefaultReference(window, "styleSheet"));
        }
        #endregion
    }
}

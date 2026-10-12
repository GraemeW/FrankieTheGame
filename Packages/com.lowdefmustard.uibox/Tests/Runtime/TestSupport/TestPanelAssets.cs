using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace LowDefMustard.UIBox.Tests
{
    public static class TestPanelAssets
    {
        // Note:  Single spot for the test asset paths ~ loaded through the AssetDatabase, so panel tests only run in the editor

        // Const Tunables
        private const string _folder = "Packages/com.lowdefmustard.uibox/Tests/Runtime/TestAssets/";

        // Two ChoiceEntryContainers (the second authored vertical) + a BackExitButton
        public static VisualTreeAsset menuView => Load<VisualTreeAsset>("TestMenuView.uxml");
        // A TextEntryContainer, a ChoiceEntryContainer + a BackExitButton
        public static VisualTreeAsset textScanView => Load<VisualTreeAsset>("TestTextScanView.uxml");
        // A TestHeaderLabel with authored text
        public static VisualTreeAsset headerView => Load<VisualTreeAsset>("TestHeaderView.uxml");
        // No containers at all
        public static VisualTreeAsset emptyView => Load<VisualTreeAsset>("TestEmptyView.uxml");
        // Fixed-size choice rows + row / column container layouts
        public static StyleSheet styleSheet => Load<StyleSheet>("TestPanel.uss");
        public static ThemeStyleSheet theme => Load<ThemeStyleSheet>("TestTheme.tss");

        private static T Load<T>(string fileName) where T : Object
        {
#if UNITY_EDITOR
            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(_folder + fileName);
            Assert.IsNotNull(asset, $"Missing test asset:  {_folder}{fileName}");
            return asset;
#else
            Assert.Ignore("Panel tests load their assets through the AssetDatabase (editor only).");
            return null;
#endif
        }
    }
}

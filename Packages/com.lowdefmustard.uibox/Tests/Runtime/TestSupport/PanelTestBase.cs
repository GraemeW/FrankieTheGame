using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace LowDefMustard.UIBox.Tests
{
    public abstract class PanelTestBase
    {
        // Test Notes:
        //  - a view only binds once its PanelRenderer has loaded its UI onto a panel
        //      - this happens in a frame's panel update, so tests must wait for it
        //  - bindings, styles and layout resolve in that same update
        //      - after a model / view change, Settle() before asserting on elements

        // Const Tunables
        private const int _maxLoadFrames = 10;
        private const int _settleFrames = 2;

        // State
        private readonly List<GameObject> spawned = new();
        private PanelSettings panelSettings;

        // Setup
        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in spawned.Where(go => go != null))
            {
                Object.Destroy(go);
            }
            spawned.Clear();

            if (panelSettings != null) { Object.Destroy(panelSettings); }
            panelSettings = null;
        }

        #region SpawnMethods
        protected TView CreateView<TView>(VisualTreeAsset visualTreeAsset, StyleSheet styleSheet = null, Transform parent = null) where TView : UIToolkitBoxView
        {
            PanelRenderer panelRenderer = CreatePanelRenderer(typeof(TView).Name, visualTreeAsset, parent);
            var view = panelRenderer.gameObject.AddComponent<TView>();
            if (styleSheet != null) { view.AddStyleSheet(styleSheet); }
            return view;
        }

        protected PanelRenderer CreatePanelRenderer(string name, VisualTreeAsset visualTreeAsset, Transform parent = null)
        {
            var go = new GameObject(name);
            spawned.Add(go);
            if (parent != null) { go.transform.SetParent(parent, false); }

            var panelRenderer = go.AddComponent<PanelRenderer>();
            panelRenderer.visualTreeAsset = visualTreeAsset;
            panelRenderer.panelSettings = GetPanelSettings();
            return panelRenderer;
        }
        #endregion

        #region PanelMethods
        protected static IEnumerator WaitForLoadedUI(Component panelComponent, VisualElement previousRoot = null) // previousRoot:  wait for a reload instead
        {
            for (int frame = 0; frame < _maxLoadFrames; frame++)
            {
                VisualElement root = FindLoadedRoot(panelComponent);
                if (root != null && root != previousRoot) { break; }
                yield return null;
            }
            yield return Settle();
        }

        protected static IEnumerator Settle()
        {
            for (int frame = 0; frame < _settleFrames; frame++) { yield return null; }
        }

        protected static VisualElement GetLoadedRoot(Component panelComponent)
        {
            VisualElement root = FindLoadedRoot(panelComponent);
            Assert.IsNotNull(root, $"{panelComponent.name}:  the PanelRenderer never loaded its UI onto a panel.");
            return root;
        }

        protected static void Click(Button button)
        {
            // Note:  Submit raises Button.clicked just as a pointer click does, without depending on pointer picking
            using NavigationSubmitEvent submitEvent = NavigationSubmitEvent.GetPooled();
            submitEvent.target = button;
            button.SendEvent(submitEvent);
        }

        protected static Label GetChoiceLabel(VisualElement choiceRow) => choiceRow.Q<Label>(className: USSClassNames.ChoiceEntry.label);
        #endregion

        #region PrivateMethods
        private PanelSettings GetPanelSettings()
        {
            if (panelSettings != null) { return panelSettings; }

            panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            panelSettings.themeStyleSheet = TestPanelAssets.theme;
            return panelSettings;
        }

        private static VisualElement FindLoadedRoot(Component panelComponent)
        {
            // Note:  The root is only handed out through the reload callback (invoked on registration when the UI is already loaded)
            VisualElement loadedRoot = null;

            var panelRenderer = panelComponent.GetComponent<PanelRenderer>();
            panelRenderer.RegisterUIReloadCallback(HandleUIReload);
            panelRenderer.UnregisterUIReloadCallback(HandleUIReload);
            return loadedRoot;

            // Local Functions
            void HandleUIReload(PanelRenderer reloadedPanelRenderer, VisualElement rootElement, int version) => loadedRoot = rootElement;
        }
        #endregion
    }
}

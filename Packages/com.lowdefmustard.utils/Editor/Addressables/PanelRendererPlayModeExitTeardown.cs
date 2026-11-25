using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace LowDefMustard.Utils.Editor
{
    [InitializeOnLoad]
    public static class PanelRendererPlayModeExitTeardown
    {
        // Why?
        //  - Specific use case for Addressables loading from bundles (i.e. `Play Mode Script` -> set to `Use Existing Build`)
        //  - Addressables unloads its bundles after Play mode
        //  - UI Toolkit PanelRenderers (on prefabs loaded from a bundle, or still alive in scenes) clean up mid-unload
        //      - , thus reading PanelSettings from the bundle being unloaded
        //      - , thus resulting in "Assertion failed ... m_PreventLoadingFromFile" + "UnloadAssetBundle failed"
        //  - so on exit:  bundle-loaded prefab renderers drop their PanelSettings, scene renderers deactivate while their bundle is loaded
        
        // On Finding:  goal is to find addressables objects currently in memory
        //  - NG - AssetDatabase would only get us project assets on disk (i.e. exactly what we don't want to touch)
        //  - NG - Object.FindObjectsByType would only get us scene objects (incl. instantiated copies), not the bundle-loaded prefab assets
        //  - NG - AssetBundle.GetAllLoadedAssetBundles + LoadAllAssets would load from the bundle file mid-unload (i.e. the assertion itself)
        //  - OK - Resources.FindObjectsOfTypeAll to capture every loaded PanelRenderer, then screen:  persistent + no asset path == bundle-loaded
        
        static PanelRendererPlayModeExitTeardown()
        {
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange playModeStateChange)
        {
            if (playModeStateChange != PlayModeStateChange.ExitingPlayMode) { return; }
            foreach (PanelRenderer panelRenderer in Resources.FindObjectsOfTypeAll<PanelRenderer>()) 
            {
                if (!EditorUtility.IsPersistent(panelRenderer)) { panelRenderer.enabled = false; } 
                else if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(panelRenderer))) { panelRenderer.panelSettings = null; }
            }
        }
    }
}

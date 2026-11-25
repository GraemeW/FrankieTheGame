using System;
using System.Collections;
using UnityEngine.AddressableAssets;
using UnityEngine.Localization.Settings;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using LowDefMustard.Core;
using LowDefMustard.Utils;

namespace LowDefMustard.Zones
{
    public static class AddressableSceneLoader
    {
        // Notes:
        //  - Scenes load as Addressables entries keyed by the scene asset's GUID ~ survives renames + moves
        //  - Single mode:  Addressables releases the previous scene's handle itself, so critical to never mix in SceneManager.LoadScene*

        public static IEnumerator LoadSceneAsync(string sceneGuid, Action<bool> onLoadComplete)
        {
            if (string.IsNullOrWhiteSpace(sceneGuid))
            {
                LDMLog.Warning(LDMLogCategory.Zones, "Cannot load a scene without a GUID. Ignoring the request.");
                onLoadComplete?.Invoke(false);
                yield break;
            }

            yield return LocalizationSettings.InitializationOperation; // Synchronous Addressables loads (WaitForCompletion, LocalizedString.GetLocalizedString on an unloaded table) deadlock inside a scene load from a bundle (so yield prior to scene load)
            
            // Loading flag cleared on sceneLoaded (after the new scene's Awake/OnEnable, before its Start), so Start-time cache reads may wait
            AddressablesSceneLoadState.isLoadingScene = true; 
            SceneManager.sceneLoaded += ClearLoadingFlag;
            
            AsyncOperationHandle<SceneInstance> handle = Addressables.LoadSceneAsync(sceneGuid, LoadSceneMode.Single);
            yield return handle;
            ClearLoadingFlag(default, default);
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                onLoadComplete?.Invoke(true);
                yield break;
            }

            LDMLog.Warning(LDMLogCategory.Zones, $"Failed to load scene '{sceneGuid}' through Addressables ({handle.OperationException?.Message}). Check it's an Addressables scene entry in a group with Include GUIDs in Catalogue.");
            Addressables.Release(handle);
            onLoadComplete?.Invoke(false);
        }

        private static void ClearLoadingFlag(Scene scene, LoadSceneMode loadSceneMode)
        {
            SceneManager.sceneLoaded -= ClearLoadingFlag;
            AddressablesSceneLoadState.isLoadingScene = false;
        }
    }
}

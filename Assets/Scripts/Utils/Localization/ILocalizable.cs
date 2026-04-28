using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization.Tables;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace Frankie.Utils
{
    public interface ILocalizable
    {
        // ---------------------CRITICAL NOTES ON CONFIGURATION---------------------
        // 1 - For Scriptable Objects, ILocalizable should be placed on the parent-most object
        //     LocalizationDeletionHandler.OnWillDeleteAsset() does not trigger for scriptable objects that are childed to other scriptable objects!
        //     The parent-most object must take gather localization entries from all children for GetLocalizationEntries()
        // 2 - For MonoBehaviours, the following must be manually configured:
        //     A. Add [ExecuteInEditMode] attribute to the class
        //     B. Include `ILocalizable.TriggerOnDestroy(this)` to the OnDestroy() method
        // Note that in the case of MonoBehaviours:
        //     - cleanup for prefabs/prefab variants is handled by LocalizationDeletionHandler.OnWillDeleteAsset()
        //     - cleanup for instanced objects in scenes is handled by OnDestroy()
        // ---------------------CRITICAL NOTES ON CONFIGURATION---------------------
        
        #region PublicMethodsProperties
        public LocalizationTableType localizationTableType { get; }
        public List<TableEntryReference> GetLocalizationEntries();
        public static event Action<LocalizationTableType, Object, ILocalizable> onBeforeDestroyedInEditor;

        public static void TriggerOnDestroy(ILocalizable localizable)
        {
#if UNITY_EDITOR
            if (localizable is not MonoBehaviour monoBehaviour) { return; }
            if (!localizable.IsStandardEditorState(monoBehaviour.gameObject)) { return; }
            onBeforeDestroyedInEditor?.Invoke(localizable.localizationTableType, monoBehaviour.gameObject, localizable);
#endif
        }
        #endregion
        
        #region PrivateMethods
#if UNITY_EDITOR
        private bool IsStandardEditorState(GameObject gameObject)
        {
            if (!Application.isEditor || Application.isPlaying) { return false; } // Avoid calls outside editor
            if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode) { return false; } // Avoid calls due to play mode start/stop
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) { return false; } // Avoid calls during Unity domain backup
            if (gameObject == null) { return false; } // Avoid calls due to mis-configuration
            if (EditorUtility.IsPersistent(gameObject)) { return false; } // Avoid calls due to prefab deletion
            if (PrefabStageUtility.GetCurrentPrefabStage() != null) { return false; } // Avoid calls while in prefab editor
            if (!gameObject.scene.isLoaded) { return false; } // Avoid calls due to scene changes
            
            return true;
        }
#endif
        #endregion
    }
}

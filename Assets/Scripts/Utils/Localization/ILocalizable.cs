using UnityEngine;
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
        //     The LocalizationDeletionHandler does not trigger for scriptable objects that are childed to other scriptable objects!
        // 2 - For MonoBehaviours, the following must be manually configured:
        //     A. Add [ExecuteInEditMode] attribute to the class
        //     B. Include `ILocalizable.TriggerOnDestroy(this)` to the OnDestroy() method
        // Note that in the case of MonoBehaviours, cleanup for prefabs is handled by LocalizationDeletionHandler, while cleanup for game objects in scenes is handled by OnDestroy() 
        //
        // HandleDeletion() must be implemented to clean up any localized entries in the associated StringTables 
        // ---------------------CRITICAL NOTES ON CONFIGURATION---------------------
        
        #region StandardPropertiesAndMethods
        public void HandleDeletion();

        public static void TriggerOnDestroy(ILocalizable localizable)
        {
#if UNITY_EDITOR
            if (localizable is not MonoBehaviour monoBehaviour) { return; }
            if (!localizable.IsStandardEditorState(monoBehaviour.gameObject)) { return; }
            localizable.HandleDeletion();
#endif
        }
        #endregion
        
        #region EditorExclusiveMethods
#if UNITY_EDITOR
        private bool IsStandardEditorState(GameObject gameObject)
        {
            if (!Application.isEditor) { return false; } // Avoid calls outside editor
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

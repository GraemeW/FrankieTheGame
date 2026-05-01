using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace Frankie.Utils
{
    public static class FrankieNonEditorEditorTools
    {
        public static bool IsStandardEditorState(GameObject gameObject)
        {
#if UNITY_EDITOR
            if (!Application.isEditor || Application.isPlaying) { return false; } // Avoid calls outside editor
            if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode) { return false; } // Avoid calls due to play mode start/stop
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) { return false; } // Avoid calls during Unity domain backup
            if (gameObject == null) { return false; } // Avoid calls due to mis-configuration
            if (!gameObject.scene.isLoaded) { return false; } // Avoid calls due to scene changes
            if (EditorUtility.IsPersistent(gameObject)) { return false; } // Avoid calls due to prefab deletion
            if (PrefabStageUtility.GetCurrentPrefabStage() != null) { return false; } // Avoid calls while in prefab editor
            if (EditorSceneManager.IsPreviewScene(gameObject.scene)) { return false; } // Avoid calls while in preview scenes
#endif
            return true;
        }
    }
}

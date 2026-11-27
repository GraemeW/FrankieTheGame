#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

namespace LowDefMustard.Utils.Tests
{
    public static class SceneTestUtility
    {
        #region Scenes
        // Lifecycle:
        //  - call ResetToEmptyScene before disposing the folder to ensure nothing scratch is open when it's deleted
        // CreateSavedScene:
        //  - w/ Additive: must have a saved active scene first ~ Unity refuses an additive NewScene while the active scene is untitled
        //  - populate event: order of operation critical ~ runs before the save / while the new scene is active to ensure populate objects saved in scene

        public static Scene CreateSavedScene(TempAssetFolder folder, string sceneName, NewSceneMode mode = NewSceneMode.Single, Action<Scene> populate = null)
        {
            return CreateSavedScene(folder.Path($"{sceneName}.unity"), mode, populate);
        }

        public static Scene CreateSavedScene(string scenePath, NewSceneMode mode = NewSceneMode.Single, Action<Scene> populate = null)
        {
            Scene previousActiveScene = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, mode);
            populate?.Invoke(scene);
            EditorSceneManager.SaveScene(scene, scenePath);
            
            if (mode == NewSceneMode.Additive && previousActiveScene.IsValid()) { SceneManager.SetActiveScene(previousActiveScene); } // Hand active scene back
            return scene;
        }
        
        public static void ResetToEmptyScene()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
        #endregion

        #region PreviewScenes
        public static Scene CreatePreviewScene() => EditorSceneManager.NewPreviewScene();
        public static void ClosePreviewScene(Scene previewScene) { if (previewScene.IsValid()) { EditorSceneManager.ClosePreviewScene(previewScene); } }
        #endregion

        #region SceneObjects
        // Note:
        //  - requires any valid, loaded scene (preview or regular) ~ new GameObjects always land in the active scene otherwise
        //  - objects in a saved scene are in memory only until that scene is saved again

        public static GameObject CreateGameObjectInScene(Scene scene, string objectName)
        {
            var gameObject = new GameObject(objectName);
            SceneManager.MoveGameObjectToScene(gameObject, scene);
            return gameObject;
        }

        public static T CreateComponentInScene<T>(Scene scene, string objectName) where T : Component
        {
            return CreateGameObjectInScene(scene, objectName).AddComponent<T>();
        }
        #endregion
    }
}
#endif

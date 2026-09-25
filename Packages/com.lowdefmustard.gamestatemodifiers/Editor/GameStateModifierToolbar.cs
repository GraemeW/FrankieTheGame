using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace LowDefMustard.GameStateModifiers.Editor
{
    public static class GameStateModifierToolbar
    {
        // Const Tunables
        private const string _assetsFolderRef = "Assets";
        
        #region PublicMethods
        [MenuItem("Tools/GameStateModifiers/CleanAllDanglingModifiers", false, 401)]
        public static void CleanAllDanglingModifiers()
        {
            List<GameStateModifier> gameStateModifiers = FindAllGameStateModifiers(_assetsFolderRef);
            int gameStateModifierCount = gameStateModifiers.Count;
            int removedHandlerCount = 0;

            try
            {
                for (int i = 0; i < gameStateModifierCount; i++)
                {
                    GameStateModifier gameStateModifier = gameStateModifiers[i];
                    if (gameStateModifier == null) { continue; }
                    
                    EditorUtility.DisplayProgressBar("GameStateModifiers:  Cleaning Dangling Modifiers", $"Progress: {i}/{gameStateModifierCount}", i / (float)gameStateModifierCount);
                    removedHandlerCount += gameStateModifier.CleanDanglingModifierHandlerData();
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("GameStateModifiers:  Cleaning Dangling Modifiers", $"Total Removed Handlers: {removedHandlerCount}", "OK");
            }
        }

        [MenuItem("Tools/GameStateModifiers/ForceSerializeAllHandlers", false, 402)]
        public static void SerializeAllHandlersAcrossAllScenes()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            List<string> scenePaths = FindAllScenePaths(_assetsFolderRef);
            int scenePathsCount = scenePaths.Count;

            try
            {
                for (int i = 0; i < scenePathsCount; i++)
                {
                    string scenePath = scenePaths[i];
                    if (string.IsNullOrEmpty(scenePath)) { continue; }

                    Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                    EditorUtility.DisplayProgressBar("GameStateModifiers:  Serialize All Handlers", $"Progress: {i}/{scenePathsCount}", i / (float)scenePathsCount);
                    
                    foreach (IGameStateModifierHandler gameStateModifierHandler in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include).OfType<IGameStateModifierHandler>())
                    {
                        string gameStateModifierHandlerName = gameStateModifierHandler.gameObject.transform.parent != null ? $"{gameStateModifierHandler.gameObject.transform.parent.name}/{gameStateModifierHandler.gameObject.name}" : $"{gameStateModifierHandler.gameObject.name}";
                        Debug.Log($"Force Serialize: {gameStateModifierHandlerName}");
                        gameStateModifierHandler.OnBeforeSerialize();
                    }

                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

        }
        #endregion
        
        #region HelperMethods

        private static List<string> FindAllScenePaths(string rootFolder)
        {
            if (!Directory.Exists(rootFolder)) { return new List<string>(); }
            
            string[] guids = AssetDatabase.FindAssets("t:SceneAsset", new[] { rootFolder });
            List<string> scenePaths = new();
            foreach (string guid in guids)
            {
                if (string.IsNullOrEmpty(guid)) { continue; }

                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path)) { continue; }
                
                scenePaths.Add(path);
            }

            return scenePaths;
        }
        
        private static List<GameStateModifier> FindAllGameStateModifiers(string rootFolder)
        {
            if (!Directory.Exists(rootFolder)) { return new List<GameStateModifier>(); }
            
            string[] guids = AssetDatabase.FindAssets("t:GameStateModifier", new[] { rootFolder });

            List<GameStateModifier> gameStateModifiers = new();
            foreach (string guid in guids)
            {
                if (string.IsNullOrEmpty(guid)) { continue; }
                
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path)) { continue; }
                
                var gameStateModifier = AssetDatabase.LoadAssetAtPath<GameStateModifier>(path);
                if (gameStateModifier != null) { gameStateModifiers.Add(gameStateModifier); }
            }

            return gameStateModifiers;
        }
        #endregion
    }
}

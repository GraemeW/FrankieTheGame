using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using LowDefMustard.Core;
using LowDefMustard.Utils;

namespace LowDefMustard.GameStateModifiers.Editor
{
    public static class GameStateModifierToolbar
    {
        // Methodology:
        //  - Bulk passes visit each scene once && build a look-up as they go
        //  - Modifiers are then cleaned once each against that look-up (i.e. rather than re-opening a scene per entry per handler, which was really slow/painful)

        // Const Tunables
        private const string _assetsFolderRef = "Assets";
        private const string _cleanTitle = "GameStateModifiers:  Cleaning Dangling Modifiers";
        private const string _serializeTitle = "GameStateModifiers:  Serialize All Handlers";
        
        #region PublicMethods
        [MenuItem("Tools/GameStateModifiers/CleanAllDanglingModifiers", false, 25)]
        public static void CleanAllDanglingModifiers()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) { return; }

            int removedHandlerCount = 0;
            bool isCancelled = false;
            try
            {
                List<string> scenePaths = FindAllScenePaths(_assetsFolderRef);
                var handlerLookup = new GameStateModifierHandlerLookup();
                for (int i = 0; i < scenePaths.Count && !isCancelled; i++)
                {
                    string sceneName = Path.GetFileNameWithoutExtension(scenePaths[i]);
                    isCancelled = EditorUtility.DisplayCancelableProgressBar(_cleanTitle, $"Indexing scene {i + 1}/{scenePaths.Count}:  {sceneName}", i / (float)scenePaths.Count * 0.5f);
                    if (!isCancelled) { IndexSceneAdditively(scenePaths[i], handlerLookup); }
                }

                if (!isCancelled) { removedHandlerCount = CleanAllModifiers(handlerLookup, true, _cleanTitle, 0.5f, out isCancelled); }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
            EditorUtility.DisplayDialog(_cleanTitle, isCancelled ? $"Cancelled.  Removed Handlers before cancelling: {removedHandlerCount}" : $"Total Removed Handlers: {removedHandlerCount}", "OK");
        }

        [MenuItem("Tools/GameStateModifiers/ForceSerializeAllHandlers", false, 26)]
        public static void SerializeAllHandlersAcrossAllScenes()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) { return; }

            List<string> scenePaths = FindAllScenePaths(_assetsFolderRef);
            int scenePathsCount = scenePaths.Count;
            int reconciledHandlerCount = 0;
            int removedHandlerCount = 0;
            bool isCancelled = false;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                var handlerLookup = new GameStateModifierHandlerLookup();
                // ReSharper disable once ConditionIsAlwaysTrueOrFalse ~ belt + suspenders (anyway)
                for (int i = 0; i < scenePathsCount && !isCancelled; i++)
                {
                    string scenePath = scenePaths[i];
                    if (string.IsNullOrEmpty(scenePath)) { continue; }

                    string sceneName = Path.GetFileNameWithoutExtension(scenePath);
                    isCancelled = EditorUtility.DisplayCancelableProgressBar(_serializeTitle, $"Scene {i + 1}/{scenePathsCount}:  opening {sceneName}", GetSceneProgress(i, 0, 1, scenePathsCount));
                    if (isCancelled) { break; }
                    
                    Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                    handlerLookup.AddZone(scene.name);
                    List<IGameStateModifierHandler> handlers = FindHandlersInScene(scene);
                    int sceneReconciledCount = 0;
                    for (int j = 0; j < handlers.Count; j++)
                    {
                        IGameStateModifierHandler handler = handlers[j];
                        if (j % 10 == 0) { EditorUtility.DisplayProgressBar(_serializeTitle, $"Scene {i + 1}/{scenePathsCount}:  {sceneName} ~ handler {j + 1}/{handlers.Count} ({handler.GetType().Name})", GetSceneProgress(i, j, handlers.Count, scenePathsCount)); }
                        if (!EditorStateCheck.IsStandardEditorState(handler.gameObject)) { continue; }

                        if (handler.ReconcileGameStateModifiers(false))
                        {
                            IGameStateModifierHandler.RecordHandlerModifications(handler); // OnBeforeSerialize's own deferred record would land after the scene is closed
                            sceneReconciledCount++;
                        }
                        handlerLookup.AddHandler(scene.name, handler.handlerGUID, handler.GetGameStateModifiers().Where(modifier => modifier != null).Select(modifier => modifier.GetGUID()));
                    }
                    reconciledHandlerCount += sceneReconciledCount;
                    LDMLog.Info(LDMLogCategory.World, $"Force-serialized scene '{sceneName}':  {sceneReconciledCount}/{handlers.Count} handlers re-linked.");

                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                }

                // Cleanup deferred to here (once per modifier, against the full lookup) -> skipped on cancel, since a partial lookup would fall back to slow per-entry checks
                if (!isCancelled) { removedHandlerCount = CleanAllModifiers(handlerLookup, false, _serializeTitle, 0.9f, out isCancelled); }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            string summary = $"{reconciledHandlerCount} handlers re-linked, {removedHandlerCount} dangling entries removed ({stopwatch.Elapsed.TotalSeconds:F1} s).";
            LDMLog.Info(LDMLogCategory.World, isCancelled ? $"Force-serialize cancelled:  {summary}" : $"Force-serialize complete:  {summary}");
        }
        #endregion
        
        #region HelperMethods
        private static float GetSceneProgress(int sceneIndex, int handlerIndex, int handlerCount, int sceneCount)
        {
            // Scenes span the first 90%, modifier cleanup the rest
            float sceneFraction = handlerCount > 0 ? handlerIndex / (float)handlerCount : 0f;
            return (sceneIndex + sceneFraction) / Mathf.Max(1, sceneCount) * 0.9f;
        }
        
        private static void IndexSceneAdditively(string scenePath, GameStateModifierHandlerLookup handlerLookup)
        {
            Scene scene = SceneManager.GetSceneByPath(scenePath);
            bool isSceneAlreadyLoaded = scene.isLoaded;
            if (!isSceneAlreadyLoaded) { scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive); }
            try
            {
                handlerLookup.AddZone(scene.name);
                foreach (IGameStateModifierHandler handler in FindHandlersInScene(scene))
                {
                    handlerLookup.AddHandler(scene.name, handler.handlerGUID, handler.GetGameStateModifiers().Where(modifier => modifier != null).Select(modifier => modifier.GetGUID()));
                }
            }
            finally
            {
                if (!isSceneAlreadyLoaded) { EditorSceneManager.CloseScene(scene, true); }
            }
        }

        private static int CleanAllModifiers(GameStateModifierHandlerLookup handlerLookup, bool removeOnSceneCheck, string title, float progressStart, out bool isCancelled)
        {
            isCancelled = false;
            List<GameStateModifier> gameStateModifiers = FindAllGameStateModifiers(_assetsFolderRef);
            int removedHandlerCount = 0;
            for (int i = 0; i < gameStateModifiers.Count; i++)
            {
                GameStateModifier gameStateModifier = gameStateModifiers[i];
                if (gameStateModifier == null) { continue; }

                float progress = progressStart + (1f - progressStart) * i / Mathf.Max(1, gameStateModifiers.Count);
                isCancelled = EditorUtility.DisplayCancelableProgressBar(title, $"Cleaning modifier {i + 1}/{gameStateModifiers.Count}:  {gameStateModifier.name} ({gameStateModifier.linkedHandlers.Count} entries)", progress);
                if (isCancelled) { break; }
                removedHandlerCount += gameStateModifier.CleanDanglingModifierHandlerData(removeOnSceneCheck, handlerLookup);
            }
            return removedHandlerCount;
        }

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
        
        private static List<IGameStateModifierHandler> FindHandlersInScene(Scene scene)
        {
            return Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include)
                .Where(monoBehaviour => monoBehaviour.gameObject.scene == scene)
                .OfType<IGameStateModifierHandler>()
                .ToList();
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

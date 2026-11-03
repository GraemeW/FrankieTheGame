using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using LowDefMustard.Core;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace LowDefMustard.GameStateModifiers
{
    public abstract class GameStateModifier : ScriptableObject, ISerializationCallbackReceiver
    {
        // Standard Properties
        [Tooltip("Auto-generated GUID. Clear to generate a new one.")] [SerializeField] protected string guid = string.Empty;
        public string GetGUID() => guid;
        public List<ZoneToGameObjectLinkData> gameStateModifierHandlerData = new(); // Custom view in GameStateModifierEditor
        
        // Handles
        public delegate bool HasScenePathDelegate(string input, out string scenePath);
        [NoAutoStaticsCleanup] public static HasScenePathDelegate scenePathProvider;
        
        // Static State
#if UNITY_EDITOR
        [NoAutoStaticsCleanup] private static Dictionary<string, GameStateModifier> _gameStateModifierCache;
#endif
        
        #region InterfaceMethods
        public virtual void OnBeforeSerialize()
        {
#if UNITY_EDITOR
            if (string.IsNullOrWhiteSpace(guid))
            {
                guid = System.Guid.NewGuid().ToString("D", CultureInfo.InvariantCulture);
            }
#endif
        }

        public virtual void OnAfterDeserialize()
        {
            // Unused, required for interface
        }
        #endregion
        
        
#if UNITY_EDITOR
        #region EditorStaticMethods
        public static GameStateModifier GetGameStateModifier(string guid)
        {
            bool wasCacheBuiltThisRequest = false;
            if (_gameStateModifierCache == null)
            {
                wasCacheBuiltThisRequest = true;
                _gameStateModifierCache = BuildCache();
            }
            
            if (_gameStateModifierCache.TryGetValue(guid, out GameStateModifier gameStateModifier)) { return gameStateModifier; }
            if (wasCacheBuiltThisRequest) { return null;}
            
            // Cache may be stale, so re-build
            BuildCache();
            return _gameStateModifierCache.TryGetValue(guid, out gameStateModifier) ? gameStateModifier : null;
        }
        
        private static Dictionary<string, GameStateModifier> BuildCache()
        {
            var newGameStateModifierCache = new Dictionary<string, GameStateModifier>();
            string[] assetGuids = AssetDatabase.FindAssets("t:" + nameof(GameStateModifier));
            if (assetGuids == null || assetGuids.Length == 0) { return newGameStateModifierCache; }

            LDMLog.Info(LDMLogCategory.World, $"Building the game state modifier cache from {assetGuids.Length} assets.");
            foreach (string assetGUID in assetGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(assetGUID);
                GameStateModifier gameStateModifier = AssetDatabase.LoadAssetAtPath<GameStateModifier>(path);
                if (gameStateModifier == null) { continue; }

                string gameStateModifierGUID = gameStateModifier.GetGUID();
                if (!newGameStateModifierCache.TryAdd(gameStateModifierGUID, gameStateModifier))
                {
                    LDMLog.Error(LDMLogCategory.World, $"Duplicate game state modifier GUID '{gameStateModifierGUID}' on '{gameStateModifier.name}'. Only the first asset with this GUID is reachable.", gameStateModifier);
                }
            }
            return newGameStateModifierCache;
        }
        
        public static string GetGameStateModifierHandlerDataRef() => nameof(gameStateModifierHandlerData);
        
        private static bool DoesGameStateModifierHandlerExist(string scenePath, string handlerGUID, out IGameStateModifierHandler gameStateModifierHandler)
        {
            gameStateModifierHandler = null;
            if (string.IsNullOrEmpty(scenePath)) { return false; }
            
            Scene checkScene = SceneManager.GetSceneByPath(scenePath);
            bool isSceneAlreadyLoaded = checkScene.isLoaded;
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                // Open additively -- avoid save prompt / disturbing other open scenes

            bool objectFound;
            try
            {
                gameStateModifierHandler = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include).OfType<IGameStateModifierHandler>().FirstOrDefault(gameStateModifierHandler => gameStateModifierHandler.handlerGUID == handlerGUID); 
                objectFound = gameStateModifierHandler != null;
            }
            finally
            {
                if (!isSceneAlreadyLoaded) { EditorSceneManager.CloseScene(scene, true); }
            }
            return objectFound;
        }
        #endregion
        
        #region EditorPublicMethods
        public void AddOrUpdateGameStateModifierHandler(ZoneToGameObjectLinkData zoneToGameObjectLinkData)
        {
            for (int i = 0; i < gameStateModifierHandlerData.Count; i++)
            {
                if (!gameStateModifierHandlerData[i].Equals(zoneToGameObjectLinkData)) { continue; }

                ZoneToGameObjectLinkData existingLinkData = gameStateModifierHandlerData[i];
                existingLinkData.UpdateRecord(zoneToGameObjectLinkData.zoneName, zoneToGameObjectLinkData.gameObjectName, zoneToGameObjectLinkData.parentObjectName);
                gameStateModifierHandlerData[i] = existingLinkData; // struct copy, write back explicitly
                EditorUtility.SetDirty(this);
                return;
            }

            string parentStem = zoneToGameObjectLinkData.GetParentLabelStem();
            LDMLog.Info(LDMLogCategory.World, $"Adding handler '{zoneToGameObjectLinkData.zoneName ?? ""}/{parentStem}{zoneToGameObjectLinkData.gameObjectName ?? ""}' to game state modifier '{name}'.", this);
            gameStateModifierHandlerData.Add(zoneToGameObjectLinkData);
            EditorUtility.SetDirty(this);
        }

        public void RemoveGameStateModifierHandler(string handlerGUID)
        {
            LDMLog.Info(LDMLogCategory.World, $"Removing handler '{handlerGUID}' from game state modifier '{name}' (its GameObject was deleted).", this);
            gameStateModifierHandlerData.RemoveAll(match => match.guid == handlerGUID);
            EditorUtility.SetDirty(this);
        }

        public int CleanDanglingModifierHandlerData(bool removeOnSceneCheck = true)
        {
            int removedCount = 0;
            removedCount += RemoveNonExistentEntries(removeOnSceneCheck);
            removedCount += RemoveDuplicateEntries();
            EditorUtility.SetDirty(this);
            return removedCount;
        }
        #endregion

        #region EditorPrivateMethods
        private int RemoveNonExistentEntries(bool removeOnSceneCheck = true)
        {
            int removedCount = 0;
            for (int i = gameStateModifierHandlerData.Count - 1; i >= 0; i--)
            {
                ZoneToGameObjectLinkData handlerLinkData = gameStateModifierHandlerData[i];
                string zoneName = handlerLinkData.zoneName;
                string parentStem = handlerLinkData.GetParentLabelStem();
                string handlerName = handlerLinkData.gameObjectName;
                string handlerGUID = handlerLinkData.guid;

                if (string.IsNullOrEmpty(handlerGUID))
                {
                    LDMLog.Info(LDMLogCategory.World, $"Removing handler entry '{zoneName ?? ""}/{parentStem}{handlerName ?? ""}' from game state modifier '{name}' (empty GUID).", this);
                }
                else
                {
                    bool sceneFound = scenePathProvider != null ? scenePathProvider.Invoke(zoneName, out string scenePath) : DefaultGetScenePath(zoneName, out scenePath);
                    IGameStateModifierHandler gameStateModifierHandler = null;
                    bool objectFound = sceneFound && !string.IsNullOrWhiteSpace(handlerName) && DoesGameStateModifierHandlerExist(scenePath, handlerGUID, out gameStateModifierHandler);
                    bool isModifierLinked = objectFound && gameStateModifierHandler != null && gameStateModifierHandler.GetGameStateModifiers().Any(checkModifier => checkModifier.guid == guid);

                    // Found -- Skip Removal
                    if (isModifierLinked) { continue; }
                    if (!sceneFound && !removeOnSceneCheck) { continue; }
                    
                    string reason = !sceneFound ? $"zone '{zoneName ?? ""}' not found" : !objectFound ? $"object '{parentStem}{handlerName ?? ""}' not found" : $"not linked to handler '{parentStem}{handlerName}'";
                    LDMLog.Info(LDMLogCategory.World, $"Removing handler entry '{zoneName ?? ""}/{parentStem}{handlerName ?? ""}' from game state modifier '{name}' ({reason}).", this);
                }

                gameStateModifierHandlerData.RemoveAt(i);

                removedCount++;
            }
            return removedCount;
        }

        public static bool DefaultGetScenePath(string zoneName, out string scenePath)
        {
            scenePath = string.Empty;
            if (string.IsNullOrEmpty(zoneName)) { return false; }
            string[] guids = AssetDatabase.FindAssets($"{zoneName} t:Scene");
            
            if (guids.Length == 0) { return false; }
            scenePath = AssetDatabase.GUIDToAssetPath(guids[0]);
            return !string.IsNullOrWhiteSpace(scenePath);
        }
        
        // Note:  Internal for test visibility
        internal int RemoveDuplicateEntries()
        {
            int initialCount = gameStateModifierHandlerData.Count;
            gameStateModifierHandlerData = gameStateModifierHandlerData.Distinct().ToList();
            int removedCount = initialCount - gameStateModifierHandlerData.Count;
            
            if (removedCount > 0) { LDMLog.Warning(LDMLogCategory.World, $"Removed {removedCount} duplicate handler entries from game state modifier '{name}'.", this); }
            return removedCount;
        }
        #endregion
#endif
    }
}

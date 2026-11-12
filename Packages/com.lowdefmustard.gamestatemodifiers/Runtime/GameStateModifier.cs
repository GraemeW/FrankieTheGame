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
        public List<LinkedHandlerRecord> linkedHandlers = new(); // Custom view in GameStateModifierEditor
        
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
                ResetGameStateModifierCache(); // in-memory GUID change ~ no asset import fires until the asset is saved
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
        public static string GetLinkedHandlersRef() => nameof(linkedHandlers);
        public static void ResetGameStateModifierCache() => _gameStateModifierCache = null;
        public static GameStateModifier GetGameStateModifier(string guid)
        {
            // Misses are genuine -> asset changes reset the cache instead (GameStateModifierCacheInvalidator, plus new GUIDs in OnBeforeSerialize)
            if (string.IsNullOrWhiteSpace(guid)) { return null; }
            _gameStateModifierCache ??= BuildCache();
            
            if (!_gameStateModifierCache.TryGetValue(guid, out GameStateModifier gameStateModifier)) { return null; }
            if (gameStateModifier != null) { return gameStateModifier; }
            
            // Cached entry destroyed in memory (e.g. asset unloaded) -> rebuild once
            _gameStateModifierCache = BuildCache();
            return _gameStateModifierCache.GetValueOrDefault(guid);
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
        public void AddOrUpdateGameStateModifierHandler(LinkedHandlerRecord linkedHandlerRecord)
        {
            for (int i = 0; i < linkedHandlers.Count; i++)
            {
                if (!linkedHandlers[i].Equals(linkedHandlerRecord)) { continue; }

                LinkedHandlerRecord existingRecord = linkedHandlers[i];
                existingRecord.UpdateRecord(linkedHandlerRecord.zoneName, linkedHandlerRecord.gameObjectName, linkedHandlerRecord.parentObjectName, linkedHandlerRecord.handlerTypeName);
                linkedHandlers[i] = existingRecord; // struct copy, write back explicitly
                EditorUtility.SetDirty(this);
                return;
            }

            string parentStem = linkedHandlerRecord.GetParentLabelStem();
            LDMLog.Info(LDMLogCategory.World, $"Adding handler '{linkedHandlerRecord.zoneName ?? ""}/{parentStem}{linkedHandlerRecord.gameObjectName ?? ""}' to game state modifier '{name}'.", this);
            linkedHandlers.Add(linkedHandlerRecord);
            EditorUtility.SetDirty(this);
        }

        public void RemoveGameStateModifierHandler(string handlerGUID)
        {
            LDMLog.Info(LDMLogCategory.World, $"Removing handler '{handlerGUID}' from game state modifier '{name}' (its GameObject was deleted).", this);
            linkedHandlers.RemoveAll(match => match.handlerGUID == handlerGUID);
            EditorUtility.SetDirty(this);
        }

        public int CleanDanglingModifierHandlerData(bool removeOnSceneCheck = true, GameStateModifierHandlerLookup handlerLookup = null)
        {
            // handlerLookup:  optional snapshot from a bulk pass ~ entries in its zones are checked against it instead of opening their scene
            int removedCount = 0;
            removedCount += RemoveNonExistentEntries(removeOnSceneCheck, handlerLookup);
            removedCount += RemoveDuplicateEntries();
            EditorUtility.SetDirty(this);
            return removedCount;
        }
        #endregion

        #region EditorPrivateMethods
        private int RemoveNonExistentEntries(bool removeOnSceneCheck, GameStateModifierHandlerLookup handlerLookup)
        {
            int removedCount = 0;
            for (int i = linkedHandlers.Count - 1; i >= 0; i--)
            {
                LinkedHandlerRecord linkedHandler = linkedHandlers[i];
                HandlerEntryStatus entryStatus = GetEntryStatus(linkedHandler, handlerLookup);
                if (!ShouldRemoveEntry(entryStatus, removeOnSceneCheck)) { continue; }

                LogEntryRemoval(linkedHandler, entryStatus);
                linkedHandlers.RemoveAt(i);
                removedCount++;
            }
            return removedCount;
        }
        
        // Note:  Internal for test visibility
        internal int RemoveDuplicateEntries()
        {
            int initialCount = linkedHandlers.Count;
            linkedHandlers = linkedHandlers.Distinct().ToList();
            int removedCount = initialCount - linkedHandlers.Count;
            
            if (removedCount > 0) { LDMLog.Warning(LDMLogCategory.World, $"Removed {removedCount} duplicate handler entries from game state modifier '{name}'.", this); }
            return removedCount;
        }
        
        private static bool TryGetScenePath(string zoneName, out string scenePath) => scenePathProvider != null ? scenePathProvider.Invoke(zoneName, out scenePath) : DefaultGetScenePath(zoneName, out scenePath);

        public static bool DefaultGetScenePath(string zoneName, out string scenePath)
        {
            scenePath = string.Empty;
            if (string.IsNullOrEmpty(zoneName)) { return false; }
            string[] guids = AssetDatabase.FindAssets($"{zoneName} t:Scene");
            
            if (guids.Length == 0) { return false; }
            scenePath = AssetDatabase.GUIDToAssetPath(guids[0]);
            return !string.IsNullOrWhiteSpace(scenePath);
        }

        private HandlerEntryStatus GetEntryStatus(LinkedHandlerRecord linkedHandler, GameStateModifierHandlerLookup handlerLookup)
        {
            if (string.IsNullOrEmpty(linkedHandler.handlerGUID)) { return HandlerEntryStatus.EmptyGUID; }
            
            // Lookup only trusted for the zones it indexed -> anything else checked against its scene
            bool isZoneIndexed = handlerLookup != null && handlerLookup.HasZone(linkedHandler.zoneName);
            return isZoneIndexed ? GetEntryStatusFromLookup(linkedHandler, handlerLookup) : GetEntryStatusFromScene(linkedHandler);
        }

        private HandlerEntryStatus GetEntryStatusFromLookup(LinkedHandlerRecord linkedHandler, GameStateModifierHandlerLookup handlerLookup)
        {
            if (string.IsNullOrWhiteSpace(linkedHandler.gameObjectName) || !handlerLookup.HasHandler(linkedHandler.zoneName, linkedHandler.handlerGUID)) { return HandlerEntryStatus.ObjectNotFound; }
            return handlerLookup.IsLinked(linkedHandler.zoneName, linkedHandler.handlerGUID, guid) ? HandlerEntryStatus.Linked : HandlerEntryStatus.NotLinked;
        }

        private HandlerEntryStatus GetEntryStatusFromScene(LinkedHandlerRecord linkedHandler)
        {
            if (!TryGetScenePath(linkedHandler.zoneName, out string scenePath)) { return HandlerEntryStatus.ZoneNotFound; }
            if (string.IsNullOrWhiteSpace(linkedHandler.gameObjectName)) { return HandlerEntryStatus.ObjectNotFound; }
            if (!DoesGameStateModifierHandlerExist(scenePath, linkedHandler.handlerGUID, out IGameStateModifierHandler gameStateModifierHandler) || gameStateModifierHandler == null) { return HandlerEntryStatus.ObjectNotFound; }

            bool isLinked = gameStateModifierHandler.GetGameStateModifiers().Any(checkModifier => checkModifier != null && checkModifier.guid == guid);
            return isLinked ? HandlerEntryStatus.Linked : HandlerEntryStatus.NotLinked;
        }

        private static bool ShouldRemoveEntry(HandlerEntryStatus entryStatus, bool removeOnSceneCheck)
        {
            return entryStatus switch
            {
                HandlerEntryStatus.Linked => false,
                HandlerEntryStatus.ZoneNotFound => removeOnSceneCheck, // scene lookups can fail transiently (serialization quirks) -> optionally kept
                _ => true
            };
        }

        private void LogEntryRemoval(LinkedHandlerRecord linkedHandler, HandlerEntryStatus entryStatus)
        {
            string zoneName = linkedHandler.zoneName ?? "";
            string handlerLabel = $"{linkedHandler.GetParentLabelStem()}{linkedHandler.gameObjectName ?? ""}";
            string reason = entryStatus switch
            {
                HandlerEntryStatus.EmptyGUID => "empty GUID",
                HandlerEntryStatus.ZoneNotFound => $"zone '{zoneName}' not found",
                HandlerEntryStatus.ObjectNotFound => $"object '{handlerLabel}' not found",
                _ => $"not linked to handler '{handlerLabel}'"
            };
            LDMLog.Info(LDMLogCategory.World, $"Removing handler entry '{zoneName}/{handlerLabel}' from game state modifier '{name}' ({reason}).", this);
        }
        #endregion
#endif
    }
}

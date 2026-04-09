using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using Frankie.ZoneManagement;

namespace Frankie.Core.GameStateModifiers
{
    public abstract class GameStateModifier : ScriptableObject, ISerializationCallbackReceiver
    {
        #region StandardPropertiesMethods
        [Tooltip("Auto-generated UUID for saving/loading. Clear this field if you want to generate a new one.")]
        [SerializeField] protected string guid;
        
        public List<ZoneToGameObjectLinkData> gameStateModifierHandlerData = new(); // Custom view in GameStateModifierEditor

        public string GetGUID() => guid;
        #endregion
        
        #region InterfaceMethods
        public virtual void OnBeforeSerialize()
        {
#if UNITY_EDITOR
            if (string.IsNullOrWhiteSpace(guid))
            {
                guid = System.Guid.NewGuid().ToString();
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
        public static string GetGameStateModifierHandlerDataRef() => nameof(gameStateModifierHandlerData);
        
        private static bool DoesGameStateModifierHandlerExist(string scenePath, string handlerGUID)
        {
            if (string.IsNullOrEmpty(scenePath)) { return false; }
            
            Scene checkScene = SceneManager.GetSceneByPath(scenePath);
            bool isSceneAlreadyLoaded = checkScene.isLoaded;
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                // Open additively -- avoid save prompt / disturbing other open scenes

            bool objectFound;
            try
            {
                objectFound = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include).OfType<IGameStateModifierHandler>().Any(gameStateModifierHandler => gameStateModifierHandler.handlerGUID == handlerGUID);
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
            foreach (ZoneToGameObjectLinkData checkLinkData in gameStateModifierHandlerData.Where(checkLinkData => checkLinkData.guid == zoneToGameObjectLinkData.guid))
            {
                checkLinkData.UpdateRecord(zoneToGameObjectLinkData.zoneName, zoneToGameObjectLinkData.gameObjectName);
                Debug.Log($"GameStateModifier {name} :: Updating GameStateModifierHandler - {zoneToGameObjectLinkData.zoneName}/{zoneToGameObjectLinkData.gameObjectName}.");
                return;
            }
            Debug.Log($"GameStateModifier {name} :: Adding new GameStateModifierHandler - {zoneToGameObjectLinkData.zoneName}/{zoneToGameObjectLinkData.gameObjectName}.");
            gameStateModifierHandlerData.Add(zoneToGameObjectLinkData);
            EditorUtility.SetDirty(this);
        }

        public void RemoveGameStateModifierHandler(string handlerGUID)
        {
            Debug.Log($"GameStateModifier {name} :: Removing GameStateHandler with GUID {handlerGUID} due to Object Deletion.");
            gameStateModifierHandlerData.RemoveAll(match => match.guid == handlerGUID);
            EditorUtility.SetDirty(this);
        }

        public int CleanDanglingModifierHandlerData()
        {
            int removedCount = 0;
            removedCount += RemoveNonExistentEntries();
            removedCount += RemoveDuplicateEntries();
            EditorUtility.SetDirty(this);
            return removedCount;
        }
        #endregion

        #region EditorPrivateMethods
        private int RemoveNonExistentEntries()
        {
            int removedCount = 0;
            Zone.BuildCacheIfEmpty();
            for (int i = gameStateModifierHandlerData.Count - 1; i >= 0; i--)
            {
                ZoneToGameObjectLinkData handlerLinkData = gameStateModifierHandlerData[i];
                string zoneName = handlerLinkData.zoneName;
                string handlerName = handlerLinkData.gameObjectName;
                string handlerGUID = handlerLinkData.guid;

                bool sceneFound = false;
                if (string.IsNullOrEmpty(handlerGUID))
                {
                    Debug.Log($"GameStateModifier {name} :: Removing entry {zoneName}/{handlerName} — Empty GUID.");
                }
                else
                {
                    Zone zone = Zone.GetFromName(zoneName);
                    string scenePath = string.Empty;
                    if (zone != null)
                    {
                        scenePath = zone.GetSceneReference().GetScenePath();
                        sceneFound = !string.IsNullOrWhiteSpace(scenePath);
                    }

                    bool objectFound = sceneFound && !string.IsNullOrWhiteSpace(handlerName) && DoesGameStateModifierHandlerExist(scenePath, handlerGUID);

                    // Found -- Skip Removal
                    if (sceneFound && objectFound)
                    {
                        continue;
                    }


                    string reason = !sceneFound ? $"Zone {zoneName} not found" : $"object {handlerName} not found";
                    Debug.Log($"GameStateModifier {name} ::  Removing entry {zoneName}/{handlerName} — {reason}.");
                }

                gameStateModifierHandlerData.RemoveAt(i);

                removedCount++;
            }
            return removedCount;
        }
        
        private int RemoveDuplicateEntries()
        {
            int initialCount = gameStateModifierHandlerData.Count;
            gameStateModifierHandlerData = gameStateModifierHandlerData.Distinct().ToList();
            int removedCount = initialCount - gameStateModifierHandlerData.Count;
            
            if (removedCount > 0) { Debug.Log($"GameStateModifier {name} :: Duplicate GUIDs found!  Removed {removedCount} entries."); }
            return removedCount;
        }
        #endregion
#endif
    }
}

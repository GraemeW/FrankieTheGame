using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using Frankie.ZoneManagement;

namespace Frankie.Core.GameStateModifiers
{
    public abstract class GameStateModifier : ScriptableObject, ISerializationCallbackReceiver
    {
        #region Properties
        [SerializeField] private string guid;
        public List<ZoneToGameObjectLinkData> gameStateModifierHandlerData = new(); // Custom view in GameStateModifierEditor
        #endregion
        
        #region StaticMethods
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
        
        #region PublicMethods
        public string GetGUID() => guid;
        public void AddOrUpdateGameStateModifierHandler(ZoneToGameObjectLinkData zoneToGameObjectLinkData)
        {
            foreach (ZoneToGameObjectLinkData checkLinkData in gameStateModifierHandlerData.Where(checkLinkData => checkLinkData.guid == zoneToGameObjectLinkData.guid))
            {
                checkLinkData.UpdateRecord(zoneToGameObjectLinkData.zoneName, zoneToGameObjectLinkData.gameObjectName);
                return;
            }
            gameStateModifierHandlerData.Add(zoneToGameObjectLinkData);
        }

        public int CleanDanglingModifierHandlerData()
        {
            int removedCount = 0;
            removedCount += RemoveNonExistentEntries();
            removedCount += RemoveDuplicateEntries();
            return removedCount;
        }
        #endregion

        #region PrivateMethods
        private int RemoveNonExistentEntries()
        {
            int removedCount = 0;
            for (int i = gameStateModifierHandlerData.Count - 1; i >= 0; i--)
            {
                ZoneToGameObjectLinkData handlerLinkData = gameStateModifierHandlerData[i];
                string zoneName = handlerLinkData.zoneName;
                string handlerName = handlerLinkData.gameObjectName;
                string handlerGUID = handlerLinkData.guid;

                bool sceneFound = false;
                if (string.IsNullOrEmpty(handlerGUID))
                {
                    Debug.Log($"[SceneObjectPair] Removing entry {zoneName}/{handlerName} — Empty GUID.");
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
                    if (sceneFound && objectFound) { continue; }
                    
                    
                    string reason = !sceneFound ? $"Zone {zoneName} not found" : $"object {handlerName} not found";
                    Debug.Log($"[SceneObjectPair] Removing entry {zoneName}/{handlerName} — {reason}.");
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
            
            if (removedCount > 0) { Debug.Log($"Duplicate GUIDs found!  Removed {removedCount} entries."); }
            return removedCount;
        }
        #endregion
        
        #region InterfaceMethods
        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            if (string.IsNullOrWhiteSpace(guid))
            {
                guid = System.Guid.NewGuid().ToString();
            }
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            // Unused, required for interface
        }
        #endregion
    }
}

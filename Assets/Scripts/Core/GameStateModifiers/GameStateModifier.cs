using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using Frankie.ZoneManagement;

namespace Frankie.Core.GameStateModifiers
{
    public abstract class GameStateModifier : ScriptableObject
    {
        public static string GetGameStateModifierHandlerDataRef() => nameof(gameStateModifierHandlerData);
        public List<ZoneToGameObjectLinkData> gameStateModifierHandlerData = new();

        public void AddOrUpdateGameStateModifierHandler(ZoneToGameObjectLinkData zoneToGameObjectLinkData)
        {
            foreach (ZoneToGameObjectLinkData checkLinkData in gameStateModifierHandlerData.Where(checkLinkData => checkLinkData.guid == zoneToGameObjectLinkData.guid))
            {
                checkLinkData.UpdateRecord(zoneToGameObjectLinkData.zoneName, zoneToGameObjectLinkData.gameObjectName);
                return;
            }
            gameStateModifierHandlerData.Add(zoneToGameObjectLinkData);
        }

        public void CleanDanglingModifierHandlerData()
        {
            RemoveNonExistentEntries();
            RemoveDuplicateEntries();
        }

        public int RemoveNonExistentEntries()
        {
            int removedCount = 0;
            for (int i = gameStateModifierHandlerData.Count - 1; i >= 0; i--)
            {
                ZoneToGameObjectLinkData zoneToGameObjectLinkData = gameStateModifierHandlerData[i];
                string zoneName = zoneToGameObjectLinkData.zoneName;
                string gameObjectName = zoneToGameObjectLinkData.gameObjectName;
                string guid = zoneToGameObjectLinkData.guid;
                
                bool sceneFound = false;
                Zone zone = Zone.GetFromName(zoneName);
                string scenePath = string.Empty;
                if (zone != null)
                {
                    scenePath = zone.GetSceneReference().GetScenePath();
                    sceneFound = !string.IsNullOrWhiteSpace(scenePath);
                }
                
                // TODO:  Replace game object selection with GUID-based search instead of name-based search
                bool objectFound = sceneFound && !string.IsNullOrWhiteSpace(gameObjectName) && DoesGameStateModifierHandlerExist(scenePath, gameObjectName);
                if (sceneFound && objectFound) { continue; }
                
                string reason = !sceneFound ? $"Zone {zoneName} not found" : $"object {gameObjectName} not found";
                Debug.Log($"[SceneObjectPair] Removing entry {zoneName}/{gameObjectName} — {reason}.");
                gameStateModifierHandlerData.RemoveAt(i);
                
                removedCount++;
            }
            return removedCount;
        }
        
        private static bool DoesGameStateModifierHandlerExist(string scenePath, string objectName)
        {
            if (string.IsNullOrEmpty(scenePath)) { return false; }
            
            Scene checkScene = SceneManager.GetSceneByPath(scenePath);
            bool isSceneAlreadyLoaded = checkScene.isLoaded;

            // Open additively -- avoid save prompt / disturbing other open scenes
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);

            bool found;
            try
            {
                found = System.Array.Exists( FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None), go => go.scene == scene && go.name == objectName);
            }
            finally
            {
                if (!isSceneAlreadyLoaded) { EditorSceneManager.CloseScene(scene, true); }
            }

            return found;
        }

        public void RemoveDuplicateEntries()
        {
            
        }
    }
}

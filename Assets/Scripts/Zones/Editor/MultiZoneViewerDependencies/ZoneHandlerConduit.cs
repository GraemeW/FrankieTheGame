using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Frankie.ZoneManagement.UIEditor
{
    public static class ZoneHandlerConduit
    {
        public static HashSet<string> GetLinkedScenePaths(Zone rootZone, int maxZoneCount)
        {
            HashSet<string> linkedScenePaths = new();
            Queue<string> scenePathsToTraverse = new();
            
            if (rootZone == null || rootZone.GetSceneReference().SceneName == null) { return linkedScenePaths; }
            
            string rootScenePath = rootZone.GetSceneReference().GetScenePath();
            if (string.IsNullOrEmpty(rootScenePath)) { return linkedScenePaths; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) { return  linkedScenePaths; }
            
            string originalScenePath = SceneManager.GetActiveScene().path;

            scenePathsToTraverse.Enqueue(rootScenePath);
            int currentZoneCount = 0;
            while (scenePathsToTraverse.Count > 0)
            {
                string currentScenePath = scenePathsToTraverse.Dequeue();
                if (string.IsNullOrEmpty(currentScenePath) || linkedScenePaths.Contains(currentScenePath)) { continue; }
                EditorUtility.DisplayProgressBar("MultiZone Viewer", "Capturing all linked zones", (float)currentZoneCount / maxZoneCount);
                
                linkedScenePaths.Add(currentScenePath);
                EditorSceneManager.OpenScene(currentScenePath, OpenSceneMode.Single);
                
                List<ZoneHandler> zoneHandlers = Object.FindObjectsByType<ZoneHandler>().ToList();
                foreach (ZoneHandler zoneHandler in zoneHandlers)
                {
                    if (zoneHandler.GetZoneNode() == null) { continue; }
                    ZoneNode zoneNode = zoneHandler.GetZoneNode();
                    if (!zoneNode.HasLinkedSceneReference()) { continue; }
                    
                    ZoneNode linkedZoneNode = zoneNode.GetLinkedZoneNode();
                    Zone linkedZone = linkedZoneNode.GetZone();
                    
                    SceneReference sceneReference = linkedZone.GetSceneReference();
                    string scenePath = sceneReference.GetScenePath();
                    if (linkedScenePaths.Contains(scenePath)) { continue; }
                    
                    scenePathsToTraverse.Enqueue(scenePath);
                }
                currentZoneCount++;
            }
            
            EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single);
            return linkedScenePaths;
        }

        public static void Bonus(Zone rootZone)
        {
            if (rootZone == null || rootZone.GetSceneReference().SceneName == null) { return;}
            
            string rootScenePath = rootZone.GetSceneReference().GetScenePath();
            if (string.IsNullOrEmpty(rootScenePath)) { return; }
            
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) { return; }
            
            EditorSceneManager.OpenScene(rootScenePath, OpenSceneMode.Single);
            List<ZoneHandler> zoneHandlers = Object.FindObjectsByType<ZoneHandler>().ToList();
            Debug.Log($"Zone Handler Count: {zoneHandlers.Count}");
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Frankie.ZoneManagement.UIEditor
{
    public static class ZoneHandlerConduit
    {
        public static IEnumerable<string> OpenLinkedScenePaths(Zone rootZone, int maxZoneCount, HashSet<string> existingViewScenePaths)
        {
            if (rootZone == null || rootZone.GetSceneReference().SceneName == null) { yield break; }
            string rootScenePath = rootZone.GetSceneReference().GetScenePath();
            if (string.IsNullOrEmpty(rootScenePath)) { yield break;  }
         
            HashSet<string> uniqueScenePaths = new();
            Queue<string> scenePathsToTraverse = new();
            foreach (string existingScenePath in existingViewScenePaths) { scenePathsToTraverse.Enqueue(existingScenePath); }
            if (!existingViewScenePaths.Contains(rootScenePath)) { scenePathsToTraverse.Enqueue(rootScenePath); }

            int currentZoneCount = 0;
            while (scenePathsToTraverse.Count > 0)
            {
                string currentScenePath = scenePathsToTraverse.Dequeue();
                
                // Skip any scenes we've already been on -- needed since existingSceneViews can cause dupes with ZoneHandler-added scenes
                if (string.IsNullOrEmpty(currentScenePath) || uniqueScenePaths.Contains(currentScenePath)) { continue; }
                EditorUtility.DisplayProgressBar("MultiZone Viewer", "Capturing all linked zones", (float)currentZoneCount / maxZoneCount);
                
                // Open scene, then yield back for camera capture 
                EditorSceneManager.OpenScene(currentScenePath, OpenSceneMode.Single);
                uniqueScenePaths.Add(currentScenePath);
                yield return currentScenePath;
                
                // Finally, crawl to expand the list of viable scenes and iterate back up
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
                    
                    // TODO:  Pass back by ref
                    
                    if (uniqueScenePaths.Contains(scenePath)) { continue; }
                    scenePathsToTraverse.Enqueue(scenePath);
                }
                currentZoneCount++;
            }
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

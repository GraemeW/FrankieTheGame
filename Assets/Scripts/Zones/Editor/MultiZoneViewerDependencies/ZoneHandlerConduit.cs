using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Frankie.ZoneManagement.Editor
{
    public static class ZoneHandlerConduit
    {
        #region PublicMethods
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
                foreach (ZoneNode zoneNode in FilterToLinking(FindZoneNodes()))
                {
                    ZoneNode linkedZoneNode = zoneNode.GetLinkedZoneNode();
                    Zone linkedZone = linkedZoneNode.GetZone();
                    
                    SceneReference sceneReference = linkedZone.GetSceneReference();
                    string scenePath = sceneReference.GetScenePath();
                    
                    if (uniqueScenePaths.Contains(scenePath)) { continue; }
                    scenePathsToTraverse.Enqueue(scenePath);
                }
                currentZoneCount++;
            }
        }
        
        public static List<ZoneHandlerNodeData> BuildZoneHandlerNodeData()
        {
            List<ZoneHandlerNodeData> zoneHandlerNodeDataSet = new();
            foreach (ZoneHandler zoneHandler in Object.FindObjectsByType<ZoneHandler>())
            {
                if (zoneHandler.GetZoneNode() == null) { continue; }
                ZoneHandlerNodeData zoneHandlerNodeData = new ZoneHandlerNodeData(zoneHandler.GetZoneNode(), zoneHandler.GetWarpPosition());
                zoneHandlerNodeDataSet.Add(zoneHandlerNodeData);
            }
            return zoneHandlerNodeDataSet;
        }

        public static List<ZoneHandlerLinkData> GenerateZoneHandlerLinks(List<ZoneHandlerNodeData> zoneHandlerNodeDataSet, Dictionary<string, Bounds> zoneDimensionsLookup)
        {
            List<ZoneHandlerLinkData> zoneHandlerLinkDataSet = new();
            Dictionary<(string, string), ZoneHandlerNodeData> zoneHandlerNodeDataLookup = BuildZoneHandlerNodeDataLookup(zoneHandlerNodeDataSet);
            foreach (ZoneHandlerNodeData zoneHandlerNodeData in zoneHandlerNodeDataSet)
            {
                if (zoneHandlerNodeData.zoneNode == null || !zoneHandlerNodeData.zoneNode.HasLinkedSceneReference()) { continue; }
                
                ZoneHandlerNodeData sourceZoneHandlerNodeData = zoneHandlerNodeData;
                string sourceZoneName = sourceZoneHandlerNodeData.zoneNode.GetZoneName();
                string targetZoneName = zoneHandlerNodeData.zoneNode.GetLinkedZoneNode().GetZoneName();
                
                if (!zoneDimensionsLookup.ContainsKey(sourceZoneName) || !zoneDimensionsLookup.ContainsKey(targetZoneName)) { continue; }
                
                string targetZoneNodeID = zoneHandlerNodeData.zoneNode.GetLinkedZoneNode().GetNodeID();
                if (!zoneHandlerNodeDataLookup.TryGetValue((targetZoneName, targetZoneNodeID), out ZoneHandlerNodeData targetZoneHandlerNodeData)) { continue; }
                
                var zoneHandlerLinkData = new ZoneHandlerLinkData(
                    sourceZoneHandlerNodeData, zoneDimensionsLookup[sourceZoneName],
                    targetZoneHandlerNodeData, zoneDimensionsLookup[targetZoneName]);
                zoneHandlerLinkDataSet.Add(zoneHandlerLinkData);
            }

            return zoneHandlerLinkDataSet;
        }
        #endregion

        #region PrivateMethods
        private static List<ZoneNode> FindZoneNodes()
        {
            return (from zoneHandler in Object.FindObjectsByType<ZoneHandler>() where zoneHandler.GetZoneNode() != null select zoneHandler.GetZoneNode()).ToList();
        }
    
        private static IEnumerable<ZoneNode> FilterToLinking(IList<ZoneNode> zoneNodes)
        {
            return zoneNodes.Where(zoneNode => zoneNode.HasLinkedSceneReference());
        }

        private static Dictionary<(string, string), ZoneHandlerNodeData> BuildZoneHandlerNodeDataLookup(List<ZoneHandlerNodeData> zoneHandlerNodeDataSet)
        {
            Dictionary<(string, string), ZoneHandlerNodeData> zoneHandlerNodeDataLookup = new();
            foreach (ZoneHandlerNodeData zoneHandlerNodeData in zoneHandlerNodeDataSet)
            {
                ZoneNode zoneNode = zoneHandlerNodeData.zoneNode;
                if (zoneNode == null) { continue; }
                
                zoneHandlerNodeDataLookup[(zoneNode.GetZoneName(), zoneNode.GetNodeID())] = zoneHandlerNodeData;
            }
            return zoneHandlerNodeDataLookup;
        }
        #endregion
    }
}

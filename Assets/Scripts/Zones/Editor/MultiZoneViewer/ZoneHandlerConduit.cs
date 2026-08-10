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
            foreach (ZoneHandlerBase zoneHandler in Object.FindObjectsByType<ZoneHandlerBase>(FindObjectsInactive.Include))
            {
                if (zoneHandler.GetZoneNode() == null) { continue; }
                ZoneHandlerNodeData zoneHandlerNodeData = new ZoneHandlerNodeData(zoneHandler.GetZoneNode(), zoneHandler.GetWarpPosition());
                zoneHandlerNodeDataSet.Add(zoneHandlerNodeData);
            }
            return zoneHandlerNodeDataSet;
        }
        
        public static Dictionary<string, List<ZoneNodeData>> BuildZoneNodeData(List<ZoneHandlerNodeData> zoneHandlerNodeDataSet, Dictionary<string, Bounds> zoneDimensionsLookup)
        {
            Dictionary<string, List<ZoneNodeData>> zoneNodeDataByZoneName = new();
            Dictionary<(string zoneName, string zoneNodeID), Vector2> relativePositionLookup = new();

            foreach (ZoneHandlerNodeData zoneHandlerNodeData in zoneHandlerNodeDataSet)
            {
                if (zoneHandlerNodeData.zoneNode == null) { continue; }

                string zoneName = zoneHandlerNodeData.zoneNode.GetZoneName();
                if (!zoneDimensionsLookup.TryGetValue(zoneName, out Bounds zoneBounds)) { continue; }

                string zoneNodeID = zoneHandlerNodeData.zoneNode.GetNodeID();
                Vector2 relativePosition = GetRelativePosition(zoneHandlerNodeData.position, zoneBounds);

                if (!zoneNodeDataByZoneName.TryGetValue(zoneName, out List<ZoneNodeData> dataSet))
                {
                    dataSet = new List<ZoneNodeData>();
                    zoneNodeDataByZoneName[zoneName] = dataSet;
                }
                dataSet.Add(new ZoneNodeData(zoneNodeID, relativePosition));
                relativePositionLookup[(zoneName, zoneNodeID)] = relativePosition;
            }

            foreach (ZoneHandlerNodeData zoneHandlerNodeData in zoneHandlerNodeDataSet)
            {
                if (zoneHandlerNodeData.zoneNode == null || !zoneHandlerNodeData.zoneNode.HasLinkedSceneReference()) { continue; }

                string sourceZoneName = zoneHandlerNodeData.zoneNode.GetZoneName();
                string sourceZoneNodeID = zoneHandlerNodeData.zoneNode.GetNodeID();
                if (!zoneNodeDataByZoneName.TryGetValue(sourceZoneName, out List<ZoneNodeData> sourceDataSet)) { continue; }

                ZoneNode targetZoneNode = zoneHandlerNodeData.zoneNode.GetLinkedZoneNode();
                string targetZoneName = targetZoneNode.GetZoneName();
                string targetZoneNodeID = targetZoneNode.GetNodeID();
                if (!relativePositionLookup.TryGetValue((targetZoneName, targetZoneNodeID), out Vector2 targetRelativePosition)) { continue; }

                int sourceIndex = sourceDataSet.FindIndex(candidateZoneNodeData => candidateZoneNodeData.zoneNodeID == sourceZoneNodeID);
                if (sourceIndex < 0) { continue; }

                ZoneNodeData sourceZoneNodeData = sourceDataSet[sourceIndex];
                sourceZoneNodeData.SetLink(targetZoneName, targetZoneNodeID, targetRelativePosition);
                sourceDataSet[sourceIndex] = sourceZoneNodeData;
            }

            return zoneNodeDataByZoneName;
        }

        private static Vector2 GetRelativePosition(Vector2 position, Bounds bounds)
        {
            Vector2 topLeft = new Vector2(bounds.min.x, bounds.max.y);
            float xRelative = Mathf.Clamp01((position.x - topLeft.x) / bounds.size.x);
            float yRelative = Mathf.Clamp01((topLeft.y - position.y) / bounds.size.y);
            return new Vector2(xRelative, yRelative);
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
        #endregion
    }
}

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Frankie.ZoneManagement.UIEditor
{
    [System.Serializable]
    public class MultiZoneView : ScriptableObject
    {
        // Tunables
        [field: SerializeField] public List<ZoneViewData> zoneViewDataSet { get; private set; } = new();
        
        // State
        private Dictionary<string, ZoneViewData> zoneViewDataLookup = null;
        
        #region CachingMethods
        private ZoneViewData FindZoneViewData(string zoneName, bool forceBuildLookup = false)
        {
            if (zoneViewDataLookup == null || forceBuildLookup) { BuildZoneViewLookup(); }
            return zoneViewDataLookup.GetValueOrDefault(zoneName);
        }

        private void BuildZoneViewLookup()
        {
            zoneViewDataLookup ??= new Dictionary<string, ZoneViewData>();
            foreach (ZoneViewData zoneViewData in zoneViewDataSet)
            {
                zoneViewDataLookup[zoneViewData.zoneName] = zoneViewData;
            }
        }
        #endregion
        
        #region PublicMethods
        public void CleanDanglingZoneViewData()
        {
            zoneViewDataSet.RemoveAll(zoneViewData => zoneViewData == null);
        }
        
        public ZoneViewData CreateOrUpdateZoneViewData(string zoneName, string scenePath, string snapshotPNGPath, Vector2 dimensions, Vector2 topLeftPosition, bool keepExistingPosition, bool keepExistingDimensions)
        {
            foreach (ZoneViewData checkZoneViewData in zoneViewDataSet)
            {
                if (checkZoneViewData == null) { continue; }
                if (zoneName == checkZoneViewData.zoneName)
                {
                    if (keepExistingPosition) { topLeftPosition = checkZoneViewData.topLeftPosition; }
                    if (keepExistingDimensions) { dimensions = checkZoneViewData.dimensions; }
                    checkZoneViewData.Setup(zoneName, scenePath, snapshotPNGPath, dimensions, topLeftPosition);
                    return checkZoneViewData;
                }
            }
            
            ZoneViewData zoneViewData = CreateInstance<ZoneViewData>();
            zoneViewDataSet.Add(zoneViewData);
            zoneViewData.Setup(zoneName, scenePath, snapshotPNGPath, dimensions, topLeftPosition);
            AssetDatabase.AddObjectToAsset(zoneViewData, this);
            
            return zoneViewData;
        }

        public HashSet<string> GetScenePaths()
        {
            HashSet<string> scenePaths = new();
            foreach (ZoneViewData zoneViewData in zoneViewDataSet)
            {
                if (zoneViewData == null) { continue; }
                if (string.IsNullOrWhiteSpace(zoneViewData.scenePath)) { continue; }
                scenePaths.Add(zoneViewData.scenePath);
            }
            return scenePaths;
        }

        public void UpdateZoneHandlerLinkData(List<ZoneHandlerLinkData> zoneHandlerLinkDataSet)
        {
            BuildZoneViewLookup();
            
            foreach (ZoneHandlerLinkData zoneHandlerLinkData in zoneHandlerLinkDataSet)
            {
                if (string.IsNullOrWhiteSpace(zoneHandlerLinkData.sourceZoneName) || string.IsNullOrWhiteSpace(zoneHandlerLinkData.targetZoneName)) { continue; }
                if (zoneHandlerLinkData.sourceZoneName == zoneHandlerLinkData.targetZoneName) { continue; }
                
                ZoneViewData sourceZoneViewData = FindZoneViewData(zoneHandlerLinkData.sourceZoneName);
                sourceZoneViewData.CreateOrUpdateZoneLinkData(zoneHandlerLinkData);
            }
        }
        #endregion
    }
}

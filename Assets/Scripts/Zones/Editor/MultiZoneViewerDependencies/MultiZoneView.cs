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
        
        #region PublicMethods

        public void CleanDanglingZoneViewData()
        {
            zoneViewDataSet.RemoveAll(zoneViewData => zoneViewData == null);
        }
        
        public ZoneViewData CreateOrUpdateZoneViewData(string zoneName, string scenePath, string snapshotPNGPath, Vector2 dimensions, Vector2 topLeftPosition, bool keepExistingPosition)
        {
            foreach (ZoneViewData checkZoneViewData in zoneViewDataSet)
            {
                if (checkZoneViewData == null) { continue; }
                if (zoneName == checkZoneViewData.zoneName)
                {
                    if (keepExistingPosition) { checkZoneViewData.Setup(zoneName, scenePath, snapshotPNGPath); }
                    else { checkZoneViewData.Setup(zoneName, scenePath, snapshotPNGPath, dimensions, topLeftPosition); }
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
        #endregion
    }
}

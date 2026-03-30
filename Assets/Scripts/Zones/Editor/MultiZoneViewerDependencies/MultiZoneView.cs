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
        
        public void CreateOrUpdateZoneViewData(string zoneName, string snapshotPNGPath, Vector2 topLeftPosition)
        {
            foreach (ZoneViewData checkZoneViewData in zoneViewDataSet)
            {
                if (checkZoneViewData == null) { continue; }
                if (zoneName == checkZoneViewData.zoneName)
                {
                    checkZoneViewData.Setup(zoneName, snapshotPNGPath, topLeftPosition);
                    return;
                }
            }
            
            ZoneViewData zoneViewData = CreateInstance<ZoneViewData>();
            zoneViewDataSet.Add(zoneViewData);
            zoneViewData.Setup(zoneName, snapshotPNGPath, topLeftPosition);
            AssetDatabase.AddObjectToAsset(zoneViewData, this);
        }
        #endregion
    }
}

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Frankie.ZoneManagement.Editor
{
    [System.Serializable]
    public class ZoneViewData : ScriptableObject
    {
        // Tunables
        [field: SerializeField] public string zoneName { get; private set; }
        [field: SerializeField] public string scenePath { get; private set; }
        [field: SerializeField] public string snapshotPath { get; private set; }
        public Vector2 topLeftPosition;
        public Vector2 dimensions;
        [field: SerializeField] public List<ZoneHandlerLinkData> zoneHandlerLinkDataSet { get; private set; } = new();
        [field: SerializeField] public List<ZoneNodeDotData> zoneNodeDotDataSet { get; private set; } = new();

        public void Setup(string setZoneName, string setScenePath, string setSnapshotPath, Vector2 setDimensions, Vector2 setTopLeftPosition)
        {
            name = setZoneName;
            scenePath = setScenePath;
            zoneName = setZoneName;
            snapshotPath = setSnapshotPath;
            dimensions = setDimensions;
            topLeftPosition = setTopLeftPosition;
            zoneHandlerLinkDataSet = new List<ZoneHandlerLinkData>();
            zoneNodeDotDataSet = new List<ZoneNodeDotData>();
            EditorUtility.SetDirty(this);
        }

        public void SetZoneNodeDotData(List<ZoneNodeDotData> setZoneNodeDotDataSet)
        {
            zoneNodeDotDataSet = setZoneNodeDotDataSet;
            EditorUtility.SetDirty(this);
        }

        public bool RemoveZoneLinkDataForSource(string sourceZoneNodeID)
        {
            int removedCount = zoneHandlerLinkDataSet.RemoveAll(zoneHandlerLinkData => zoneHandlerLinkData.sourceZoneNodeID == sourceZoneNodeID);
            if (removedCount > 0) { EditorUtility.SetDirty(this); }
            return removedCount > 0;
        }

        public void CreateOrUpdateZoneLinkData(ZoneHandlerLinkData zoneHandlerLinkData)
        {
            bool matchFound = false;
            foreach (ZoneHandlerLinkData matchZoneHandlerLinkData in zoneHandlerLinkDataSet)
            {
                if (!matchZoneHandlerLinkData.MatchSource(zoneHandlerLinkData)) { continue; }
                
                matchZoneHandlerLinkData.UpdateZoneHandlerLinkData(zoneHandlerLinkData);
                matchFound = true;
                break;
            }

            if (!matchFound)
            {
                zoneHandlerLinkDataSet.Add(zoneHandlerLinkData);
            }
        }
    }
}

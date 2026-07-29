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
        [field: SerializeField] public List<ZoneNodeData> zoneNodeDataSet { get; private set; } = new();

        public void Setup(string setZoneName, string setScenePath, string setSnapshotPath, Vector2 setDimensions, Vector2 setTopLeftPosition)
        {
            name = setZoneName;
            scenePath = setScenePath;
            zoneName = setZoneName;
            snapshotPath = setSnapshotPath;
            dimensions = setDimensions;
            topLeftPosition = setTopLeftPosition;
            zoneNodeDataSet = new List<ZoneNodeData>();
            EditorUtility.SetDirty(this);
        }

        public void SetZoneNodeData(List<ZoneNodeData> setZoneNodeDataSet)
        {
            zoneNodeDataSet = setZoneNodeDataSet;
            EditorUtility.SetDirty(this);
        }

        public bool TryGetZoneNodeData(string zoneNodeID, out ZoneNodeData zoneNodeData)
        {
            foreach (ZoneNodeData candidateZoneNodeData in zoneNodeDataSet)
            {
                if (candidateZoneNodeData.zoneNodeID != zoneNodeID) { continue; }
                zoneNodeData = candidateZoneNodeData;
                return true;
            }
            zoneNodeData = default;
            return false;
        }

        public bool TrySetLink(string zoneNodeID, string linkedZoneName, string linkedZoneNodeID, Vector2 linkedRelativePosition)
        {
            int index = zoneNodeDataSet.FindIndex(candidateZoneNodeData => candidateZoneNodeData.zoneNodeID == zoneNodeID);
            if (index < 0) { return false; }

            ZoneNodeData zoneNodeData = zoneNodeDataSet[index];
            zoneNodeData.SetLink(linkedZoneName, linkedZoneNodeID, linkedRelativePosition);
            zoneNodeDataSet[index] = zoneNodeData;
            EditorUtility.SetDirty(this);
            return true;
        }

        public bool TryClearLink(string zoneNodeID)
        {
            int index = zoneNodeDataSet.FindIndex(candidateZoneNodeData => candidateZoneNodeData.zoneNodeID == zoneNodeID);
            if (index < 0 || !zoneNodeDataSet[index].HasLink()) { return false; } // Nothing to clear

            ZoneNodeData zoneNodeData = zoneNodeDataSet[index];
            zoneNodeData.ClearLink();
            zoneNodeDataSet[index] = zoneNodeData;
            EditorUtility.SetDirty(this);
            return true;
        }
        
        public void UpdateZoneNodePositions(Dictionary<string, Vector2> relativePositionByNodeID)
        {
            bool changed = false;
            for (int i = 0; i < zoneNodeDataSet.Count; i++)
            {
                if (!relativePositionByNodeID.TryGetValue(zoneNodeDataSet[i].zoneNodeID, out Vector2 relativePosition)) { continue; }

                ZoneNodeData zoneNodeData = zoneNodeDataSet[i];
                zoneNodeData.relativePosition = relativePosition;
                zoneNodeDataSet[i] = zoneNodeData;
                changed = true;
            }
            if (changed) { EditorUtility.SetDirty(this); }
        }
    }
}

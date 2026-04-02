using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Frankie.ZoneManagement.UIEditor
{
    [System.Serializable]
    public class ZoneViewData : ScriptableObject
    {
        // Tunables
        public string zoneName { get; private set; }
        public string scenePath { get; private set; }
        public string snapshotPath { get; private set; }
        public Vector2 topLeftPosition;
        public Vector2 dimensions;
        public List<ZoneHandlerLinkData> zoneHandlerLinkDataSet { get; private set; } = new();

        public void Setup(string setZoneName, string setScenePath, string setSnapshotPath, Vector2 setDimensions, Vector2 setTopLeftPosition)
        {
            name = setZoneName;
            scenePath = setScenePath;
            zoneName = setZoneName;
            snapshotPath = setSnapshotPath;
            dimensions = setDimensions;
            topLeftPosition = setTopLeftPosition;
            zoneHandlerLinkDataSet = new List<ZoneHandlerLinkData>();
            EditorUtility.SetDirty(this);
        }

        public void Setup(string setZoneName, string setScenePath, string setSnapshotPath)
        {
            name = setZoneName;
            scenePath = setScenePath;
            zoneName = setZoneName;
            snapshotPath = setSnapshotPath;
            zoneHandlerLinkDataSet = new List<ZoneHandlerLinkData>();
            EditorUtility.SetDirty(this);
        }
    }
}

using UnityEditor;
using UnityEngine;

namespace Frankie.ZoneManagement.UIEditor
{
    [System.Serializable]
    public class ZoneViewData : ScriptableObject
    {
        // Tunables
        public string zoneName;
        public string scenePath;
        public string snapshotPath;
        public Vector2 topLeftPosition;

        public void Setup(string setZoneName, string setScenePath, string setSnapshotPath, Vector2 setTopLeftPosition)
        {
            name = setZoneName;
            scenePath = setScenePath;
            zoneName = setZoneName;
            snapshotPath = setSnapshotPath;
            topLeftPosition = setTopLeftPosition;
            EditorUtility.SetDirty(this);
        }
    }
}

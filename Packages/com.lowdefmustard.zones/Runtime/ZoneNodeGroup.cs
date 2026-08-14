using System.Collections.Generic;
using UnityEngine;

namespace LowDefMustard.Zones
{
    [System.Serializable]
    public class ZoneNodeGroup
    {
        [Header("Saved State")]
        [SerializeField] private string zoneNodeGroupName;
        [SerializeField] private Rect rect;
        [SerializeField] private List<string> containedNodeIDs = new();
        [HideInInspector][SerializeField] private string zoneName;
        
        // Const
        private const string _defaultZoneGroupName = "--Zone Group--";
        private const float _zoneNodeGroupPadding = 20f;
        private const float _headerOffset = 25f;
        
        // Cached State
        private Zone cachedZone;
        
        public ZoneNodeGroup(string zoneName)
        {
            zoneNodeGroupName = _defaultZoneGroupName;
            this.zoneName = zoneName;
        }
        
#if UNITY_EDITOR
        #region Getters
        public string GetZoneNodeGroupName() => zoneNodeGroupName ?? _defaultZoneGroupName;
        public Rect GetRect() => rect;
        public IReadOnlyList<string> GetContainedNodeIDs() => containedNodeIDs;
        public bool ContainsNodeID(string nodeID) => containedNodeIDs.Contains(nodeID);
        #endregion

        #region Setters
        public void SetZoneNodeGroupName(string setZoneNodeGroupName) => zoneNodeGroupName = setZoneNodeGroupName; 
        public void SetRect(Rect newRect) => rect = newRect;
        public void AddNodeID(string nodeID)
        {
            if (!containedNodeIDs.Contains(nodeID)) { containedNodeIDs.Add(nodeID); }
        }
        public void RemoveNodeID(string nodeID) => containedNodeIDs.Remove(nodeID);
        #endregion
        
        #region PublicMethods
        public void RecomputeGroupRect()
        {
            if (containedNodeIDs.Count == 0) { return; }
            if (cachedZone == null) { cachedZone = Zone.GetFromName(zoneName); }
            if (cachedZone == null) { return; }

            Rect? bounds = null;
            foreach (string nodeID in containedNodeIDs)
            {
                ZoneNode node = cachedZone.GetNodeFromID(nodeID);
                if (node == null) { continue; }
                bounds = bounds == null ? node.GetRect() : EncapsulateRect(bounds.Value, node.GetRect());
            }
            if (bounds == null) { return; }

            var paddedBounds = new Rect(bounds.Value.x - _zoneNodeGroupPadding, bounds.Value.y - _zoneNodeGroupPadding - _headerOffset, bounds.Value.width + _zoneNodeGroupPadding * 2f, bounds.Value.height + _zoneNodeGroupPadding * 2f + _headerOffset);
            SetRect(paddedBounds);
        }
        #endregion

        #region PrivateMethods
        private static Rect EncapsulateRect(Rect a, Rect b)
        {
            float xMin = Mathf.Min(a.xMin, b.xMin);
            float yMin = Mathf.Min(a.yMin, b.yMin);
            float xMax = Mathf.Max(a.xMax, b.xMax);
            float yMax = Mathf.Max(a.yMax, b.yMax);
            return new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
        }
        #endregion
#endif
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace Frankie.ZoneManagement
{
    [System.Serializable]
    public class ZoneNodeGroup
    {
        // Tunables
        [SerializeField] private float zoneNodeGroupPadding = 20f; 
        [SerializeField] private Rect rect;
        [SerializeField] private List<string> containedNodeIDs = new();
        [HideInInspector][SerializeField] private string zoneName;

        // Cached State
        private Zone cachedZone;
        
        public ZoneNodeGroup(string zoneName)
        {
            this.zoneName = zoneName;
        }
        
        #region Getters
        public Rect GetRect() => rect;
        public IReadOnlyList<string> GetContainedNodeIDs() => containedNodeIDs;
        public bool ContainsNodeID(string nodeID) => containedNodeIDs.Contains(nodeID);
        #endregion

        #region Setters
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

            var paddedBounds = new Rect(bounds.Value.x - zoneNodeGroupPadding, bounds.Value.y - zoneNodeGroupPadding, bounds.Value.width + zoneNodeGroupPadding * 2f, bounds.Value.height + zoneNodeGroupPadding * 2f);
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
    }
}

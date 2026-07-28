using UnityEngine;

namespace Frankie.ZoneManagement.Editor
{
    [System.Serializable]
    public struct ZoneHandlerLinkData
    {
        public string sourceZoneName;
        public string sourceZoneNodeID;
        public Vector2 sourceNodeRelativePosition;
        public string targetZoneName;
        public string targetZoneNodeID;
        public Vector2 targetNodeRelativePosition;

        public ZoneHandlerLinkData(ZoneHandlerNodeData sourceZoneHandlerNodeData, Bounds sourceZoneBounds, ZoneHandlerNodeData targetZoneHandlerNodeData, Bounds targetZoneBounds)
        {
            if (sourceZoneHandlerNodeData.zoneNode == null || targetZoneHandlerNodeData.zoneNode == null)
            {
                sourceZoneName = string.Empty;
                sourceZoneNodeID = string.Empty;
                sourceNodeRelativePosition = Vector2.zero;
                targetZoneName = string.Empty;
                targetZoneNodeID = string.Empty;
                targetNodeRelativePosition = Vector2.zero;
                return;
            }
            
            sourceZoneName = sourceZoneHandlerNodeData.zoneNode.GetZoneName();
            sourceZoneNodeID = sourceZoneHandlerNodeData.zoneNode.GetNodeID();
            sourceNodeRelativePosition = GetRelativePosition(sourceZoneHandlerNodeData.position, sourceZoneBounds);
            
            targetZoneName = targetZoneHandlerNodeData.zoneNode.GetZoneName();
            targetZoneNodeID = targetZoneHandlerNodeData.zoneNode.GetNodeID();
            targetNodeRelativePosition = GetRelativePosition(targetZoneHandlerNodeData.position, targetZoneBounds);
        }
        
        public ZoneHandlerLinkData(string sourceZoneName, string sourceZoneNodeID, Vector2 sourceNodeRelativePosition, string targetZoneName, string targetZoneNodeID, Vector2 targetNodeRelativePosition)
        {
            this.sourceZoneName = sourceZoneName;
            this.sourceZoneNodeID = sourceZoneNodeID;
            this.sourceNodeRelativePosition = sourceNodeRelativePosition;
            this.targetZoneName = targetZoneName;
            this.targetZoneNodeID = targetZoneNodeID;
            this.targetNodeRelativePosition = targetNodeRelativePosition;
        }

        public bool MatchSource(ZoneHandlerLinkData matchZoneHandlerLinkData) => matchZoneHandlerLinkData.sourceZoneName == sourceZoneName && matchZoneHandlerLinkData.sourceZoneNodeID == sourceZoneNodeID;

        public void UpdateZoneHandlerLinkData(ZoneHandlerLinkData updatedZoneHandlerLinkData)
        {
            sourceNodeRelativePosition = updatedZoneHandlerLinkData.sourceNodeRelativePosition;
            targetZoneName = updatedZoneHandlerLinkData.targetZoneName;
            targetZoneNodeID = updatedZoneHandlerLinkData.targetZoneNodeID;
            targetNodeRelativePosition = updatedZoneHandlerLinkData.targetNodeRelativePosition;
        }

        public static Vector2 GetRelativePosition(Vector2 position, Bounds bounds)
        {
            Vector2 topLeft = new Vector2(bounds.min.x, bounds.max.y);
            float xRelative = Mathf.Clamp01((position.x - topLeft.x) / bounds.size.x);
            float yRelative = Mathf.Clamp01((topLeft.y - position.y) / bounds.size.y);
            return new Vector2(xRelative, yRelative);
        }
    }
}

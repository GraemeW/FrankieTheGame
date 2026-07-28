using UnityEngine;

namespace Frankie.ZoneManagement.Editor
{
    [System.Serializable]
    public struct ZoneNodeData
    {
        public string zoneNodeID;
        public Vector2 relativePosition;

        public string linkedZoneName;
        public string linkedZoneNodeID;
        public Vector2 linkedRelativePosition;
        public bool HasLink() => !string.IsNullOrEmpty(linkedZoneNodeID);

        public ZoneNodeData(string zoneNodeID, Vector2 relativePosition)
        {
            this.zoneNodeID = zoneNodeID;
            this.relativePosition = relativePosition;
            linkedZoneName = string.Empty;
            linkedZoneNodeID = string.Empty;
            linkedRelativePosition = Vector2.zero;
        }

        public void SetLink(string setLinkedZoneName, string setLinkedZoneNodeID, Vector2 setLinkedRelativePosition)
        {
            linkedZoneName = setLinkedZoneName;
            linkedZoneNodeID = setLinkedZoneNodeID;
            linkedRelativePosition = setLinkedRelativePosition;
        }

        public void ClearLink()
        {
            linkedZoneName = string.Empty;
            linkedZoneNodeID = string.Empty;
            linkedRelativePosition = Vector2.zero;
        }
    }
}

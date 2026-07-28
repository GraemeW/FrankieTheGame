using UnityEngine;

namespace Frankie.ZoneManagement.Editor
{
    [System.Serializable]
    public struct ZoneNodeDotData
    {
        public string zoneNodeID;
        public Vector2 relativePosition;

        public ZoneNodeDotData(string zoneNodeID, Vector2 relativePosition)
        {
            this.zoneNodeID = zoneNodeID;
            this.relativePosition = relativePosition;
        }
    }
}

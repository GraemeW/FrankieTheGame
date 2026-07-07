using UnityEngine;

namespace Frankie.ZoneManagement.Editor
{
    public struct ZoneHandlerNodeData
    {
        public readonly ZoneNode zoneNode;
        public Vector2 position;

        public ZoneHandlerNodeData(ZoneNode zoneNode, Vector2 position)
        {
            this.zoneNode = zoneNode;
            this.position = position;
        }   
    }
}

namespace Frankie.ZoneManagement
{
    public struct ZoneNodePair
    {
        public ZoneNodePair(Zone zone, ZoneNode zoneNode)
        {
            this.zone = zone;
            this.zoneNode = zoneNode;
        }

        public Zone zone;
        public ZoneNode zoneNode;
    }
}
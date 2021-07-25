namespace Frankie.ZoneManagement
{
    public struct ZoneIDNodeIDPair
    {
        public ZoneIDNodeIDPair(string zoneID, string nodeID)
        {
            this.zoneID = zoneID;
            this.nodeID = nodeID;
        }

        public string zoneID;
        public string nodeID;
    }
}
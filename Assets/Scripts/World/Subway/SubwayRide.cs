using Frankie.ZoneManagement;

namespace Frankie.Control.Specialization
{
    [System.Serializable]
    public class SubwayRide
    {
        public string rideName = "Subway Stop Name";
        public ZoneHandler zoneHandler = null;
        public PatrolPath path = null;
    }
}

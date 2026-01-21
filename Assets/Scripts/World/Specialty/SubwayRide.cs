using Frankie.Control;
using Frankie.ZoneManagement;

namespace Frankie.World
{
    [System.Serializable]
    public class SubwayRide
    {
        public string rideName = "Subway Stop Name";
        public ZoneHandler zoneHandler;
        public PatrolPath path;
    }
}

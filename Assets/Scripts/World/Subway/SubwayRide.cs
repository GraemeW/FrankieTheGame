using Frankie.ZoneManagement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

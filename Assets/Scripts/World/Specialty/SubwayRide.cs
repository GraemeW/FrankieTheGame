using UnityEngine;
using UnityEngine.Localization;
using Frankie.Control;
using Frankie.ZoneManagement;
using Frankie.Utils;

namespace Frankie.World
{
    [System.Serializable]
    public class SubwayRide
    {
        [SerializeField][SimpleLocalizedString(LocalizationTableType.ChecksWorldObjects, true)] public LocalizedString localizedRideName;
        [SerializeField] public ZoneHandler zoneHandler;
        [SerializeField] public PatrolPath path;
    }
}

using UnityEngine;
using UnityEngine.Localization;
using LowDefMustard.Control;
using LowDefMustard.Localization;
using Frankie.Zones;

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

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using Frankie.Stats;
using Frankie.Utils.Localization;

namespace Frankie.Inventory.UI
{
    public readonly struct StatChangeLine
    {
        public readonly string name;
        public readonly string oldValue;
        public readonly string newValue;
        public readonly StatChangeDirection direction;

        public StatChangeLine(StatComparison statComparison)
        {
            int oldValueRounded = Mathf.RoundToInt(statComparison.oldValue);
            int newValueRounded = Mathf.RoundToInt(statComparison.newValue);

            name = LocalizationNames.GetLocalizedName(statComparison.stat);
            oldValue = oldValueRounded.ToString(CultureInfo.InvariantCulture);
            newValue = newValueRounded.ToString(CultureInfo.InvariantCulture);
            direction = StatChangeDirection.Neutral;
            if (oldValueRounded < newValueRounded) { direction = StatChangeDirection.Better; }
            else if (newValueRounded < oldValueRounded) { direction = StatChangeDirection.Worse; }
        }

        public static IReadOnlyList<StatChangeLine> GetStatChanges(BaseStats baseStats, Equipment equipment, EquipableItemBase equipableItem, EquipLocation equipLocation)
        {
            return Equipment.GetStatComparisons(baseStats, equipment, equipableItem, equipLocation).Select(statComparison => new StatChangeLine(statComparison)).ToList();
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using Frankie.Utils.Localization;

namespace Frankie.Inventory
{
    public static class EquipLocationDisplay
    {
        // Const
        private const LocalizationTableType _localizationTableType = LocalizationTableType.Inventory;
        
        // State
        private static Dictionary<EquipLocation, LocalizedString> _equipLocationNameCache;

        public static string GetLocalizedName(EquipLocation equipLocation)
        {
            _equipLocationNameCache ??= BuildNameCache();
            return _equipLocationNameCache.ContainsKey(equipLocation) ? _equipLocationNameCache[equipLocation].GetSafeLocalizedString() : equipLocation.ToString();
        }

        private static Dictionary<EquipLocation, LocalizedString> BuildNameCache()
        {
            var newEquipLocationNameCache = new Dictionary<EquipLocation, LocalizedString>();
            foreach (EquipLocation equipLocation in Enum.GetValues(typeof(EquipLocation)))
            {
                newEquipLocationNameCache.Add(equipLocation, LocalizationTool.GetLocalizedString(_localizationTableType, GetKey(equipLocation)));
            }
            return newEquipLocationNameCache;
        }
        
        private static string GetKey(EquipLocation equipLocation) => $"{nameof(EquipLocation)}.{equipLocation.ToString()}";

        public static void GenerateDefaultEntries()
        {
            // For generating default English entries (if ever need re-generation)
#if UNITY_EDITOR 
            foreach (EquipLocation equipLocation in Enum.GetValues(typeof(EquipLocation)))
            {
                TableEntryReference tableEntryReference = GetKey(equipLocation);
                string englishEquipLocationName = equipLocation.ToString();
                LocalizationTool.AddUpdateEnglishEntry(_localizationTableType, tableEntryReference, englishEquipLocationName);
            }
#endif
        }
    }
}

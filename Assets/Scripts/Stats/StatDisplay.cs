using System;
using System.Collections.Generic;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using Frankie.Utils.Localization;

namespace Frankie.Stats
{
    public static class StatDisplay
    {
        // Const
        private const LocalizationTableType _localizationTableType = LocalizationTableType.Core;
        
        // State
        private static Dictionary<Stat, LocalizedString> _statNameCache;

        public static string GetLocalizedName(Stat stat)
        {
            _statNameCache ??= BuildCache();
            return _statNameCache.ContainsKey(stat) ? _statNameCache[stat].GetSafeLocalizedString() : stat.ToString();
        }

        private static Dictionary<Stat, LocalizedString> BuildCache()
        {
            var newStatNameCache = new Dictionary<Stat, LocalizedString>();
            foreach (Stat stat in Enum.GetValues(typeof(Stat)))
            {
                newStatNameCache.Add(stat, LocalizationTool.GetLocalizedString(_localizationTableType, GetKey(stat)));
            }
            return newStatNameCache;
        }
        
        private static string GetKey(Stat stat) => $"{nameof(Stat)}.{stat.ToString()}";

        public static void GenerateDefaultEntries()
        {
            // For generating default English entries (if ever need re-generation)
#if UNITY_EDITOR 
            foreach (Stat stat in Enum.GetValues(typeof(Stat)))
            {
                TableEntryReference tableEntryReference = GetKey(stat);
                string englishStatName = stat.ToString();
                if (stat == Stat.InitialLevel) { englishStatName = "Level"; }
                LocalizationTool.AddUpdateEnglishEntry(_localizationTableType, tableEntryReference, englishStatName);
            }
#endif
        }
    }
}

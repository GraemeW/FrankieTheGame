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

        public static string GetStatDisplayName(Stat stat)
        {
            _statNameCache ??= BuildStatNameCache();
            return _statNameCache.ContainsKey(stat) ? _statNameCache[stat].GetSafeLocalizedString() : stat.ToString();
        }

        private static Dictionary<Stat, LocalizedString> BuildStatNameCache()
        {
            var newStatNameCache = new Dictionary<Stat, LocalizedString>();
            foreach (Stat stat in Enum.GetValues(typeof(Stat)))
            {
                newStatNameCache.Add(stat, LocalizationTool.GetLocalizedString(_localizationTableType, GetStatKey(stat)));
            }
            return newStatNameCache;
        }
        
        private static string GetStatKey(Stat stat) => $"{nameof(Stat)}.{stat.ToString()}";

        public static void GenerateDefaultStatLocalizedEntries()
        {
            // For generating default English entries (if ever need re-generation)
#if UNITY_EDITOR 
            foreach (Stat stat in Enum.GetValues(typeof(Stat)))
            {
                TableEntryReference tableEntryReference = GetStatKey(stat);
                string englishStatName = stat.ToString();
                if (stat == Stat.InitialLevel) { englishStatName = "Level"; }
                LocalizationTool.AddUpdateEnglishEntry(_localizationTableType, tableEntryReference, englishStatName);
            }
#endif
        }
    }
}

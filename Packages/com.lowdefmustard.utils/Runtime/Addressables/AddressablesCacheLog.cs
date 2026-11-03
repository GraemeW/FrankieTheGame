using UnityEngine;
using LowDefMustard.Core;

namespace LowDefMustard.Utils
{
    public static class AddressablesCacheLog
    {
        [HideInCallstack]
        public static void LogDuplicate(LDMLogCategory category, string label, string id, Object existing, Object incoming)
        {
            LDMLog.Error(category, $"Duplicate {label} ID '{id}' ('{existing.name}' and '{incoming.name}'). Overwriting the cached entry with '{incoming.name}'.", incoming);
        }

        [HideInCallstack]
        public static void LogBuild(LDMLogCategory category, string label, bool isLoadSucceeded, int count)
        {
            if (!isLoadSucceeded) { LDMLog.Error(category, $"Failed to load '{label}' assets from Addressables. Lookups will return nothing."); }
            else if (count == 0) { LDMLog.Warning(category, $"No '{label}' assets found in Addressables. Check they are marked Addressable with that label."); }
            else { LDMLog.Info(category, $"Built the '{label}' cache ({count} assets)."); }
        }
    }
}

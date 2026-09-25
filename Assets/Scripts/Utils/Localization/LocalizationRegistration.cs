using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Utils.Localization
{
    public static class LocalizationRegistration
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Register()
        {
#if !UNITY_EDITOR
            ManualRegister();
#endif
        }

        public static void ManualRegister()
        {
            LocalizationTool.RegisterTableCollectionNames(new Dictionary<LocalizationTableType, string>
            {
                { LocalizationTableType.ChecksWorldObjects, "ChecksWorldObjects" },
                { LocalizationTableType.Core, "Core" },
                { LocalizationTableType.Inventory, "Inventory" },
                { LocalizationTableType.Quests, "Quests" },
                { LocalizationTableType.Skills, "Skills" },
                { LocalizationTableType.Speech, "Speech" },
                { LocalizationTableType.UI, "UI" },
                { LocalizationTableType.Zones, "Zones" },
            });
        }
    }
}

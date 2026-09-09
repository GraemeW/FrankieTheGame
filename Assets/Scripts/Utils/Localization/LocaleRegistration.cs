using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Utils.Localization
{
    // Critical Note:
    // Deliberately separate from LocalizationRegistration (Editor) because this needs to be registered at runtime
    
    public static class LocaleRegistration
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Register()
        {
            LocalizationLocale.RegisterLocaleCodes(new Dictionary<SupportedLocalizationType, string>
            {
                { SupportedLocalizationType.English, "en" },
                { SupportedLocalizationType.French, "fr" },
                { SupportedLocalizationType.Japanese, "ja" }
            }, SupportedLocalizationType.English);
        }
    }
}

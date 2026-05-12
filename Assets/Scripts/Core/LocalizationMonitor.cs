using UnityEngine;
using Frankie.Saving;
using Frankie.Utils.Localization;

namespace Frankie.Core
{
    public class LocalizationMonitor : MonoBehaviour
    {
        private void Start()
        {
            if (!PlayerPrefsController.LanguageKeyExists()) { return; }
            
            string languageCode = PlayerPrefsController.GetLanguageCode();
            SupportedLocalizationType currentLocale = LocalizationTool.GetLocalizationByCode(languageCode);
            LocalizationTool.SetLocale(currentLocale);
        }
    }
}

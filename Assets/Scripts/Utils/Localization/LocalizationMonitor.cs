using UnityEngine;
using Frankie.Saving;

namespace Frankie.Utils.Localization
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

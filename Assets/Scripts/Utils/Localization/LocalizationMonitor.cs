using UnityEngine;
using LowDefMustard.Localization;
using Frankie.Saving;

namespace Frankie.Utils.Localization
{
    public class LocalizationMonitor : MonoBehaviour
    {
        private void Start()
        {
            if (!PlayerPrefsController.LanguageKeyExists()) { return; }
            
            string languageCode = PlayerPrefsController.GetLanguageCode();
            LocalizationLocale.SetLocale(languageCode);
        }
    }
}

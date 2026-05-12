using UnityEngine.Localization;

namespace Frankie.Utils.Localization
{
    public static class LocalizedStringExtensions
    {
        public static string GetSafeLocalizedString(this LocalizedString localizedString)
        {
            if (localizedString == null || localizedString.IsEmpty) { return ""; }
            return localizedString.GetLocalizedString() ?? "";
        }
    }
}

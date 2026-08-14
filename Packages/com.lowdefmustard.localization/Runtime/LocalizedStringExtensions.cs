using UnityEngine.Localization;

namespace LowDefMustard.Localization
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

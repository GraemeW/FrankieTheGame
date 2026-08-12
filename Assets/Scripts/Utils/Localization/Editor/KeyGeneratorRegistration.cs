using LowDefMustard.Localization;
using UnityEditor;

namespace Frankie.Utils.Localization.Editor
{
    [InitializeOnLoad]
    public static class KeyGeneratorRegistration
    {
        static KeyGeneratorRegistration()
        {
            SimpleLocalizedStringDrawer.TypeSpecificKeyGenerator = LocalizationNames.GenerateTypeSpecificKey;
        }
    }
}

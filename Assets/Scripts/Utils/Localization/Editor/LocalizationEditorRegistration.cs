using UnityEditor;
using LowDefMustard.Zones;
using LowDefMustard.Localization;
using LowDefMustard.Localization.Editor;

namespace Frankie.Utils.Localization.Editor
{
    [InitializeOnLoad]
    public static class LocalizationEditorRegistration
    {
        static LocalizationEditorRegistration()
        {
            LocalizationRegistration.ManualRegister();
            
            LocalizableClassTableTypeRegistry.Register(typeof(Zone), LocalizationTableType.Zones);
            LocalizableClassTableTypeRegistry.Register(typeof(ZoneNode), LocalizationTableType.Zones);
            
            SimpleLocalizedStringDrawer.typeSpecificKeyGenerator = LocalizationNames.GenerateTypeSpecificKey;
        }
    }
}

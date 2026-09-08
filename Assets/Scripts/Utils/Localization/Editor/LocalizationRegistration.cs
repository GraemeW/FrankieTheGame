using System.Collections.Generic;
using UnityEditor;
using LowDefMustard.Zones;
using LowDefMustard.Localization;
using LowDefMustard.Localization.Editor;

namespace Frankie.Utils.Localization.Editor
{
    [InitializeOnLoad]
    public static class LocalizationRegistration
    {
        static LocalizationRegistration()
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
            
            LocalizableClassTableTypeRegistry.Register(typeof(Zone), LocalizationTableType.Zones);
            LocalizableClassTableTypeRegistry.Register(typeof(ZoneNode), LocalizationTableType.Zones);
            
            SimpleLocalizedStringDrawer.typeSpecificKeyGenerator = LocalizationNames.GenerateTypeSpecificKey;
        }
    }
}

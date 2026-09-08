using System;
using System.Collections.Generic;

namespace LowDefMustard.Localization
{
    // Resolves ILocalizationToolBridge for a closed LocalizationToolBase<T>, given only T's runtime
    // Required since LocalizationDeletionHandler/SimpleLocalizedStringAttribute have no compile-time knowledge of which T a given project uses
    // AssetModificationProcessor discovery breaks if any generic type sits in its inheritance hierarchy
    
    public static class LocalizationToolBridgeRegistry
    {
        // State
        private static readonly Dictionary<Type, ILocalizationToolBridge> _registeredBridges = new();

        public static void Register(Type tableTypeEnum, ILocalizationToolBridge bridge)
        {
#if UNITY_EDITOR
            _registeredBridges[tableTypeEnum] = bridge;
#endif
        }

        public static bool TryGetBridge(Type tableTypeEnum, out ILocalizationToolBridge bridge)
        {
            bridge = null;
#if !UNITY_EDITOR
            tableTypeEnum = null;
#endif
            return tableTypeEnum != null && _registeredBridges.TryGetValue(tableTypeEnum, out bridge);
        }
    }
}

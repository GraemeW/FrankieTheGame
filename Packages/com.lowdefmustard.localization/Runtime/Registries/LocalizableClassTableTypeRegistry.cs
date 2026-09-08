using System;
using System.Collections.Generic;
using UnityEngine;

namespace LowDefMustard.Localization
{
    // Maps a CLR Type (an ILocalizableCore-implementing class) directly to a table-type enum value,
    //  - required for implementers that can't carry T generically
    public static class LocalizableClassTableTypeRegistry
    {
        // State
        private static readonly Dictionary<Type, Enum> _registeredTableTypes = new();

        public static void Register(Type owningType, Enum tableType)
        {
            if (owningType == null) { Debug.LogWarning($"{nameof(LocalizableClassTableTypeRegistry)}.{nameof(Register)} called with a null owningType."); return; }
            if (tableType == null) { Debug.LogWarning($"{nameof(LocalizableClassTableTypeRegistry)}.{nameof(Register)} called with a null tableType for '{owningType.Name}'."); return; }

            _registeredTableTypes[owningType] = tableType;
        }
        
        public static Enum GetTableType(Type owningType)
        {
            if (owningType != null && _registeredTableTypes.TryGetValue(owningType, out Enum tableType)) { return tableType; }
            return null;
        }
    }
}

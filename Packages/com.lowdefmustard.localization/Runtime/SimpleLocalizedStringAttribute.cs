using System;
using UnityEngine;

namespace LowDefMustard.Localization
{
    public class SimpleLocalizedStringAttribute : PropertyAttribute
    {
        // Deliberately non-generic:  a generic class can never derive from System.Attribute
        
        public Enum localizationTableType { get; }
        public bool isKeyEditable { get; }

        // Note:
        //  - localizationTableType must be a caller-defined `enum : struct, Enum` value
        //  - Must match the TTableType a matching LocalizationToolBase<TTableType>-derived alias was configured with
        public SimpleLocalizedStringAttribute(object localizationTableType, bool isKeyEditable)
        {
            if (localizationTableType is not Enum enumValue)
            {
                throw new ArgumentException($"{nameof(localizationTableType)} must be an enum value.", nameof(localizationTableType));
            }

            this.localizationTableType = enumValue;
            this.isKeyEditable = isKeyEditable;
        }

        // Constructor for fields on a class that can't reference a project-specific enum type directly
        //  - e.g. a shared package - see LocalizableClassTableTypeRegistry
        //  - drawer falls back to resolving the table type at runtime instead of via this attribute
        public SimpleLocalizedStringAttribute(bool isKeyEditable)
        {
            localizationTableType = null;
            this.isKeyEditable = isKeyEditable;
        }
    }
}

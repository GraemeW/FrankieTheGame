using UnityEngine;

namespace Frankie.Utils
{
    public class SimpleLocalizedStringAttribute : PropertyAttribute
    {
        public LocalizationTableType localizationTableType { get; }
        public bool isKeyEditable { get; }

        public SimpleLocalizedStringAttribute(LocalizationTableType localizationTableType, bool isKeyEditable)
        {
            this.localizationTableType = localizationTableType;
            this.isKeyEditable = isKeyEditable;
        }
    }
}

using UnityEngine;

namespace Frankie.Utils
{
    public class SimpleLocalizedStringAttribute : PropertyAttribute
    {
        public LocalizationTableType localizationTableType { get; }

        public SimpleLocalizedStringAttribute(LocalizationTableType localizationTableType)
        {
            this.localizationTableType = localizationTableType;
        }
    }
}

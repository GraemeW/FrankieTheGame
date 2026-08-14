using UnityEngine;

namespace LowDefMustard.Utils
{
    public class RestrictedEnumAttribute : PropertyAttribute
    {
        public readonly int[] hiddenValues;

        public RestrictedEnumAttribute(params int[] setHiddenValues)
        {
            hiddenValues = setHiddenValues;
        }
    }
}

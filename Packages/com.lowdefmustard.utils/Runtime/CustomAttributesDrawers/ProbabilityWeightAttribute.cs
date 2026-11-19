using UnityEngine;

namespace LowDefMustard.Utils
{
    public class ProbabilityWeightAttribute : PropertyAttribute
    {
        public readonly int min;
        public readonly bool isEmptyValueSkipped;

        public ProbabilityWeightAttribute(int min = 0, bool isEmptyValueSkipped = false)
        {
            this.min = min;
            this.isEmptyValueSkipped = isEmptyValueSkipped;
        }
    }
}

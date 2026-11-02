using System;

namespace LowDefMustard.Rendering
{
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class BattleEffectAxisLabelsWhenAttribute : Attribute
    {
        // Note:
        //  - Inspector only:  relabels a Vector2 range's axes while a sibling (enum / int) setting holds the value

        // State
        public string fieldName { get; }
        public int value { get; }
        public string xLabel { get; }
        public string yLabel { get; }

        // Constructor
        public BattleEffectAxisLabelsWhenAttribute(string fieldName, int value, string xLabel, string yLabel)
        {
            this.fieldName = fieldName;
            this.value = value;
            this.xLabel = xLabel;
            this.yLabel = yLabel;
        }
    }
}

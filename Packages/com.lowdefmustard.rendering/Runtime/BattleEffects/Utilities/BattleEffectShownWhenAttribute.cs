using System;
using System.Linq;

namespace LowDefMustard.Rendering
{
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class BattleEffectShownWhenAttribute : Attribute
    {
        // Note:
        //  - Inspector only:  shows a shape setting only while another of the shape's (enum / int) settings holds one of the values

        // State
        public string fieldName { get; }
        private readonly int[] values;

        // Constructor
        public BattleEffectShownWhenAttribute(string fieldName, params int[] values)
        {
            this.fieldName = fieldName;
            this.values = values;
        }

        // Public Methods
        public bool IsShownFor(int value) => values.Contains(value);
    }
}

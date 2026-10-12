using System;
using UnityEngine.UIElements;

namespace LowDefMustard.UIBox
{
    public sealed class SeparatorEntryHandle : EntryHandle
    {
        // State
        private readonly ChoiceSeparatorType separatorType;

        // Constructor
        public SeparatorEntryHandle(UIToolkitBoxView view, Type containerType, ChoiceSeparatorType separatorType) : base(view, containerType)
        {
            this.separatorType = separatorType;
        }

        // Overrides
        protected override VisualElement CreateElement() => new ChoiceSeparatorElement { separatorType = separatorType };
    }
}

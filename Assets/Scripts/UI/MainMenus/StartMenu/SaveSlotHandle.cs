using System;
using UnityEngine.UIElements;
using LowDefMustard.UIBox;

namespace Frankie.Menu.UI
{
    public sealed class SaveSlotHandle : ChoiceHandle
    {
        // State
        private readonly SaveSlotModel model;

        // Constructor
        public SaveSlotHandle(UIToolkitBoxView view, SaveSlotModel model, Action onChoose) : base(view, onChoose)
        {
            this.model = model;
        }

        // Overrides
        protected override VisualElement CreateChoiceElement() => new SaveSlotElement { dataSource = model };
        protected override void SetHighlighted(bool enable) => model.isHighlighted = enable;
        public override void SetText(string text) => model.characterName = text;
    }
}

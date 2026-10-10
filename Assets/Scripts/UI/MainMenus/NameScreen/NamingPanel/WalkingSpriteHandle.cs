using System;
using UnityEngine.UIElements;
using LowDefMustard.UIBox;
using Frankie.Utils.UI;

namespace Frankie.Menu.UI
{
    public sealed class WalkingSpriteHandle : EntryHandle
    {
        // State
        private readonly SpriteModel spriteModel;
        public float offStageFraction { get; private set; } // 0: on stage, 1: fully off-stage

        // Constructor
        public WalkingSpriteHandle(UIToolkitBoxView view, SpriteModel spriteModel, Type containerType, float offStageFraction) : base(view, containerType)
        {
            this.spriteModel = spriteModel;
            this.offStageFraction = offStageFraction;
        }

        // Overrides
        protected override VisualElement CreateElement()
        {
            var spriteElement = new SpriteElement { dataSource = spriteModel };
            ApplyOffset(spriteElement);
            return spriteElement;
        }

        #region PublicMethods
        public void SetOffStageFraction(float setOffStageFraction)
        {
            offStageFraction = setOffStageFraction;
            if (element != null) { ApplyOffset(element); }
        }
        #endregion

        #region PrivateMethods
        private void ApplyOffset(VisualElement spriteElement)
        {
            spriteElement.style.translate = new Translate(Length.Percent(-100f * offStageFraction), 0f);
        }
        #endregion
    }
}

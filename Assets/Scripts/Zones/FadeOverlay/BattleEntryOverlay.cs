using Unity.Properties;
using UnityEngine.UIElements;
using LowDefMustard.UIBox;

namespace Frankie.Zones.UI
{
    [UxmlElement]
    public sealed partial class BattleEntryOverlay : VisualElement
    {
        // Note:
        //  - Overlay look is defined in USS (see:  .battle-entry-overlay) via shader material
        //      - Default is:  a backdrop filter that twirls everything rendered behind this element
        //  - The filter animates itself on shader time ~ this element only shows / hides with the fade

        // Const
        private const string _ussClassName = "battle-entry-overlay";

        // State
        private bool internalIsActive;

        [CreateProperty] public bool isActive
        {
            get => internalIsActive;
            set
            {
                internalIsActive = value;
                ApplyIsActive();
            }
        }

        public BattleEntryOverlay()
        {
            AddToClassList(_ussClassName);
            pickingMode = PickingMode.Ignore;
            ApplyIsActive();
            SetBinding(nameof(isActive), UIToolkitBindings.ToTarget(nameof(FaderModel.isBattleEntryActive)));
        }

        private void ApplyIsActive()
        {
            style.display = internalIsActive ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}

using Unity.Properties;
using UnityEngine.UIElements;
using LowDefMustard.UIBox;

namespace Frankie.Inventory.UI
{
    [UxmlElement]
    public sealed partial class StatChangeSection : VisualElement
    {
        // Const Tunables
        private const string _ussClassName = "stat-change-section";
        private const string _emptyUssClassName = _ussClassName + "--empty";

        // State
        private bool internalHasStatChanges = false;

        [CreateProperty] public bool hasStatChanges
        {
            get => internalHasStatChanges;
            set
            {
                internalHasStatChanges = value;
                EnableInClassList(_emptyUssClassName, !value);
            }
        }

        public StatChangeSection()
        {
            AddToClassList(_ussClassName);
            AddToClassList(_emptyUssClassName);
            pickingMode = PickingMode.Ignore;
            SetBinding(nameof(hasStatChanges), UIToolkitBindings.ToTarget(nameof(ItemBoxModel.hasStatChanges)));
        }
    }
}

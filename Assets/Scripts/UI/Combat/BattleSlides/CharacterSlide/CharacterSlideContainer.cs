using UnityEngine.UIElements;

namespace Frankie.Combat.UI
{
    [UxmlElement]
    public sealed partial class CharacterSlideContainer : VisualElement
    {
        // Const Tunables
        private const string _ussClassName = "character-slides";

        public CharacterSlideContainer()
        {
            AddToClassList(_ussClassName);
            pickingMode = PickingMode.Ignore;
        }
    }
}

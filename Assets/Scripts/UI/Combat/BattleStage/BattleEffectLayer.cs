using UnityEngine.UIElements;

namespace Frankie.Combat.UI
{
    [UxmlElement]
    public sealed partial class BattleEffectLayer : VisualElement
    {
        // Holds targeted battle effects (i.e. over an enemy slide)
        
        // Const Tunables
        private const string _ussClassName = "battle-effects";

        public BattleEffectLayer()
        {
            AddToClassList(_ussClassName);
            pickingMode = PickingMode.Ignore;
        }
    }
}

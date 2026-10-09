using UnityEngine.UIElements;

namespace Frankie.Combat.UI
{
    [UxmlElement]
    public sealed partial class BattleScreenEffectLayer : VisualElement
    {
        // Holds full-screen battle effects
        //  - Sits over BattleUI incl. background bands + party/enemy slides)
        //  - Does *not* sit over overlay boxes (options, skills, messages)

        // Const Tunables
        private const string _ussClassName = "battle-screen-effects";

        public BattleScreenEffectLayer()
        {
            AddToClassList(_ussClassName);
            pickingMode = PickingMode.Ignore;
        }
    }
}

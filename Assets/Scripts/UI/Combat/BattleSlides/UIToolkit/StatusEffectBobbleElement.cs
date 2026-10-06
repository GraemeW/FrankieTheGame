using UnityEngine.UIElements;
using Frankie.Stats;

namespace Frankie.Combat.UI
{
    public sealed class StatusEffectBobbleElement : VisualElement
    {
        // Const Tunables
        private const string _ussClassName = "status-effect-bobble";
        private const string _iconUssClassName = _ussClassName + "__icon";
        private const string _modifierUssClassName = _ussClassName + "__modifier";
        private const string _increaseUssClassName = _ussClassName + "--increase";
        private const string _healingUssClassName = _ussClassName + "--healing";
        private const string _damageUssClassName = _ussClassName + "--damage";
        private const string _brawnUssClassName = _ussClassName + "--brawn";
        private const string _beautyUssClassName = _ussClassName + "--beauty";
        private const string _smartsUssClassName = _ussClassName + "--smarts";
        private const string _nimbleUssClassName = _ussClassName + "--nimble";
        private const string _luckUssClassName = _ussClassName + "--luck";
        private const string _pluckUssClassName = _ussClassName + "--pluck";
        private const string _stoicUssClassName = _ussClassName + "--stoic";

        public StatusEffectBobbleElement(Stat statusEffectType, bool isIncrease)
        {
            AddToClassList(_ussClassName);
            pickingMode = PickingMode.Ignore;
            EnableInClassList(_increaseUssClassName, isIncrease);
            string iconUssClassName = GetIconUssClassName(statusEffectType, isIncrease);
            if (iconUssClassName != null) { AddToClassList(iconUssClassName); }

            AddPart(_iconUssClassName);
            AddPart(_modifierUssClassName);
        }

        #region PrivateMethods
        private static string GetIconUssClassName(Stat statusEffectType, bool isIncrease)
        {
            return statusEffectType switch
            {
                Stat.HP => isIncrease ? _healingUssClassName : _damageUssClassName,
                Stat.Brawn => _brawnUssClassName,
                Stat.Beauty => _beautyUssClassName,
                Stat.Smarts => _smartsUssClassName,
                Stat.Nimble => _nimbleUssClassName,
                Stat.Luck => _luckUssClassName,
                Stat.Pluck => _pluckUssClassName,
                Stat.Stoic => _stoicUssClassName,
                _ => null // No icon (e.g. AP), modifier only
            };
        }

        private void AddPart(string partUssClassName)
        {
            var part = new VisualElement { pickingMode = PickingMode.Ignore };
            part.AddToClassList(partUssClassName);
            Add(part);
        }
        #endregion
    }
}

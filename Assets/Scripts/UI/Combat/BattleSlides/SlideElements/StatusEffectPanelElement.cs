using System.Collections.Generic;
using Unity.Properties;
using UnityEngine.UIElements;
using LowDefMustard.UIBox;

namespace Frankie.Combat.UI
{
    public sealed class StatusEffectPanelElement : VisualElement
    {
        // Const Tunables
        private const string _ussClassName = "status-effect-panel";
        private const int _maxStatusEffectsShown = 8;

        // State
        private IReadOnlyList<PersistentStatus> internalStatusEffects;

        [CreateProperty] public IReadOnlyList<PersistentStatus> statusEffects
        {
            get => internalStatusEffects;
            set
            {
                internalStatusEffects = value;
                Rebuild();
            }
        }

        public StatusEffectPanelElement(string sourcePropertyName)
        {
            AddToClassList(_ussClassName);
            pickingMode = PickingMode.Ignore;
            SetBinding(nameof(statusEffects), UIToolkitBindings.ToTarget(sourcePropertyName));
        }

        private void Rebuild()
        {
            Clear();
            if (internalStatusEffects == null) { return; }

            int shownCount = 0;
            foreach (PersistentStatus persistentStatus in internalStatusEffects)
            {
                if (persistentStatus == null) { continue; }
                if (shownCount >= _maxStatusEffectsShown) { break; }
                Add(new StatusEffectBobbleElement(persistentStatus.GetStatusEffectType(), persistentStatus.IsIncrease()));
                shownCount++;
            }
        }
    }
}

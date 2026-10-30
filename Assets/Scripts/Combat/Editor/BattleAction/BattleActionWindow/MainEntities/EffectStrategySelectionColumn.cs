using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace Frankie.Combat.Editor
{
    public sealed class EffectStrategySelectionColumn : VisualElement
    {
        // Const Tunables
        private const string _titleText = "Selected Effect Strategies";
        private const string _subtitleText = "from the battle action's slots";
        private const string _emptySelectionHint = "Select effect strategy slots in the battle action (shift / ctrl for several) to edit them here";

        // Element References
        private readonly VisualElement entries;
        private readonly Label selectionHint;

        // State
        private readonly List<(int index, EffectStrategy strategy)> shownSlots = new();
        private readonly Action changed;

        // Constructor
        public EffectStrategySelectionColumn(Action changed)
        {
            this.changed = changed;
            style.flexGrow = 1f;
            BattleActionWindowStyles.ApplyDependentColumn(this);
            Add(BattleActionWindowStyles.CreateColumnHeader(_titleText, _subtitleText, out _));

            selectionHint = BattleActionWindowStyles.CreateHint(_emptySelectionHint);
            entries = new VisualElement();
            Add(BuildScrollView(selectionHint, entries));
        }

        #region PublicMethods
        public void SetSlots(IReadOnlyList<(int index, EffectStrategy strategy)> slots)
        {
            // Note:  Rebuilt only when the shown slots change ~ would otherwise drop focus mid-edit
            if (slots.SequenceEqual(shownSlots)) { return; }
            shownSlots.Clear();
            shownSlots.AddRange(slots);

            entries.Clear();
            foreach ((int index, EffectStrategy strategy) in shownSlots)
            {
                var element = new EffectStrategyElement(strategy, $"#{index}", changed) { style = { marginTop = BattleActionWindowStyles.sectionSpacing / 2f } };
                entries.Add(element);
            }
            selectionHint.style.display = shownSlots.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void RefreshNames() => entries.Query<EffectStrategyElement>().ForEach(element => element.RefreshName());
        #endregion

        #region GUI
        private static ScrollView BuildScrollView(Label setSelectionHint, VisualElement setEntries)
        {
            ScrollView scrollView = BattleActionWindowStyles.CreateColumnScrollView();
            scrollView.Add(setSelectionHint);
            scrollView.Add(setEntries);
            return scrollView;
        }
        #endregion
    }
}

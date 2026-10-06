using System;
using System.Collections.Generic;
using LowDefMustard.UIBox;
using Frankie.Combat;

namespace Frankie.Stats.UI
{
    public sealed class PartySelector
    {
        // Party header shared by character-driven boxes
        // Note:  Box still supplies the choice factory (e.g. UIBoxBase.AddNonDestroyChoiceOption), so choices keep box-level cursor + sounds
        
        // Actions
        public delegate IUIChoice ChoiceFactory(string choiceText, Action onChoose, Action onHighlight);

        // State
        private readonly List<CombatParticipant> internalCharacters = new();
        private readonly List<BattleEntity> internalBattleEntities = new();
        private readonly List<IUIChoice> internalChoices = new();

        // Constructor
        public PartySelector(PartyCombatConduit partyCombatConduit, ChoiceFactory choiceFactory, Action<CombatParticipant> onChoose, Action<CombatParticipant> onHighlight)
        {
            foreach (CombatParticipant character in partyCombatConduit.GetPartyCombatParticipants())
            {
                internalCharacters.Add(character);
                internalBattleEntities.Add(new BattleEntity(character));
                internalChoices.Add(choiceFactory(character.GetCombatName(), () => onChoose?.Invoke(character), () => onHighlight?.Invoke(character)));
            }
        }

        #region PublicMethods
        public IReadOnlyList<CombatParticipant> characters => internalCharacters;
        public IReadOnlyList<BattleEntity> battleEntities => internalBattleEntities; // For targeting (unused by display-only boxes)
        public IReadOnlyList<IUIChoice> choices => internalChoices;
        public CombatParticipant firstCharacter => internalCharacters.Count > 0 ? internalCharacters[0] : null;
        public bool isSolo => internalCharacters.Count == 1;
        #endregion
    }
}

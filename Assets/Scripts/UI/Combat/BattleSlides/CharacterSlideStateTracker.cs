namespace Frankie.Combat.UI
{
    public sealed class CharacterSlideStateTracker
    {
        // State
        private CharacterSlideState state = CharacterSlideState.Ready;
        private CharacterSlideState stateBeforeTarget = CharacterSlideState.Ready;

        public void Set(CharacterSlideState setState) => state = setState;

        public void SetSelected(CombatParticipant combatParticipant, BattleEntitySelectionType selectionType, bool enable)
        {
            switch (selectionType)
            {
                case BattleEntitySelectionType.Actor when combatParticipant.IsDead():
                    state = CharacterSlideState.Dead;
                    break;
                case BattleEntitySelectionType.Actor when combatParticipant.IsInCooldown():
                    state = CharacterSlideState.Cooldown;
                    break;
                case BattleEntitySelectionType.Actor when enable:
                    state = CharacterSlideState.Selected;
                    break;
                case BattleEntitySelectionType.Actor:
                    state = CharacterSlideState.Ready;
                    break;
                case BattleEntitySelectionType.Target when enable:
                    stateBeforeTarget = state;
                    state = CharacterSlideState.Target;
                    break;
                case BattleEntitySelectionType.Target:
                    state = stateBeforeTarget;
                    break;
            }
        }

        // Dead characters bypass the states that don't override the dead highlight
        public CharacterSlideState GetDisplayState(CombatParticipant combatParticipant)
        {
            if (combatParticipant.IsDead() && state is CharacterSlideState.Ready or CharacterSlideState.Selected or CharacterSlideState.Cooldown) { return CharacterSlideState.Dead; }
            return state;
        }
    }
}

using System;
using Unity.Properties;
using LowDefMustard.UIBox;

namespace Frankie.Combat.UI
{
    public sealed class SkillHandlerModel : BindableModel, IDisposable
    {
        // Live readout of a character's skill selection (current branch + active skill), shared by every screen that shows it
        // Note:  Values are raw (skills, not display text); a direction is null when it holds no usable skill (trait level, remaining AP)

        // State
        private Skill internalUpSkill;
        private Skill internalLeftSkill;
        private Skill internalRightSkill;
        private Skill internalDownSkill;
        private Skill internalActiveSkill;
        private bool isDisposed = false;

        // Cached References
        private readonly CombatParticipant combatParticipant;

        // Constructor
        public SkillHandlerModel(SkillHandler skillHandler)
        {
            this.skillHandler = skillHandler;
            skillHandler.TryGetComponent(out combatParticipant);
            Refresh();

            skillHandler.skillSelectionChanged += Refresh;
            if (combatParticipant != null) { combatParticipant.SubscribeToStateUpdates(HandleStateAltered); }
        }

        #region BoundProperties
        [CreateProperty] public Skill upSkill
        {
            get => internalUpSkill;
            private set => SetProperty(ref internalUpSkill, value);
        }

        [CreateProperty] public Skill leftSkill
        {
            get => internalLeftSkill;
            private set => SetProperty(ref internalLeftSkill, value);
        }

        [CreateProperty] public Skill rightSkill
        {
            get => internalRightSkill;
            private set => SetProperty(ref internalRightSkill, value);
        }

        [CreateProperty] public Skill downSkill
        {
            get => internalDownSkill;
            private set => SetProperty(ref internalDownSkill, value);
        }

        [CreateProperty] public Skill activeSkill
        {
            get => internalActiveSkill;
            private set => SetProperty(ref internalActiveSkill, value);
        }
        #endregion

        #region PublicMethods
        public SkillHandler skillHandler { get; }

        public void Dispose()
        {
            if (isDisposed) { return; }
            isDisposed = true;

            if (skillHandler != null) { skillHandler.skillSelectionChanged -= Refresh; }
            if (combatParticipant != null) { combatParticipant.UnsubscribeToStateUpdates(HandleStateAltered); }
        }
        #endregion

        #region PrivateMethods
        // Skill availability depends on remaining AP, not only on the current branch
        private void HandleStateAltered(StateAlteredInfo stateAlteredInfo)
        {
            if (stateAlteredInfo.stateAlteredType is StateAlteredType.IncreaseAP or StateAlteredType.DecreaseAP or StateAlteredType.AdjustAPNonSpecific) { Refresh(); }
        }

        private void Refresh()
        {
            if (skillHandler == null || !skillHandler.HasSkillTree()) { return; }

            skillHandler.GetPlayerSkillsForCurrentBranch(out Skill up, out Skill left, out Skill right, out Skill down);
            upSkill = up;
            leftSkill = left;
            rightSkill = right;
            downSkill = down;
            activeSkill = skillHandler.GetActiveSkill();
        }
        #endregion
    }
}

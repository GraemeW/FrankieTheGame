using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using LowDefMustard.UIBox;
using Frankie.Utils.Localization;

namespace Frankie.Combat.UI
{
    public sealed class CharacterSlideHandle : EntryHandle, ICharacterSlide
    {
        // UI Toolkit character slide:
        //  - character values come from a CombatParticipantModel
        //  - slide-only state (highlight, cooldown, effects) from the slide model
        // Note:  Call Release() when the hosting box is done with the slide to dispose character model
        
        // State
        private readonly BattleEntity battleEntity;
        private readonly CombatParticipantModel characterModel;
        private readonly CharacterSlideModel model = new();
        private readonly CharacterSlideStateTracker stateTracker = new();
        private readonly List<Action> clickActions = new();
        private bool isReleased = false;

        // Constructor
        public CharacterSlideHandle(UIToolkitBoxView view, BattleEntity battleEntity, CharacterSlideText slideText) : base(view, typeof(CharacterSlideContainer))
        {
            this.battleEntity = battleEntity;
            slideText?.ApplyTo(model);

            characterModel = new CombatParticipantModel(battleEntity.combatParticipant);
            characterModel.stateAltered += ParseState;
            UpdateSlideState();
        }

        #region EntryHandle
        protected override VisualElement CreateElement()
        {
            var characterSlideElement = new CharacterSlideElement { dataSource = model, characterDataSource = characterModel };
            characterSlideElement.clicked += HandleClicked;
            return characterSlideElement;
        }

        protected override void UnhookElement(VisualElement detachingElement)
        {
            if (detachingElement is CharacterSlideElement characterSlideElement) { characterSlideElement.clicked -= HandleClicked; }
        }
        #endregion

        #region PublicMethods
        public void Release()
        {
            if (isReleased) { return; }
            isReleased = true;

            characterModel.stateAltered -= ParseState;
            characterModel.Dispose();
            clickActions.Clear();
            Remove();
        }
        #endregion

        #region ICharacterSlide
        public BattleEntity GetBattleEntity() => battleEntity;

        public void HighlightSlide(BattleEntitySelectionType selectionType, IEnumerable<BattleEntity> battleEntities)
        {
            // Note:  Always clear before (re-)highlighting - the tracker restores its pre-target state on clear (as per BattleSlide)
            HighlightSlide(selectionType, false);
            if (battleEntities != null && battleEntities.Any(target => target.combatParticipant == battleEntity.combatParticipant)) { HighlightSlide(selectionType, true); }
        }

        public void HighlightSlide(BattleEntitySelectionType selectionType, bool enable)
        {
            stateTracker.SetSelected(battleEntity.combatParticipant, selectionType, enable);
            UpdateSlideState();
        }

        public void AddButtonClickEvent(Action action)
        {
            if (action != null) { clickActions.Add(action); }
        }

        public void RemoveButtonClickEvents() => clickActions.Clear();
        #endregion

        #region PrivateMethods
        private void HandleClicked()
        {
            if (isReleased) { return; }
            foreach (Action clickAction in clickActions.ToList()) { clickAction.Invoke(); }
        }

        private void UpdateSlideState() => model.slideState = stateTracker.GetDisplayState(battleEntity.combatParticipant);

        // Slide-only reactions - character values (HP/AP, status effects) are already updated on the character model
        private void ParseState(StateAlteredInfo stateAlteredInfo)
        {
            CombatParticipant combatParticipant = battleEntity.combatParticipant;
            switch (stateAlteredInfo.stateAlteredType)
            {
                case StateAlteredType.CooldownSet:
                    stateTracker.Set(CharacterSlideState.Cooldown);
                    model.cooldown = CooldownTiming.Restart(stateAlteredInfo.points);
                    UpdateSlideState();
                    break;
                case StateAlteredType.CooldownExpired:
                    stateTracker.Set(CharacterSlideState.Ready);
                    model.cooldown = CooldownTiming.Restart(0f);
                    UpdateSlideState();
                    break;
                case StateAlteredType.IncreaseHP:
                case StateAlteredType.DecreaseHP:
                case StateAlteredType.AdjustHPNonSpecific:
                {
                    float points = stateAlteredInfo.points;
                    model.QueueDamageText(new DamageTextData(DamageTextType.HealthChanged, points));
                    if (stateAlteredInfo.stateAlteredType == StateAlteredType.DecreaseHP)
                    {
                        model.Shake(points > combatParticipant.GetHP());
                        model.BlipDim();
                    }
                    break;
                }
                case StateAlteredType.AdjustAPNonSpecific:
                    model.QueueDamageText(new DamageTextData(DamageTextType.APChanged, stateAlteredInfo.points));
                    break;
                case StateAlteredType.HitMiss:
                    model.QueueDamageText(new DamageTextData(DamageTextType.HitMiss));
                    break;
                case StateAlteredType.HitCrit:
                    model.QueueDamageText(new DamageTextData(DamageTextType.HitCrit));
                    break;
                case StateAlteredType.StatusEffectApplied:
                {
                    PersistentStatus persistentStatus = stateAlteredInfo.persistentStatus;
                    if (persistentStatus == null) { break; }

                    string statusEffectText = LocalizationNames.GetStatusEffectText(persistentStatus.GetStatusEffectType(), persistentStatus.IsIncrease());
                    if (!string.IsNullOrWhiteSpace(statusEffectText)) { model.QueueDamageText(new DamageTextData(DamageTextType.Informational, statusEffectText)); }
                    break;
                }
                case StateAlteredType.Dead:
                    stateTracker.Set(CharacterSlideState.Dead);
                    UpdateSlideState();
                    break;
                case StateAlteredType.Resurrected:
                    stateTracker.Set(CharacterSlideState.Ready);
                    UpdateSlideState();
                    break;
                case StateAlteredType.ActionDequeued:
                    model.BlipGrow();
                    break;
            }
        }
        #endregion
    }
}

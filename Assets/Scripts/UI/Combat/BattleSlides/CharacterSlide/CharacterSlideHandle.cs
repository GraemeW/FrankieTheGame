using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using LowDefMustard.UIBox;
using Frankie.Utils.Localization;

namespace Frankie.Combat.UI
{
    public sealed class CharacterSlideHandle : EntryHandle
    {
        // Data Models:
        //  - character values (hp/ap, status effects) come from CombatParticipantModel
        //  - slide-only state (targeting, cooldown, effects) from the slide model
        // Note:  Call Release() when the hosting box is done with the slide to dispose character model
        
        // State
        private readonly BattleEntity battleEntity;
        private readonly CombatParticipantModel characterModel;
        private readonly CharacterSlideModel model = new();
        private bool isSelected = false;
        private bool isTargeted = false;
        private readonly List<Action> clickActions = new();
        private bool isReleased = false;

        // Constructor
        public CharacterSlideHandle(UIToolkitBoxView view, BattleEntity battleEntity, BattleSlideText slideText) : base(view, typeof(CharacterSlideContainer))
        {
            this.battleEntity = battleEntity;
            if (slideText != null) { slideText.ApplyTo(model); }
            if (!TryGetCharacter(out CombatParticipant character)) { isReleased = true; return; } // Nothing to show or listen to

            characterModel = new CombatParticipantModel(character);
            characterModel.stateAltered += ParseState;
            RefreshSlideState();
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

            if (characterModel != null)
            {
                characterModel.stateAltered -= ParseState;
                characterModel.Dispose();
            }
            clickActions.Clear();
            Remove();
        }
        #endregion

        #region SlideMethods
        public BattleEntity GetBattleEntity() => battleEntity;

        public void HighlightSlide(BattleEntitySelectionType selectionType, IEnumerable<BattleEntity> battleEntities)
        {
            bool isIncluded = TryGetCharacter(out CombatParticipant character) && battleEntities != null && battleEntities.Any(target => target != null && target.combatParticipant == character);
            HighlightSlide(selectionType, isIncluded);
        }

        public void HighlightSlide(BattleEntitySelectionType selectionType, bool enable)
        {
            switch (selectionType)
            {
                case BattleEntitySelectionType.Actor:
                    isSelected = enable;
                    break;
                case BattleEntitySelectionType.Target:
                    isTargeted = enable;
                    break;
            }
            RefreshSlideState();
        }

        public void AddButtonClickEvent(Action action) { if (action != null) { clickActions.Add(action); } }
        public void RemoveButtonClickEvents() => clickActions.Clear();
        #endregion

        #region PrivateMethods
        private void HandleClicked()
        {
            if (isReleased) { return; }
            foreach (Action clickAction in clickActions.ToList()) { clickAction.Invoke(); }
        }

        private bool TryGetCharacter(out CombatParticipant character)
        {
            character = battleEntity?.combatParticipant;
            return character != null;
        }

        // Displayed state priority order:  targeted > dead > selected (only while able to act) > cooldown > ready
        private void RefreshSlideState()
        {
            if (isReleased || !TryGetCharacter(out CombatParticipant character)) { return; }

            if (isTargeted) { model.slideState = CharacterSlideState.Target; }
            else if (character.IsDead()) { model.slideState = CharacterSlideState.Dead; }
            else if (character.IsInCooldown()) { model.slideState = CharacterSlideState.Cooldown; }
            else { model.slideState = isSelected ? CharacterSlideState.Selected : CharacterSlideState.Ready; }
        }

        // Slide-only reactions - character values (HP/AP, status effects) are already updated on the character model
        private void ParseState(StateAlteredInfo stateAlteredInfo)
        {
            if (isReleased || !TryGetCharacter(out CombatParticipant character)) { return; }

            switch (stateAlteredInfo.stateAlteredType)
            {
                case StateAlteredType.CooldownSet:
                    model.cooldown = CooldownTiming.Restart(stateAlteredInfo.points);
                    RefreshSlideState();
                    break;
                case StateAlteredType.CooldownExpired:
                    model.cooldown = CooldownTiming.Restart(0f);
                    RefreshSlideState();
                    break;
                case StateAlteredType.IncreaseHP:
                case StateAlteredType.DecreaseHP:
                case StateAlteredType.AdjustHPNonSpecific:
                {
                    float points = stateAlteredInfo.points;
                    model.QueueDamageText(new DamageTextData(DamageTextType.HealthChanged, points));
                    if (stateAlteredInfo.stateAlteredType == StateAlteredType.DecreaseHP)
                    {
                        model.Shake(points > character.GetHP());
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
                case StateAlteredType.Resurrected:
                    RefreshSlideState();
                    break;
                case StateAlteredType.ActionDequeued:
                    model.BlipGrow();
                    break;
            }
        }
        #endregion
    }
}

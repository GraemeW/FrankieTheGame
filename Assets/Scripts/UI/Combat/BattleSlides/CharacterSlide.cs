using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Frankie.Combat.UI
{
    public class CharacterSlide : BattleSlide
    {
        // Tunables
        [Header("Character Slide HookUps")]
        [SerializeField] private TextMeshProUGUI characterNameField;
        [SerializeField] private TextMeshProUGUI currentHPHundreds;
        [SerializeField] private TextMeshProUGUI currentHPTens;
        [SerializeField] private TextMeshProUGUI currentHPOnes;
        [SerializeField] private TextMeshProUGUI currentAPHundreds;
        [SerializeField] private TextMeshProUGUI currentAPTens;
        [SerializeField] private TextMeshProUGUI currentAPOnes;
        [SerializeField] private Image selectHighlight;

        [Header("Highlight Colors")]
        [SerializeField] private Color selectedCharacterFrameColor = Color.green;
        [SerializeField] private Color cooldownCharacterFrameColor = Color.gray;
        [SerializeField] private Color targetedCharacterFrameColor = Color.blue;
        [SerializeField] private Color deadCharacterFrameColor = Color.red;

        // State
        private Color defaultColor = Color.white;
        private SlideState slideState;
        private SlideState lastSlideState;

        // Static
        private static void BreakApartNumber(float number, out int hundreds, out int tens, out int ones)
        {
            int roundedNumber = Mathf.RoundToInt(number);
            hundreds = roundedNumber / 100;
            tens = (roundedNumber % 100) / 10;
            ones = roundedNumber % 10;
        }

        private enum SlideState
        {
            Ready,
            Selected,
            Cooldown,
            Target,
            Dead
        }

        // Functions
        protected override void Awake()
        {
            base.Awake();
            defaultColor = selectHighlight.color;
        }

        public override void SetBattleEntity(BattleEntity setBattleEntity)
        {
            base.SetBattleEntity(setBattleEntity);
            UpdateName(battleEntity.combatParticipant.GetCombatName());
            UpdateHP(battleEntity.combatParticipant.GetHP());
            UpdateAP(battleEntity.combatParticipant.GetAP());
            UpdateColor();
        }

        protected override void SetSelected(CombatParticipantType combatParticipantType, bool enable)
        {
            switch (combatParticipantType)
            {
                case CombatParticipantType.Either:
                    return;
                case CombatParticipantType.Friendly when battleEntity.combatParticipant.IsDead():
                    slideState = SlideState.Dead;
                    break;
                case CombatParticipantType.Friendly when battleEntity.combatParticipant.IsInCooldown():
                    slideState = SlideState.Cooldown;
                    break;
                case CombatParticipantType.Friendly when enable:
                    slideState = SlideState.Selected;
                    break;
                case CombatParticipantType.Friendly:
                    slideState = SlideState.Ready;
                    break;
                case CombatParticipantType.Foe when enable:
                    lastSlideState = slideState; slideState = SlideState.Target;
                    break;
                case CombatParticipantType.Foe:
                    slideState = lastSlideState;
                    break;
            }
            UpdateColor();
        }

        protected override void ParseState(StateAlteredInfo stateAlteredInfo)
        {
            switch (stateAlteredInfo.stateAlteredType)
            {
                case StateAlteredType.CooldownSet:
                    slideState = SlideState.Cooldown;
                    cooldownTimer.ResetTimer(stateAlteredInfo.points);
                    UpdateColor();
                    break;
                case StateAlteredType.CooldownExpired:
                    slideState = SlideState.Ready;
                    cooldownTimer.ResetTimer(0f);
                    UpdateColor();
                    break;
                case StateAlteredType.IncreaseHP:
                case StateAlteredType.DecreaseHP:
                case StateAlteredType.AdjustHPNonSpecific:
                    UpdateHP(battleEntity.combatParticipant.GetHP());
                    if (stateAlteredInfo.stateAlteredType == StateAlteredType.IncreaseHP)
                    {
                        float points = stateAlteredInfo.points;
                        damageTextSpawner.AddToQueue(new DamageTextData(DamageTextType.HealthChanged, points));
                    }
                    else if (stateAlteredInfo.stateAlteredType == StateAlteredType.DecreaseHP)
                    {
                        float points = stateAlteredInfo.points;
                        damageTextSpawner.AddToQueue(new DamageTextData(DamageTextType.HealthChanged, points));
                        bool strongShakeEnable = points > battleEntity.combatParticipant.GetHP();
                        ShakeSlide(strongShakeEnable);
                        BlipFadeSlide();
                    }
                    break;
                case StateAlteredType.IncreaseAP:
                case StateAlteredType.DecreaseAP:
                    break;
                case StateAlteredType.AdjustAPNonSpecific:
                    // Adjust character slide AP on non-specific (i.e. even those announced 'quietly')
                    // Sound effects otherwise update on increase/decrease
                    UpdateAP(battleEntity.combatParticipant.GetAP());
                    damageTextSpawner.AddToQueue(new DamageTextData(DamageTextType.APChanged, stateAlteredInfo.points));
                    break;
                case StateAlteredType.HitMiss:
                    damageTextSpawner.AddToQueue(new DamageTextData(DamageTextType.HitMiss));
                    break;
                case StateAlteredType.HitCrit:
                    damageTextSpawner.AddToQueue(new DamageTextData(DamageTextType.HitCrit));
                    break;
                case StateAlteredType.StatusEffectApplied:
                    PersistentStatus persistentStatus = stateAlteredInfo.persistentStatus;
                    if (persistentStatus != null)
                    {
                        AddStatusEffectBobble(persistentStatus);
                    }
                    break;
                case StateAlteredType.BaseStateEffectApplied:
                    break;
                case StateAlteredType.Dead:
                    slideState = SlideState.Dead;
                    UpdateColor();
                    break;
                case StateAlteredType.Resurrected:
                    slideState = SlideState.Ready;
                    UpdateColor();
                    break;
                case StateAlteredType.FriendFound:
                case StateAlteredType.FriendIgnored:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        protected override bool HandleClickInBattle()
        {
            if (base.HandleClickInBattle()) return false;
            
            // Only callable if battle action not set via battle slide (one click behaviour)
            CombatParticipant selectedCharacter = GetBattleEntity().combatParticipant;
            if (!BattleController.IsCombatParticipantAvailableToAct(selectedCharacter)) { return false; }
                
            List<BattleEntity> selectedBattleEntity = new() { new BattleEntity(selectedCharacter) };
            BattleEventBus<BattleEntitySelectedEvent>.Raise(new BattleEntitySelectedEvent(CombatParticipantType.Friendly, selectedBattleEntity));
            return true;
        }

        // Private functions
        private void UpdateColor()
        {
            if (battleEntity.combatParticipant.IsDead() && // Bypass irrelevant slide states on character death
                slideState is SlideState.Ready or SlideState.Selected or SlideState.Cooldown)
            {
                selectHighlight.color = deadCharacterFrameColor;
                return;
            }

            selectHighlight.color = slideState switch
            {
                SlideState.Ready => defaultColor,
                SlideState.Selected => selectedCharacterFrameColor,
                SlideState.Cooldown => cooldownCharacterFrameColor,
                SlideState.Target => targetedCharacterFrameColor,
                SlideState.Dead => deadCharacterFrameColor,
                _ => Color.white,
            };
        }

        private void UpdateName(string characterName)
        {
            characterNameField.text = characterName;
        }

        private void UpdateHP(float hitPoints)
        {
            BreakApartNumber(hitPoints, out int hundreds, out int tens, out int ones);
            if (hundreds > 0)
            { 
                currentHPHundreds.text = hundreds.ToString();
                currentHPTens.text = tens.ToString();
                currentHPOnes.text = ones.ToString();
            }
            else if (tens > 0)
            {
                currentHPHundreds.text = "";
                currentHPTens.text = tens.ToString();
                currentHPOnes.text = ones.ToString();
            }
            else
            {
                currentHPHundreds.text = "";
                currentHPTens.text = "";
                currentHPOnes.text = ones.ToString();
            }
        }

        private void UpdateAP(float actionPoints)
        {
            BreakApartNumber(actionPoints, out int hundreds, out int tens, out int ones);
            if (hundreds > 0)
            {
                currentAPHundreds.text = hundreds.ToString();
                currentAPTens.text = tens.ToString();
                currentAPOnes.text = ones.ToString();
            }
            else if (tens > 0)
            {
                currentAPHundreds.text = "";
                currentAPTens.text = tens.ToString();
                currentAPOnes.text = ones.ToString();
            }
            else
            {
                currentAPHundreds.text = "";
                currentAPTens.text = "";
                currentAPOnes.text = ones.ToString();
            }
        }
    }
}

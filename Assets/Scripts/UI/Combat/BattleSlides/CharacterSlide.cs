using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Frankie.Inventory.UI;

namespace Frankie.Combat.UI
{
    public class CharacterSlide : BattleSlide
    {
        // Tunables
        [Header("Character Slide HookUps")]
        [SerializeField] TextMeshProUGUI characterNameField = null;
        [SerializeField] TextMeshProUGUI currentHPHundreds = null;
        [SerializeField] TextMeshProUGUI currentHPTens = null;
        [SerializeField] TextMeshProUGUI currentHPOnes = null;
        [SerializeField] TextMeshProUGUI currentAPHundreds = null;
        [SerializeField] TextMeshProUGUI currentAPTens = null;
        [SerializeField] TextMeshProUGUI currentAPOnes = null;
        [SerializeField] Image selectHighlight = null;

        [Header("Highlight Colors")]
        [SerializeField] Color selectedCharacterFrameColor = Color.green;
        [SerializeField] Color cooldownCharacterFrameColor = Color.gray;
        [SerializeField] Color targetedCharacterFrameColor = Color.blue;
        [SerializeField] Color deadCharacterFrameColor = Color.red;

        // State
        SlideState slideState = default;
        SlideState lastSlideState = default;

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
        protected override void OnEnable()
        {
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            // Base implementation removes all button listeners
            base.OnDisable();
        }

        public override void SetBattleEntity(BattleEntity battleEntity)
        {
            base.SetBattleEntity(battleEntity);
            UpdateName(this.battleEntity.combatParticipant.GetCombatName());
            UpdateHP(this.battleEntity.combatParticipant.GetHP());
            UpdateAP(this.battleEntity.combatParticipant.GetAP());
            UpdateColor();
        }

        protected override void SetSelected(CombatParticipantType combatParticipantType, bool enable)
        {
            if (combatParticipantType == CombatParticipantType.Either) { return; }
            
            if (combatParticipantType == CombatParticipantType.Friendly)
            {
                if (battleEntity.combatParticipant.IsDead()) { slideState = SlideState.Dead; }
                else if (battleEntity.combatParticipant.IsInCooldown()) { slideState = SlideState.Cooldown; }
                else if (enable) { slideState = SlideState.Selected; }
                else { slideState = SlideState.Ready; }
            }
            else if (combatParticipantType == CombatParticipantType.Foe)
            {
                if (enable) { lastSlideState = slideState; slideState = SlideState.Target; }
                else { slideState = lastSlideState; }
            }
            UpdateColor();
        }

        protected override void ParseState(CombatParticipant combatParticipant, StateAlteredData stateAlteredData)
        {
            switch (stateAlteredData.stateAlteredType)
            {
                case StateAlteredType.CooldownSet:
                    slideState = SlideState.Cooldown;
                    cooldownTimer.ResetTimer(stateAlteredData.points);
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
                    UpdateHP(this.battleEntity.combatParticipant.GetHP());
                    if (stateAlteredData.stateAlteredType == StateAlteredType.IncreaseHP)
                    {
                        float points = stateAlteredData.points;
                        damageTextSpawner.AddToQueue(new DamageTextData(DamageTextType.HealthChanged, points));
                    }
                    else if (stateAlteredData.stateAlteredType == StateAlteredType.DecreaseHP)
                    {
                        float points = stateAlteredData.points;
                        damageTextSpawner.AddToQueue(new DamageTextData(DamageTextType.HealthChanged, points));
                        bool strongShakeEnable = false;
                        if (points > combatParticipant.GetHP()) { strongShakeEnable = true; }
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
                    UpdateAP(this.battleEntity.combatParticipant.GetAP());
                    damageTextSpawner.AddToQueue(new DamageTextData(DamageTextType.APChanged, stateAlteredData.points));
                    break;
                case StateAlteredType.HitMiss:
                    damageTextSpawner.AddToQueue(new DamageTextData(DamageTextType.HitMiss));
                    break;
                case StateAlteredType.HitCrit:
                    damageTextSpawner.AddToQueue(new DamageTextData(DamageTextType.HitCrit));
                    break;
                case StateAlteredType.StatusEffectApplied:
                    PersistentStatus persistentStatus = stateAlteredData.persistentStatus;
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
            }
        }

        protected override bool HandleClickInBattle()
        {
            if (!base.HandleClickInBattle())
            {
                // Only callable if battle action not set via battle slide (one click behaviour)
                if (battleController.SetSelectedCharacter(GetBattleEntity().combatParticipant)) { return true; }
            }
            return false;
        }

        // Private functions
        private void UpdateColor()
        {
            if (battleEntity.combatParticipant.IsDead() && // Bypass irrelevant slide states on character death
                (slideState == SlideState.Ready || slideState == SlideState.Selected || slideState == SlideState.Cooldown))
            {
                selectHighlight.color = deadCharacterFrameColor;
                return;
            }

            selectHighlight.color = slideState switch
            {
                SlideState.Ready => Color.white,
                SlideState.Selected => selectedCharacterFrameColor,
                SlideState.Cooldown => cooldownCharacterFrameColor,
                SlideState.Target => targetedCharacterFrameColor,
                SlideState.Dead => deadCharacterFrameColor,
                _ => Color.white,
            };
        }

        private void UpdateName(string name)
        {
            characterNameField.text = name;
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

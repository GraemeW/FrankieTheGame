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
            int ceilingNumber = Mathf.CeilToInt(number);
            hundreds = ceilingNumber / 100;
            tens = (ceilingNumber % 100) / 10;
            ones = ceilingNumber % 10;
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
            if (battleController != null) { AddButtonClickEvent( delegate { battleController.SetSelectedCharacter(GetCombatParticipant()); }); }
        }

        protected override void OnDisable()
        {
            // Base implementation removes all button listeners
            base.OnDisable();
        }

        public override void SetCombatParticipant(CombatParticipant combatParticipant)
        {
            base.SetCombatParticipant(combatParticipant);
            UpdateName(this.combatParticipant.GetCombatName());
            UpdateHP(this.combatParticipant.GetHP());
            UpdateAP(this.combatParticipant.GetAP());
        }

        protected override void SetSelected(CombatParticipantType combatParticipantType, bool enable)
        {
            if (combatParticipantType == CombatParticipantType.Either) { return; }
            

            if (combatParticipantType == CombatParticipantType.Character)
            {
                if (combatParticipant.IsDead()) { slideState = SlideState.Dead; }
                else if (combatParticipant.IsInCooldown()) { slideState = SlideState.Cooldown; }
                else if (enable) { slideState = SlideState.Selected; }
                else { slideState = SlideState.Ready; }
            }
            else if (combatParticipantType == CombatParticipantType.Target)
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
                    UpdateHP(this.combatParticipant.GetHP());
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
                    }
                    break;
                case StateAlteredType.IncreaseAP:
                case StateAlteredType.DecreaseAP:
                    UpdateAP(this.combatParticipant.GetAP());
                    damageTextSpawner.AddToQueue(new DamageTextData(DamageTextType.APChanged, stateAlteredData.points));
                    break;
                case StateAlteredType.HitMiss:
                    damageTextSpawner.AddToQueue(new DamageTextData(DamageTextType.HitMiss));
                    break;
                case StateAlteredType.HitCrit:
                    damageTextSpawner.AddToQueue(new DamageTextData(DamageTextType.HitCrit));
                    break;
                case StateAlteredType.StatusEffectApplied:
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
            }
        }

        // Private functions
        private void UpdateColor()
        {
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

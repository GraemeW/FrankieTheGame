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
        [Header("HookUps")]
        [SerializeField] TextMeshProUGUI characterNameField = null;
        [SerializeField] TextMeshProUGUI currentHPHundreds = null;
        [SerializeField] TextMeshProUGUI currentHPTens = null;
        [SerializeField] TextMeshProUGUI currentHPOnes = null;
        [SerializeField] TextMeshProUGUI currentAPHundreds = null;
        [SerializeField] TextMeshProUGUI currentAPTens = null;
        [SerializeField] TextMeshProUGUI currentAPOnes = null;
        [SerializeField] Image selectHighlight = null;
        [SerializeField] DamageTextSpawner damageTextSpawner = null;

        [Header("Flavour")]
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
            if (stateAlteredData.stateAlteredType == StateAlteredType.IncreaseHP 
                || stateAlteredData.stateAlteredType == StateAlteredType.DecreaseHP 
                || stateAlteredData.stateAlteredType == StateAlteredType.AdjustHPNonSpecific)
            {
                UpdateHP(this.combatParticipant.GetHP());
                if (stateAlteredData.stateAlteredType == StateAlteredType.IncreaseHP)
                {
                    float points = stateAlteredData.points;
                    damageTextSpawner.Spawn(points);
                }
                else if (stateAlteredData.stateAlteredType == StateAlteredType.DecreaseHP)
                {
                    float points = stateAlteredData.points;
                    damageTextSpawner.Spawn(points);
                    bool strongShakeEnable = false;
                    if (points > combatParticipant.GetHP()) { strongShakeEnable = true; }
                    ShakeSlide(strongShakeEnable);
                }
            }
            else if (stateAlteredData.stateAlteredType == StateAlteredType.IncreaseAP 
                || stateAlteredData.stateAlteredType == StateAlteredType.DecreaseAP)
            {
                UpdateAP(this.combatParticipant.GetAP());
            }

            if (stateAlteredData.stateAlteredType == StateAlteredType.CooldownSet)
            {
                slideState = SlideState.Cooldown;
                UpdateColor();
            }
            else if (stateAlteredData.stateAlteredType == StateAlteredType.CooldownExpired)
            {
                slideState = SlideState.Ready;
                UpdateColor();
            }
            else if (stateAlteredData.stateAlteredType == StateAlteredType.Dead)
            {
                slideState = SlideState.Dead;
                UpdateColor();
            }
            else if (stateAlteredData.stateAlteredType == StateAlteredType.Resurrected)
            {
                slideState = SlideState.Ready;
                UpdateColor();
            }
        }

        // Private functions
        private void UpdateColor()
        {
            if (slideState == SlideState.Ready)
            {
                selectHighlight.color = Color.white;
            }
            else if (slideState == SlideState.Selected)
            {
                selectHighlight.color = selectedCharacterFrameColor;
            }
            else if (slideState == SlideState.Cooldown)
            {
                selectHighlight.color = cooldownCharacterFrameColor;
            }
            else if (slideState == SlideState.Target)
            {
                selectHighlight.color = targetedCharacterFrameColor;
            }
            else if (slideState == SlideState.Dead)
            {
                selectHighlight.color = deadCharacterFrameColor;
            }
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

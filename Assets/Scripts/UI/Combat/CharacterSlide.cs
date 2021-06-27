using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

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
        [SerializeField] Color targetedCharacterFrameColor = Color.red;

        // State
        [SerializeField] SlideState slideState = default;
        [SerializeField] SlideState lastSlideState = default;

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
            ready,
            selected,
            cooldown,
            target
        }

        // Functions
        protected override void OnEnable()
        {
            base.OnEnable();
            if (battleController != null) { GetComponent<Button>().onClick.AddListener(delegate { battleController.SetSelectedCharacter(GetCombatParticipant()); }); }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            GetComponent<Button>().onClick.RemoveAllListeners();
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
            if (combatParticipantType == CombatParticipantType.DefaultNone) { return; }

            if (combatParticipantType == CombatParticipantType.Character)
            {
                if (combatParticipant.IsInCooldown()) { slideState = SlideState.cooldown; }
                else if (enable) { slideState = SlideState.selected; }
                else { slideState = SlideState.ready; }
            }
            else if (combatParticipantType == CombatParticipantType.Target)
            {
                if (enable) { lastSlideState = slideState; slideState = SlideState.target; }
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
            // TODO:  add behavior for other state altered types (death, etc.)
            // TODO:  update slide graphics / animation for each behavior

            if (stateAlteredData.stateAlteredType == StateAlteredType.CooldownSet)
            {
                slideState = SlideState.cooldown;
                UpdateColor();
            }
            else if (stateAlteredData.stateAlteredType == StateAlteredType.CooldownExpired)
            {
                slideState = SlideState.ready;
                UpdateColor();
            }
        }

        // Private functions
        private void UpdateColor()
        {
            if (slideState == SlideState.ready)
            {
                selectHighlight.color = Color.white;
            }
            else if (slideState == SlideState.selected)
            {
                selectHighlight.color = selectedCharacterFrameColor;
            }
            else if (slideState == SlideState.cooldown)
            {
                selectHighlight.color = cooldownCharacterFrameColor;
            }
            else if (slideState == SlideState.target)
            {
                selectHighlight.color = targetedCharacterFrameColor;
            }
        }

        private void UpdateName(string name)
        {
            characterNameField.text = name;
        }

        private void UpdateHP(float hitPoints)
        {
            BreakApartNumber(hitPoints, out int hundreds, out int tens, out int ones);
            if (hundreds > 0) { currentHPHundreds.text = hundreds.ToString(); }
                else { currentHPHundreds.text = ""; }
            if (tens > 0) { currentHPTens.text = tens.ToString(); }
                else { currentHPTens.text = ""; }
            if (ones > 0) { currentHPOnes.text = ones.ToString(); }
                else { currentHPOnes.text = "0"; }
        }

        private void UpdateAP(float actionPoints)
        {
            BreakApartNumber(actionPoints, out int hundreds, out int tens, out int ones);
            if (hundreds > 0) { currentAPHundreds.text = hundreds.ToString(); }
            else { currentAPHundreds.text = ""; }
            if (tens > 0) { currentAPTens.text = tens.ToString(); }
            else { currentAPTens.text = ""; }
            if (ones > 0) { currentAPOnes.text = ones.ToString(); }
            else { currentAPOnes.text = "0"; }
        }
    }
}

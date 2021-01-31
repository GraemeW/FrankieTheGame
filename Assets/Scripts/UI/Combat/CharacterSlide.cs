using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using static Frankie.Combat.CombatParticipant;
using UnityEngine.UI;

namespace Frankie.Combat.UI
{
    public class CharacterSlide : MonoBehaviour
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

        [Header("Flavour")]
        [SerializeField] Color selectedCharacterFrameColor = Color.green;
        [SerializeField] Color cooldownCharacterFrameColor = Color.gray;

        // State
        CombatParticipant character = null;
        SlideState slideState = default;

        // Static
        private static void BreakApartNumber(float number, out int hundreds, out int tens, out int ones)
        {
            hundreds = (int)number / 100;
            tens = ((int)number % 100) / 10;
            ones = (int)number % 10;
        }

        private enum SlideState
        {
            ready,
            selected,
            cooldown
        }

        // Functions
        private void OnDisable()
        {
            if (character != null)
            {
                character.stateAltered -= ParseCharacterState;
            }
        }

        public void SetCharacter(CombatParticipant combatParticipant)
        {
            character = combatParticipant;
            character.stateAltered += ParseCharacterState;
            UpdateName(character.GetCombatName());
            UpdateHP(character.GetHP());
            UpdateAP(character.GetAP());
        }

        public CombatParticipant GetCharacter()
        {
            return character;
        }

        public void SetSelected(bool enable)
        {
            if (character.IsInCooldown()) { slideState = SlideState.cooldown; }
            else if (enable) { slideState = SlideState.selected; }
            else { slideState = SlideState.ready; }
            UpdateColor();
        }

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
        }

        private void ParseCharacterState(StateAlteredType stateAlteredType)
        {
            if (stateAlteredType == StateAlteredType.IncreaseHP || stateAlteredType == StateAlteredType.DecreaseHP)
            {
                UpdateHP(character.GetHP());
            }
            else if (stateAlteredType == StateAlteredType.IncreaseAP || stateAlteredType == StateAlteredType.DecreaseAP)
            {
                UpdateAP(character.GetAP());
            }
            // TODO:  add behavior for other state altered types (death, etc.)
            // TODO:  update slide graphics / animation for each behavior

            if (stateAlteredType == StateAlteredType.CooldownSet)
            {
                slideState = SlideState.cooldown;
                UpdateColor();
            }
            else if (stateAlteredType == StateAlteredType.CooldownExpired)
            {
                slideState = SlideState.ready;
                UpdateColor();
            }
        }

        private void UpdateName(string name)
        {
            characterNameField.text = name;
        }

        public void UpdateHP(float hitPoints)
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

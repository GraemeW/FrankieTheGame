using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using static Frankie.Combat.CombatParticipant;

namespace Frankie.Combat.UI
{
    public class CharacterSlide : MonoBehaviour
    {
        // Tunables
        [SerializeField] TextMeshProUGUI characterNameField = null;
        [SerializeField] TextMeshProUGUI currentHPHundreds = null;
        [SerializeField] TextMeshProUGUI currentHPTens = null;
        [SerializeField] TextMeshProUGUI currentHPOnes = null;
        [SerializeField] TextMeshProUGUI currentAPHundreds = null;
        [SerializeField] TextMeshProUGUI currentAPTens = null;
        [SerializeField] TextMeshProUGUI currentAPOnes = null;

        // State
        CombatParticipant character = null;

        // Static
        private static void BreakApartNumber(float number, out int hundreds, out int tens, out int ones)
        {
            hundreds = (int)number / 100;
            tens = ((int)number % 100) / 10;
            ones = (int)number % 10;
        }


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

        private void ParseCharacterState(StateAlteredType stateAlteredType)
        {
            // TODO:  update slide graphics / animation as a function of altered type input
            if (stateAlteredType == StateAlteredType.IncreaseHP || stateAlteredType == StateAlteredType.DecreaseHP)
            {
                UpdateHP(character.GetHP());
            }
            else if (stateAlteredType == StateAlteredType.IncreaseAP || stateAlteredType == StateAlteredType.DecreaseAP)
            {
                UpdateAP(character.GetAP());
            }

            // TODO:  add behavior for other state altered types (death, etc.)
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

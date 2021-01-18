using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

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

        public void UpdateName(string name)
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

        public void UpdateAP(float actionPoints)
        {
            BreakApartNumber(actionPoints, out int hundreds, out int tens, out int ones);
            if (hundreds > 0) { currentAPHundreds.text = hundreds.ToString(); }
            else { currentAPHundreds.text = ""; }
            if (tens > 0) { currentAPTens.text = tens.ToString(); }
            else { currentAPTens.text = ""; }
            if (ones > 0) { currentAPOnes.text = ones.ToString(); }
            else { currentAPOnes.text = "0"; }
        }

        private void BreakApartNumber(float number, out int hundreds, out int tens, out int ones)
        {
            hundreds = (int)number / 100;
            tens = ((int)number % 100) / 10;
            ones = (int)number % 10;
        }
    }
}

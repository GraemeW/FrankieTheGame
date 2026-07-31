using TMPro;
using UnityEngine;

namespace Frankie.Menu.UI
{
    public class InputDisplay : MonoBehaviour
    {
        [Header("Inputs")]
        [SerializeField] private char padCharacter = '·';
        [SerializeField] private int maxSize = 8;
        [Header("Hookups")]
        [SerializeField] private TMP_Text displayText;

        // State
        private string currentDisplayText = "";
        
        #region UnityMethods
        private void Awake()
        { 
            if (displayText == null) { return;  }
            displayText.text = new string(padCharacter, maxSize);
        }
        #endregion

        #region PublicMethods
        public string GetCurrentText() => currentDisplayText;
        
        public bool TryAddText(char character)
        {
            if (currentDisplayText.Length >= maxSize) { return false; }
            currentDisplayText += character;
            RefreshDisplay();
            return true;
        }

        public bool TryRemoveText()
        {
            if (currentDisplayText.Length == 0) { return false; }
            currentDisplayText = currentDisplayText.Remove(currentDisplayText.Length - 1);
            RefreshDisplay();
            return true;
        }

        public void ClearDisplay()
        {
            currentDisplayText = "";
            RefreshDisplay();
        }

        public void OverrideDisplay(string newDisplayText)
        {
            currentDisplayText = newDisplayText;
            RefreshDisplay();
        }
        #endregion

        #region PrivateMethods
        private void RefreshDisplay()
        {
            displayText.text = currentDisplayText.PadRight(maxSize, padCharacter);
        }
        #endregion
    }
}

using UnityEngine;
using LowDefMustard.UIBox;

namespace Frankie.Menu.UI
{
    public class KeyboardRow : MonoBehaviour
    {
        // Tunables
        [SerializeField] private UIChoiceButton keyboardButtonPrefab;
        [SerializeField] private GameObject spacerPrefab;
        
        #region PublicMethods
        public UIChoiceButton AddKeyToRow(char character)
        {
            UIChoiceButton keyboardButton = Instantiate(keyboardButtonPrefab, transform);
            keyboardButton.SetText(character.ToString());
            return keyboardButton;
        }

        public void AddSpacerToRow()
        {
            Instantiate(spacerPrefab, transform);
        }
        #endregion
    }
}

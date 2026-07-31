using System.Collections.Generic;
using System.Linq;
using Frankie.Utils;
using Frankie.Utils.Localization;
using Frankie.Utils.UI;
using UnityEngine;
using UnityEngine.Localization;

namespace Frankie.Menu.UI
{
    public class Keyboard : UIBox<UIBoxState>
    {
        [Header("Standard Settings")]
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedKeyboardKeys;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedKeyboardKeysUpper;
        [SerializeField] private string additionalSpecialCharacters = "0123456789._-@!*^";
        [SerializeField] private int standardKeysPerRow = 8;
        [SerializeField] private int spacersToSpecialKeys = 2;
        [Header("Hookups")]
        [SerializeField] private Transform keyboardRowOrigin;
        [SerializeField] private Transform upperKeyboardRowOrigin;
        [SerializeField] private Transform optionRowOrigin;
        [SerializeField] private UIChoiceButton lowerCaseButton;
        [SerializeField] private UIChoiceButton upperCaseButton;
        [SerializeField] private Transform adminRowOrigin;
        [Header("Prefabs")]
        [SerializeField] private KeyboardRow keyboardRowPrefab;
        
        // State
        private bool isUpper = false;
        private readonly List<Key> standardKeys = new();
        private readonly List<Key> upperKeys = new();
        private readonly List<UIChoice> optionKeys = new();
        private readonly List<UIChoice> adminKeys = new();

        // UIBox Configuration
        protected override EnumLookup<UIBoxState, UIBoxStateBehaviour> BuildStateBehaviours()
        {
            var stateBehaviours = new EnumLookup<UIBoxState, UIBoxStateBehaviour>();
            stateBehaviours.TrySet(UIBoxState.Default, new UIBoxStateBehaviour( 
                setupChoiceOptions: ImplementSetupChoiceOptions,
                reconcileChoiceOptions: ImplementReconcileChoiceOptions,
                moveCursor: (controllerInputType, _) => StandardMoveCursorSpatial(controllerInputType))
            );
            return stateBehaviours;
        }

        #region UnityMethods
        protected override void AwakeTriggered()
        {
            preventEscapeOptionExit = true;
            InitializeOptionButtons();
        }
        #endregion
        
        #region PublicMethods
        public void Setup(InputDisplay inputDisplay)
        {
            if (inputDisplay == null) { return; }
            
            foreach (Key key in standardKeys.Where(key => key.keyboardButton != null))
            {
                key.keyboardButton.RemoveOnClickListeners();
                key.keyboardButton.AddOnClickListener(() => UpdateDisplay(inputDisplay, key.character));
            }
            
            foreach (Key key in upperKeys.Where(key => key.keyboardButton != null))
            {
                key.keyboardButton.RemoveOnClickListeners();
                key.keyboardButton.AddOnClickListener(() => UpdateDisplay(inputDisplay, key.character));
            }
            
            SetKeyboardToUpper(false);
            if (IsChoiceAvailable())
            {
                highlightedChoiceOption = choiceOptions[0];
                highlightedChoiceOption.Highlight(true);
            }
        }
        #endregion

        #region UIBoxConfiguration
        private void ImplementSetupChoiceOptions()
        {
            ClearExistingRows();
            
            int globalChoiceOrder = 0;
            string standardKeyString = localizedKeyboardKeys.GetSafeLocalizedString();
            standardKeys.AddRange(BuildKeyboardChoices(standardKeyString, true, ref globalChoiceOrder));
            
            string upperKeyString = localizedKeyboardKeysUpper.GetSafeLocalizedString();
            upperKeys.AddRange(BuildKeyboardChoices(upperKeyString, false, ref globalChoiceOrder));

            optionKeys.AddRange(FindOptionChoices(ref globalChoiceOrder));
            adminKeys.AddRange(FindAdminChoices(ref globalChoiceOrder));
        }
        
        private void ImplementReconcileChoiceOptions()
        {
            choiceOptions.Clear();
            choiceOptions.AddRange(isUpper ? upperKeys.Select(key => key.keyboardButton) : standardKeys.Select(key => key.keyboardButton));
            choiceOptions.AddRange(optionKeys);
            choiceOptions.AddRange(adminKeys);
            SetChoiceAvailable(choiceOptions.Count > 0);
        }
        #endregion

        #region PrivateSetup
        private List<Key> BuildKeyboardChoices(string keyboardKeys, bool isStandardOrigin, ref int globalChoiceOrder)
        {
            List<Key> keys = new();
            if (string.IsNullOrEmpty(keyboardKeys)) { return keys; }
            
            // Format per row: [StandardKeys][Spacers][SpecialKeys]
            int rowCount = Mathf.CeilToInt(keyboardKeys.Length / (float)standardKeysPerRow);
            int specialKeysPerRow = additionalSpecialCharacters.Length > 0 ? Mathf.CeilToInt(additionalSpecialCharacters.Length / (float)rowCount) : 0;
            int standardIndex = 0;
            int specialIndex = 0;
            
            for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
            {
                KeyboardRow keyboardRow = Instantiate(keyboardRowPrefab, isStandardOrigin ? keyboardRowOrigin : upperKeyboardRowOrigin );

                // Standard (localized) keys section
                for (int i = 0; i < standardKeysPerRow; i++)
                {
                    if (standardIndex < keyboardKeys.Length)
                    {
                        char character = keyboardKeys[standardIndex++];
                        UIChoiceButton keyButton = keyboardRow.AddKeyToRow(character);
                        keyButton.UseHighlightSelected(true);
                        keyButton.SetChoiceOrder(globalChoiceOrder++);
                        keys.Add(new Key(keyButton, character));
                    }
                    else { keyboardRow.AddSpacerToRow(); }
                }

                // Spacer section separating standard keys from special keys
                for (int i = 0; i < spacersToSpecialKeys; i++) { keyboardRow.AddSpacerToRow(); }

                // Special characters section (numbers/punctuation), divvied evenly across rows
                for (int i = 0; i < specialKeysPerRow; i++)
                {
                    if (specialIndex < additionalSpecialCharacters.Length)
                    {
                        char character = additionalSpecialCharacters[specialIndex++];
                        UIChoiceButton keyButton = keyboardRow.AddKeyToRow(character);
                        keyButton.UseHighlightSelected(true);
                        keyButton.SetChoiceOrder(globalChoiceOrder++);
                        keys.Add(new Key(keyButton, character));
                    }
                    else { keyboardRow.AddSpacerToRow(); }
                }
            }
            return keys;
        }

        private List<UIChoiceButton> FindOptionChoices(ref int globalChoiceOrder)
        {
            List<UIChoiceButton> optionChoices = new List<UIChoiceButton>();
            if (optionRowOrigin == null) { return optionChoices; }
            
            foreach (UIChoiceButton optionChoice in optionRowOrigin.GetComponentsInChildren<UIChoiceButton>().Where(choice => !choiceOptions.Contains(choice)).OrderBy(choice => choice.choiceOrder))
            {
                optionChoice.UseHighlightSelected(true);
                optionChoice.SetChoiceOrder(globalChoiceOrder++);
                optionChoices.Add(optionChoice);
            }
            return optionChoices;
        }

        private List<UIChoiceButton> FindAdminChoices(ref int globalChoiceOrder)
        {
            List<UIChoiceButton> adminChoices = new List<UIChoiceButton>();
            if (adminRowOrigin == null) { return adminChoices; }
            
            foreach (UIChoiceButton adminChoice in adminRowOrigin.GetComponentsInChildren<UIChoiceButton>().Where(choice => !choiceOptions.Contains(choice)).OrderBy(choice => choice.choiceOrder))
            {
                adminChoice.UseHighlightSelected(true);
                adminChoice.SetChoiceOrder(globalChoiceOrder++);
                adminChoices.Add(adminChoice);
            }
            return adminChoices;
        }

        private void InitializeOptionButtons()
        {
            lowerCaseButton.AddOnClickListener(() => SetKeyboardToUpper(false));
            upperCaseButton.AddOnClickListener(() => SetKeyboardToUpper(true));
        }

        private void InitializeAdminButtons()
        {
            
        }
        #endregion

        #region PrivateUtility
        private static void UpdateDisplay(InputDisplay inputDisplay, char character)
        {
            if (inputDisplay == null) { return; }
            inputDisplay.TryAddText(character);
        }
        
        private void SetKeyboardToUpper(bool enable)
        {
            isUpper = enable;
            keyboardRowOrigin.gameObject.SetActive(!enable);
            upperKeyboardRowOrigin.gameObject.SetActive(enable);
            ReconcileChoiceOptions();
        }
        
        private void ClearExistingRows()
        {
            standardKeys.Clear();
            foreach (Transform keyboardRow in keyboardRowOrigin) { Destroy(keyboardRow.gameObject); }
            upperKeys.Clear();
            foreach (Transform keyboardRow in upperKeyboardRowOrigin) { Destroy(keyboardRow.gameObject); }
            
            optionKeys.Clear();
            adminKeys.Clear();
            choiceOptions.Clear();
            highlightedChoiceOption = null;
        }
        #endregion
    }
}

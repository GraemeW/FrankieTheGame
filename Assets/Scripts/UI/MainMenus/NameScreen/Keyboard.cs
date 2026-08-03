using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization;
using Frankie.Control;
using Frankie.Speech.UI;
using Frankie.Utils;
using Frankie.Utils.Localization;
using Frankie.Utils.UI;

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
        [SerializeField] private InputDisplay inputDisplay;
        [SerializeField] private DialogueBox questionTextScan;
        [SerializeField] private Transform keyboardRowOrigin;
        [SerializeField] private Transform upperKeyboardRowOrigin;
        [SerializeField] private Transform optionRowOrigin;
        [SerializeField] private UIChoiceButton lowerCaseButton;
        [SerializeField] private UIChoiceButton upperCaseButton;
        [SerializeField] private Transform adminRowOrigin;
        [SerializeField] private UIChoiceButton dontCareButton;
        [SerializeField] private UIChoiceButton backspaceButton;
        [SerializeField] private UIChoiceButton confirmButton;
        [Header("Prefabs")]
        [SerializeField] private KeyboardRow keyboardRowPrefab;
        
        // State
        private bool areChoiceOptionsSetup = false;
        private bool isUpper = false;
        private readonly List<Key> standardKeys = new();
        private readonly List<Key> upperKeys = new();
        private readonly List<UIChoice> optionKeys = new();
        private readonly List<UIChoice> adminKeys = new();
        private int nextDontCareIndex = 0;
        private readonly List<string> dontCareAnswers = new();
        
        // Cached References
        private NameScreenOrchestrator nameScreenOrchestrator;

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
            nameScreenOrchestrator = GetComponentInParent<NameScreenOrchestrator>();
            questionTextScan.SetHandleGlobalInput(false);
        }

        protected override void StartTriggered()
        {
            if (nameScreenOrchestrator != null && nameScreenOrchestrator.TryGetController(out BaseController baseController)) { baseController.AddInputReceiver(this, null); }
        }

        protected override void EnableTriggered()
        {
            SubscribeToQuestionUpdates(true);
            
            ReconcileChoiceOptions();
            SetupButtonEvents();
            SetKeyboardToUpper(false);
            if (IsChoiceAvailable())
            {
                highlightedChoiceOption = choiceOptions[0];
                highlightedChoiceOption.Highlight(true);
            }
        }

        protected override void DisableTriggered()
        {
            SubscribeToQuestionUpdates(false);
        }
        #endregion

        #region UIBoxConfiguration
        private void ImplementSetupChoiceOptions()
        {
            if (areChoiceOptionsSetup) { return; }
            
            areChoiceOptionsSetup = true;
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

        #region EventHandling
        private void SubscribeToQuestionUpdates(bool enable)
        {
            if (nameScreenOrchestrator == null)  { return; }

            nameScreenOrchestrator.stateChanged -= SetupCurrentQuestion;
            if (enable) { nameScreenOrchestrator.stateChanged += SetupCurrentQuestion; }
        }
        
        private void SetupCurrentQuestion(NameScreenState nameScreenState, NameScreenQuestion question)
        {
            if (nameScreenState != NameScreenState.Naming) { return; }
            
            inputDisplay.ClearDisplay();
            
            questionTextScan.ClearOldDialogue();
            questionTextScan.Setup(question.question);
            
            nameScreenOrchestrator.InitializeThing(question.thingPrefab);
            
            SetDontCareAnswers(question.dontCareAnswers);
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
        
        private void SetupButtonEvents()
        {
            foreach (Key key in standardKeys.Where(key => key.keyboardButton != null))
            {
                key.keyboardButton.AddOnClickListener(() => AddCharacterToDisplay(key.character));
            }
            
            foreach (Key key in upperKeys.Where(key => key.keyboardButton != null))
            {
                key.keyboardButton.AddOnClickListener(() => AddCharacterToDisplay(key.character));
            }
            InitializeOptionButtons();
            InitializeAdminButtons();

        }

        private void InitializeOptionButtons()
        {
            lowerCaseButton.AddOnClickListener(() => SetKeyboardToUpper(false));
            upperCaseButton.AddOnClickListener(() => SetKeyboardToUpper(true));
        }

        private void InitializeAdminButtons()
        {
            if (inputDisplay != null)
            {
                backspaceButton.AddOnClickListener(() => RemoveCharacterFromDisplay());
                dontCareButton.AddOnClickListener(() => SetDontCareEntry());
            }

            if (nameScreenOrchestrator != null)
            {
                confirmButton.AddOnClickListener(() => nameScreenOrchestrator.AdvanceNamingRoutine());
            }
        }
        #endregion

        #region PrivateUtility
        private void SetDontCareAnswers(List<string> setDontCareAnswers)
        {
            nextDontCareIndex = 0;
            dontCareAnswers.Clear();
            dontCareAnswers.AddRange(setDontCareAnswers);
        }

        private void SetDontCareEntry()
        {
            if (dontCareAnswers.Count == 0) { return; }
            if (nextDontCareIndex >= dontCareAnswers.Count) { nextDontCareIndex = 0; }
            
            string dontCareAnswer = dontCareAnswers[nextDontCareIndex];
            inputDisplay.ClearDisplay();
            inputDisplay.OverrideDisplay(dontCareAnswer);
            
            nextDontCareIndex++;
        }
        
        private void AddCharacterToDisplay(char character)
        {
            if (inputDisplay == null) { return; }
            inputDisplay.TryAddText(character);
        }

        private void RemoveCharacterFromDisplay()
        {
            if (inputDisplay == null) { return; }
            inputDisplay.TryRemoveText();
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

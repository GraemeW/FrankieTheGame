using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using LowDefMustard.Control;
using LowDefMustard.Utils;
using LowDefMustard.Localization;
using Frankie.Speech.UI;
using Frankie.Utils.UI;

namespace Frankie.Menu.UI
{
    public class NamingPanel : UIBox<UIBoxState>, ILocalizable
    {
        [Header("Keyboard Parameters")]
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedKeyboardKeys;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedKeyboardKeysUpper;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString lowerText;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString upperText;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString backspaceText;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString dontCareText;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString confirmText;
        [SerializeField] private string additionalSpecialCharacters = "0123456789._-@!*^";
        [SerializeField] private int standardKeysPerRow = 8;
        [SerializeField] private int spacersToSpecialKeys = 3;
        [SerializeField] private int specialKeysPerRow = 5;
        [Header("Thing Parameters")]
        [SerializeField] private float thingSize = 180;
        [SerializeField] private float thingWalkTimeEstimate = 1.5f;
        [SerializeField] private bool includeWalkTimeOnLastElement = false;
        [Header("Prefabs")]
        [SerializeField] private KeyboardRow keyboardRowPrefab;
        [Header("Keyboard Hookups")]
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
        [Header("Thing Hookups")]
        [SerializeField] private Transform stagePosition;
        [SerializeField] private RelativeUISequencer offStagePosition;
        [SerializeField] private RelativeUISequencer leftWalkCover;
        
        // Core State
        private bool areChoiceOptionsSetup = false;
        private bool isUpper = false;
        // Keyboard State
        private readonly List<Key> standardKeys = new();
        private readonly List<Key> upperKeys = new();
        private readonly List<UIChoice> optionKeys = new();
        private readonly List<UIChoice> adminKeys = new();
        // Don't Care State
        private int nextDontCareIndex = 0;
        private readonly List<string> dontCareAnswers = new();
        // Thing State
        private GameObject thing;
        private Coroutine thingCoroutine;
        
        // Cached References
        private NameScreenOrchestrator nameScreenOrchestrator;
        
        // Localization
        public LocalizationTableType localizationTableType { get; } = LocalizationTableType.UI;
        public List<TableEntryReference> GetLocalizationEntries()
        {
            return new List<TableEntryReference>
            {
                localizedKeyboardKeys.TableEntryReference,
                localizedKeyboardKeysUpper.TableEntryReference,
                lowerText.TableEntryReference,
                upperText.TableEntryReference,
                backspaceText.TableEntryReference,
                dontCareText.TableEntryReference,
                confirmText.TableEntryReference,
            };
        }

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
            SetupButtonLocalization();
            SetupButtonEvents(true);
            
            ReconcileChoiceOptions();
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
            SetupButtonEvents(false);
        }

        protected override void DestroyTriggered()
        {
            if (thingCoroutine != null) { StopCoroutine(thingCoroutine); }
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
            switch (nameScreenState)
            {
                case NameScreenState.Naming:
                    inputDisplay.ClearDisplay();
                    questionTextScan.ClearOldDialogue();
                    questionTextScan.Setup(question.localizedQuestion.GetSafeLocalizedString());
                    InitializeThing(question.thingPrefab);
                    SetDontCareAnswers(SanitizeDontCareAnswers(question.localizedDontCareAnswers));
                    break;
                case NameScreenState.NamingComplete:
                    CloseOutNamingPanel();
                    break;
                default:
                case NameScreenState.Intro:
                case NameScreenState.Confirm:
                    break;
            }
        }
        #endregion
        
        #region PrivateSetup
        private List<Key> BuildKeyboardChoices(string keyboardKeys, bool isStandardOrigin, ref int globalChoiceOrder)
        {
            List<Key> keys = new();
            if (string.IsNullOrEmpty(keyboardKeys)) { return keys; }
            
            // Format per row: [StandardKeys][Spacers][SpecialKeys]
            int rowCount = Mathf.CeilToInt(keyboardKeys.Length / (float)standardKeysPerRow);
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

                // Special characters section (numbers/punctuation)
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

        private void SetupButtonLocalization()
        {
            lowerCaseButton.SetText(lowerText.GetSafeLocalizedString());
            upperCaseButton.SetText(upperText.GetSafeLocalizedString());
            backspaceButton.SetText(backspaceText.GetSafeLocalizedString());
            dontCareButton.SetText(dontCareText.GetSafeLocalizedString());
            confirmButton.SetText(confirmText.GetSafeLocalizedString());
        }
        
        private void SetupButtonEvents(bool enable)
        {
            foreach (Key key in standardKeys.Where(key => key.keyboardButton != null))
            {
                key.keyboardButton.RemoveOnClickListeners();
                if (enable) { key.keyboardButton.AddOnClickListener(() => AddCharacterToDisplay(key.character)); }
            }
            
            foreach (Key key in upperKeys.Where(key => key.keyboardButton != null))
            {
                key.keyboardButton.RemoveOnClickListeners();
                if (enable) { key.keyboardButton.AddOnClickListener(() => AddCharacterToDisplay(key.character)); }
            }
            InitializeOptionButtons(enable);
            InitializeAdminButtons(enable);
        }

        private void InitializeOptionButtons(bool enable)
        {
            lowerCaseButton.RemoveOnClickListeners();
            upperCaseButton.RemoveOnClickListeners();
            if (!enable) { return; }
            
            lowerCaseButton.AddOnClickListener(() => SetKeyboardToUpper(false));
            upperCaseButton.AddOnClickListener(() => SetKeyboardToUpper(true));
        }

        private void InitializeAdminButtons(bool enable)
        {
            backspaceButton.RemoveOnClickListeners();
            dontCareButton.RemoveOnClickListeners();
            confirmButton.RemoveOnClickListeners();
            if (!enable) { return; }
            
            backspaceButton.AddOnClickListener(RemoveCharacterFromDisplay);
            dontCareButton.AddOnClickListener(SetDontCareEntry);
            if (nameScreenOrchestrator != null) { confirmButton.AddOnClickListener(TryAdvanceNamingRoutine); }
        }

        private void TryAdvanceNamingRoutine()
        {
            if (string.IsNullOrEmpty(inputDisplay.GetCurrentText())) { return; }
            nameScreenOrchestrator.AdvanceNamingRoutine(inputDisplay.GetCurrentText());
        }
        #endregion

        #region DontCareEntrySetup
        private void SetDontCareAnswers(List<string> setDontCareAnswers)
        {
            nextDontCareIndex = 0;
            dontCareAnswers.Clear();
            dontCareAnswers.AddRange(setDontCareAnswers);
        }
        
        private static List<string> SanitizeDontCareAnswers(IEnumerable<DontCareAnswer> passDontCareAnswers)
        {
            var sanitizedDontCareAnswers = new List<string>();
            if (passDontCareAnswers == null)  { return sanitizedDontCareAnswers; }
            
            foreach (DontCareAnswer dontCareAnswer in passDontCareAnswers)
            {
                if (dontCareAnswer?.entry == null) { continue; }
                string sanitizedDontCareAnswer = dontCareAnswer.entry.GetSafeLocalizedString();
                
                if (string.IsNullOrEmpty(sanitizedDontCareAnswer)) { continue; }
                sanitizedDontCareAnswers.Add(sanitizedDontCareAnswer);
            }
            return sanitizedDontCareAnswers;
        }

        private void SetDontCareEntry()
        {
            if (inputDisplay == null || dontCareAnswers.Count == 0) { return; }
            if (nextDontCareIndex >= dontCareAnswers.Count) { nextDontCareIndex = 0; }
            
            string dontCareAnswer = dontCareAnswers[nextDontCareIndex];
            inputDisplay.ClearDisplay();
            inputDisplay.OverrideDisplay(dontCareAnswer);
            
            nextDontCareIndex++;
        }
        #endregion
        
        #region KeyboardDisplay
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
        
        #region ThingVisualization
        private void InitializeThing(GameObject newThingPrefab)
        {
            if (thingCoroutine != null) { StopCoroutine(thingCoroutine); }
            thingCoroutine = StartCoroutine(SwapThingToWalkInFrame(newThingPrefab));
        }

        private void CloseOutNamingPanel()
        {
            if (thingCoroutine != null) { StopCoroutine(thingCoroutine); }
            thingCoroutine = StartCoroutine(WalkOffThingAdvanceState());
        }
        
        private IEnumerator SwapThingToWalkInFrame(GameObject newThingPrefab)
        {
            yield return WalkOffThing();
            if (newThingPrefab == null) { thing = null; yield break;}
            
            GameObject newThing = Instantiate(newThingPrefab, offStagePosition.transform);
            thing = newThing;
            if (thing == null) { yield break; }
            
            yield return null;
            if (thing.TryGetComponent(out RectTransform rectTransform)) { rectTransform.sizeDelta = new Vector2(thingSize, thingSize); }
            yield return null;
            if (thing.TryGetComponent(out UICharacter uiCharacter)) { uiCharacter.MoveTowards(stagePosition.position); }
            else { thing.transform.position = stagePosition.position; }
        }

        private IEnumerator WalkOffThing()
        {
            yield return null;
            if (offStagePosition != null) { offStagePosition.AssertAlignment(); }
            if (leftWalkCover != null) { leftWalkCover.AssertAlignment(); }
            yield return null;
            if (thing == null) { yield break; }
            
            if (thing.TryGetComponent(out UICharacter uiCharacter))
            {
                uiCharacter.MoveTowards(offStagePosition.transform.position);
                yield return new WaitForSeconds(thingWalkTimeEstimate);
            }
            Destroy(thing);
        }

        private IEnumerator WalkOffThingAdvanceState()
        {
            if (includeWalkTimeOnLastElement)
            {
                yield return WalkOffThing();
                yield return null;
            }
            else
            {
                if (thing != null) { Destroy(thing); }
            }
            
            if (nameScreenOrchestrator != null) { nameScreenOrchestrator.AdvanceState(); }
        }
        #endregion
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using LowDefMustard.Control;
using LowDefMustard.UIBox;
using LowDefMustard.Utils;
using LowDefMustard.Localization;
using Frankie.Speech.UI;
using Frankie.Utils.Localization;

namespace Frankie.Menu.UI
{
    [RequireComponent(typeof(UIToolkitMenuView))]
    [RequireComponent(typeof(NamingStage))]
    public sealed class NamingPanel : UIBox<UIBoxState>, ILocalizable
    {
        [Header("Keyboard")]
        [SerializeField] private NamingKeyboard keyboard = new();
        [SerializeField] private NameInput nameInput = new();
        [Header("Text")]
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString lowerText;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString upperText;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString backspaceText;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString dontCareText;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString confirmText;
        [Header("Behaviour")]
        [SerializeField] private bool walkOffBeforeAdvancing = false;
        [Header("Hookups")]
        [SerializeField] private DialogueBox questionTextScan; // Own panel text scan

        // State
        private readonly NamingPanelModel namingPanelModel = new();
        private readonly DontCareCycle dontCareCycle = new();
        private readonly List<IUIChoice> caseAndAdminChoices = new();
        private readonly List<(IUIChoice choice, LocalizedString localizedText)> localizedChoices = new();

        // Cached References
        private UIToolkitMenuView menuView;
        private NamingStage namingStage;
        private NameScreenOrchestrator nameScreenOrchestrator;

        // Localization
        public LocalizationTableType localizationTableType { get; } = LocalizationTableType.UI;
        public List<TableEntryReference> GetLocalizationEntries()
        {
            var localizationEntries = new List<TableEntryReference>(keyboard.GetLocalizationEntries())
            {
                lowerText.TableEntryReference,
                upperText.TableEntryReference,
                backspaceText.TableEntryReference,
                dontCareText.TableEntryReference,
                confirmText.TableEntryReference,
            };
            return localizationEntries;
        }

        // UIBox Configuration
        protected override EnumLookup<UIBoxState, UIBoxStateBehaviour> BuildStateBehaviours()
        {
            var stateBehaviours = new EnumLookup<UIBoxState, UIBoxStateBehaviour>();
            stateBehaviours.TrySet(UIBoxState.Default, new UIBoxStateBehaviour(
                reconcileChoiceOptions: ReconcileKeyboardChoices,
                moveCursor: (controllerInputType, _) => StandardMoveCursorSpatial(controllerInputType))
            );
            return stateBehaviours;
        }

        #region UnityMethods
        protected override void AwakeTriggered()
        {
            preventEscapeOptionExit = true;
            clearVolatileOptionsOnEnable = false;

            menuView = GetComponent<UIToolkitMenuView>();
            namingStage = GetComponent<NamingStage>();
            nameScreenOrchestrator = GetComponentInParent<NameScreenOrchestrator>();

            menuView.SetDataSource(namingPanelModel);
            nameInput.Initialize(namingPanelModel);
            if (questionTextScan != null) { questionTextScan.SetHandleGlobalInput(false); }
            BuildChoiceOptions();
        }

        protected override void StartTriggered()
        {
            if (nameScreenOrchestrator != null && nameScreenOrchestrator.TryGetController(out BaseController baseController)) { baseController.AddInputReceiver(this, null); }
        }

        protected override void EnableTriggered()
        {
            SubscribeToQuestionUpdates(true);
            SetKeyboardToUpper(false);
            ResetAllTextElements(); // Also places the cursor (nothing is highlighted on enable)
        }

        protected override void DisableTriggered()
        {
            SubscribeToQuestionUpdates(false);
        }
        #endregion

        #region UIBoxConfiguration
        private void BuildChoiceOptions()
        {
            foreach (EntryHandle keyHandle in keyboard.CreateEntries(menuView, nameInput.Add, WithSelectSound)) { menuView.AddEntry(keyHandle); }
            keyboard.ApplyLayout(namingPanelModel);

            AddLocalizedChoice(lowerText, () => SetKeyboardToUpper(false), typeof(KeyboardCaseRow));
            AddLocalizedChoice(upperText, () => SetKeyboardToUpper(true), typeof(KeyboardCaseRow));
            AddLocalizedChoice(dontCareText, SetDontCareEntry, typeof(KeyboardAdminRow));
            AddLocalizedChoice(backspaceText, nameInput.RemoveLast, typeof(KeyboardAdminRow));
            AddLocalizedChoice(confirmText, TryAdvanceNamingRoutine, typeof(KeyboardAdminRow));
        }

        private void AddLocalizedChoice(LocalizedString localizedText, Action action, Type containerType)
        {
            var choiceEntryHandle = new ChoiceEntryHandle(menuView, localizedText.GetSafeLocalizedString(), true, WithSelectSound(action), null, containerType);
            menuView.AddEntry(choiceEntryHandle);
            caseAndAdminChoices.Add(choiceEntryHandle);
            localizedChoices.Add((choiceEntryHandle, localizedText));
        }

        private Action WithSelectSound(Action action) => () => StandardChoiceExecution(action, false);

        private void ReconcileKeyboardChoices()
        {
            // Only the active letter case is navigable
            choiceOptions.Clear();
            choiceOptions.AddRange(keyboard.GetKeys(namingPanelModel.isUpper));
            choiceOptions.AddRange(caseAndAdminChoices);
            SetChoiceAvailable(choiceOptions.Count > 0);
        }

        #endregion

        #region PublicMethods
        public void ResetAllTextElements()
        {
            foreach ((IUIChoice choice, LocalizedString localizedText) in localizedChoices) { choice.SetText(localizedText.GetSafeLocalizedString()); }

            foreach (EntryHandle keyHandle in keyboard.RebuildLetterEntries(menuView, nameInput.Add, WithSelectSound)) { menuView.AddEntry(keyHandle); }
            keyboard.ApplyLayout(namingPanelModel);
            ReconcileChoiceOptions();
            if (IsChoiceAvailable() && !IsChoiceAlive(highlightedChoiceOption)) { SetHighlightedChoice(choiceOptions[0]); } // The cursor was on a key that no longer exists
        }
        #endregion

        #region EventHandling
        private void SubscribeToQuestionUpdates(bool enable)
        {
            if (nameScreenOrchestrator == null) { return; }

            nameScreenOrchestrator.stateChanged -= HandleStateChange;
            if (enable) { nameScreenOrchestrator.stateChanged += HandleStateChange; }
        }

        private void HandleStateChange(NameScreenState nameScreenState, NameScreenQuestion question)
        {
            switch (nameScreenState)
            {
                case NameScreenState.Naming:
                    SetupQuestion(question);
                    break;
                case NameScreenState.NamingComplete:
                    namingStage.ClearCharacter(walkOffBeforeAdvancing, AdvanceOrchestratorState);
                    break;
                case NameScreenState.Intro:
                case NameScreenState.FrameFlavouring:
                case NameScreenState.Confirm:
                case NameScreenState.Exit:
                default:
                    break;
            }
        }

        private void SetupQuestion(NameScreenQuestion question)
        {
            if (question == null) { return; }

            nameInput.Clear();
            dontCareCycle.SetAnswers(question.localizedDontCareAnswers);
            namingStage.ShowCharacter(question.GetCharacterPrefab());
            if (questionTextScan == null) { return; }
            questionTextScan.ClearOldDialogue();
            questionTextScan.Setup(question.localizedQuestion.GetSafeLocalizedString());
        }

        private void AdvanceOrchestratorState()
        {
            if (nameScreenOrchestrator != null) { nameScreenOrchestrator.AdvanceState(); }
        }
        #endregion

        #region KeyboardActions
        private void SetKeyboardToUpper(bool enable)
        {
            namingPanelModel.isUpper = enable;
            ReconcileChoiceOptions();
        }

        private void SetDontCareEntry()
        {
            if (dontCareCycle.TryGetNext(out string dontCareAnswer)) { nameInput.SetText(dontCareAnswer); }
        }

        private void TryAdvanceNamingRoutine()
        {
            if (nameScreenOrchestrator == null || !nameInput.HasText()) { return; }
            nameScreenOrchestrator.AdvanceNamingRoutine(nameInput.GetCurrentText());
        }
        #endregion
    }
}

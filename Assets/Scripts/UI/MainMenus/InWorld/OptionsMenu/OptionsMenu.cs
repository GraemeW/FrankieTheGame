using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using LowDefMustard.Control;
using LowDefMustard.UIBox;
using LowDefMustard.Utils;
using LowDefMustard.Localization;
using Frankie.Speech.UI;
using Frankie.Saving;
using Frankie.Utils.Localization;

namespace Frankie.Menu.UI
{
    // Coordinates the option groups (volume, display, language):  build order, save / cancel, unsaved-changes confirmation
    [RequireComponent(typeof(UIToolkitMenuView))]
    public sealed class OptionsMenu : UIBox<UIBoxState>, ILocalizable
    {
        [Header("Text")]
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedOptionsHeader;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedConfirmText;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedCancelText;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedConfirmChangesText;
        [Header("Options")]
        [SerializeField] private VolumeOptions volumeOptions = new();
        [SerializeField] private DisplayOptions displayOptions = new();
        [SerializeField] private LanguageOptions languageOptions = new();
        [Header("Prefabs")]
        [SerializeField] private DialogueOptionBox dialogueOptionBoxPrefab;

        // Cached References
        private UIToolkitMenuView menuView;
        private StartMenu cachedStartMenu;
        private EscapeMenu cachedEscapeMenu;

        // State
        private readonly OptionsMenuModel optionsMenuModel = new();
        private readonly List<(IUIChoice choice, LocalizedString localizedText)> localizedChoices = new();
        private bool wasChangeMade = false;
        private Coroutine displayCoroutine; // Single slot:  screen changes never overlap, and none outlives the menu

        // UIBox Configuration
        protected override EnumLookup<UIBoxState,UIBoxStateBehaviour> BuildStateBehaviours()
        {
            var optionsMenuConfiguration = new EnumLookup<UIBoxState,UIBoxStateBehaviour>();
            var defaultStateBehaviour = new UIBoxStateBehaviour(
                setupChoiceOptions: ImplementSetUpChoiceOptions,
                isBackInput: ImplementIsBackInput,
                tryHandleBackNavigation: ImplementTryHandleBackNavigation);
            optionsMenuConfiguration.TrySet(UIBoxState.Default, defaultStateBehaviour);
            return optionsMenuConfiguration;
        }

        #region UnityMethods
        protected override void AwakeTriggered()
        {
            menuView = GetComponent<UIToolkitMenuView>();
            menuView.SetDataSource(optionsMenuModel);
        }

        protected override void StartTriggered()
        {
            cachedStartMenu = FindAnyObjectByType<StartMenu>();
            ResetText();
        }

        protected override void EnableTriggered()
        {
            SubscribeToEvents(true);
        }

        protected override void DisableTriggered()
        {
            SubscribeToEvents(false);
            StopDisplayCoroutine();
        }
        #endregion

        #region LocalizationMethods
        public LocalizationTableType localizationTableType { get; } = LocalizationTableType.UI;
        public List<TableEntryReference> GetLocalizationEntries()
        {
            var localizationEntries = new List<TableEntryReference>
            {
                localizedOptionsHeader.TableEntryReference,
                localizedConfirmText.TableEntryReference,
                localizedCancelText.TableEntryReference,
                localizedConfirmChangesText.TableEntryReference,
            };
            localizationEntries.AddRange(volumeOptions.GetLocalizationEntries());
            localizationEntries.AddRange(displayOptions.GetLocalizationEntries());
            localizationEntries.AddRange(languageOptions.GetLocalizationEntries());
            return localizationEntries;
        }

        private void ResetText()
        {
            optionsMenuModel.headerText = localizedOptionsHeader.GetSafeLocalizedString();
            volumeOptions.ResetText();
            displayOptions.ResetText(optionsMenuModel);
            languageOptions.ResetText(optionsMenuModel);
            foreach ((IUIChoice choice, LocalizedString localizedText) in localizedChoices) { choice.SetText(localizedText.GetSafeLocalizedString()); }
        }
        #endregion

        #region PublicMethods
        public void Setup(EscapeMenu setEscapeMenu)
        {
            cachedEscapeMenu = setEscapeMenu;
            if (gameObject.activeSelf) { SubscribeToEscapeMenu(true); }
        }
        #endregion

        #region ChoiceSetup
        private void ImplementSetUpChoiceOptions()
        {
            if (choiceOptions.Count == 0) { BuildChoiceOptions(); }
            ReconcileChoiceOptions();
        }

        private void BuildChoiceOptions()
        {
            // Note:  Creation order is navigation order
            AddEntries(volumeOptions.CreateEntries(menuView));
            AddEntries(displayOptions.CreateEntries(menuView, RunDisplayCoroutine, WithSelectSound));
            AddEntries(languageOptions.CreateEntries(menuView, WithSelectSound));
            AddLocalizedChoice(localizedConfirmText, SaveAndExit);
            AddLocalizedChoice(localizedCancelText, Cancel);
        }

        private void AddEntries(IEnumerable<EntryHandle> entryHandles)
        {
            foreach (EntryHandle entryHandle in entryHandles)
            {
                menuView.AddEntry(entryHandle);
                if (entryHandle is IUIChoice choice) { choiceOptions.Add(choice); }
            }
        }

        private void AddLocalizedChoice(LocalizedString localizedText, Action action)
        {
            var choiceEntryHandle = new ChoiceEntryHandle(menuView, localizedText.GetSafeLocalizedString(), true, WithSelectSound(action), typeof(OptionsConfirmSection));
            AddEntries(new EntryHandle[] { choiceEntryHandle });
            localizedChoices.Add((choiceEntryHandle, localizedText));
        }

        private Action WithSelectSound(Action action) => () => StandardChoiceExecution(action, false);
        #endregion

        #region PrivateMethods
        private void SubscribeToEvents(bool enable)
        {
            SubscribeToEscapeMenu(enable);
            volumeOptions.changed -= HandleOptionChanged;
            displayOptions.changed -= HandleOptionChanged;
            languageOptions.localeChanged -= HandleLocaleChanged;
            if (!enable) { return; }

            volumeOptions.changed += HandleOptionChanged;
            displayOptions.changed += HandleOptionChanged;
            languageOptions.localeChanged += HandleLocaleChanged;
        }

        private void SubscribeToEscapeMenu(bool enable)
        {
            if (cachedEscapeMenu == null) { return; }
            cachedEscapeMenu.escapeMenuItemSelected -= Cancel;
            if (enable) { cachedEscapeMenu.escapeMenuItemSelected += Cancel; }
        }

        private void HandleOptionChanged() => wasChangeMade = true;

        private void HandleLocaleChanged()
        {
            wasChangeMade = true;
            ResetText();
            if (cachedStartMenu != null) { cachedStartMenu.ResetAllTextElements(); }
            if (cachedEscapeMenu != null) { cachedEscapeMenu.ResetAllTextElements(); }
        }

        private void Save()
        {
            displayOptions.Save();
            volumeOptions.Save();
            PlayerPrefsController.SaveToDisk();
        }

        private void SaveAndExit()
        {
            Save();
            destroyQueued = true;
        }

        private void RevertImmediateOptions()
        {
            volumeOptions.Revert();
            languageOptions.Revert();
        }

        private void ForceCancel()
        {
            StopDisplayCoroutine(); // Pending screen change would otherwise land after the forced revert
            RevertImmediateOptions();
            displayOptions.ForceRevert();
        }

        private void Cancel()
        {
            // Since resolution updates done over several frames, need to kick off Coroutine
            RunDisplayCoroutine(CancelRoutine());
        }

        private void RunDisplayCoroutine(IEnumerator routine)
        {
            StopDisplayCoroutine();
            displayCoroutine = StartCoroutine(routine);
        }

        private void StopDisplayCoroutine()
        {
            if (displayCoroutine == null) { return; }
            StopCoroutine(displayCoroutine);
            displayCoroutine = null;
        }

        private IEnumerator CancelRoutine()
        {
            RevertImmediateOptions();
            yield return displayOptions.Revert();
            destroyQueued = true;
        }

        private void SpawnConfirmationMenu()
        {
            var choiceActionPairs = new List<ChoiceActionPair>
            {
                new(localizedConfirmText.GetSafeLocalizedString(), Save),
                new(localizedCancelText.GetSafeLocalizedString(), ForceCancel)
            };

            DialogueOptionBox confirmChoiceOptionMenu = Instantiate(dialogueOptionBoxPrefab, transform.parent);
            confirmChoiceOptionMenu.Setup(localizedConfirmChangesText.GetSafeLocalizedString());
            confirmChoiceOptionMenu.OverrideChoiceOptions(choiceActionPairs);

            controller.AddInputReceiver(confirmChoiceOptionMenu, () => destroyQueued = true);
        }
        #endregion

        #region InputHandling
        private bool ImplementIsBackInput(ControllerInputType controllerInputType) => controllerInputType is ControllerInputType.Cancel or ControllerInputType.Option or ControllerInputType.Escape;

        private bool ImplementTryHandleBackNavigation(ControllerInputType controllerInputType)
        {
            if (!wasChangeMade) { return false; }
            SpawnConfirmationMenu();
            return true;
        }
        #endregion
    }
}

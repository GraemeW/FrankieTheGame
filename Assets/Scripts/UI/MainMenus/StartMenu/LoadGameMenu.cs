using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using LowDefMustard.Control;
using LowDefMustard.UIBox;
using LowDefMustard.Utils;
using LowDefMustard.Localization;
using Frankie.Saving;
using Frankie.Speech.UI;
using Frankie.Utils.Localization;

namespace Frankie.Menu.UI
{
    [RequireComponent(typeof(UIToolkitMenuView))]
    public sealed class LoadGameMenu : UIBox<UIBoxState>, ILocalizable
    {
        [Header("Configuration")]
        [SerializeField] private int maxSaves = 5;
        [Header("Text")]
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedLoadHeaderText;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedLevelLabelText;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedOptionNewGameText;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedOptionLoadGameText;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedOptionDeleteGameText;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedMessageGameSelectOptionText;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedMessageConfirmDeletionText;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedMessageAffirmative;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedMessageNegative;
        [Header("Prefabs")]
        [SerializeField] private DialogueOptionBox dialogueOptionBoxPrefab;

        // State
        private readonly LoadGameMenuModel loadGameMenuModel = new();

        // Cached References
        private UIToolkitMenuView menuView;

        // UIBox Configuration
        protected override EnumLookup<UIBoxState, UIBoxStateBehaviour> BuildStateBehaviours()
        {
            var loadGameMenuConfiguration = new EnumLookup<UIBoxState, UIBoxStateBehaviour>();
            loadGameMenuConfiguration.TrySet(UIBoxState.Default, new UIBoxStateBehaviour(setupChoiceOptions: ResetUI));
            return loadGameMenuConfiguration;
        }

        #region UnityMethods
        protected override void AwakeTriggered()
        {
            menuView = GetComponent<UIToolkitMenuView>();
            menuView.SetDataSource(loadGameMenuModel);
        }

        protected override void StartTriggered()
        {
            loadGameMenuModel.headerText = localizedLoadHeaderText.GetSafeLocalizedString();
        }
        #endregion
        
        #region LocalizationMethods
        public LocalizationTableType localizationTableType { get; } = LocalizationTableType.UI;
        public List<TableEntryReference> GetLocalizationEntries()
        {
            return new List<TableEntryReference>
            {
                localizedLoadHeaderText.TableEntryReference,
                localizedLevelLabelText.TableEntryReference,
                localizedOptionNewGameText.TableEntryReference,
                localizedOptionLoadGameText.TableEntryReference,
                localizedOptionDeleteGameText.TableEntryReference,
                localizedMessageGameSelectOptionText.TableEntryReference,
                localizedMessageConfirmDeletionText.TableEntryReference,
                localizedMessageAffirmative.TableEntryReference,
                localizedMessageNegative.TableEntryReference
            };
        }
        #endregion
        
        #region PrivateMethods
        private void ResetUI()
        {
            ClearChoiceSelections();
            menuView.ClearEntries();
            choiceOptions.Clear();

            for (int index = 0; index < maxSaves; index++)
            {
                string saveName = SaveFileManager.GetSaveNameForIndex(index);
                var saveSlotModel = new SaveSlotModel
                {
                    indexText = index.ToString(CultureInfo.InvariantCulture),
                    levelLabel = localizedLevelLabelText.GetSafeLocalizedString()
                };

                Action onChoose;
                if (SaveFileManager.HasSave(saveName))
                {
                    SaveFileManager.GetInfoFromSave(saveName, out string characterName, out int level);
                    saveSlotModel.characterName = characterName;
                    saveSlotModel.levelText = level.ToString(CultureInfo.InvariantCulture);
                    onChoose = () => SpawnGameSelectOptions(saveName);
                }
                else
                {
                    saveSlotModel.characterName = localizedOptionNewGameText.GetSafeLocalizedString();
                    saveSlotModel.levelText = 0.ToString(CultureInfo.InvariantCulture);
                    onChoose = () => StartNewGame(saveName);
                }

                var saveSlotHandle = new SaveSlotHandle(menuView, saveSlotModel, () => StandardChoiceExecution(onChoose, false));
                menuView.AddEntry(saveSlotHandle);
                choiceOptions.Add(saveSlotHandle);
            }

            AddSeparator();
            AddChoiceOption(localizedMessageNegative.GetSafeLocalizedString(), null); // Destroys the menu (cancel)
            ReconcileChoiceOptions();
        }

        private void StartNewGame(string saveName)
        {
            SetActiveInput(false);
            SaveFileManager.NewGame(saveName);
        }

        private void SpawnGameSelectOptions(string saveName)
        {
            DialogueOptionBox dialogueOptionBox = Instantiate(dialogueOptionBoxPrefab, transform.parent);
            dialogueOptionBox.Setup(localizedMessageGameSelectOptionText.GetSafeLocalizedString());
            var choiceActionPairs = new List<ChoiceActionPair>
            {
                new(localizedOptionLoadGameText.GetSafeLocalizedString(), () =>
                {
                    SetActiveInput(false);
                    // Prevent LoadGameMenu from re-enablement after dialogueOptionBox disappears
                    TriggerUIBoxModified(ReceiverModifiedType.ClientDisable, new ReceiverModifiedData(this));
                    SaveFileManager.LoadGame(saveName);
                }),
                new(localizedOptionDeleteGameText.GetSafeLocalizedString(), () =>
                {
                    SpawnConfirmDeletionOptions(saveName);
                    Destroy(dialogueOptionBox.gameObject);
                })
            };
            dialogueOptionBox.OverrideChoiceOptions(choiceActionPairs);
            controller.AddInputReceiver(dialogueOptionBox, null);
        }

        private void SpawnConfirmDeletionOptions(string saveName)
        {
            DialogueOptionBox dialogueOptionBox = Instantiate(dialogueOptionBoxPrefab, transform.parent);
            dialogueOptionBox.Setup(localizedMessageConfirmDeletionText.GetSafeLocalizedString());
            var choiceActionPairs = new List<ChoiceActionPair>
            {
                new(localizedMessageAffirmative.GetSafeLocalizedString(), () =>
                {
                    SaveFileManager.Delete(saveName);
                    Destroy(dialogueOptionBox.gameObject);
                    ResetUI();
                }),
                new(localizedMessageNegative.GetSafeLocalizedString(), () => Destroy(dialogueOptionBox.gameObject))
            };

            dialogueOptionBox.OverrideChoiceOptions(choiceActionPairs);
            controller.AddInputReceiver(dialogueOptionBox, null);
        }
        #endregion
    }
}

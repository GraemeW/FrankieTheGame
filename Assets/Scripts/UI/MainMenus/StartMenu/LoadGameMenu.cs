using UnityEngine;
using System.Collections.Generic;
using TMPro;
using Frankie.Core;
using Frankie.Utils;
using Frankie.Utils.UI;
using Frankie.Speech.UI;
using Frankie.Utils.Localization;
using Frankie.ZoneManagement;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace Frankie.Menu.UI
{
    public class LoadGameMenu : UIBox, ILocalizable
    {
        [Header("Configuration")]
        [SerializeField] private int maxSaves = 5;
        [Header("Text")]
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedLoadHeaderText;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedOptionNewGameText;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedOptionLoadGameText;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedOptionDeleteGameText;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedMessageGameSelectOptionText;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedMessageConfirmDeletionText;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedMessageAffirmative;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedMessageNegative;
        [Header("Hookups and Prefabs")]
        [SerializeField] private TMP_Text loadHeaderField;
        [SerializeField] private UIChoiceButton cancelOption;
        [SerializeField] protected DialogueOptionBox dialogueOptionBoxPrefab;

        // State
        private Zone newGameZoneOverride;
        
        #region UnityMethods
        private void Start()
        {
            if (loadHeaderField != null) { loadHeaderField.SetText(localizedLoadHeaderText.GetSafeLocalizedString());}
        }
        
        protected override void OnEnable()
        {
            base.OnEnable();
            ResetUI();
        }
        #endregion
        
        #region LocalizationMethods
        public LocalizationTableType localizationTableType { get; } = LocalizationTableType.UI;
        public List<TableEntryReference> GetLocalizationEntries()
        {
            return new List<TableEntryReference>
            {
                localizedLoadHeaderText.TableEntryReference,
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
        
        #region PublicMethods
        public void Setup(Zone setNewGameZoneOverride)
        {
            newGameZoneOverride = setNewGameZoneOverride;
        }
        
        public void Cancel()
        {
            HandleClientExit();
            Destroy(gameObject);
        }
        #endregion

        #region PrivateMethods
        private void ResetUI()
        {
            foreach (Transform child in optionParent)
            {
                Destroy(child.gameObject);
            }

            choiceOptions.Clear();
            for (int index = 0; index < maxSaves; index++)
            {
                string saveName = SavingWrapper.GetSaveNameForIndex(index);

                GameObject loadGameEntryObject = Instantiate(optionButtonPrefab, optionParent);
                var loadGameEntry = loadGameEntryObject.GetComponent<LoadGameEntry>();
                if (SavingWrapper.HasSave(saveName))
                {
                    SavingWrapper.GetInfoFromName(saveName, out string characterName, out int level);
                    loadGameEntry.Setup(index, characterName, level, () => SpawnGameSelectOptions(saveName));
                }
                else
                {
                    loadGameEntry.Setup(index, localizedOptionNewGameText.GetSafeLocalizedString(), 0, () =>
                    {
                        EnableInput(false);
                        SavingWrapper.NewGame(saveName, newGameZoneOverride);
                    });
                }
                loadGameEntry.SetChoiceOrder(choiceOptions.Count + 1);
                choiceOptions.Add(loadGameEntry);
            }

            cancelOption.SetChoiceOrder(maxSaves);
            choiceOptions.Add(cancelOption);
        }

        private void SpawnGameSelectOptions(string saveName)
        {
            DialogueOptionBox dialogueOptionBox = Instantiate(dialogueOptionBoxPrefab, transform.parent);
            dialogueOptionBox.Setup(localizedMessageGameSelectOptionText.GetSafeLocalizedString());
            var choiceActionPairs = new List<ChoiceActionPair>
            {
                new(localizedOptionLoadGameText.GetSafeLocalizedString(), () =>
                {
                    EnableInput(false);
                    SavingWrapper.LoadGame(saveName);
                }),
                new(localizedOptionDeleteGameText.GetSafeLocalizedString(), () =>
                {
                    SpawnConfirmDeletionOptions(saveName);
                    Destroy(dialogueOptionBox.gameObject);
                })
            };

            dialogueOptionBox.OverrideChoiceOptions(choiceActionPairs);
            PassControl(dialogueOptionBox);
            dialogueOptionBox.ClearDisableCallbacksOnChoose(true);
        }

        private void SpawnConfirmDeletionOptions(string saveName)
        {
            DialogueOptionBox dialogueOptionBox = Instantiate(dialogueOptionBoxPrefab, transform.parent);
            dialogueOptionBox.Setup(localizedMessageConfirmDeletionText.GetSafeLocalizedString());
            var choiceActionPairs = new List<ChoiceActionPair>
            {
                new(localizedMessageAffirmative.GetSafeLocalizedString(), () =>
                {
                    SavingWrapper.Delete(saveName);
                    Destroy(dialogueOptionBox.gameObject);
                    ResetUI();
                }),
                new(localizedMessageNegative.GetSafeLocalizedString(), () => Destroy(dialogueOptionBox.gameObject))
            };

            dialogueOptionBox.OverrideChoiceOptions(choiceActionPairs);
            PassControl(dialogueOptionBox);
        }
        #endregion
    }
}

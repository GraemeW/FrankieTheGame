using UnityEngine;
using System.Collections.Generic;
using Frankie.Core;
using Frankie.Utils;
using Frankie.Utils.UI;
using Frankie.Speech.UI;
using Frankie.ZoneManagement;

namespace Frankie.Menu.UI
{
    public class LoadGameMenu : UIBox
    {
        [Header("Main Properties")]
        [SerializeField] private int maxSaves = 5;
        [SerializeField] private string optionNewGameText = "New Game";
        [SerializeField] private string optionLoadGameText = "Load Game";
        [SerializeField] private string optionDeleteGameText = "Delete Game";
        [SerializeField] private string messageGameSelectOptionText = "What do you want to do?";
        [SerializeField] private string messageConfirmDeletionText = "Are you really sure?";
        [SerializeField] private string messageAffirmative = "Yeah";
        [SerializeField] private string messageNegative = "Nah";
        [Header("Hookups and Prefabs")]
        [SerializeField] private UIChoiceButton cancelOption;
        [SerializeField] protected DialogueOptionBox dialogueOptionBoxPrefab;

        // State
        private Zone newGameZoneOverride;
        
        #region UnityMethods
        protected override void OnEnable()
        {
            base.OnEnable();
            ResetUI();
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
                LoadGameEntry loadGameEntry = loadGameEntryObject.GetComponent<LoadGameEntry>();
                if (SavingWrapper.HasSave(saveName))
                {
                    SavingWrapper.GetInfoFromName(saveName, out string characterName, out int level);
                    loadGameEntry.Setup(index, characterName, level, () => SpawnGameSelectOptions(saveName));
                }
                else
                {
                    loadGameEntry.Setup(index, optionNewGameText, 0, () => SavingWrapper.NewGame(saveName, newGameZoneOverride));
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
            dialogueOptionBox.Setup(messageGameSelectOptionText);
            var choiceActionPairs = new List<ChoiceActionPair>
            {
                new(optionLoadGameText, () => SavingWrapper.LoadGame(saveName)),
                new(optionDeleteGameText, () =>
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
            dialogueOptionBox.Setup(messageConfirmDeletionText);
            var choiceActionPairs = new List<ChoiceActionPair>
            {
                new(messageAffirmative, () =>
                {
                    SavingWrapper.Delete(saveName);
                    Destroy(dialogueOptionBox.gameObject);
                    ResetUI();
                }),
                new(messageNegative, () => Destroy(dialogueOptionBox.gameObject))
            };

            dialogueOptionBox.OverrideChoiceOptions(choiceActionPairs);
            PassControl(dialogueOptionBox);
        }
        #endregion
    }
}

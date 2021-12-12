using Frankie.Core;
using Frankie.Utils;
using Frankie.Utils.UI;
using Frankie.Speech.UI;
using UnityEngine;
using System.Collections.Generic;

namespace Frankie.Menu.UI
{
    public class LoadGameMenu : UIBox
    {
        [Header("Main Properties")]
        [SerializeField] int maxSaves = 5;
        [SerializeField] string optionNewGameText = "New Game";
        [SerializeField] string optionLoadGameText = "Load Game";
        [SerializeField] string optionDeleteGameText = "Delete Game";
        [SerializeField] string messageGameSelectOptionText = "What do you want to do?";
        [SerializeField] string messageConfirmDeletionText = "Are you really sure?";
        [SerializeField] string messageAffirmative = "Yeah";
        [SerializeField] string messageNegative = "Nah";
        [Header("Hookups and Prefabs")]
        [SerializeField] UIChoiceOption cancelOption = null;
        [SerializeField] protected DialogueOptionBox dialogueOptionBoxPrefab = null;

        // State
        LazyValue<SavingWrapper> savingWrapper;

        private void Awake()
        {
            savingWrapper = new LazyValue<SavingWrapper>(() => FindObjectOfType<SavingWrapper>());
        }

        private void Start()
        {
            savingWrapper.ForceInit();

        }

        protected override void OnEnable()
        {
            base.OnEnable();
            ResetUI();
        }

        private void ResetUI()
        {
            foreach (Transform child in optionParent)
            {
                Destroy(child.gameObject);
            }

            if (savingWrapper.value == null) { return; }

            choiceOptions.Clear();
            for (int index = 0; index < maxSaves; index++)
            {
                string saveName = SavingWrapper.GetSaveNameForIndex(index);

                GameObject loadGameEntryObject = Instantiate(optionPrefab, optionParent);
                LoadGameEntry loadGameEntry = loadGameEntryObject.GetComponent<LoadGameEntry>();
                if (savingWrapper.value.HasSave(saveName))
                {
                    SavingWrapper.GetInfoFromName(saveName, out string characterName, out int level);
                    loadGameEntry.Setup(index, characterName, level, () => SpawnGameSelectOptions(saveName));
                }
                else
                {
                    loadGameEntry.Setup(index, optionNewGameText, 0, () => savingWrapper.value.NewGame(saveName));
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
            List<ChoiceActionPair> choiceActionPairs = new List<ChoiceActionPair>();
            choiceActionPairs.Add(new ChoiceActionPair(optionLoadGameText, () => savingWrapper.value.Load(saveName)));
            choiceActionPairs.Add(new ChoiceActionPair(optionDeleteGameText, () => { SpawnConfirmDeletionOptions(saveName); Destroy(dialogueOptionBox.gameObject); }));

            dialogueOptionBox.OverrideChoiceOptions(choiceActionPairs);
            PassControl(dialogueOptionBox);
            dialogueOptionBox.ClearDisableCallbacksOnChoose(true);
        }

        private void SpawnConfirmDeletionOptions(string saveName)
        {
            DialogueOptionBox dialogueOptionBox = Instantiate(dialogueOptionBoxPrefab, transform.parent);
            dialogueOptionBox.Setup(messageConfirmDeletionText);
            List<ChoiceActionPair> choiceActionPairs = new List<ChoiceActionPair>();
            choiceActionPairs.Add(new ChoiceActionPair(messageAffirmative, () => { savingWrapper.value.Delete(saveName); Destroy(dialogueOptionBox.gameObject); ResetUI(); }));
            choiceActionPairs.Add(new ChoiceActionPair(messageNegative, () => Destroy(dialogueOptionBox.gameObject)));

            dialogueOptionBox.OverrideChoiceOptions(choiceActionPairs);
            PassControl(dialogueOptionBox);
        }

        public void Cancel()
        {
            HandleClientExit();
            Destroy(gameObject);
        }
    }
}

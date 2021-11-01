using Frankie.Core;
using Frankie.Utils;
using Frankie.Utils.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Menu.UI
{
    public class LoadGameMenu : UIBox
    {
        [SerializeField] int maxSaves = 5;
        [SerializeField] UIChoiceOption cancelOption = null;

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
                    loadGameEntry.Setup(index, characterName, level, () => savingWrapper.value.Load(saveName));
                }
                else
                {
                    loadGameEntry.Setup(index, "New Game", 0, () => savingWrapper.value.NewGame(saveName));
                }
                loadGameEntry.SetChoiceOrder(choiceOptions.Count + 1);
                choiceOptions.Add(loadGameEntry);
            }

            cancelOption.SetChoiceOrder(maxSaves);
            choiceOptions.Add(cancelOption);
        }

        public void Cancel()
        {
            HandleClientExit();
            Destroy(gameObject);
        }
    }
}

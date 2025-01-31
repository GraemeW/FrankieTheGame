using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frankie.Utils;
using Frankie.Quests;
using Frankie.Stats;
using Frankie.Inventory;
using UnityEngine.SceneManagement;

namespace Frankie.Core
{
    public class FrankieDebugger : MonoBehaviour
    {
        // Cached References
        PlayerInput playerInput = null;
        GameObject player = null;
        GameObject saver = null;

        // Lazy Values
        ReInitLazyValue<SavingWrapper> savingWrapper = null;
        ReInitLazyValue<QuestList> questList = null;
        ReInitLazyValue<Party> party = null;
        ReInitLazyValue<Wallet> wallet = null;

        // Static
        static int fundsToAddToWallet = 10000;

        #region UnityMethods
        private void Awake()
        {
            // References
            playerInput = new PlayerInput();
            player = GameObject.FindGameObjectWithTag("Player");
            saver = GameObject.FindGameObjectWithTag("Saver");
            savingWrapper = new ReInitLazyValue<SavingWrapper>(SetupSavingWrapper);
            questList = new ReInitLazyValue<QuestList>(() => QuestList.GetQuestList(ref player));
            party = new ReInitLazyValue<Party>(SetupParty);
            wallet = new ReInitLazyValue<Wallet>(SetupWallet);

            // Debug Hook-Ups
            playerInput.Admin.Save.performed += context => Save();
            playerInput.Admin.Load.performed += context => Continue();
            playerInput.Admin.Delete.performed += context => Delete();
            playerInput.Admin.QuestLog.performed += context => PrintQuests();
            playerInput.Admin.LevelUpParty.performed += context => LevelUpParty();
            playerInput.Admin.AddFundsToWallet.performed += context => AddFundsToWallet();
        }

        private void Start()
        {
            savingWrapper.ForceInit();
            questList.ForceInit();
            party.ForceInit();
            wallet.ForceInit();
        }

        private void OnEnable()
        {
            playerInput.Admin.Enable();
            SceneManager.sceneLoaded += ResetReferences;
        }

        private void OnDisable()
        {
            playerInput.Admin.Disable();
            SceneManager.sceneLoaded -= ResetReferences;
        }
        #endregion

        #region SavingWrapperDebug
        private SavingWrapper SetupSavingWrapper()
        {
            if (saver == null) { saver = GameObject.FindGameObjectWithTag("Saver"); }
            return saver.GetComponent<SavingWrapper>();
        }

        private Party SetupParty()
        {
            if (player == null) { player = GameObject.FindGameObjectWithTag("Player"); }
            return player?.GetComponent<Party>();
        }

        private Wallet SetupWallet()
        {
            if (player == null) { player = GameObject.FindGameObjectWithTag("Player"); }
            return player?.GetComponent<Wallet>();
        }

        private void Save()
        {
            savingWrapper.value.Save();
        }

        private void Continue()
        {
            savingWrapper.value.Continue();
        }

        private void Delete()
        {
            savingWrapper.value.Delete();
        }
        #endregion

        #region QuestListDebug
        private void ResetReferences(Scene scene, LoadSceneMode loadSceneMode)
        {
            // Since Debugger is a persistent object, force reset on scene load
            questList = null;
            questList = new ReInitLazyValue<QuestList>(() => QuestList.GetQuestList(ref player));
            questList.ForceInit();

            wallet = null;
            wallet = new ReInitLazyValue<Wallet>(SetupWallet);
            wallet.ForceInit();

            party = null;
            party = new ReInitLazyValue<Party>(SetupParty);
            party.ForceInit();
        }

        private void PrintQuests()
        {
            UnityEngine.Debug.Log("Printing Quests:");
            foreach (QuestStatus questStatus in questList.value.GetActiveQuests())
            {
                Quest quest = questStatus.GetQuest();
                UnityEngine.Debug.Log($"Quest: {quest.name} - {quest.GetDetail()}");
                UnityEngine.Debug.Log($"Completed:  {questStatus.GetCompletedObjectiveCount()} of {quest.GetObjectiveCount()} objectives");
                UnityEngine.Debug.Log($"Status:  {questStatus.IsComplete()}, Reward Disposition:  {questStatus.IsRewardGiven()})");
                UnityEngine.Debug.Log("---Fin---");
            }
        }
        #endregion

        #region PartyDebug
        private void LevelUpParty()
        {
            UnityEngine.Debug.Log("Leveling up party:");
            foreach (BaseStats character in party.value.GetParty())
            {
                UnityEngine.Debug.Log($"{character.GetCharacterProperties().GetCharacterNamePretty()} has gained a level");
                character.IncrementLevel();
            }
        }
        #endregion

        #region WalletDebug
        private void AddFundsToWallet()
        {
            UnityEngine.Debug.Log($"Adding ${fundsToAddToWallet} to wallet");
            wallet.value.UpdateCash(fundsToAddToWallet);
        }
        #endregion


    }
}

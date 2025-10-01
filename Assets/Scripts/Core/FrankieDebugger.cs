using UnityEngine;
using Frankie.Utils;
using Frankie.Quests;
using Frankie.Stats;
using Frankie.Inventory;
using UnityEngine.SceneManagement;
using Frankie.Saving;

namespace Frankie.Core
{
    public class FrankieDebugger : MonoBehaviour
    {
        [SerializeField] bool resetSaveOnStart = false;

        // Cached References
        PlayerInput playerInput = null;

        // Lazy Values
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
            questList = new ReInitLazyValue<QuestList>(SetupQuestList);
            party = new ReInitLazyValue<Party>(SetupParty);
            wallet = new ReInitLazyValue<Wallet>(SetupWallet);

            // Debug Hook-Ups
            playerInput.Admin.Save.performed += context => Save();
            playerInput.Admin.Load.performed += context => Continue();
            playerInput.Admin.Delete.performed += context => Delete();
            playerInput.Admin.ClearPlayerPrefs.performed += context => ClearPlayerPrefs();
            playerInput.Admin.QuestLog.performed += context => PrintQuests();
            playerInput.Admin.LevelUpParty.performed += context => LevelUpParty();
            playerInput.Admin.AddFundsToWallet.performed += context => AddFundsToWallet();
        }

        private void Start()
        {
            questList.ForceInit();
            party.ForceInit();
            wallet.ForceInit();

            if (resetSaveOnStart)
            {
                Delete();
                Save();
            }
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

        private QuestList SetupQuestList() => Player.FindPlayerObject()?.GetComponent<QuestList>();
        private Party SetupParty() => Player.FindPlayerObject()?.GetComponent<Party>();
        private Wallet SetupWallet() => Player.FindPlayerObject()?.GetComponent<Wallet>();
        #endregion

        #region SavingWrapperDebug
        private void Save()
        {
            UnityEngine.Debug.Log($"Frankie Debugger:  Saving Game...");
            SavingWrapper.SetSaveToDebug();
            SavingWrapper.Save();
        }

        private void Continue()
        {
            UnityEngine.Debug.Log($"Frankie Debugger:  Loading Game...");
            SavingWrapper.Continue();
        }

        private void Delete()
        {
            UnityEngine.Debug.Log($"Frankie Debugger:  Deleting Game...");
            SavingWrapper.Delete();
            SavingWrapper.DeleteSession();
            SavingWrapper.DeleteDebugSave();
        }

        private void ClearPlayerPrefs()
        {
            UnityEngine.Debug.Log($"Frankie Debugger:  Clearing Player Prefs...");
            PlayerPrefsController.ClearPlayerPrefs();
        }
        #endregion

        #region QuestListDebug
        private void ResetReferences(Scene scene, LoadSceneMode loadSceneMode)
        {
            // Since Debugger is a persistent object, force reset on scene load
            questList.ForceInit();
            wallet.ForceInit();
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

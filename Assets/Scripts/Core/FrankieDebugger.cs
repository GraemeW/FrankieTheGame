using UnityEngine;
using UnityEngine.SceneManagement;
using Frankie.Utils;
using Frankie.Quests;
using Frankie.Stats;
using Frankie.Inventory;
using Frankie.Saving;

namespace Frankie.Core
{
    public class FrankieDebugger : MonoBehaviour
    {
        [SerializeField] private bool resetSaveOnStart = false;

        // Cached References
        private PlayerInput playerInput;

        // Lazy Values
        private ReInitLazyValue<QuestList> questList;
        private ReInitLazyValue<Party> party;
        private ReInitLazyValue<Wallet> wallet;

        // Static
        private const int _fundsToAddToWallet = 10000;

        #region UnityMethods
        private void Awake()
        {
            // References
            playerInput = new PlayerInput();
            questList = new ReInitLazyValue<QuestList>(SetupQuestList);
            party = new ReInitLazyValue<Party>(SetupParty);
            wallet = new ReInitLazyValue<Wallet>(SetupWallet);

            // Debug Hook-Ups
            playerInput.Admin.Save.performed += _ => Save();
            playerInput.Admin.Load.performed += _ => Continue();
            playerInput.Admin.Delete.performed += _ => Delete();
            playerInput.Admin.ClearPlayerPrefs.performed += _ => ClearPlayerPrefs();
            playerInput.Admin.QuestLog.performed += _ => PrintQuests();
            playerInput.Admin.LevelUpParty.performed += _ => LevelUpParty();
            playerInput.Admin.AddFundsToWallet.performed += _ => AddFundsToWallet();
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
            Debug.Log($"Frankie Debugger:  Saving Game...");
            SavingWrapper.SetSaveToDebug();
            SavingWrapper.Save();
        }

        private void Continue()
        {
            Debug.Log($"Frankie Debugger:  Loading Game...");
            SavingWrapper.Continue();
        }

        private void Delete()
        {
            Debug.Log($"Frankie Debugger:  Deleting Game...");
            SavingWrapper.Delete();
            SavingWrapper.DeleteSession();
            SavingWrapper.DeleteDebugSave();
            Debug.Log($"Initializing Save for Debug...");
            Save();
            Continue();
        }

        private void ClearPlayerPrefs()
        {
            Debug.Log($"Frankie Debugger:  Clearing Player Prefs...");
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
            Debug.Log("Printing Quests:");
            foreach (QuestStatus questStatus in questList.value.GetActiveQuests())
            {
                Quest quest = questStatus.GetQuest();
                Debug.Log($"Quest: {quest.name} - {quest.GetDetail()}");
                Debug.Log($"Completed:  {questStatus.GetCompletedObjectiveCount()} of {quest.GetObjectiveCount()} objectives");
                Debug.Log($"Status:  {questStatus.IsComplete()}, Reward Disposition:  {questStatus.IsRewardGiven()})");
                Debug.Log("---Fin---");
            }
        }
        #endregion

        #region PartyDebug
        private void LevelUpParty()
        {
            Debug.Log("Leveling up party:");
            foreach (BaseStats character in party.value.GetParty())
            {
                Debug.Log($"{character.GetCharacterProperties().GetCharacterNamePretty()} has gained a level");
                character.IncrementLevel();
            }
        }
        #endregion

        #region WalletDebug
        private void AddFundsToWallet()
        {
            Debug.Log($"Adding ${_fundsToAddToWallet} to wallet");
            wallet.value.UpdateCash(_fundsToAddToWallet);
        }
        #endregion


    }
}

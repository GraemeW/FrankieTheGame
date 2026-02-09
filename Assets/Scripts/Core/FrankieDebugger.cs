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
        // Tunables
        [SerializeField] private int fundsToAddToWallet = 100;
        [SerializeField] private bool resetSaveOnStart = false;

        // Cached References
        private PlayerInput playerInput;

        // Lazy Values
        private ReInitLazyValue<QuestList> questList;
        private ReInitLazyValue<Party> party;
        private ReInitLazyValue<Wallet> wallet;

        #region StaticMethods
        private static QuestList SetupQuestList()
        {
            GameObject playerObject = Player.FindPlayerObject();
            return playerObject !=null ? playerObject.GetComponent<QuestList>() : null;
        }
        private static Party SetupParty()
        {
            GameObject playerObject = Player.FindPlayerObject();
            return playerObject !=null ? playerObject.GetComponent<Party>() : null;
        }

        private static Wallet SetupWallet()
        {
            GameObject playerObject = Player.FindPlayerObject();
            return playerObject !=null ? playerObject.GetComponent<Wallet>() : null;
        }
        #endregion
        
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
            playerInput.Admin.NewGame.performed += _ => NewSave();
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

            if (resetSaveOnStart) { NewSave(); }
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
        private void Save()
        {
            Debug.Log($"Frankie Debugger:  Saving Game...");
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
            SavingWrapper.DeleteSession();
            SavingWrapper.Delete();
        }

        private void NewSave()
        {
            Save();
            Delete();
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
            Debug.Log($"Adding ${fundsToAddToWallet} to wallet");
            wallet.value.UpdateCash(fundsToAddToWallet);
        }
        #endregion
    }
}

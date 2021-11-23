using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frankie.Utils;
using Frankie.Quests;
using System.Linq;

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

        #region UnityMethods
        private void Awake()
        {
            // References
            playerInput = new PlayerInput();
            player = GameObject.FindGameObjectWithTag("Player");
            saver = GameObject.FindGameObjectWithTag("Saver");
            savingWrapper = new ReInitLazyValue<SavingWrapper>(SetupSavingWrapper);
            questList = new ReInitLazyValue<QuestList>(() => QuestList.GetQuestList(ref player));

            // Debug Hook-Ups
            playerInput.Debug.Save.performed += context => Save();
            playerInput.Debug.Load.performed += context => Continue();
            playerInput.Debug.Delete.performed += context => Delete();
            playerInput.Debug.QuestLog.performed += context => PrintQuests();
        }

        private void Start()
        {
            savingWrapper.ForceInit();
            questList.ForceInit();
        }

        private void OnEnable()
        {
            playerInput.Debug.Enable();
        }

        private void OnDisable()
        {
            playerInput.Debug.Disable();
        }
        #endregion

        #region SavingWrapperDebug
        private SavingWrapper SetupSavingWrapper()
        {
            if (saver == null) { saver = GameObject.FindGameObjectWithTag("Saver"); }
            return saver.GetComponent<SavingWrapper>();
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
    }
}

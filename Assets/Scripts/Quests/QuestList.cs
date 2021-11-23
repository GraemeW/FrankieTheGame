using Frankie.Core;
using Frankie.Inventory;
using Frankie.Saving;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Frankie.Quests
{
    public class QuestList : MonoBehaviour, IPredicateEvaluator, ISaveable
    {
        // Tunables
        List<QuestStatus> questStatuses = new List<QuestStatus>();

        // Cached References
        PartyKnapsackConduit partyKnapsackConduit = null;

        // Events
        public event Action questListUpdated;

        public static QuestList GetQuestList(ref GameObject player)
        {
            if (player == null) { player = GameObject.FindGameObjectWithTag("Player"); }
            return player.GetComponent<QuestList>();
        }

        #region UnityMethods
        private void Awake()
        {
            partyKnapsackConduit = GetComponent<PartyKnapsackConduit>();
        }
        #endregion

        #region PublicMethods
        public IEnumerable<QuestStatus> GetActiveQuests()
        {
            return questStatuses.Where(c => !c.IsComplete());
        }

        public bool HasQuest(Quest quest)
        {
            return (GetQuestStatus(quest) != null);
        }

        public QuestStatus GetQuestStatus(Quest quest)
        {
            if (quest == null) { return null; }

            foreach (QuestStatus questStatus in questStatuses)
            {
                if (questStatus.GetQuest().GetUniqueID() == quest.GetUniqueID())
                {
                    return questStatus;
                }
            }
            return null;
        }

        public void AddQuest(Quest quest)
        {
            if (HasQuest(quest)) { return; }

            QuestStatus newQuestStatus = new QuestStatus(quest);
            questStatuses.Add(newQuestStatus);
            CompleteObjectivesForItemsInKnapsack();

            if (questListUpdated != null)
            {
                questListUpdated();
            }
        }

        public void CompleteObjective(Quest quest, QuestObjective objective)
        {
            QuestStatus questStatus = GetQuestStatus(quest);
            if (questStatus == null) { return; }
            if (questStatus.IsComplete() && questStatus.IsRewardGiven()) { return; } // Disallow completion of quests // disbursement of rewards multiple times

            questStatus.SetObjective(objective, true);

            // Standard reward handling otherwise
            if (questStatus.IsComplete() && !questStatus.IsRewardGiven())
            {
                if (TryGiveReward(quest))
                {
                    questStatus.SetRewardGiven();
                }
            }

            questListUpdated?.Invoke();
        }
        #endregion

        #region PrivateMethods
        private bool TryGiveReward(Quest quest)
        {
            if (partyKnapsackConduit == null) { return false; }

            List<Reward> rewards = quest.GetRewards();
            if (rewards.Count > partyKnapsackConduit.GetNumberOfFreeSlotsInParty()) { return false; }
            
            foreach (Reward reward in quest.GetRewards())
            {
                partyKnapsackConduit.AddToFirstEmptyPartySlot(reward.item);
            }
            return true;
        }

        private void CompleteObjectivesForItemsInKnapsack()
        {
            foreach (Knapsack knapsack in partyKnapsackConduit.GetKnapsacks())
            {
                knapsack.CompleteObjective();
            }
        }
        #endregion

        #region Interfaces
        // Predicates
        public bool? Evaluate(Predicate predicate)
        {
            PredicateQuestList predicateQuestList = predicate as PredicateQuestList;
            return predicateQuestList != null ? predicateQuestList.Evaluate(this) : null;
        }

        // Save System
        public SaveState CaptureState()
        {
            List<SerializableQuestStatus> serializableQuestStatuses = new List<SerializableQuestStatus>();
            foreach (QuestStatus questStatus in questStatuses)
            {
                serializableQuestStatuses.Add(questStatus.CaptureState());
            }

            SaveState saveState = new SaveState(GetLoadPriority(), serializableQuestStatuses);
            return saveState;
        }

        public void RestoreState(SaveState saveState)
        {
            List<SerializableQuestStatus> serializableQuestStatuses = saveState.GetState() as List<SerializableQuestStatus>;
            if (serializableQuestStatuses == null) { return; }
            questStatuses.Clear();

            foreach (SerializableQuestStatus serializableQuestStatus in serializableQuestStatuses)
            {
                QuestStatus questStatus = new QuestStatus(serializableQuestStatus);
                questStatuses.Add(questStatus);
            }

            if (questListUpdated != null)
            {
                questListUpdated();
            }
        }

        public LoadPriority GetLoadPriority()
        {
            return LoadPriority.ObjectProperty;
        }
        #endregion
    }
}
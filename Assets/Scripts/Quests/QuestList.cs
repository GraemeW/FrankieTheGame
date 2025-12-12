using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Frankie.Core;
using Frankie.Inventory;
using Frankie.Saving;

namespace Frankie.Quests
{
    [RequireComponent(typeof(PartyKnapsackConduit))]
    public class QuestList : MonoBehaviour, IPredicateEvaluator, ISaveable
    {
        // Tunables
        private readonly List<QuestStatus> questStatuses = new();

        // Cached References
        private PartyKnapsackConduit partyKnapsackConduit;

        // Events
        public event Action questListUpdated;

        #region UnityMethods
        private void Awake()
        {
            partyKnapsackConduit = GetComponent<PartyKnapsackConduit>();
        }
        #endregion

        #region PublicMethods
        public QuestStatus GetQuestStatus(Quest quest)
        {
            if (quest == null) { return null; }
            return questStatuses.FirstOrDefault(questStatus => questStatus.GetQuest().GetQuestID() == quest.GetQuestID());
        }
        
        public bool HasQuest(Quest quest) => (GetQuestStatus(quest) != null);
        
        public IEnumerable<QuestStatus> GetActiveQuests() => questStatuses.Where(c => !c.IsComplete());
        
        public QuestStatus TryAddQuest(Quest quest)
        {
            QuestStatus existingQuestStatus = GetQuestStatus(quest);
            if (existingQuestStatus != null) { return existingQuestStatus; }

            var newQuestStatus = new QuestStatus(quest);
            questStatuses.Add(newQuestStatus);
            CompleteObjectivesForItemsInKnapsack();

            questListUpdated?.Invoke();
            
            return newQuestStatus;
        }

        public void CompleteObjective(QuestObjective questObjective)
        {
            Quest quest = Quest.GetFromID(questObjective.GetQuestID());
            if (quest == null) { return; }

            // Auto-add the quest if it's not already present
            QuestStatus questStatus = TryAddQuest(quest);
            if (questStatus == null) { return; }
            
            if (questStatus.IsComplete() && questStatus.IsRewardGiven()) { return; } // Disallow completion of quests // disbursement of rewards multiple times

            questStatus.SetObjective(questObjective, true);

            // Standard reward handling otherwise
            if (questStatus.IsComplete() && !questStatus.IsRewardGiven())
            {
                questStatus.SetRewardGiven(true); // Initially set reward given BEFORE giving reward to prevent knapsackUpdated loops
                if (!TryGiveReward(quest))
                {
                    questStatus.SetRewardGiven(false); // Allow re-tries on giving awards if failing
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
            var predicateQuestList = predicate as PredicateQuestList;
            return predicateQuestList != null ? predicateQuestList.Evaluate(this) : null;
        }

        // Save System
        public LoadPriority GetLoadPriority() => LoadPriority.ObjectProperty;
        
        public SaveState CaptureState()
        {
            List<SerializableQuestStatus> serializableQuestStatuses = questStatuses.Select(questStatus => questStatus.CaptureState()).ToList();
            var saveState = new SaveState(GetLoadPriority(), serializableQuestStatuses);
            return saveState;
        }

        public void RestoreState(SaveState saveState)
        {
            var serializableQuestStatuses = saveState.GetState(typeof(List<SerializableQuestStatus>)) as List<SerializableQuestStatus>;
            if (serializableQuestStatuses == null) { return; }
            questStatuses.Clear();

            foreach (SerializableQuestStatus serializableQuestStatus in serializableQuestStatuses)
            {
                QuestStatus questStatus = new QuestStatus(serializableQuestStatus);
                questStatuses.Add(questStatus);
            }
            questListUpdated?.Invoke();
        }
        #endregion
    }
}

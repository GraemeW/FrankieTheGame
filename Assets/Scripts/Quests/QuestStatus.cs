using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Frankie.Quests
{
    public class QuestStatus
    {
        // State
        Quest quest;
        List<QuestObjective> completedObjectives = new List<QuestObjective>();
        bool rewardGiven = false;

        // Methods
        #region Constructors
        public QuestStatus(Quest quest)
        {
            this.quest = quest;
        }

        public QuestStatus(SerializableQuestStatus restoreState)
        {
            quest = Quest.GetFromID(restoreState.questID);
            completedObjectives = restoreState.completedObjectiveIDs.Select(c => QuestObjective.GetFromID(c)).ToList();
        }
        #endregion

        #region PublicMethods
        public Quest GetQuest()
        {
            return quest;
        }

        public int GetCompletedObjectiveCount()
        {
            return completedObjectives.Count;
        }

        public bool GetStatusForObjective(QuestObjective matchObjective)
        {
            foreach (QuestObjective questObjective in completedObjectives)
            {
                if (questObjective.GetUniqueID() == matchObjective.GetUniqueID())
                {
                    return true;
                }
            }
            return false;
        }

        public void SetObjective(QuestObjective objective, bool isComplete)
        {
            if (!quest.HasObjective(objective)) { return; }

            bool inCompletedObjectivesList = GetStatusForObjective(objective);
            if (isComplete && !inCompletedObjectivesList)
            {
                completedObjectives.Add(objective);
            }

            if (!isComplete && inCompletedObjectivesList)
            {
                completedObjectives.Remove(objective);
            }
        }

        public bool IsComplete()
        {
            return (completedObjectives.Count >= quest.GetObjectiveCount());
        }

        public void SetRewardGiven(bool enable)
        {
            rewardGiven = enable;
        }

        public bool IsRewardGiven()
        {
            return rewardGiven;
        }
        #endregion

        #region Interface
        public SerializableQuestStatus CaptureState()
        {
            SerializableQuestStatus serializableQuestStatus = new SerializableQuestStatus();
            serializableQuestStatus.questID = quest.GetUniqueID();
            List<string> completedObjectiveIDs = completedObjectives.Select(c => c.uniqueID).ToList();
            serializableQuestStatus.completedObjectiveIDs = completedObjectiveIDs;
            return serializableQuestStatus;
        }
        #endregion
    }
}


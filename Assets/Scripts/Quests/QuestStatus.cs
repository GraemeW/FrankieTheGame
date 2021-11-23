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
        public QuestStatus(Quest quest)
        {
            this.quest = quest;
        }

        public QuestStatus(SerializableQuestStatus restoreState)
        {
            quest = Quest.GetFromID(restoreState.questID);
            completedObjectives = restoreState.completedObjectiveIDs.Select(c => QuestObjective.GetFromID(c)).ToList();
        }

        public Quest GetQuest()
        {
            return quest;
        }

        public int GetCompletedObjectiveCount()
        {
            return completedObjectives.Count;
        }

        public bool GetStatusForObjective(QuestObjective objective)
        {
            return completedObjectives.Contains(objective);
        }

        public void SetObjective(QuestObjective objective, bool isComplete)
        {
            if (!quest.HasObjective(objective)) { return; }

            if (isComplete && !completedObjectives.Contains(objective))
            {
                completedObjectives.Add(objective);
            }

            if (!isComplete && completedObjectives.Contains(objective))
            {
                completedObjectives.Remove(objective);
            }
        }

        public bool IsComplete()
        {
            return (completedObjectives.Count >= quest.GetObjectiveCount());
        }


        public void SetRewardGiven()
        {
            rewardGiven = true;
        }

        public bool IsRewardGiven()
        {
            return rewardGiven;
        }

        public SerializableQuestStatus CaptureState()
        {
            SerializableQuestStatus serializableQuestStatus = new SerializableQuestStatus();
            serializableQuestStatus.questID = quest.GetUniqueID();
            List<string> completedObjectiveIDs = completedObjectives.Select(c => c.uniqueID).ToList();
            foreach (string completedObjectiveID in completedObjectiveIDs)
            {
                UnityEngine.Debug.Log(completedObjectiveID);
            }
            serializableQuestStatus.completedObjectiveIDs = completedObjectiveIDs;
            return serializableQuestStatus;
        }
    }
}


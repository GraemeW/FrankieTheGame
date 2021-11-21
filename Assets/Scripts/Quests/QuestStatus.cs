using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Quests
{
    public class QuestStatus
    {
        // State
        Quest quest;
        List<string> completedObjectives = new List<string>();
        bool rewardGiven = false;

        // Methods
        public QuestStatus(Quest quest)
        {
            this.quest = quest;
        }

        public QuestStatus(SerializableQuestStatus restoreState)
        {
            quest = Quest.GetFromID(restoreState.questID);
            completedObjectives = restoreState.completedObjectives;
        }

        public Quest GetQuest()
        {
            return quest;
        }

        public int GetCompletedObjectiveCount()
        {
            return completedObjectives.Count;
        }

        public bool GetStatusForObjectiveID(string objectiveID)
        {
            return completedObjectives.Contains(objectiveID);
        }

        public void SetObjective(string objectiveID, bool isComplete)
        {
            if (!quest.HasObjective(objectiveID)) { return; }

            if (isComplete && !completedObjectives.Contains(objectiveID))
            {
                completedObjectives.Add(objectiveID);
            }

            if (!isComplete && completedObjectives.Contains(objectiveID))
            {
                completedObjectives.Remove(objectiveID);
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
            serializableQuestStatus.completedObjectives = completedObjectives;
            return serializableQuestStatus;
        }
    }
}


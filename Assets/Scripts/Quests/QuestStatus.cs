using System.Collections.Generic;
using System.Linq;

namespace Frankie.Quests
{
    public class QuestStatus
    {
        // State
        private readonly Quest quest;
        private readonly List<QuestObjective> completedObjectives = new();
        private bool rewardGiven = false;

        // Methods
        #region Constructors
        public QuestStatus(Quest quest)
        {
            this.quest = quest;
        }

        public QuestStatus(SerializableQuestStatus restoreState)
        {
            quest = Quest.GetFromID(restoreState.questID);
            if (quest == null) { return; }
            completedObjectives = restoreState.completedObjectiveIDs.Select(c => quest.GetObjectiveFromID(c)).ToList();
        }
        #endregion

        #region PublicMethods
        public Quest GetQuest() => quest;
        public int GetCompletedObjectiveCount() => completedObjectives.Count;
        public bool GetStatusForObjective(QuestObjective matchObjective)
        {
            return completedObjectives.Any(questObjective => questObjective.GetObjectiveID() == matchObjective.GetObjectiveID());
        }
        public bool IsComplete() => (completedObjectives.Count >= quest.GetObjectiveCount());
        public bool IsRewardGiven() => rewardGiven;
        
        public void SetObjective(QuestObjective objective, bool isComplete)
        {
            if (!quest.HasObjective(objective)) { return; }

            bool inCompletedObjectivesList = GetStatusForObjective(objective);
            switch (isComplete)
            {
                case true when !inCompletedObjectivesList:
                    completedObjectives.Add(objective);
                    break;
                case false when inCompletedObjectivesList:
                    completedObjectives.Remove(objective);
                    break;
            }
        }

        public void SetRewardGiven(bool enable)
        {
            rewardGiven = enable;
        }
        #endregion

        #region Interface
        public SerializableQuestStatus CaptureState()
        {
            var serializableQuestStatus = new SerializableQuestStatus { questID = quest.GetQuestID() };
            List<string> completedObjectiveIDs = completedObjectives.Select(c => c.objectiveID).ToList();
            serializableQuestStatus.completedObjectiveIDs = completedObjectiveIDs;
            return serializableQuestStatus;
        }
        #endregion
    }
}

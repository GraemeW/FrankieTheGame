using System.Collections.Generic;

namespace Frankie.Quests
{
    [System.Serializable]
    public struct SerializableQuestStatus
    {
        public string questID;
        public List<string> completedObjectiveIDs;

        public SerializableQuestStatus(string questID)
        {
            this.questID = questID;
            completedObjectiveIDs = new List<string>();
        }
    }
}

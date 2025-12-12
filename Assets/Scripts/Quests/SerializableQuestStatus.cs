using System.Collections.Generic;

namespace Frankie.Quests
{
    [System.Serializable]
    public struct SerializableQuestStatus
    {
        public string questID;
        public List<string> completedObjectiveIDs;
    }
}

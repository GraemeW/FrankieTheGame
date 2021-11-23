using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Frankie.Quests
{
    [System.Serializable]
    public struct SerializableQuestStatus
    {
        public string questID;
        public List<string> completedObjectiveIDs;
    }
}
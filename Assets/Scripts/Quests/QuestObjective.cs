using UnityEngine;

namespace Frankie.Quests
{
    [System.Serializable]
    public class QuestObjective : ScriptableObject, ISerializationCallbackReceiver
    {
        // Tunables
        public string objectiveID;
        public string description;
        [HideInInspector][SerializeField] private string questID;

        // Methods
        #region PublicMethods
        public string GetObjectiveID() => objectiveID;
        public string GetQuestID() => questID;
        public void SetObjectiveID(string setObjectiveID)
        {
            objectiveID = setObjectiveID;
        }
        public void SetQuestID(string setQuestID)
        {
            questID = setQuestID;
        }
        #endregion

        #region UnityMethods
        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            // Generate and save a new UUID if this is blank
            if (string.IsNullOrWhiteSpace(objectiveID))
            {
                objectiveID = System.Guid.NewGuid().ToString();
            }
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            // Unused, required for interface
        }
        #endregion
    }
}

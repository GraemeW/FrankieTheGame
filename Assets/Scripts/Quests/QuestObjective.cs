using Frankie.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Quests
{
    [System.Serializable]
    public class QuestObjective : ScriptableObject, ISerializationCallbackReceiver
    {
        // Tunables
        public string objectiveID = null;
        public string description = null;
        [HideInInspector][SerializeField] string questID = null;

        // Methods
        #region PublicMethods
        public void SetObjectiveID(string objectiveID)
        {
            this.objectiveID = objectiveID;
        }
        public string GetObjectiveID() => objectiveID;
        public void SetQuestID(string questID)
        {
            this.questID = questID;
        }
        public string GetQuestID() => questID;
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
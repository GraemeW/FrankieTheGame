using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Quests
{
    [System.Serializable]
    public class Objective : ISerializationCallbackReceiver
    {
        public string uniqueID = null;
        public string description = null;

        public Objective(string description)
        {
            if (string.IsNullOrWhiteSpace(uniqueID)) { uniqueID = System.Guid.NewGuid().ToString(); }
            this.description = description;
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            if (string.IsNullOrWhiteSpace(uniqueID)) { uniqueID = System.Guid.NewGuid().ToString(); }
        }
        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
        }
    }
}
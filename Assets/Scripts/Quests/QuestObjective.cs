using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Quests
{
    [CreateAssetMenu(fileName = "New Quest Objective", menuName = "Quests/New Quest Objective")]
    public class QuestObjective : ScriptableObject, ISerializationCallbackReceiver
    {
        // Tunables
        public string uniqueID = null;
        public string description = null;

        // State
        static Dictionary<string, QuestObjective> questObjectiveLookupCache;

        // Methods
        public static QuestObjective GetFromID(string uniqueID)
        {
            if (questObjectiveLookupCache == null)
            {
                questObjectiveLookupCache = new Dictionary<string, QuestObjective>();
                var itemList = Resources.LoadAll<QuestObjective>("");
                foreach (var item in itemList)
                {
                    if (questObjectiveLookupCache.ContainsKey(item.uniqueID))
                    {
                        Debug.LogError(string.Format("Looks like there's a duplicate ID for objects: {0} and {1}", questObjectiveLookupCache[item.uniqueID], item));
                        continue;
                    }

                    questObjectiveLookupCache[item.uniqueID] = item;
                }
            }

            if (uniqueID == null || !questObjectiveLookupCache.ContainsKey(uniqueID)) return null;
            return questObjectiveLookupCache[uniqueID];
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
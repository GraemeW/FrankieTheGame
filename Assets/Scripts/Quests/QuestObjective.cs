using Frankie.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Frankie.Quests
{
    [CreateAssetMenu(fileName = "New Quest Objective", menuName = "Quests/New Quest Objective")]
    public class QuestObjective : ScriptableObject, ISerializationCallbackReceiver, IAddressablesCache
    {
        // Tunables
        public string uniqueID = null;
        public string description = null;

        // State
        static AsyncOperationHandle<IList<QuestObjective>> addressablesLoadHandle;
        static Dictionary<string, QuestObjective> questObjectiveLookupCache;

        // Methods
        #region AddressablesCaching
        public static QuestObjective GetFromID(string uniqueID)
        {
            if (string.IsNullOrWhiteSpace(uniqueID)) { return null; }

            BuildCacheIfEmpty();
            if (uniqueID == null || !questObjectiveLookupCache.ContainsKey(uniqueID)) return null;
            return questObjectiveLookupCache[uniqueID];
        }

        public static void BuildCacheIfEmpty()
        {
            if (questObjectiveLookupCache == null)
            {
                BuildQuestObjectiveCache();
            }
        }

        private static void BuildQuestObjectiveCache()
        {
            questObjectiveLookupCache = new Dictionary<string, QuestObjective>();
            addressablesLoadHandle = Addressables.LoadAssetsAsync(typeof(QuestObjective).Name, (QuestObjective questObjective) =>
            {
                if (questObjectiveLookupCache.ContainsKey(questObjective.uniqueID))
                {
                    Debug.LogError(string.Format("Looks like there's a duplicate ID for objects: {0} and {1}", questObjectiveLookupCache[questObjective.uniqueID], questObjective));
                }

                questObjectiveLookupCache[questObjective.uniqueID] = questObjective;
            }
            );
            addressablesLoadHandle.WaitForCompletion();
        }

        public static void ReleaseCache()
        {
            Addressables.Release(addressablesLoadHandle);
        }
        #endregion

        #region PublicMethods
        public string GetUniqueID()
        {
            return uniqueID;
        }
        #endregion

        #region UnityMethods
        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            if (string.IsNullOrWhiteSpace(uniqueID)) { uniqueID = System.Guid.NewGuid().ToString(); }
        }
        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
        }
        #endregion
    }
}
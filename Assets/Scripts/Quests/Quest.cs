using Frankie.Core;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Frankie.Quests
{
    [CreateAssetMenu(fileName = "Quest", menuName = "Quests/New Quest")]
    public class Quest : ScriptableObject, ISerializationCallbackReceiver, IAddressablesCache
    {
        // Tunables
        [Tooltip("Auto-generated UUID for saving/loading. Clear this field if you want to generate a new one.")]
        [SerializeField] string uniqueID = null;
        [SerializeField] string detail = "";
        [SerializeField] List<QuestObjective> objectives = new List<QuestObjective>();
        [SerializeField] List<Reward> rewards = new List<Reward>();

        // State
        static AsyncOperationHandle<IList<Quest>> addressablesLoadHandle;
        static Dictionary<string, Quest> questLookupCache;

        #region AddressablesCaching
        public static Quest GetFromID(string uniqueID)
        {
            if (string.IsNullOrWhiteSpace(uniqueID)) { return null; }

            BuildCacheIfEmpty();
            if (uniqueID == null || !questLookupCache.ContainsKey(uniqueID)) return null;
            return questLookupCache[uniqueID];
        }

        public static void BuildCacheIfEmpty()
        {
            if (questLookupCache == null)
            {
                BuildQuestCache();
            }
        }

        private static void BuildQuestCache()
        {
            questLookupCache = new Dictionary<string, Quest>();
            addressablesLoadHandle = Addressables.LoadAssetsAsync(typeof(Quest).Name, (Quest quest) =>
            {
                if (questLookupCache.ContainsKey(quest.uniqueID))
                {
                    Debug.LogError(string.Format("Looks like there's a duplicate ID for objects: {0} and {1}", questLookupCache[quest.uniqueID], quest));
                }

                questLookupCache[quest.uniqueID] = quest;
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

        public string GetDetail()
        {
            return detail;
        }

        public bool HasObjective(QuestObjective matchObjective)
        {
            foreach (QuestObjective questObjective in objectives)
            {
                if (questObjective.GetUniqueID() == matchObjective.GetUniqueID())
                {
                    return true;
                }
            }
            return false;
        }

        public int GetObjectiveCount()
        {
            return objectives.Count;
        }

        public IEnumerable<QuestObjective> GetObjective()
        {
            return objectives;
        }

        public bool HasReward()
        {
            return (rewards.Count > 0);
        }

        public List<Reward> GetRewards()
        {
            return rewards;
        }
        #endregion

        #region UnityMethods
        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            if (string.IsNullOrWhiteSpace(uniqueID))
            {
                uniqueID = System.Guid.NewGuid().ToString();
            }
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
        }
        #endregion
    }
}

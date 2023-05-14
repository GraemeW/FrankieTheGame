using Frankie.Core;
using Frankie.ZoneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
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
        [SerializeField] string questID = null;
        [SerializeField] string detail = "";
        [SerializeField] List<string> questObjectiveNames = new List<string>();
        [SerializeField] List<Reward> rewards = new List<Reward>();

        // State
        // Objectives
        [SerializeField] List<QuestObjective> questObjectives = new List<QuestObjective>();
        [HideInInspector][SerializeField] Dictionary<string, QuestObjective> objectiveIDLookup = new Dictionary<string, QuestObjective>();
        [HideInInspector][SerializeField] Dictionary<string, QuestObjective> objectiveNameLookup = new Dictionary<string, QuestObjective>();
        // Quest
        static AsyncOperationHandle<IList<Quest>> addressablesLoadHandle;
        static Dictionary<string, Quest> questLookupCache;

        #region AddressablesCaching
        public static Quest GetFromID(string questID)
        {
            if (string.IsNullOrWhiteSpace(questID)) { return null; }

            BuildCacheIfEmpty();
            if (questID == null || !questLookupCache.ContainsKey(questID)) return null;
            return questLookupCache[questID];
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
                if (questLookupCache.ContainsKey(quest.questID))
                {
                    Debug.LogError(string.Format("Looks like there's a duplicate ID for objects: {0} and {1}", questLookupCache[quest.questID], quest));
                }

                questLookupCache[quest.questID] = quest;
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
        public string GetQuestID() => questID;

        public string GetDetail() => detail;

        public bool HasObjective(QuestObjective matchObjective)
        {
            foreach (QuestObjective questObjective in questObjectives)
            {
                if (questObjective.GetObjectiveID() == matchObjective.GetObjectiveID())
                {
                    return true;
                }
            }
            return false;
        }

        public int GetObjectiveCount() => questObjectives.Count;

        public QuestObjective GetObjectiveFromID(string objectiveID)
        {
            return objectiveIDLookup[objectiveID] != null ? objectiveIDLookup[objectiveID] : null;
        }

        public bool HasReward() => (rewards.Count > 0);
        public List<Reward> GetRewards() => rewards;
        #endregion

        #region EditorMethods
#if UNITY_EDITOR
        public void GenerateObjectiveFromNames()
        {
            foreach (string questObjectiveName in questObjectiveNames)
            {
                if (string.IsNullOrWhiteSpace(questObjectiveName)) { continue; }
                if (objectiveNameLookup.ContainsKey(questObjectiveName)) { continue; }

                QuestObjective questObjective = CreateObjective(questObjectiveName);
            }
            CleanUpObjectives();
            OnValidate();
            EditorUtility.SetDirty(this);
        }

        private QuestObjective CreateObjective(string name)
        {
            QuestObjective questObjective = CreateInstance<QuestObjective>();
            Undo.RegisterCreatedObjectUndo(questObjective, "Created Quest Objective");

            questObjective.name = name;
            questObjective.SetObjectiveID(System.Guid.NewGuid().ToString());
            questObjective.SetQuestID(GetQuestID());

            Undo.RecordObject(this, "Add Quest Objective");
            questObjectives.Add(questObjective);
            return questObjective;
        }

        private void CleanUpObjectives()
        {
            // Safety && Clean Up on Serialized Objective List
            UniquifyObjectives(); // Strictly speaking, this should not be necessary (logic above won't lead to this scenario)

            // Then delete any no-longer-existing items in the string list
            HashSet<string> newObjectiveMap = UniquifyObjectiveNames();
            DeleteMissingObjectiveNames(newObjectiveMap);
        }

        private void DeleteMissingObjectiveNames(HashSet<string> newObjectiveMap)
        {
            List<QuestObjective> objectivesToDelete
                = questObjectives.Where(o => (o != null && !(newObjectiveMap.Contains(o.name)))).ToList();

            foreach (QuestObjective questObjective in objectivesToDelete)
            {
                if (questObjective == null) { continue; }

                questObjectives.Remove(questObjective);
                Undo.DestroyObjectImmediate(questObjective);
            }
        }

        private HashSet<QuestObjective> UniquifyObjectives()
        {
            HashSet<QuestObjective> objectiveMap = new HashSet<QuestObjective>();
            foreach(QuestObjective questObjective in questObjectives)
            {
                objectiveMap.Add(questObjective);
            }
            objectiveMap.Remove(null);
            questObjectives = objectiveMap.ToList();
            return objectiveMap;
        }

        private HashSet<string> UniquifyObjectiveNames()
        {
            HashSet<string> objectiveNameMap = new HashSet<string>();
            foreach (string questObjectiveName in questObjectiveNames)
            {
                objectiveNameMap.Add(questObjectiveName);
            }

            questObjectiveNames = objectiveNameMap.ToList();
            return objectiveNameMap;
        }
#endif
        #endregion

        #region UnityMethods
        private void OnValidate()
        {
            objectiveIDLookup = new Dictionary<string, QuestObjective>();
            objectiveNameLookup = new Dictionary<string, QuestObjective>();
            foreach (QuestObjective questObjective in questObjectives)
            {
                if (questObjective == null) { continue; }

                string objectiveID = questObjective.GetObjectiveID();
                if (objectiveIDLookup.ContainsKey(objectiveID)) { continue; }
                objectiveIDLookup.Add(objectiveID, questObjective);

                if (objectiveNameLookup.ContainsKey(questObjective.name)) { continue; }
                objectiveNameLookup.Add(questObjective.name, questObjective);
            }
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
#if UNITY_EDITOR
            // Generate and save a new UUID if this is blank
            if (string.IsNullOrWhiteSpace(questID))
            {
                questID = System.Guid.NewGuid().ToString();
            }

            // Serialize quest objectives as childed
            if (AssetDatabase.GetAssetPath(this) != "")
            {
                foreach (QuestObjective questObjective in questObjectives)
                {
                    if (questObjective == null) { continue; }

                    if (AssetDatabase.GetAssetPath(questObjective) == "")
                    {
                        AssetDatabase.AddObjectToAsset(questObjective, this);
                    }
                }
            }
#endif
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            // Unused, required for interface
        }
        #endregion
    }
}

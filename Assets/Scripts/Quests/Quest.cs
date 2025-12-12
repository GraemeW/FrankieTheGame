using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Frankie.Core;

namespace Frankie.Quests
{
    [CreateAssetMenu(fileName = "Quest", menuName = "Quests/New Quest")]
    public class Quest : ScriptableObject, ISerializationCallbackReceiver, IAddressablesCache
    {
        // Tunables
        [SerializeField] [Tooltip("Auto-generated UUID for saving/loading. Clear this field if you want to generate a new one.")] private string questID;
        [SerializeField] private string detail = "";
        [SerializeField] private List<string> questObjectiveNames = new();
        [SerializeField] private List<Reward> rewards = new();

        // State
        // Objectives
        [HideInInspector][SerializeField] private List<QuestObjective> questObjectives = new();
        [HideInInspector][SerializeField] private Dictionary<string, QuestObjective> objectiveIDLookup = new();
        [HideInInspector][SerializeField] private Dictionary<string, QuestObjective> objectiveNameLookup = new();
        // Quest
        private static AsyncOperationHandle<IList<Quest>> _addressablesLoadHandle;
        private static Dictionary<string, Quest> _questLookupCache;

        #region AddressablesCaching
        public static Quest GetFromID(string questID)
        {
            if (string.IsNullOrWhiteSpace(questID)) { return null; }
            
            BuildCacheIfEmpty();
            return _questLookupCache.GetValueOrDefault(questID);
        }

        public static void BuildCacheIfEmpty()
        {
            if (_questLookupCache != null) { return; }
            BuildQuestCache();
        }

        private static void BuildQuestCache()
        {
            _questLookupCache = new Dictionary<string, Quest>();
            _addressablesLoadHandle = Addressables.LoadAssetsAsync(nameof(Quest), (Quest quest) =>
            {
                if (_questLookupCache.TryGetValue(quest.questID, out Quest tryQuest))
                {
                    Debug.LogError($"Looks like there's a duplicate ID for objects: {tryQuest} and {quest}");
                }
                _questLookupCache[quest.questID] = quest;
            }
            );
            _addressablesLoadHandle.WaitForCompletion();
        }

        public static void ReleaseCache()
        {
            Addressables.Release(_addressablesLoadHandle);
        }
        #endregion

        #region PublicMethods
        public string GetQuestID() => questID;

        public string GetDetail() => detail;

        public bool HasObjective(QuestObjective matchObjective)
        {
            return questObjectives.Any(questObjective => questObjective.GetObjectiveID() == matchObjective.GetObjectiveID());
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
                CreateObjective(questObjectiveName);
            }
            CleanUpObjectives();
            OnValidate();
            EditorUtility.SetDirty(this);
        }

        private void CreateObjective(string objectiveName)
        {
            QuestObjective questObjective = CreateInstance<QuestObjective>();
            Undo.RegisterCreatedObjectUndo(questObjective, "Created Quest Objective");

            questObjective.name = objectiveName;
            questObjective.SetObjectiveID(System.Guid.NewGuid().ToString());
            questObjective.SetQuestID(GetQuestID());

            Undo.RecordObject(this, "Add Quest Objective");
            questObjectives.Add(questObjective);
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

            foreach (QuestObjective questObjective in objectivesToDelete.Where(questObjective => questObjective != null))
            {
                questObjectives.Remove(questObjective);
                Undo.DestroyObjectImmediate(questObjective);
            }
        }

        private void UniquifyObjectives()
        {
            var objectiveMap = new HashSet<QuestObjective>();
            foreach (QuestObjective questObjective in questObjectives)
            {
                objectiveMap.Add(questObjective);
            }
            objectiveMap.Remove(null);
            questObjectives = objectiveMap.ToList();
        }

        private HashSet<string> UniquifyObjectiveNames()
        {
            var objectiveNameMap = new HashSet<string>();
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
            foreach (QuestObjective questObjective in questObjectives.Where(questObjective => questObjective != null))
            {
                string objectiveID = questObjective.GetObjectiveID();
                if (!objectiveIDLookup.TryAdd(objectiveID, questObjective)) { continue; }
                if (!objectiveNameLookup.TryAdd(questObjective.name, questObjective)) { continue; }
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
            if (AssetDatabase.GetAssetPath(this) == "") { return; }
            foreach (QuestObjective questObjective in questObjectives.Where(questObjective => questObjective != null))
            {
                if (AssetDatabase.GetAssetPath(questObjective) == "")
                {
                    AssetDatabase.AddObjectToAsset(questObjective, this);
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

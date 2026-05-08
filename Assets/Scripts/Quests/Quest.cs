using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.Localization;
using Frankie.Core.GameStateModifiers;
using Frankie.Utils.Addressables;
using Frankie.Utils.Localization;
using UnityEngine.Localization.Tables;

namespace Frankie.Quests
{
    [CreateAssetMenu(fileName = "Quest", menuName = "Quests/New Quest", order = 10)]
    public class Quest : GameStateModifier, IAddressablesCache, ILocalizable
    {
        // Tunables
        [SerializeField][SimpleLocalizedString(LocalizationTableType.Quests, true)] private LocalizedString localizedDisplayName; 
        [SerializeField][SimpleLocalizedString(LocalizationTableType.Quests, true)] private LocalizedString localizedDetail;
        [SerializeField] private List<string> questObjectiveNames = new();
        [SerializeField] private List<Reward> rewards = new();

        // State
        [HideInInspector][SerializeField] private string cachedName;
        public string iCachedName { get => cachedName; set => cachedName = value; }
        // Objectives
        [HideInInspector][SerializeField] private List<QuestObjective> questObjectives = new();
        private Dictionary<string, QuestObjective> objectiveNameEditorLookup = new();
        // Quest
        private static AsyncOperationHandle<IList<Quest>> _addressablesLoadHandle;
        private static Dictionary<string, Quest> _questLookupCache;

        #region UnityMethods
        private void OnValidate()
        {
#if UNITY_EDITOR
            objectiveNameEditorLookup = new Dictionary<string, QuestObjective>();
            foreach (QuestObjective questObjective in questObjectives.Where(questObjective => questObjective != null))
            {
                if (!objectiveNameEditorLookup.TryAdd(questObjective.name, questObjective)) { continue; }
            }
#endif
        }

        public override void OnBeforeSerialize()
        {
#if UNITY_EDITOR
            base.OnBeforeSerialize();

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
        #endregion

        #region Getters

        public string GetName() => localizedDisplayName.GetSafeLocalizedString();
        public string GetDetail() => localizedDetail.GetSafeLocalizedString();
        public QuestObjective GetObjectiveFromID(string objectiveID) => questObjectives.FirstOrDefault(questObjective => questObjective.GetObjectiveID() == objectiveID);
        public bool HasObjective(QuestObjective matchObjective) => questObjectives.Any(questObjective => questObjective.GetObjectiveID() == matchObjective.GetObjectiveID());
        public int GetObjectiveCount() => questObjectives.Count;
        public bool HasReward() => (rewards.Count > 0);
        public List<Reward> GetRewards() => rewards;
        
        public LocalizationTableType localizationTableType { get; } = LocalizationTableType.Quests;
        public List<TableEntryReference> GetLocalizationEntries()
        {
            var entries = new List<TableEntryReference>
            {
                localizedDisplayName.TableEntryReference,
                localizedDetail.TableEntryReference
            };
            foreach (QuestObjective questObjective in questObjectives)
            {
                entries.AddRange(questObjective.GetLocalizationEntries());
            }
            return entries;
        }
        
        public List<(string propertyName, LocalizedString localizedString, bool setToName)> GetPropertyLinkedLocalizationEntries()
        {
            return new List<(string propertyName, LocalizedString localizedString, bool setToName)>
            {
                (nameof(localizedDisplayName), localizedDisplayName, true),
                (nameof(localizedDetail), localizedDetail, false)
            };
        }
        #endregion
        
#if UNITY_EDITOR
        #region EditorMethods
        public void GenerateObjectiveFromNames()
        {
            questObjectiveNames = questObjectiveNames.Distinct().ToList();
            foreach (string questObjectiveName in questObjectiveNames)
            {
                if (string.IsNullOrWhiteSpace(questObjectiveName)) { continue; }
                if (objectiveNameEditorLookup.ContainsKey(questObjectiveName)) { continue; }
                
                CreateObjective(questObjectiveName);
            }
            RemoveDanglingObjectives();
            OnValidate();
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssetIfDirty(this);
        }

        private void CreateObjective(string objectiveName)
        {
            var questObjective = CreateInstance<QuestObjective>();
            Undo.RegisterCreatedObjectUndo(questObjective, "Created Quest Objective");
            questObjective.Setup(name, GetGUID(), objectiveName);
            Undo.RecordObject(this, "Add Quest Objective");
            
            questObjectives.Add(questObjective);
        }

        private void RemoveDanglingObjectives()
        {
            List<QuestObjective> questObjectivesToDelete = questObjectives.Where(questObjective => !questObjectiveNames.Contains(questObjective.name)).ToList();
            foreach (QuestObjective questObjective in questObjectivesToDelete)
            {
                questObjective.DeleteLocalization();
                questObjectives.Remove(questObjective);
                Undo.DestroyObjectImmediate(questObjective);
            }
        }

        public void TriggerOnRename()
        {
            foreach (QuestObjective questObjective in questObjectives)
            {
                questObjective.SetQuestName(name);
            }
        }
        #endregion
#endif

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
                    if (_questLookupCache.TryGetValue(quest.guid, out Quest tryQuest))
                    {
                        Debug.LogError($"Looks like there's a duplicate ID for objects: {tryQuest} and {quest}");
                    }
                    _questLookupCache[quest.guid] = quest;
                }
            );
            _addressablesLoadHandle.WaitForCompletion();
        }

        public static void ReleaseCache()
        {
            Addressables.Release(_addressablesLoadHandle);
        }
        #endregion
    }
}

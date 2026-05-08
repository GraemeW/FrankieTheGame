using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Frankie.Utils.Localization;

namespace Frankie.Quests
{
    [System.Serializable]
    public class QuestObjective : ScriptableObject, ISerializationCallbackReceiver
    {
        // Tunables
        public string objectiveID;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.Quests, true)] private LocalizedString localizedDisplayName;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.Quests, true)] private LocalizedString localizedDetail;

        // Const
        private const LocalizationTableType _localizationTableType = LocalizationTableType.Quests;
        
        // State
        [HideInInspector][SerializeField] private string questName;
        [HideInInspector][SerializeField] private string questID;
        
        // Methods
        #region Getters
        public string GetObjectiveID() => objectiveID;
        public string GetQuestID() => questID;
        public List<TableEntryReference> GetLocalizationEntries()
        {
            return new List<TableEntryReference>
            {
                localizedDisplayName.TableEntryReference,
                localizedDetail.TableEntryReference
            };
        }
        #endregion

#if UNITY_EDITOR
        #region EditorMethods
        public void Setup(string setQuestName, string setQuestID, string setObjectiveName)
        {
            questID = setQuestID;
            objectiveID = System.Guid.NewGuid().ToString();
            
            questName = setQuestName;
            name = setObjectiveName;
            
            string nameKey = GetNameLocalizationKey();
            string detailKey = GetDetailLocalizationKey();
            LocalizationTool.TryLocalizeEntry(_localizationTableType, localizedDisplayName, nameKey, name);
            LocalizationTool.InitializeLocalEntry(_localizationTableType, localizedDetail, detailKey);
            
            EditorUtility.SetDirty(this);
        }

        public void SetQuestName(string setQuestName)
        {
            Undo.RecordObject(this, "Set Quest Name");
            TryRenameExistingKeys(UpdateQuestName);
            EditorUtility.SetDirty(this);
            return;
            
            void UpdateQuestName() => questName = setQuestName;
        }

        public void SetObjectiveName(string setObjectiveName)
        {
            Undo.RecordObject(this, "Set Objective Name");
            TryRenameExistingKeys(UpdateQuestName);
            EditorUtility.SetDirty(this);
            return;
            
            void UpdateQuestName() => name = setObjectiveName;
        }

        public void DeleteLocalization()
        {
            Undo.RecordObject(this, "Delete Objective Localization");
            TryDeleteLocalization();
            EditorUtility.SetDirty(this);
        }
        #endregion
        
        #region LocalizationUtility
        private string GetNameLocalizationKey() => GetNameLocalizationKey(name);
        private string GetNameLocalizationKey(string id) => $"Quest.{questName ?? ""}.Objective.{id}";
        private string GetDetailLocalizationKey() => GetDetailLocalizationKey(name);
        private string GetDetailLocalizationKey(string id) => $"{GetNameLocalizationKey(id)}.Detail";

        private void TryRenameExistingKeys(Action updateAction)
        {
            if (updateAction == null) { return; }

            TableEntryReference oldNameKey = GetNameLocalizationKey();
            TableEntryReference oldDetailKey = GetDetailLocalizationKey();
            
            updateAction.Invoke();
            
            string newNameKey = GetNameLocalizationKey();
            string newDetailKey = GetDetailLocalizationKey();
            LocalizationTool.MakeOrRenameKey(_localizationTableType, oldNameKey, newNameKey);
            LocalizationTool.MakeOrRenameKey(_localizationTableType, oldDetailKey, newDetailKey);
        }

        private void TryDeleteLocalization()
        {
            LocalizationTool.RemoveEntry(_localizationTableType, GetNameLocalizationKey());
            LocalizationTool.RemoveEntry(_localizationTableType, GetDetailLocalizationKey());
            localizedDisplayName.SetReference("", "");
            localizedDetail.SetReference("", "");
        }
        #endregion
#endif

        #region InterfaceMethods
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

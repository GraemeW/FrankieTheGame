using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using Frankie.Core.Predicates;
using Frankie.Utils.Localization;

namespace Frankie.ZoneManagement
{
    [System.Serializable]
    public class ZoneNode : ScriptableObject
    {
        // Tunables
        [Header("Zone Node Properties")]
        [SerializeField][SimpleLocalizedString(LocalizationTableType.Zones, false)] private LocalizedString localizedDisplayName;
        [SerializeField] private List<string> children = new();
        [SerializeField] private ZoneNode externalZoneLinkToZoneNode;
        [SerializeField] private Rect rect = new(30, 30, 430, 150);
        [HideInInspector][SerializeField] private string zoneName = "";
        [HideInInspector][SerializeField] private Rect draggingRect = new(0, 0, 430, 45);
        [Header("Additional Properties")]
        [SerializeField] private Condition condition;
        
        #region Getters
        public string GetDisplayName() => localizedDisplayName.GetSafeLocalizedString();
        public string GetZoneName() => zoneName;
        public Zone GetZone() => Zone.GetFromName(zoneName);
        public string GetNodeID() => name;
        public List<string> GetChildren() => children.Count == 0 ? null : children;
        public ZoneNode GetLinkedZoneNode() => externalZoneLinkToZoneNode;
        public bool HasLinkedSceneReference()
        {
            if (externalZoneLinkToZoneNode == null) { return false; }
            Zone linkedZone = externalZoneLinkToZoneNode.GetZone();
            return linkedZone != null && linkedZone.GetSceneReference().IsSet();
        }
        
        private string GetNameLocalizationKey() => GetNameLocalizationKey(name);
        private string GetNameLocalizationKey(string id) => $"Zone.{zoneName ?? ""}.Node.{id}";
        public LocalizationTableType localizationTableType { get; } = LocalizationTableType.Zones;
        public List<TableEntryReference> GetLocalizationEntries()
        {
            return new List<TableEntryReference>
            {
                localizedDisplayName.TableEntryReference,
            };
        }
        #endregion

        #region PublicMethods
        public bool CheckCondition(IEnumerable<IPredicateEvaluator> evaluators) => condition.Check(evaluators);
        #endregion

#if UNITY_EDITOR
        #region ZoneEditorMethods
        public Vector2 GetPosition() => rect.position;
        public Rect GetRect() => rect;
        public Rect GetDraggingRect() => draggingRect;
        
        public void Initialize(int width, int height)
        {
            rect.width = width;
            rect.height = height;
            EditorUtility.SetDirty(this);
        }

        public void SetZoneName(string setZoneName)
        {
            if (setZoneName == zoneName) { return; }
            Undo.RecordObject(this, "Update Zone");
            
            TableEntryReference oldKey =  GetNameLocalizationKey();
            zoneName = setZoneName;
            string newKey = GetNameLocalizationKey();
            LocalizationTool.MakeOrRenameKey(localizationTableType, oldKey, newKey);

            EditorUtility.SetDirty(this);
        }

        public bool SetNodeID(string id)
        {
            if (id == name) { return false; }
            Undo.RecordObject(this, "Update ID");
            
            TryRenameExistingKey(id);
            name = id;
            
            string key = GetNameLocalizationKey();
            LocalizationTool.TryLocalizeEntry(localizationTableType, localizedDisplayName, key, name);
            EditorUtility.SetDirty(this);
            return true;
        }
        
        public void UpdateChildNodeID(string oldID, string newID)
        {
            if (!children.Contains(oldID)) { return; }
            children.Remove(oldID);
            children.Add(newID);
        }

        public void AddChild(string childID)
        {
            Undo.RecordObject(this, "Add Node Relation");
            children.Add(childID);
            EditorUtility.SetDirty(this);
        }

        public void RemoveChild(string childID)
        {
            Undo.RecordObject(this, "Remove Node Relation");
            children.Remove(childID);
            EditorUtility.SetDirty(this);
        }

        public void SetPosition(Vector2 position)
        {
            Undo.RecordObject(this, "Move Zone Node");
            rect.position = position;
            EditorUtility.SetDirty(this);
        }

        public void SetDraggingRect(Rect setDraggingRect)
        {
            if (setDraggingRect == draggingRect) return;
            draggingRect = setDraggingRect;
            EditorUtility.SetDirty(this);
        }

        public void DeleteLocalizationEntries()
        {
            Undo.RecordObject(this, "Delete Localization Entries");
            TryDeleteLocalization();
            EditorUtility.SetDirty(this);
        }
        #endregion
        
        #region LocalizationUtility
        private void TryRenameExistingKey(string id)
        {
            TableEntryReference oldKey = GetNameLocalizationKey();
            string newKey = GetNameLocalizationKey(id);
            LocalizationTool.MakeOrRenameKey(localizationTableType, oldKey, newKey);
        }
        
        private void TryDeleteLocalization()
        {
            LocalizationTool.RemoveEntry(localizationTableType, GetNameLocalizationKey());
            localizedDisplayName.SetReference("", "");
        }
        #endregion
#endif
    }
}

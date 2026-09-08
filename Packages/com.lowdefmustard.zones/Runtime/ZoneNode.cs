using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using LowDefMustard.Utils;
using LowDefMustard.Localization;

namespace LowDefMustard.Zones
{
    [Serializable]
    public class ZoneNode : ScriptableObject, IStandardGraphNode, ILocalizableCore
    {
        // Tunables
        [Header("Zone Node Properties")]
        [SerializeField][SimpleLocalizedString(false)] private LocalizedString localizedDisplayName;
        [SerializeField] private List<string> children = new();
        [SerializeField] private ZoneNode externalZoneLinkToZoneNode;
        [SerializeField] private Rect rect = new(30, 30, 350, 125);
        [HideInInspector][SerializeField] private string zoneName = "";
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

        Enum ILocalizableCore.localizationTableTypeValue => LocalizableClassTableTypeRegistry.GetTableType(GetType());

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

        #region NodeInterface
        // Note:  Must be outside pragma for compilation
        public ScriptableObject scriptableObject => this;
        public Vector2 GetPosition() => rect.position;
        public void SetPosition(Vector2 position)
        {
#if UNITY_EDITOR
            Undo.RecordObject(this, "Move Zone Node");
            rect.position = position;
            EditorUtility.SetDirty(this);
#endif
        }
        #endregion
        
#if UNITY_EDITOR
        #region ZoneEditorMethods
        public Rect GetRect() => rect;
        
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
            if (TryGetLocalizationBridge(out Enum tableType, out ILocalizationToolBridge localizationToolBridge))
            {
                localizationToolBridge.MakeOrRenameKey(tableType, oldKey, newKey);
            }
            EditorUtility.SetDirty(this);
        }

        public bool SetNodeID(string id)
        {
            if (id == name) { return false; }
            Undo.RecordObject(this, "Update ID");
            
            TryRenameExistingKey(id);
            name = id;
            
            string key = GetNameLocalizationKey();
            if (TryGetLocalizationBridge(out Enum tableType, out ILocalizationToolBridge localizationToolBridge))
            {
                localizationToolBridge.TryLocalizeEntry(tableType, localizedDisplayName, key, name);
            }
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

        public void DeleteLocalizationEntries()
        {
            Undo.RecordObject(this, "Delete Localization Entries");
            TryDeleteLocalization();
            EditorUtility.SetDirty(this);
        }
        #endregion
        
        #region MultiZoneEditorMethods
        public bool TrySetExternalLink(ZoneNode targetZoneNode)
        {
            if (targetZoneNode == null || targetZoneNode == this) { return false; }
            if (targetZoneNode.GetZoneName() == zoneName) { return false; }

            Undo.RecordObject(this, "Link Zone Node");
            externalZoneLinkToZoneNode = targetZoneNode;
            EditorUtility.SetDirty(this);
            return true;
        }
        
        public bool ClearExternalLink()
        {
            if (externalZoneLinkToZoneNode == null) { return false; }

            Undo.RecordObject(this, "Clear Zone Node Link");
            externalZoneLinkToZoneNode = null;
            EditorUtility.SetDirty(this);
            return true;
        }
        #endregion
        
        #region LocalizationUtility
        private bool TryGetLocalizationBridge(out Enum tableType, out ILocalizationToolBridge localizationToolBridge)
        {
            tableType = ((ILocalizableCore)this).localizationTableTypeValue;
            localizationToolBridge = null;

            if (tableType == null) { Debug.LogWarning($"{name} ({nameof(ZoneNode)}) has no table type registered - skipping localization operation."); return false; }
            if (!LocalizationToolBridgeRegistry.TryGetBridge(tableType.GetType(), out localizationToolBridge)) { Debug.LogWarning($"No localization bridge registered for table type - skipping localization operation for {name}"); return false; }

            return true;
        }

        private void TryRenameExistingKey(string id)
        {
            TableEntryReference oldKey = GetNameLocalizationKey();
            string newKey = GetNameLocalizationKey(id);
            if (TryGetLocalizationBridge(out Enum tableType, out ILocalizationToolBridge localizationToolBridge))
            {
                localizationToolBridge.MakeOrRenameKey(tableType, oldKey, newKey);
            }
        }
        
        private void TryDeleteLocalization()
        {
            if (TryGetLocalizationBridge(out Enum tableType, out ILocalizationToolBridge localizationToolBridge))
            {
                localizationToolBridge.RemoveEntry(tableType, GetNameLocalizationKey());
            }
            localizedDisplayName.SetReference("", "");
        }
        #endregion
#endif
    }
}

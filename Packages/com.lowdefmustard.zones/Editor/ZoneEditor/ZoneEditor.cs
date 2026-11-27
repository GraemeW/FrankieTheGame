using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.UIElements;
using LowDefMustard.Localization;
using LowDefMustard.Utils.Editor;

namespace LowDefMustard.Zones.Editor
{
    public class ZoneEditor : EditorWindow
    {
        // Note:  Internal fields/methods for test visibility
        
        // Tunables
        private const string _windowTitle = "Zone Editor";
        private const string _noZoneSelectedMessage = "No zone selected.";

        // State
        internal Zone selectedZone;
        private readonly UndoChangeSnapshot undoSnapshot = new();
        internal ZoneGraphView zoneGraphView;
        internal Label headerLabel;
        internal Label noZoneMessage;
        internal Button addGroupButton;

        #region UnityMethods
        [MenuItem("Window/Zone Editor")]
        public static void ShowEditorWindow()
        {
            GetWindow(typeof(ZoneEditor), false, _windowTitle);
        }

        [OnOpenAsset(1)]
        public static bool OnOpenAsset(EntityId instanceID, int line)
        {
            var zone = EditorUtility.EntityIdToObject(instanceID) as Zone;
            if (zone == null) { return false; }

            if (zone is ILocalizableCore localizable)
            {
                ILocalizableCore.TryLocalizeStandardEntries(localizable, zone, zone.GetPropertyLinkedLocalizationEntries(), zone.TriggerOnRename);
            }

            zone.CreateRootNodeIfMissing();
            ShowEditorWindow();
            return true;
        }

        private void OnEnable()
        {
            LocalizationLocale.TriggerLocalizationSettingsInitialization();
            EditorApplication.projectChanged -= OnProjectChanged;
            Selection.selectionChanged -= OnSelectionChanged;
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
            
            EditorApplication.projectChanged += OnProjectChanged;
            Selection.selectionChanged += OnSelectionChanged;
            Undo.undoRedoPerformed += OnUndoRedoPerformed;
        }

        private void OnDisable()
        {
            EditorApplication.projectChanged -= OnProjectChanged;
            Selection.selectionChanged -= OnSelectionChanged;
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
        }
        #endregion

        #region DrawingMethods
        public void CreateGUI()
        {
            rootVisualElement.Clear();

            headerLabel = MakeZoneNameLabel();
            rootVisualElement.Add(headerLabel);

            noZoneMessage = new Label(_noZoneSelectedMessage) { style = { paddingLeft = 6 } };
            rootVisualElement.Add(noZoneMessage);

            var toolbar = new VisualElement { style = { flexDirection = FlexDirection.Row, paddingLeft = 4, paddingTop = 2, paddingBottom = 2 } };
            addGroupButton = new Button { text = "Add Group" };
            toolbar.Add(addGroupButton);
            rootVisualElement.Add(toolbar);
            
            zoneGraphView = new ZoneGraphView { style = { flexGrow = 1, overflow = Overflow.Hidden } };
            rootVisualElement.Add(zoneGraphView);

            zoneGraphView.RegisterCallback<MouseDownEvent>(_ => Selection.activeObject = selectedZone);
            addGroupButton.RegisterCallback<ClickEvent>(_ => zoneGraphView.BeginPlacingGroup());
            
            RefreshFromSelection();
        }
        
        internal void RefreshFromSelection()
        {
            if (zoneGraphView == null) { return; } // CreateGUI has not yet run

            bool hasZone = selectedZone != null;
            noZoneMessage.style.display = hasZone ? DisplayStyle.None : DisplayStyle.Flex;
            headerLabel.style.display = hasZone ? DisplayStyle.Flex : DisplayStyle.None;
            zoneGraphView.style.display = hasZone ? DisplayStyle.Flex : DisplayStyle.None;
            addGroupButton.SetEnabled(hasZone);

            if (!hasZone)
            {
                zoneGraphView.SetZone(null); // Drop the node views ~ a deleted zone's nodes are destroyed
                undoSnapshot.Clear();
                return;
            }

            headerLabel.text = selectedZone.name;
            zoneGraphView.SetZone(selectedZone);
            undoSnapshot.Capture(selectedZone.GetAllNodes().Prepend<Object>(selectedZone));
        }
        #endregion

        #region EventHandlers
        internal void OnSelectionChanged()
        {
            switch (Selection.activeObject)
            {
                case Zone zone:
                {
                    if (zone == null || zone == selectedZone) { return; } // Same zone reselected (e.g. undo restoring the selection) ~ views already current
                    zone.CreateRootNodeIfMissing(); // Every path in (open, selection) ~ a new zone has no nodes yet
                    selectedZone = zone;
                    RefreshFromSelection();
                    break;
                }
                case ZoneNode zoneNode:
                {
                    if (zoneNode == null) { return; }
                    Zone matchZone = zoneNode.GetZone();
                    if (matchZone == null) { return; }
                    
                    if (selectedZone != matchZone)
                    {
                        selectedZone = matchZone;
                        RefreshFromSelection();
                    }
                    if (zoneGraphView == null || selectedZone == null) { return; }
                    zoneGraphView.FocusOnNode(zoneNode);
                    break;
                }
            }
        }

        internal void OnProjectChanged()
        {
            if (!IsSelectedZoneDeleted()) { return; }
            selectedZone = null;
            RefreshFromSelection();
        }

        internal void OnUndoRedoPerformed()
        {
            if (!undoSnapshot.HasChanged()) { return; }
            if (IsSelectedZoneDeleted()) { selectedZone = null; }
            RefreshFromSelection(); // Rebuilds node + group views ~ undo can add, remove or move nodes and groups
        }
        
        private bool IsSelectedZoneDeleted() => selectedZone is not null && selectedZone == null; // Destroyed but still referenced (e.g. the asset was deleted while shown) ~ Unity-null, though type checks still match
        #endregion
        
        #region StaticUIBuilders
        private static Label MakeZoneNameLabel()
        {
            return new Label
            {
                name = "zone-editor-header",
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    paddingLeft = 6,
                    paddingTop = 4,
                    paddingBottom = 4
                }
            };
        }
        #endregion
    }
}

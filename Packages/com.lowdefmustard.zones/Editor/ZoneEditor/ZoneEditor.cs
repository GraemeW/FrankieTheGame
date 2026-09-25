using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.UIElements;
using LowDefMustard.Localization;

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
            Selection.selectionChanged -= OnSelectionChanged;
            Selection.selectionChanged += OnSelectionChanged;
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionChanged;
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

            if (!hasZone) { return; }

            headerLabel.text = selectedZone.name;
            zoneGraphView.SetZone(selectedZone);
        }
        #endregion

        #region EventHandlers
        internal void OnSelectionChanged()
        {
            switch (Selection.activeObject)
            {
                case Zone zone:
                {
                    if (zone == null) { return; }
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

using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.UIElements;
using Frankie.Utils.Localization;

namespace Frankie.ZoneManagement.Editor
{
    public class ZoneEditor : EditorWindow
    {
        // Tunables
        private const string _windowTitle = "Zone Editor";
        private const string _noZoneSelectedMessage = "No zone selected.";

        // State
        private Zone selectedZone;
        private ZoneGraphView zoneGraphView;
        private Label headerLabel;
        private Label noZoneMessage;
        private Button addGroupButton;

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

            if (zone is ILocalizable localizable)
            {
                localizable.TryLocalizeStandardEntries(zone, zone.GetPropertyLinkedLocalizationEntries(), zone.TriggerOnRename);
            }

            zone.CreateRootNodeIfMissing();
            ShowEditorWindow();
            return true;
        }

        private void OnEnable()
        {
            LocalizationTool.InitializeEnglishLocale();
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
            addGroupButton = new Button(() => zoneGraphView.BeginPlacingGroup()) { text = "Add Group" };
            toolbar.Add(addGroupButton);
            rootVisualElement.Add(toolbar);
            
            zoneGraphView = new ZoneGraphView { style = { flexGrow = 1, overflow = Overflow.Hidden } };
            zoneGraphView.RegisterCallback<MouseDownEvent>(_ => Selection.activeObject = selectedZone);
            rootVisualElement.Add(zoneGraphView);

            RefreshFromSelection();
        }
        
        private void RefreshFromSelection()
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
        private void OnSelectionChanged()
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

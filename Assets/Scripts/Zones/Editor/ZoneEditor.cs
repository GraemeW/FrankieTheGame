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
        private ZoneGraphView graphView;
        private Label headerLabel;
        private Label noZoneMessage;

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

            graphView = new ZoneGraphView { style = { flexGrow = 1, overflow = Overflow.Hidden } };
            graphView.RegisterCallback<MouseDownEvent>(_ => Selection.activeObject = selectedZone);
            rootVisualElement.Add(graphView);

            RefreshFromSelection();
        }
        
        private void RefreshFromSelection()
        {
            if (graphView == null) { return; } // CreateGUI has not yet run

            bool hasZone = selectedZone != null;
            noZoneMessage.style.display = hasZone ? DisplayStyle.None : DisplayStyle.Flex;
            headerLabel.style.display = hasZone ? DisplayStyle.Flex : DisplayStyle.None;
            graphView.style.display = hasZone ? DisplayStyle.Flex : DisplayStyle.None;

            if (!hasZone) { return; }

            headerLabel.text = selectedZone.name;
            graphView.SetZone(selectedZone);
        }
        #endregion

        #region EventHandlers
        private void OnSelectionChanged()
        {
            var newZone = Selection.activeObject as Zone;
            if (newZone == null) { return; }
            selectedZone = newZone;
            RefreshFromSelection();
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

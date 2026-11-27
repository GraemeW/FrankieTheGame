using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Scripting.LifecycleManagement;
using LowDefMustard.Utils.Editor;

namespace Frankie.Speech.Editor
{
    internal sealed class DialogueVariablesPanel : VisualElement
    {
        // Note:  Behaviour set by DialogueVariablesPanelMode (via CreateOverlay / CreatePicker)
        //  - Overlay -> overlay panel in graph window ~ click-and-drag || double click to ping, height fitted to window
        //  - Picker -> picker popup ~ incl. `None` row, click to set field (via onPick), search focused on open
        
        // Const Tunables
        private const long _rebuildDelayMs = 300;
        
        private const string _searchPlaceholder = "Search dialogue variables";
        private const string _emptyText = "No dialogue variables found";
        private const string _noneLabel = "None";
        private const string _foldoutViewDataKeyPrefix = "Frankie.Speech.Editor.DialogueVariablesPanel.";
        
        private const float _iconSize = 16f;
        private const float _minHeight = 200f;
        private const float _maxHeight = 600f;
        private const float _windowMargin = 140f; // Room for graph window's toolbars + overlay header
        private const float _minWidth = 220f;
        [NoAutoStaticsCleanup] private static readonly Color _typeLabelColour = new(1f, 1f, 1f, 0.5f);

        // Element References
        private readonly VisualElement windowRoot;
        private readonly ToolbarSearchField searchField;
        private readonly ScrollView listView;

        // State
        private readonly DialogueVariablesPanelMode mode;
        private readonly Action<ScriptableObject> onPick;
        private List<DialogueVariableCatalogEntry> entries = new();
        private IVisualElementScheduledItem pendingRebuild;

        // Constructor
        private DialogueVariablesPanel(DialogueVariablesPanelMode mode, VisualElement windowRoot, Action<ScriptableObject> onPick)
        {
            this.mode = mode;
            this.windowRoot = windowRoot;
            this.onPick = onPick;
            style.height = _minHeight;
            style.minWidth = _minWidth;

            searchField = new ToolbarSearchField { style = { width = StyleKeyword.Auto, flexShrink = 0f } };
            searchField.Q<TextField>().textEdition.placeholder = _searchPlaceholder;
            searchField.RegisterValueChangedCallback(_ => RebuildList());
            Add(searchField);

            listView = new ScrollView(ScrollViewMode.Vertical) { verticalScrollerVisibility = ScrollerVisibility.Auto, style = { flexGrow = 1f, flexShrink = 1f } };
            Add(listView);

            RegisterCallback<AttachToPanelEvent>(_ => OnAttach());
            RegisterCallback<DetachFromPanelEvent>(_ => OnDetach());
        }

        #region PublicMethods
        public static DialogueVariablesPanel CreateOverlay(VisualElement windowRoot) => new(DialogueVariablesPanelMode.Overlay, windowRoot, null);
        public static DialogueVariablesPanel CreatePicker(Action<ScriptableObject> onPick) => new(DialogueVariablesPanelMode.Picker, null, onPick);
        #endregion

        #region UIBuildAttach
        private void OnAttach()
        {
            EditorApplication.projectChanged += RequestRebuild;
            windowRoot?.RegisterCallback<GeometryChangedEvent>(OnWindowGeometryChanged);
            FitToWindow();
            Rebuild();
            if (mode == DialogueVariablesPanelMode.Picker) { schedule.Execute(() => searchField.Q<TextField>().Focus()); }
        }

        private void OnDetach()
        {
            EditorApplication.projectChanged -= RequestRebuild;
            windowRoot?.UnregisterCallback<GeometryChangedEvent>(OnWindowGeometryChanged);
        }

        private void OnWindowGeometryChanged(GeometryChangedEvent geometryChangedEvent) => FitToWindow();
        
        private void RequestRebuild()
        {
            pendingRebuild?.Pause();
            pendingRebuild = schedule.Execute(Rebuild).StartingIn(_rebuildDelayMs);
        }

        private void Rebuild()
        {
            entries = DialogueVariableCatalog.Collect();
            RebuildList();
        }
        
        private void RebuildList()
        {
            listView.Clear();
            if (mode == DialogueVariablesPanelMode.Picker) { listView.Add(BuildNoneRow()); }
            string search = searchField.value?.Trim();
            List<DialogueVariableCatalogEntry> matches = entries.Where(entry => entry.variable != null && Matches(entry, search)).ToList();
            if (matches.Count == 0)
            {
                listView.Add(new Label(_emptyText) { style = { unityFontStyleAndWeight = FontStyle.Italic, marginLeft = 4f, marginTop = 4f } });
                return;
            }

            foreach (IGrouping<string, DialogueVariableCatalogEntry> group in matches.GroupBy(entry => entry.groupLabel))
            {
                bool showTypeLabel = group.Select(entry => entry.typeLabel).Distinct().Count() > 1; // multi-inherited from IDialogueVariable (e.g. KeyItem, ActionItem, etc. from InventoryItem)
                var foldout = new Foldout { text = $"{group.Key} ({group.Count()})", viewDataKey = _foldoutViewDataKeyPrefix + group.Key };
                foreach (DialogueVariableCatalogEntry entry in group) { foldout.Add(BuildRow(entry, showTypeLabel)); }
                listView.Add(foldout);
            }
        }
        #endregion

        #region UIHelpers
        private VisualElement BuildNoneRow()
        {
            VisualElement row = BuildRowBase(null, _noneLabel);
            row.Q<Label>().AddToClassList(StandardBrownTheme.dimClass);
            return row;
        }

        private VisualElement BuildRow(DialogueVariableCatalogEntry entry, bool showTypeLabel)
        {
            ScriptableObject variable = entry.variable;
            VisualElement row = BuildRowBase(variable, variable.name);
            if (showTypeLabel) { row.Add(new Label(entry.typeLabel) { style = { color = _typeLabelColour, marginLeft = 4f } }); } // show subtype label on multi-inherited
            if (mode != DialogueVariablesPanelMode.Overlay) { return row; }

            row.AddManipulator(new StandardObjectDragManipulator(() => variable));
            row.RegisterCallback<ClickEvent>(clickEvent => { if (clickEvent.clickCount == 2) { EditorGUIUtility.PingObject(variable); } });
            return row;
        }

        private VisualElement BuildRowBase(ScriptableObject variable, string label)
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, paddingTop = 1f, paddingBottom = 1f } };
            row.Add(new Image { image = variable != null ? AssetPreview.GetMiniThumbnail(variable) : null, style = { width = _iconSize, height = _iconSize, flexShrink = 0f, marginRight = 4f } });
            row.Add(new Label(label) { style = { flexGrow = 1f } });
            if (mode != DialogueVariablesPanelMode.Picker) { return row; }

            row.AddToClassList(StandardBrownTheme.rowClass);
            row.RegisterCallback<ClickEvent>(clickEvent => { if (clickEvent.button == 0) { onPick?.Invoke(variable); } });
            return row;
        }
        #endregion
        
        #region UtilityHelpers
        private void FitToWindow()
        {
            // Fixed height (w.r.t. graph window) to ensure the list scrolls instead of running off the window
            if (windowRoot == null || float.IsNaN(windowRoot.layout.height)) { return; }
            style.height = Mathf.Clamp(windowRoot.layout.height - _windowMargin, _minHeight, _maxHeight);
        }
        
        private static bool Matches(DialogueVariableCatalogEntry entry, string search)
        {
            if (string.IsNullOrEmpty(search)) { return true; }
            return entry.variable.name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 || entry.typeLabel.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
        }
        #endregion
    }
}

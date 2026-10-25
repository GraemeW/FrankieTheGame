#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace LowDefMustard.Rendering.Editor
{
    [CustomEditor(typeof(BattleEffectRecipe))]
    public sealed class BattleEffectRecipeEditor : UnityEditor.Editor
    {
        // Notes:
        //  - The selection lives in BattleEffectRecipeSelection:  the preview window's timeline sets it while scrubbed / stepped
        //  - Internal methods/properties are for test visibility

        // Const Tunables
        private const float _rowHeight = 20f;
        private const int _maxVisibleRows = 16;
        private const float _sectionSpacing = 6f;
        private const float _buttonGroupGap = 8f;
        private const float _shiftFieldWidth = 50f;
        internal const string shiftUndoName = "Shift Battle Effect Entries";
        internal const string addItemUndoName = "Add Battle Effect Item";
        internal const string addInstanceUndoName = "Add Battle Effect Instance";
        internal const string deleteItemUndoName = "Delete Battle Effect Item";
        internal const string deleteInstanceUndoName = "Delete Battle Effect Instance";
        private const string _emptySelectionHint = "Select entries above (or scrub the paused preview's timeline) to edit them";

        // Keys
        private const string _fromSelectedSessionKey = "LowDefMustard.Rendering.Editor.BattleEffectRecipeEditor.isAddingFromSelected";
        private const string _shiftAmountSessionKey = "LowDefMustard.Rendering.Editor.BattleEffectRecipeEditor.shiftAmount";
        
        // State
        private BattleEffectRecipe recipe;
        private SerializedProperty itemsProperty;
        private SerializedProperty instancesProperty;
        private Vector2Int shownListSizes = new(-1, -1); // Items, instances
        private readonly List<BattleEffectRecipeEntry> entries = new();
        private readonly List<BattleEffectEntryId> shownEntries = new();
        private bool isRefreshingList;
        
        // Cached Elements
        private ListView entryList;
        private VisualElement entryView;
        private Label selectionHint;

        #region UnityMethods
        public override VisualElement CreateInspectorGUI()
        {
            recipe = (BattleEffectRecipe)target;
            itemsProperty = serializedObject.FindProperty(BattleEffectRecipe.itemsField);
            instancesProperty = serializedObject.FindProperty(BattleEffectRecipe.instancesField);
            entries.Clear();
            shownEntries.Clear();
            shownListSizes = new Vector2Int(-1, -1);

            var root = new VisualElement();
            BuildRecipeSettings(root);
            BuildEntrySection(root);
            BuildEntryView(root);
            Refresh();

            root.TrackSerializedObjectValue(serializedObject, _ => Refresh());
            root.RegisterCallback<AttachToPanelEvent>(_ => BattleEffectRecipeSelection.changed += HandleSelectionChanged);
            root.RegisterCallback<DetachFromPanelEvent>(_ => BattleEffectRecipeSelection.changed -= HandleSelectionChanged);
            return root;
        }
        #endregion

        #region GUI
        private void BuildRecipeSettings(VisualElement root)
        {
            SerializedProperty property = serializedObject.GetIterator();
            for (bool hasProperty = property.NextVisible(true); hasProperty; hasProperty = property.NextVisible(false))
            {
                if (property.name is "m_Script" or BattleEffectRecipe.itemsField or BattleEffectRecipe.instancesField) { continue; }
                root.Add(new PropertyField(property.Copy()));
            }
        }

        private void BuildEntrySection(VisualElement root)
        {
            var foldout = new Foldout { text = "Items and Instances", value = true, style = { marginTop = _sectionSpacing } };

            // Header
            var header = new VisualElement { style = { flexDirection = FlexDirection.Row, unityFontStyleAndWeight = FontStyle.Bold } };
            header.Add(new Label("Label") { style = { flexGrow = 1f, paddingLeft = 3f } });
            header.Add(new Label("Shape / Recipe") { style = { width = BattleEffectEntryRow.kindWidth } });
            header.Add(new Label("Start") { style = { width = BattleEffectEntryRow.startWidth, marginLeft = BattleEffectEntryRow.columnGap, unityTextAlign = TextAnchor.MiddleCenter } });
            header.Add(new VisualElement { style = { width = BattleEffectEntryRow.deleteWidth } });
            foldout.Add(header);

            BuildEntryList(header);
            foldout.Add(entryList);
            foldout.Add(BuildButtonRow());
            foldout.Add(BuildShiftRow());
            root.Add(foldout);
        }

        private VisualElement BuildEntryList(VisualElement headerReference)
        {
            entryList = new ListView(entries, _rowHeight, () => new BattleEffectEntryRow(DeleteEntry), BindRow)
            {
                selectionType = SelectionType.Multiple,
                showBorder = true,
                virtualizationMethod = CollectionVirtualizationMethod.FixedHeight,
                unbindItem = (element, _) => ((BattleEffectEntryRow)element).UnbindRow()
            };
            entryList.selectedIndicesChanged += _ => HandleListSelectionChanged();
            
            // Alignment Fix:  keep header's columns over the rows' while the list's scrollbar takes up width
            var scroller = entryList.Q<Scroller>(className: Scroller.verticalVariantUssClassName);
            scroller?.RegisterCallback<GeometryChangedEvent>(_ =>
            {
                bool isScrollerShown = scroller.resolvedStyle.display == DisplayStyle.Flex && scroller.resolvedStyle.visibility == Visibility.Visible;
                headerReference.style.paddingRight = isScrollerShown ? scroller.resolvedStyle.width : 0f;
            });
            return entryList;
        }

        private VisualElement BuildButtonRow()
        {
            var buttons = new VisualElement { style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap, justifyContent = Justify.FlexEnd, alignItems = Align.Center } };
            buttons.Add(new Label("Item:"));
            buttons.Add(new Button(() => AddItem(false)) { text = "Add", tooltip = "A new item starting w/ the last entry (or the latest-starting selected)" });
            buttons.Add(new Button(() => AddItem(true)) { text = "Add+", tooltip = "A new item starting as the last entry ends (or the latest-ending selected)" });
            buttons.Add(new Label("|") { style = { marginLeft = _buttonGroupGap, marginRight = _buttonGroupGap } });
            buttons.Add(new Label("Instance:"));
            buttons.Add(new Button(() => AddInstance(false)) { text = "Add", tooltip = "A new instance offset to the last entry's start (or the latest-starting selected)" });
            buttons.Add(new Button(() => AddInstance(true)) { text = "Add+", tooltip = "A new instance offset to where the last entry ends (or the latest-ending selected)" });
            buttons.Add(new Label("|") { style = { marginLeft = _buttonGroupGap, marginRight = _buttonGroupGap } });
            
            // Selected vs. global ref (for add buttons)
            var fromSelectedToggle = new Toggle("From selected")
            {
                value = IsAddingFromSelected(),
                tooltip = "On:  Add / Add+ - time from the selected entries instead of global last"
            };
            fromSelectedToggle.labelElement.style.minWidth = StyleKeyword.Auto;
            fromSelectedToggle.labelElement.style.marginRight = _buttonGroupGap / 2f;
            fromSelectedToggle.RegisterValueChangedCallback(changeEvent => SetAddingFromSelected(changeEvent.newValue));
            buttons.Add(fromSelectedToggle);
            return buttons;
        }

        private VisualElement BuildShiftRow()
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.FlexEnd, alignItems = Align.Center } };
            row.Add(new Label("Shift selected"));
            var amountField = new FloatField { value = SessionState.GetFloat(_shiftAmountSessionKey, 0f), style = { width = _shiftFieldWidth } };
            amountField.RegisterValueChangedCallback(changeEvent => SessionState.SetFloat(_shiftAmountSessionKey, changeEvent.newValue));
            row.Add(amountField);
            row.Add(new Label("s"));
            row.Add(new Button(() => ShiftSelected(amountField.value))
            {
                text = "Shift",
                tooltip = "Moves the selected entries' start times (negative shift stops when the earliest reaches 0)"
            });
            return row;
        }

        private void BuildEntryView(VisualElement root)
        {
            selectionHint = new Label(_emptySelectionHint) { style = { marginTop = _sectionSpacing, unityFontStyleAndWeight = FontStyle.Italic } };
            entryView = new VisualElement { style = { marginTop = _sectionSpacing } };
            root.Add(selectionHint);
            root.Add(entryView);
        }

        private void BindRow(VisualElement element, int rowIndex)
        {
            BattleEffectRecipeEntry entry = entries[rowIndex];
            ((BattleEffectEntryRow)element).Bind(GetListProperty(entry.id).GetArrayElementAtIndex(entry.index), entry, rowIndex, entryList.selectedIndices.Contains(rowIndex));
        }
        #endregion

        #region EventHandlers
        private void HandleListSelectionChanged()
        {
            if (isRefreshingList) { return; } // A refresh drops the list's selection;  it's re-applied after
            RefreshRowColours();
            BattleEffectRecipeSelection.Set(recipe, entryList.selectedIndices.Where(row => row >= 0 && row < entries.Count).Select(row => entries[row].id));
        }

        private void HandleSelectionChanged(BattleEffectRecipe changedRecipe)
        {
            if (changedRecipe != recipe) { return; }
            ApplyListSelection();
            RefreshEntryView();
        }
        #endregion

        #region PrivateInternalMethods
        internal IReadOnlyList<BattleEffectRecipeEntry> GetEntries() => entries;
        internal IReadOnlyList<BattleEffectEntryId> GetShownEntries() => shownEntries;

        internal float ShiftSelected(float amountSeconds)
        {
            serializedObject.Update();
            
            List<SerializedProperty> startProperties = BattleEffectRecipeSelection.Get(recipe)
                .Where(id => id.index >= 0 && id.index < GetListProperty(id).arraySize)
                .Select(id => GetListProperty(id).GetArrayElementAtIndex(id.index).FindPropertyRelative(id.isInstance ? BattleEffectInstance.startOffsetSecondsField : BattleEffectItem.startTimeSecondsField))
                .ToList();
            if (startProperties.Count == 0) { return 0f; }
            
            float shift = Mathf.Max(amountSeconds, -startProperties.Min(property => property.floatValue)); // If shift would push start time <0, we shift the selection only the amount to get the limited-most item to zero (avoids collapsing the selection)
            if (Mathf.Approximately(shift, 0f)) { return 0f; }
            
            foreach (SerializedProperty startProperty in startProperties) { startProperty.floatValue = Mathf.Max(startProperty.floatValue + shift, 0f); }
            serializedObject.ApplyModifiedProperties();
            Undo.SetCurrentGroupName(shiftUndoName);
            Refresh();
            return shift;
        }

        internal static bool IsAddingFromSelected() => SessionState.GetBool(_fromSelectedSessionKey, true);
        internal static void SetAddingFromSelected(bool isFromSelected) => SessionState.SetBool(_fromSelectedSessionKey, isFromSelected);

        internal float GetAddTime(bool isAfterLastEntry)
        {
            IReadOnlyList<BattleEffectEntryId> selectedEntries = BattleEffectRecipeSelection.Get(recipe);
            List<BattleEffectRecipeEntry> anchors = IsAddingFromSelected() ? entries.Where(entry => selectedEntries.Contains(entry.id)).ToList() : new List<BattleEffectRecipeEntry>();
            if (anchors.Count > 0) { return isAfterLastEntry ? anchors.Max(entry => entry.endTime) : anchors.Max(entry => entry.startTime); }
            
            if (entries.Count == 0) { return 0f; }
            return isAfterLastEntry ? entries[^1].endTime : entries[^1].startTime;
        }

        internal void AddItem(bool isAfterLastEntry)
        {
            float startTime = GetAddTime(isAfterLastEntry);
            Undo.RecordObject(recipe, addItemUndoName);
            int itemIndex = recipe.AddItem(new BattleEffectItem(startTime));
            Undo.FlushUndoRecordObjects();
            Undo.SetCurrentGroupName(addItemUndoName);
            EditorUtility.SetDirty(recipe);
            serializedObject.Update();
            Refresh();
            BattleEffectRecipeSelection.Set(recipe, new[] { BattleEffectEntryId.Item(itemIndex) });
        }

        internal void AddInstance(bool isAfterLastEntry)
        {
            float startOffset = GetAddTime(isAfterLastEntry);
            Undo.RecordObject(recipe, addInstanceUndoName);
            int instanceIndex = recipe.AddInstance(new BattleEffectInstance(null, startOffset, false, false, System.Array.Empty<BattleEffectColourSwap>()));
            Undo.FlushUndoRecordObjects();
            Undo.SetCurrentGroupName(addInstanceUndoName);
            EditorUtility.SetDirty(recipe);
            serializedObject.Update();
            Refresh();
            BattleEffectRecipeSelection.Set(recipe, new[] { BattleEffectEntryId.Instance(instanceIndex) });
        }

        internal void DeleteEntry(BattleEffectEntryId entryId)
        {
            serializedObject.Update();
            SerializedProperty listProperty = GetListProperty(entryId);
            if (entryId.index < 0 || entryId.index >= listProperty.arraySize) { return; }
            listProperty.DeleteArrayElementAtIndex(entryId.index);
            serializedObject.ApplyModifiedProperties();
            Undo.SetCurrentGroupName(entryId.isInstance ? deleteInstanceUndoName : deleteItemUndoName);
            BattleEffectRecipeSelection.Remove(recipe, entryId);
            Refresh();
        }

        private SerializedProperty GetListProperty(BattleEffectEntryId entryId) => entryId.isInstance ? instancesProperty : itemsProperty;

        private void Refresh()
        {
            if (recipe == null || entryList == null) { return; }
            List<BattleEffectRecipeEntry> newEntries = BattleEffectRecipeEntries.Collect(recipe);
            if (!newEntries.SequenceEqual(entries))
            {
                entries.Clear();
                entries.AddRange(newEntries);
                entryList.style.height = Mathf.Clamp(entries.Count, 1, _maxVisibleRows) * _rowHeight + 2f;
                isRefreshingList = true;
                entryList.RefreshItems();
                isRefreshingList = false;
                ApplyListSelection();
            }
            RefreshEntryView();
        }

        private void ApplyListSelection()
        {
            IReadOnlyList<BattleEffectEntryId> selectedEntries = BattleEffectRecipeSelection.Get(recipe);
            var rows = new List<int>();
            for (int row = 0; row < entries.Count; row++)
            {
                if (selectedEntries.Contains(entries[row].id)) { rows.Add(row); }
            }
            entryList.SetSelectionWithoutNotify(rows);
            RefreshRowColours();
            if (rows.Count > 0) { entryList.ScrollToItem(rows[0]); }
        }

        private void RefreshRowColours()
        {
            // The rows' own tints cover the list's selection highlight, so they draw it themselves
            var selectedRows = new HashSet<int>(entryList.selectedIndices);
            entryList.Query<BattleEffectEntryRow>().ForEach(row => row.SetSelected(selectedRows.Contains(row.rowIndex)));
        }

        private void RefreshEntryView()
        {
            // Note:  Rebuilt only when the selected entries change (or a list resizes shifting their paths)
            //  - Does NOT rebuild on a re-sort ~ would otherwise drop focus mid-edit
            
            IReadOnlyList<BattleEffectEntryId> selectedEntries = BattleEffectRecipeSelection.Get(recipe);
            List<BattleEffectEntryId> entriesToShow = entries.Select(entry => entry.id).Where(selectedEntries.Contains).ToList();
            var listSizes = new Vector2Int(itemsProperty.arraySize, instancesProperty.arraySize);
            if (listSizes == shownListSizes && entriesToShow.Count == shownEntries.Count && entriesToShow.All(shownEntries.Contains)) { return; }

            shownEntries.Clear();
            shownEntries.AddRange(entriesToShow);
            shownListSizes = listSizes;
            
            entryView.Clear();
            foreach (BattleEffectEntryId entryId in shownEntries)
            {
                SerializedProperty entryProperty = GetListProperty(entryId).GetArrayElementAtIndex(entryId.index);
                entryView.Add(entryId.isInstance ? new BattleEffectInstanceElement(entryProperty, entryId.index) : new BattleEffectItemElement(entryProperty, entryId.index));
            }
            entryView.Bind(serializedObject);
            selectionHint.style.display = shownEntries.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }
        #endregion
    }
}
#endif

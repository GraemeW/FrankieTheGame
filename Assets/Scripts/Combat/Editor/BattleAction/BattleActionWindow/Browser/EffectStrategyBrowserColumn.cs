using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Frankie.Combat.Editor
{
    public sealed class EffectStrategyBrowserColumn : VisualElement
    {
        // Const Tunables
        private const float _detailsHeight = 234f;
        private const long _rebuildDelayMs = 300;
        private const int _headerIdOffset = 1_000_000;
        private const float _treeMinHeight = 120f;
        private const string _titleText = "Effect Strategy Browser";
        private const string _subtitleText = "all effect strategies";
        private const string _noSelectionHint = "Select an effect strategy below to view / edit it here";
        private const string _makeNewText = "Make New Effect Strategy";
        private const string _makeNewTooltip = "Creates a new asset of this type in its folder, then selects it";
        private const string _addText = "Add to Battle Action";
        private const string _addTooltip = "Appends the selected effect strategy to the battle action's effect strategies";
        private const string _filtersText = "Filters";
        private const string _allFiltersText = "All";
        private const string _noFiltersText = "None";
        
        // Keys
        private const string _hiddenCategoryPrefsKey = "Frankie.Combat.Editor.EffectStrategyBrowser.isHidden.";
        private const string _newTypePrefsKey = "Frankie.Combat.Editor.EffectStrategyBrowser.newType";
        private const string _filtersViewDataKey = "Frankie.Combat.Editor.EffectStrategyBrowser.filters";

        // Element References
        private readonly ScrollView detailsView;
        private readonly DropdownField typeField;
        private readonly Button addButton;
        private readonly ToolbarSearchField searchField;
        private readonly TreeView tree;
        private readonly Dictionary<EffectStrategyCategory, Toggle> filterToggles = new();

        // State
        private readonly Func<EffectStrategy, bool> addToBattleAction;
        private readonly Func<bool> canAddToBattleAction;
        private readonly Action changed;
        private EffectStrategy selectedStrategy;
        private EffectStrategyElement detailsElement;

        private List<TreeViewItemData<EffectStrategyCatalogEntry>> rootItems = new();
        private readonly List<Type> creatableTypes = EffectStrategyCatalog.GetCreatableTypes();
        private readonly HashSet<EffectStrategyCategory> collapsedCategories = new();
        private IVisualElementScheduledItem pendingRebuild;

        // Constructor
        public EffectStrategyBrowserColumn(Func<EffectStrategy, bool> addToBattleAction, Func<bool> canAddToBattleAction, Action changed)
        {
            this.addToBattleAction = addToBattleAction;
            this.canAddToBattleAction = canAddToBattleAction;
            this.changed = changed;

            style.flexGrow = 1f;
            BattleActionWindowStyles.ApplyLibraryColumn(this);
            Add(BattleActionWindowStyles.CreateColumnHeader(_titleText, _subtitleText, out _));

            // Readonly elements are built here, then laid out top -> bottom in the body
            detailsView = BuildDetailsView();
            typeField = BuildTypeField();
            addButton = BuildAddButton();
            searchField = BuildSearchField();
            tree = BuildTree();

            VisualElement body = BuildBody();
            body.Add(detailsView);
            body.Add(BuildCreateRow(typeField));
            body.Add(addButton);
            body.Add(BattleActionWindowStyles.CreateDivider());
            body.Add(BuildFilters());
            body.Add(searchField);
            body.Add(tree);
            Add(body);

            ShowDetails(null);
            Rebuild();
        }

        #region PublicMethods
        public void RefreshNames() => detailsElement?.RefreshName();
        public void RefreshAddButton() => addButton.SetEnabled(selectedStrategy != null && canAddToBattleAction != null && canAddToBattleAction());
        public void RequestRebuild()
        {
            pendingRebuild?.Pause();
            pendingRebuild = schedule.Execute(Rebuild).StartingIn(_rebuildDelayMs);
        }
        
        public void Rebuild()
        {
            pendingRebuild?.Pause();
            RememberExpansion();

            string search = searchField.value?.Trim();
            List<EffectStrategyCatalogEntry> entries = EffectStrategyCatalog.Collect();
            rootItems = new List<TreeViewItemData<EffectStrategyCatalogEntry>>();
            int id = 0;
            foreach (EffectStrategyCategory category in Enum.GetValues(typeof(EffectStrategyCategory)))
            {
                if (IsHidden(category)) { continue; }
                List<TreeViewItemData<EffectStrategyCatalogEntry>> children = entries
                    .Where(entry => entry.category == category && Matches(entry, search))
                    .Select(entry => new TreeViewItemData<EffectStrategyCatalogEntry>(id++, entry))
                    .ToList();
                if (children.Count == 0) { continue; }
                string headerText = $"{EffectStrategyCatalog.GetCategoryLabel(category)} ({children.Count})";
                rootItems.Add(new TreeViewItemData<EffectStrategyCatalogEntry>(GetHeaderId(category), EffectStrategyCatalogEntry.Header(category, headerText), children));
            }

            // Note:  Expand before the rebuild ~ selecting by id after expanding post-rebuild resolves against stale indices
            tree.SetRootItems(rootItems);
            foreach (TreeViewItemData<EffectStrategyCatalogEntry> rootItem in rootItems.Where(rootItem => !collapsedCategories.Contains(rootItem.data.category)))
            {
                tree.ExpandItem(rootItem.id, false, false);
            }
            tree.Rebuild();
            ApplySelection();
            RefreshAddButton();
        }
        #endregion

        #region GUI
        private static VisualElement BuildBody()
        {
            return new VisualElement { style = { flexGrow = 1f, paddingLeft = BattleActionWindowStyles.columnPadding, paddingRight = BattleActionWindowStyles.columnPadding, paddingBottom = BattleActionWindowStyles.columnPadding } };
        }

        private static ScrollView BuildDetailsView() => new(ScrollViewMode.Vertical) { style = { height = _detailsHeight, flexShrink = 0f } };

        private DropdownField BuildTypeField()
        {
            var field = new DropdownField(creatableTypes.Select(EffectStrategyCatalog.GetTypeLabel).ToList(), GetSavedTypeIndex()) { style = { flexGrow = 1f, flexShrink = 1f, marginLeft = 0f } };
            field.RegisterValueChangedCallback(_ => SaveTypeIndex());
            return field;
        }

        private VisualElement BuildCreateRow(DropdownField setTypeField)
        {
            var createRow = new VisualElement { style = { flexDirection = FlexDirection.Row, flexShrink = 0f, marginTop = 4f } };
            createRow.Add(setTypeField);
            createRow.Add(new Button(MakeNew) { text = _makeNewText, tooltip = _makeNewTooltip });
            return createRow;
        }

        private Button BuildAddButton() => new(AddSelected) { text = _addText, tooltip = _addTooltip, style = { marginLeft = 0f, marginRight = 0f, flexShrink = 0f } };

        private TreeView BuildTree()
        {
            var treeView = new TreeView
            {
                fixedItemHeight = BattleActionWindowStyles.rowHeight,
                selectionType = SelectionType.Single,
                showBorder = true,
                makeItem = () => new EffectStrategyBrowserRow(),
                style = { flexGrow = 1f, minHeight = _treeMinHeight }
            };
            treeView.bindItem = (element, index) => ((EffectStrategyBrowserRow)element).Bind(treeView.GetItemDataForIndex<EffectStrategyCatalogEntry>(index));
            treeView.selectedIndicesChanged += _ => HandleTreeSelectionChanged();
            return treeView;
        }
        
        private ToolbarSearchField BuildSearchField()
        {
            var field = new ToolbarSearchField { style = { width = StyleKeyword.Auto, flexShrink = 0f, marginLeft = 0f, marginRight = 0f, marginTop = 4f, marginBottom = 4f } };
            field.RegisterValueChangedCallback(_ => Rebuild());
            return field;
        }

        private VisualElement BuildFilters()
        {
            var foldout = new Foldout { text = _filtersText, value = false, viewDataKey = _filtersViewDataKey, style = { flexShrink = 0f } };
            var toggles = new VisualElement { style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap } };
            foreach (EffectStrategyCategory category in Enum.GetValues(typeof(EffectStrategyCategory)))
            {
                var toggle = new Toggle { text = EffectStrategyCatalog.GetCategoryLabel(category), value = !IsHidden(category), style = { marginRight = BattleActionWindowStyles.sectionSpacing } };
                toggle.RegisterValueChangedCallback(changeEvent =>
                {
                    SetHidden(category, !changeEvent.newValue);
                    Rebuild();
                });
                filterToggles[category] = toggle;
                toggles.Add(toggle);
            }
            foldout.Add(toggles);

            var extraButtons = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.FlexEnd } };
            extraButtons.Add(new Button(() => SetAllFilters(true)) { text = _allFiltersText });
            extraButtons.Add(new Button(() => SetAllFilters(false)) { text = _noFiltersText });
            foldout.Add(extraButtons);
            return foldout;
        }
        #endregion

        #region EventHandlers
        private void HandleTreeSelectionChanged()
        {
            if (tree.selectedIndex < 0) { return; }
            EffectStrategyCatalogEntry entry = tree.GetItemDataForIndex<EffectStrategyCatalogEntry>(tree.selectedIndex);
            if (entry.isHeader) { return; }
            selectedStrategy = entry.strategy;
            ShowDetails(selectedStrategy);
            RefreshAddButton();
        }
        #endregion

        #region TreeViewUtility
        private static int GetHeaderId(EffectStrategyCategory category) => _headerIdOffset + (int)category;
        private static bool Matches(EffectStrategyCatalogEntry entry, string search)
        {
            if (string.IsNullOrEmpty(search)) { return true; }
            return entry.strategy.name.Contains(search, StringComparison.OrdinalIgnoreCase) || entry.summary.Contains(search, StringComparison.OrdinalIgnoreCase);
        }
        
        private void RememberExpansion()
        {
            foreach (TreeViewItemData<EffectStrategyCatalogEntry> rootItem in rootItems)
            {
                if (tree.IsExpanded(rootItem.id)) { collapsedCategories.Remove(rootItem.data.category); }
                else { collapsedCategories.Add(rootItem.data.category); }
            }
        }
        #endregion
        
        #region SelectionUtility
        private void ApplySelection()
        {
            if (selectedStrategy == null)
            {
                selectedStrategy = null; // Clears a destroyed asset's reference
                tree.SetSelectionWithoutNotify(Array.Empty<int>());
                ShowDetails(null);
                return;
            }

            foreach (TreeViewItemData<EffectStrategyCatalogEntry> child in rootItems.SelectMany(rootItem => rootItem.children))
            {
                if (child.data.strategy != selectedStrategy) { continue; }
                tree.SetSelectionByIdWithoutNotify(new[] { child.id });
                tree.ScrollToItemById(child.id);
                return;
            }
            tree.SetSelectionWithoutNotify(Array.Empty<int>());
        }

        private void ShowDetails(EffectStrategy strategy)
        {
            detailsView.Clear();
            detailsElement = null;
            if (strategy == null)
            {
                detailsView.Add(BattleActionWindowStyles.CreateHint(_noSelectionHint));
                return;
            }
            detailsElement = new EffectStrategyElement(strategy, null, changed);
            detailsView.Add(detailsElement);
        }
        #endregion
        
        #region FilteringUtility
        private static bool IsHidden(EffectStrategyCategory category) => EditorPrefs.GetBool(_hiddenCategoryPrefsKey + category, false);
        private static void SetHidden(EffectStrategyCategory category, bool isHidden) => EditorPrefs.SetBool(_hiddenCategoryPrefsKey + category, isHidden);
        private void SetAllFilters(bool isShown)
        {
            foreach ((EffectStrategyCategory category, Toggle toggle) in filterToggles)
            {
                SetHidden(category, !isShown);
                toggle.SetValueWithoutNotify(isShown);
            }
            Rebuild();
        }
        #endregion

        #region AddMakeNewUtility
        private void AddSelected()
        {
            if (selectedStrategy == null) { return; }
            addToBattleAction(selectedStrategy);
        }
        
        private int GetSavedTypeIndex()
        {
            string savedType = EditorPrefs.GetString(_newTypePrefsKey, string.Empty);
            return Mathf.Max(0, creatableTypes.FindIndex(type => type.FullName == savedType));
        }

        private void SaveTypeIndex()
        {
            if (typeField.index < 0 || typeField.index >= creatableTypes.Count) { return; }
            EditorPrefs.SetString(_newTypePrefsKey, creatableTypes[typeField.index].FullName);
        }
        
        private void MakeNew()
        {
            if (typeField.index < 0 || typeField.index >= creatableTypes.Count) { return; }
            EffectStrategy strategy = EffectStrategyCatalog.Create(creatableTypes[typeField.index]);
            if (strategy == null) { return; }

            EffectStrategyCategory category = EffectStrategyCatalog.Categorize(strategy).category;
            if (IsHidden(category))
            {
                SetHidden(category, false);
                filterToggles[category].SetValueWithoutNotify(true);
            }
            collapsedCategories.Remove(category);
            searchField.SetValueWithoutNotify(string.Empty);

            selectedStrategy = strategy;
            ShowDetails(strategy);
            Rebuild();
        }
        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using LowDefMustard.Utils.Editor;

namespace Frankie.Combat.Editor
{
    public sealed class SkillListPanel : VisualElement
    {
        // Const Tunables
        private const string _skillsRoot = "Assets/Game/OnLoadAssets/Skills/";
        
        private const float _width = 260f;
        private const float _margin = 6f;
        private const float _rowHeight = 22f;
        private const int _headerIdOffset = 1_000_000;
        private const string _ungroupedText = "Ungrouped";
        private const string _titleText = "Skills";
        private const string _filtersText = "Filters";
        private const string _allFiltersText = "All";
        private const string _noFiltersText = "None";

        // Keys
        private const string _hiddenGroupPrefsKey = "Frankie.Combat.Editor.SkillListPanel.isHidden.";
        private const string _filtersViewDataKey = "Frankie.Combat.Editor.SkillListPanel.filters";

        // Element References
        private readonly Label titleLabel;
        private readonly VisualElement filterToggles;
        private readonly Dictionary<string, Toggle> filterToggleLookup = new();
        private readonly ToolbarSearchField searchField;
        private readonly TreeView tree;

        // State
        private IReadOnlyList<Skill> allSkills = Array.Empty<Skill>();
        private HashSet<Skill> selectedSkillTreeSkills = new();
        private List<TreeViewItemData<SkillListEntry>> rootItems = new();
        private List<string> allGroups = new();
        private List<string> shownGroups = new();
        private readonly HashSet<string> collapsedGroups = new();

        // Constructor
        public SkillListPanel()
        {
            AddToClassList(StandardBrownTheme.panelClass);
            style.position = Position.Absolute;
            style.top = _margin;
            style.right = _margin;
            style.bottom = _margin;
            style.width = _width;

            titleLabel = BuildTitle();
            filterToggles = new VisualElement { style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap } };
            searchField = BuildSearchField();
            tree = BuildTree();
            Add(titleLabel);
            Add(BuildFilters(filterToggles));
            Add(searchField);
            Add(tree);
        }

        #region PublicMethods
        public void SetSkills(IReadOnlyList<Skill> skills)
        {
            allSkills = skills ?? Array.Empty<Skill>();
            RefreshFilterToggles();
            RefreshList();
        }

        public void SetSelectedSkillTreeSkills(IEnumerable<Skill> skills)
        {
            selectedSkillTreeSkills = new HashSet<Skill>(skills ?? Enumerable.Empty<Skill>());
            tree.RefreshItems();
        }
        #endregion

        #region GUI
        private static Label BuildTitle()
        {
            var title = new Label(_titleText) { style = { paddingLeft = _margin, paddingTop = 3f, paddingBottom = 3f } };
            title.AddToClassList(StandardBrownTheme.panelHeaderClass);
            return title;
        }

        private VisualElement BuildFilters(VisualElement toggles)
        {
            var foldout = new Foldout { text = _filtersText, value = false, viewDataKey = _filtersViewDataKey, style = { flexShrink = 0f, marginLeft = 4f, marginRight = 4f, marginTop = 2f } };
            foldout.Add(toggles);

            var extraButtons = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.FlexEnd } };
            extraButtons.Add(new Button(() => SetAllFilters(true)) { text = _allFiltersText });
            extraButtons.Add(new Button(() => SetAllFilters(false)) { text = _noFiltersText });
            foldout.Add(extraButtons);
            return foldout;
        }

        private ToolbarSearchField BuildSearchField()
        {
            var field = new ToolbarSearchField { style = { width = StyleKeyword.Auto, marginLeft = 4f, marginRight = 4f, marginTop = 4f, marginBottom = 4f } };
            field.RegisterValueChangedCallback(_ => RefreshList());
            return field;
        }

        private TreeView BuildTree()
        {
            var treeView = new TreeView
            {
                fixedItemHeight = _rowHeight,
                selectionType = SelectionType.None,
                makeItem = () => new SkillListRow(),
                style = { flexGrow = 1f }
            };
            treeView.bindItem = (row, index) => BindRow((SkillListRow)row, treeView.GetItemDataForIndex<SkillListEntry>(index));
            return treeView;
        }

        private void BindRow(SkillListRow row, SkillListEntry entry)
        {
            bool isInSelectedSkillTree = !entry.isHeader && selectedSkillTreeSkills.Contains(entry.skill);
            row.Bind(entry, isInSelectedSkillTree);
        }
        #endregion

        #region Drawing
        private void RefreshList()
        {
            RememberExpansion();

            List<IGrouping<string, Skill>> groups = GetShownGroups(searchField.value?.Trim());
            rootItems = BuildTreeItems(groups);

            // Note:  Expand before the rebuild ~ expanding after resolves against stale indices
            tree.SetRootItems(rootItems);
            for (int i = 0; i < rootItems.Count; i++)
            {
                if (!collapsedGroups.Contains(groups[i].Key)) { tree.ExpandItem(rootItems[i].id, false, false); }
            }
            tree.Rebuild();
            titleLabel.text = $"{_titleText} ({groups.Sum(group => group.Count())})";
            shownGroups = groups.Select(group => group.Key).ToList();
        }

        private static List<TreeViewItemData<SkillListEntry>> BuildTreeItems(List<IGrouping<string, Skill>> groups)
        {
            // Ids:  skills count up from 0, headers from the offset (both stable only within a rebuild, expansion is keyed by group name)
            int skillId = 0;
            int headerId = _headerIdOffset;

            var headerItems = new List<TreeViewItemData<SkillListEntry>>();
            foreach (IGrouping<string, Skill> group in groups)
            {
                var skillItems = new List<TreeViewItemData<SkillListEntry>>();
                foreach (Skill skill in group)
                {
                    skillItems.Add(new TreeViewItemData<SkillListEntry>(skillId, SkillListEntry.Entry(skill)));
                    skillId++;
                }

                SkillListEntry header = SkillListEntry.Header($"{group.Key} ({skillItems.Count})");
                headerItems.Add(new TreeViewItemData<SkillListEntry>(headerId, header, skillItems));
                headerId++;
            }
            return headerItems;
        }

        private void RememberExpansion()
        {
            for (int i = 0; i < rootItems.Count && i < shownGroups.Count; i++)
            {
                if (tree.IsExpanded(rootItems[i].id)) { collapsedGroups.Remove(shownGroups[i]); }
                else { collapsedGroups.Add(shownGroups[i]); }
            }
        }
        #endregion

        #region GroupingAndFiltering
        private static string GetGroup(Skill skill)
        {
            string path = AssetDatabase.GetAssetPath(skill);
            if (!path.StartsWith(_skillsRoot, StringComparison.Ordinal)) { return _ungroupedText; }
            string relativePath = path[_skillsRoot.Length..];
            int separatorIndex = relativePath.IndexOf('/');
            return separatorIndex < 0 ? _ungroupedText : relativePath[..separatorIndex];
        }
        
        private List<IGrouping<string, Skill>> GetShownGroups(string search)
        {
            IEnumerable<Skill> matchingSkills = allSkills.Where(skill => MatchesSearch(skill, search));
            IEnumerable<IGrouping<string, Skill>> groups = matchingSkills.GroupBy(GetGroup);
            IEnumerable<IGrouping<string, Skill>> unhiddenGroups = groups.Where(group => !IsHidden(group.Key));
            return OrderByGroupName(unhiddenGroups, group => group.Key).ToList();
        }

        private static bool MatchesSearch(Skill skill, string search)
        {
            if (skill == null) { return false; }
            if (string.IsNullOrEmpty(search)) { return true; }
            return skill.name.Contains(search, StringComparison.OrdinalIgnoreCase);
        }
        
        private static IOrderedEnumerable<T> OrderByGroupName<T>(IEnumerable<T> items, Func<T, string> getGroupName)
        {
            return items.OrderBy(item => getGroupName(item) == _ungroupedText).ThenBy(getGroupName, StringComparer.OrdinalIgnoreCase); // Alphabetical, w/ ungrouped last
        }
        
        private static bool IsHidden(string group) => EditorPrefs.GetBool(_hiddenGroupPrefsKey + group, false);
        private static void SetHidden(string group, bool isHidden) => EditorPrefs.SetBool(_hiddenGroupPrefsKey + group, isHidden);

        private void RefreshFilterToggles()
        {
            List<string> groups = OrderByGroupName(allSkills.Where(skill => skill != null).Select(GetGroup).Distinct(), group => group).ToList();
            if (groups.SequenceEqual(allGroups)) { return; }
            allGroups = groups;

            filterToggles.Clear();
            filterToggleLookup.Clear();
            foreach (string group in allGroups)
            {
                var toggle = new Toggle { text = group, value = !IsHidden(group), style = { marginRight = 8f } };
                toggle.RegisterValueChangedCallback(changeEvent =>
                {
                    SetHidden(group, !changeEvent.newValue);
                    RefreshList();
                });
                filterToggleLookup[group] = toggle;
                filterToggles.Add(toggle);
            }
        }

        private void SetAllFilters(bool isShown)
        {
            foreach ((string group, Toggle toggle) in filterToggleLookup)
            {
                SetHidden(group, !isShown);
                toggle.SetValueWithoutNotify(isShown);
            }
            RefreshList();
        }
        #endregion
    }
}

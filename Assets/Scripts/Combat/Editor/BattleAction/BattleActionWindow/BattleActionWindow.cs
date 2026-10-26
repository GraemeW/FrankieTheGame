using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.UIElements;
using Frankie.Inventory;

namespace Frankie.Combat.Editor
{
    public sealed class BattleActionWindow : EditorWindow
    {
        // For editing a skill or action item w/ their corresponding battle action in one place ~ displays 3 columns (A->C left->right)
        //  - A. owner (skill || action item) + battle action (targeting, effect strategy slots)
        //  - B. the selected slots' effect strategies
        //  - C. effect strategy browser ~ for searching through existing / making new effect strategies

        // Const Tunables
        private const string _windowTitle = "Battle Action Editor";
        private const float _ownerColumnWidth = 340f;
        private const float _browserColumnWidth = 380f;
        private const float _minColumnWidth = 220f;
        private const string _styleSheetField = nameof(styleSheet);

        // Tunables
        [SerializeField] private StyleSheet styleSheet; // Set as the script's default reference

        // State
        [SerializeField] private Object owner;
        private BattleActionOwnerColumn ownerColumn;
        private EffectStrategySelectionColumn selectionColumn;
        private EffectStrategyBrowserColumn browserColumn;

        #region UnityMethods
        [MenuItem("Window/Battle Action Editor")]
        public static void ShowEditorWindow()
        {
            GetWindow<BattleActionWindow>(false, _windowTitle);
        }

        [OnOpenAsset(1)]
        public static bool OnOpenAsset(EntityId entityId, int line)
        {
            Object asset = EditorUtility.EntityIdToObject(entityId);
            if (!IsOwner(asset)) { return false; }

            var window = GetWindow<BattleActionWindow>(false, _windowTitle);
            window.SetOwner(asset);
            return true;
        }

        private void OnEnable()
        {
            Selection.selectionChanged += HandleSelectionChanged;
            Undo.undoRedoPerformed += HandleUndoRedo;
            EditorApplication.projectChanged += HandleProjectChanged;
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= HandleSelectionChanged;
            Undo.undoRedoPerformed -= HandleUndoRedo;
            EditorApplication.projectChanged -= HandleProjectChanged;
        }

        private void CreateGUI()
        {
            minSize = new Vector2(_minColumnWidth * 3f, 400f);
            if (styleSheet == null) { styleSheet = GetDefaultStyleSheet(); } // Windows opened before the default reference was set
            if (styleSheet != null) { rootVisualElement.styleSheets.Add(styleSheet); }
            rootVisualElement.AddToClassList(BattleActionWindowStyles.windowClass);

            ownerColumn = new BattleActionOwnerColumn();
            selectionColumn = new EffectStrategySelectionColumn(HandleEffectStrategyChanged);
            browserColumn = new EffectStrategyBrowserColumn(ownerColumn.AppendEffectStrategy, () => ownerColumn.HasValidBattleAction(), HandleEffectStrategyChanged);
            ownerColumn.slotsChanged += HandleSlotsChanged;
            ownerColumn.battleActionChanged += HandleBattleActionChanged;

            var innerSplit = new TwoPaneSplitView(1, _browserColumnWidth, TwoPaneSplitViewOrientation.Horizontal) { viewDataKey = "BattleActionWindow.innerSplit" };
            innerSplit.Add(WrapColumn(selectionColumn));
            innerSplit.Add(WrapColumn(browserColumn));
            var outerSplit = new TwoPaneSplitView(0, _ownerColumnWidth, TwoPaneSplitViewOrientation.Horizontal) { viewDataKey = "BattleActionWindow.outerSplit" };
            outerSplit.Add(WrapColumn(ownerColumn));
            outerSplit.Add(innerSplit);
            rootVisualElement.Add(outerSplit);

            ownerColumn.SetOwner(owner);
        }
        #endregion

        #region EventHandlers
        private void HandleSelectionChanged()
        {
            if (!IsOwner(Selection.activeObject) || Selection.activeObject == owner) { return; }
            SetOwner(Selection.activeObject);
        }

        private void HandleUndoRedo() => ownerColumn?.Refresh();

        private void HandleProjectChanged()
        {
            ownerColumn?.Refresh();
            selectionColumn?.RefreshNames();
            browserColumn?.RefreshNames();
            browserColumn?.RequestRebuild();
        }

        private void HandleSlotsChanged() => selectionColumn?.SetSlots(ownerColumn.GetSelectedSlots());
        private void HandleBattleActionChanged() => browserColumn?.RefreshAddButton();
        private void HandleEffectStrategyChanged() => browserColumn?.RequestRebuild();
        #endregion

        #region PrivateMethods
        private static bool IsOwner(Object asset) => asset is Skill or ActionItem;

        private StyleSheet GetDefaultStyleSheet()
        {
            var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(this))) as MonoImporter;
            return importer != null ? importer.GetDefaultReference(_styleSheetField) as StyleSheet : null;
        }

        private static VisualElement WrapColumn(VisualElement column)
        {
            var wrapper = new VisualElement { style = { minWidth = _minColumnWidth, flexGrow = 1f } };
            wrapper.Add(column);
            return wrapper;
        }

        private void SetOwner(Object setOwner)
        {
            owner = setOwner;
            titleContent = new GUIContent(owner != null ? $"{_windowTitle} ~ {owner.name}" : _windowTitle);
            ownerColumn?.SetOwner(owner);
        }
        #endregion
    }
}

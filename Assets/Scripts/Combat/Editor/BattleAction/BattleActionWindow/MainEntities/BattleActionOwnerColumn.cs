using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Frankie.Inventory;

namespace Frankie.Combat.Editor
{
    public sealed class BattleActionOwnerColumn : VisualElement
    {
        // Const Tunables
        private const int _maxVisibleSlots = 12;
        private const string _emptyTitle = "Battle Action Editor";
        private const string _noOwnerHint = "Double-click a Skill or Action Item (or select one) to edit it here";
        private const string _noBattleActionHint = "No battle action assigned ~ use New Battle Action above";
        private const string _noTargetingHint = "No targeting strategy assigned ~ this action can't pick targets";
        private const string _skillKindText = "Skill";
        private const string _actionItemKindText = "Action Item";
        private const string _battleActionKindText = "Battle Action";
        private const string _targetingText = "Targeting";
        private const string _effectStrategiesText = "Effect Strategies";
        private const string _newBattleActionText = "New Battle Action";
        private const string _createBattleActionTooltipFormat = "Creates {0} and assigns it";
        private const string _assignBattleActionTooltipFormat = "Assigns the existing {0} battle action (named after this {1})";
        private const string _battleActionSetTooltip = "Already using its own battle action";
        private const string _addSlotText = "+";
        private const string _addSlotTooltip = "Add an empty slot (assign it here, or select it and use the browser)";
        private const string _copyPasteTooltip = "Right-click to copy / paste";
        private const string _addSlotUndoName = "Add Effect Strategy Slot";
        private const string _deleteSlotUndoName = "Remove Effect Strategy Slot";
        private const string _moveSlotUndoName = "Move Effect Strategy Slot";
        private const string _makeNewUndoName = "Set Battle Action";
        private static readonly string[] _skippedBattleActionFields = { BattleAction.targetingStrategyField, BattleAction.effectStrategiesField };
        private static readonly string[] _noSkippedFields = Array.Empty<string>();

        // Element References
        private readonly Label titleLabel;
        private readonly Label kindLabel;
        private readonly ScrollView scrollView;
        private Button newBattleActionButton;
        private VisualElement battleActionSection;
        private VisualElement targetingCard;
        private ListView slotList;

        // State
        private readonly List<int> slotIndices = new();
        private SerializedObject ownerObject;
        private SerializedObject battleActionObject;
        private SerializedProperty slotsProperty;

        // Events
        public event Action slotsChanged;
        public event Action battleActionChanged;

        // Constructor
        public BattleActionOwnerColumn()
        {
            style.flexGrow = 1f;
            Add(BuildColumnHeader(out titleLabel, out kindLabel));
            scrollView = BattleActionWindowStyles.CreateColumnScrollView();
            Add(scrollView);
        }

        #region PublicMethods
        public bool HasValidBattleAction() => battleActionObject?.targetObject as BattleAction != null;
        
        public IReadOnlyList<(int index, EffectStrategy strategy)> GetSelectedSlots()
        {
            if (slotList == null || slotsProperty == null || battleActionObject?.targetObject == null) { return Array.Empty<(int, EffectStrategy)>(); }
            return slotList.selectedIndices
                .Where(index => index >= 0 && index < slotsProperty.arraySize)
                .OrderBy(index => index)
                .Select(index => (index, slotsProperty.GetArrayElementAtIndex(index).objectReferenceValue as EffectStrategy))
                .ToList();
        }

        public void SetOwner(UnityEngine.Object owner)
        {
            scrollView.Clear();
            ownerObject = null;
            battleActionObject = null;
            slotsProperty = null;
            slotList = null;
            newBattleActionButton = null;

            if (owner is not (Skill or ActionItem))
            {
                titleLabel.text = _emptyTitle;
                kindLabel.text = string.Empty;
                scrollView.Add(BattleActionWindowStyles.CreateHint(_noOwnerHint));
                slotsChanged?.Invoke();
                battleActionChanged?.Invoke();
                return;
            }

            ownerObject = new SerializedObject(owner);
            titleLabel.text = owner.name;
            kindLabel.text = owner is Skill ? _skillKindText : _actionItemKindText;
            BuildOwnerSection();
            battleActionSection = new VisualElement();
            scrollView.Add(battleActionSection);
            BuildBattleActionSection();
        }

        public bool AppendEffectStrategy(EffectStrategy strategy)
        {
            if (slotsProperty == null || strategy == null) { return false; }
            AddSlot(strategy);
            return true;
        }

        public void Refresh()
        {
            if (ownerObject == null) { return; }
            if (ownerObject.targetObject == null)
            {
                SetOwner(null);
                return;
            }
            ownerObject.Update();
            titleLabel.text = ownerObject.targetObject.name;
            RefreshNewBattleActionButton();
            if (battleActionObject == null) { return; }
            if (battleActionObject.targetObject == null)
            {
                BuildBattleActionSection();
                return;
            }
            battleActionObject.Update();
            RefreshSlots();
        }
        #endregion

        #region GUI
        private VisualElement BuildColumnHeader(out Label setTitleLabel, out Label setKindLabel)
        {
            VisualElement header = BattleActionWindowStyles.CreateColumnHeader(_emptyTitle, null, out setTitleLabel);
            setTitleLabel.tooltip = BattleActionWindowStyles.pingTooltip;
            setTitleLabel.RegisterCallback<ClickEvent>(_ =>
            {
                if (ownerObject?.targetObject != null) { EditorGUIUtility.PingObject(ownerObject.targetObject); }
            });
            setKindLabel = BattleActionWindowStyles.CreateDimLabel(string.Empty);
            header.Add(setKindLabel);
            return header;
        }

        private void BuildOwnerSection()
        {
            VisualElement card = BattleActionWindowStyles.CreateCard();
            card.style.marginTop = BattleActionWindowStyles.sectionSpacing / 2f;
            
            var fields = new VisualElement();
            bool isSkill = ownerObject.targetObject is Skill;
            string[] fieldNames = isSkill
                ? new[] { Skill.skillStatField, Skill.battleActionField }
                : new[] { ActionItem.consumableField, ActionItem.battleActionField };
            foreach (string fieldName in fieldNames) { fields.Add(new PropertyField(ownerObject.FindProperty(fieldName))); }
            fields.Bind(ownerObject);
            fields.TrackPropertyValue(GetBattleActionProperty(), _ => BuildBattleActionSection());
            card.Add(fields);

            var buttons = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.FlexEnd } };
            newBattleActionButton = new Button(MakeNewBattleAction) { text = _newBattleActionText };
            buttons.Add(newBattleActionButton);
            card.Add(buttons);
            scrollView.Add(card);
        }

        private void BuildBattleActionSection()
        {
            battleActionSection.Clear();
            battleActionObject = null;
            slotsProperty = null;
            slotList = null;
            RefreshNewBattleActionButton();

            var battleAction = GetBattleActionProperty().objectReferenceValue as BattleAction;
            if (battleAction == null)
            {
                Label hint = BattleActionWindowStyles.CreateHint(_noBattleActionHint);
                BattleActionWindowStyles.SetMissing(hint, true);
                battleActionSection.Add(hint);
                slotsChanged?.Invoke();
                battleActionChanged?.Invoke();
                return;
            }

            battleActionObject = new SerializedObject(battleAction);
            slotsProperty = battleActionObject.FindProperty(BattleAction.effectStrategiesField);
            
            BuildBattleActionHeader(battleAction);
            BuildTargetingSection(battleAction);
            BuildSlotSection(battleAction);
            battleActionSection.TrackSerializedObjectValue(battleActionObject, _ => RefreshSlots());
            RefreshSlots();
            battleActionChanged?.Invoke();
        }

        private void BuildBattleActionHeader(BattleAction battleAction)
        {
            VisualElement nameHeader = BattleActionWindowStyles.CreateSectionHeader(battleAction.name, out Label nameLabel);
            nameLabel.tooltip = _copyPasteTooltip;
            nameHeader.Add(BattleActionWindowStyles.CreateDimLabel(_battleActionKindText));
            nameHeader.Add(BattleActionWindowStyles.CreatePingButton(battleAction));
            AddClipboardMenu(nameHeader, battleAction, BattleActionCopyKind.All);
            battleActionSection.Add(nameHeader);

            VisualElement fieldsCard = BattleActionWindowStyles.CreateCard();
            fieldsCard.Add(BattleActionWindowStyles.CreateFields(battleActionObject, _skippedBattleActionFields));
            battleActionSection.Add(fieldsCard);
        }

        private void BuildTargetingSection(BattleAction battleAction)
        {
            VisualElement header = BattleActionWindowStyles.CreateSectionHeader(_targetingText, out Label headerLabel);
            headerLabel.tooltip = _copyPasteTooltip;
            AddClipboardMenu(header, battleAction, BattleActionCopyKind.Targeting);
            battleActionSection.Add(header);

            targetingCard = BattleActionWindowStyles.CreateCard();
            SerializedProperty targetingProperty = battleActionObject.FindProperty(BattleAction.targetingStrategyField);
            var targetingField = new PropertyField(targetingProperty);
            targetingField.BindProperty(targetingProperty);
            var targetingDetails = new VisualElement { style = { paddingLeft = 12f } };
            targetingCard.Add(targetingField);
            targetingCard.Add(targetingDetails);
            targetingCard.TrackPropertyValue(targetingProperty, property => RefreshTargetingDetails(targetingDetails, property));
            RefreshTargetingDetails(targetingDetails, targetingProperty);
            battleActionSection.Add(targetingCard);
        }

        private void BuildSlotSection(BattleAction battleAction)
        {
            VisualElement header = BattleActionWindowStyles.CreateSectionHeader(_effectStrategiesText, out Label headerLabel);
            headerLabel.tooltip = _copyPasteTooltip;
            AddClipboardMenu(header, battleAction, BattleActionCopyKind.EffectStrategies);
            battleActionSection.Add(header);

            slotList = new ListView(slotIndices, BattleActionWindowStyles.rowHeight, () => new EffectStrategySlotRow(DeleteSlot), BindSlot)
            {
                selectionType = SelectionType.Multiple,
                reorderable = true,
                reorderMode = ListViewReorderMode.Animated,
                showBorder = true,
                virtualizationMethod = CollectionVirtualizationMethod.FixedHeight,
                unbindItem = (element, _) => ((EffectStrategySlotRow)element).UnbindRow()
            };
            slotList.itemIndexChanged += MoveSlot;
            slotList.selectedIndicesChanged += _ => slotsChanged?.Invoke();
            battleActionSection.Add(slotList);

            var buttons = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.FlexEnd } };
            buttons.Add(new Button(() => AddSlot(null)) { text = _addSlotText, tooltip = _addSlotTooltip, style = { width = BattleActionWindowStyles.smallButtonWidth } });
            battleActionSection.Add(buttons);
        }

        private void BindSlot(VisualElement element, int rowIndex)
        {
            if (slotsProperty == null || rowIndex >= slotsProperty.arraySize) { return; }
            ((EffectStrategySlotRow)element).Bind(slotsProperty.GetArrayElementAtIndex(rowIndex), rowIndex);
        }

        private void AddClipboardMenu(VisualElement target, BattleAction battleAction, BattleActionCopyKind kind)
        {
            target.AddManipulator(new ContextualMenuManipulator(populateEvent => BattleActionClipboard.AppendActions(populateEvent.menu, battleAction, kind, Refresh)));
        }
        #endregion

        #region HeaderUtility
        private SerializedProperty GetBattleActionProperty() => ownerObject.FindProperty(ownerObject.targetObject is Skill ? Skill.battleActionField : ActionItem.battleActionField);

        private void RefreshNewBattleActionButton()
        {
            if (newBattleActionButton == null || ownerObject?.targetObject == null) { return; }
            bool isBattleActionSet = BattleActionAssets.TryFindFromOwner(ownerObject.targetObject, out BattleAction battleAction, out string _) && GetBattleActionProperty().objectReferenceValue == battleAction;
            newBattleActionButton.SetEnabled(!isBattleActionSet);
            newBattleActionButton.tooltip = battleAction == null
                ? string.Format(_createBattleActionTooltipFormat, BattleActionAssets.GetBattleActionPath(ownerObject.targetObject))
                : isBattleActionSet ? _battleActionSetTooltip : string.Format(_assignBattleActionTooltipFormat, battleAction.name, kindLabel.text.ToLowerInvariant());
        }

        private void MakeNewBattleAction()
        {
            // Note:  The battle action field's tracking rebuilds the section
            UnityEngine.Object owner = ownerObject?.targetObject;
            if (owner == null) { return; }
            BattleAction battleAction = BattleActionAssets.FindOrCreateBattleAction(owner);
            if (battleAction == null) { return; }

            ownerObject.Update();
            SerializedProperty battleActionProperty = GetBattleActionProperty();
            if (battleActionProperty.objectReferenceValue == battleAction) { return; }
            battleActionProperty.objectReferenceValue = battleAction;
            ownerObject.ApplyModifiedProperties();
            Undo.SetCurrentGroupName(_makeNewUndoName);
            RefreshNewBattleActionButton();
        }
        #endregion

        #region TargetingUtility
        private void RefreshTargetingDetails(VisualElement targetingDetails, SerializedProperty targetingProperty)
        {
            targetingDetails.Clear();
            var targetingStrategy = targetingProperty.objectReferenceValue as TargetingStrategy;
            BattleActionWindowStyles.SetMissing(targetingCard, targetingStrategy == null);
            if (targetingStrategy == null) { targetingDetails.Add(BattleActionWindowStyles.CreateHint(_noTargetingHint)); return; }
            
            targetingDetails.Add(BattleActionWindowStyles.CreateDimLabel(EffectStrategyCatalog.GetTypeLabel(targetingStrategy.GetType())));
            targetingDetails.Add(BattleActionWindowStyles.CreateFields(new SerializedObject(targetingStrategy), _noSkippedFields));
        }
        #endregion

        #region EffectStrategySlotUtility
        private void RefreshSlots()
        {
            if (slotList == null || slotsProperty == null) { return; }
            if (slotIndices.Count != slotsProperty.arraySize)
            {
                slotIndices.Clear();
                slotIndices.AddRange(Enumerable.Range(0, slotsProperty.arraySize));
                slotList.style.height = Mathf.Clamp(slotIndices.Count, 1, _maxVisibleSlots) * BattleActionWindowStyles.rowHeight + 2f;
                slotList.SetSelectionWithoutNotify(slotList.selectedIndices.Where(index => index < slotIndices.Count).ToList());
                slotList.RefreshItems();
            }
            slotsChanged?.Invoke();
        }

        private void AddSlot(EffectStrategy strategy)
        {
            battleActionObject.Update();
            int index = slotsProperty.arraySize;
            slotsProperty.arraySize++;
            slotsProperty.GetArrayElementAtIndex(index).objectReferenceValue = strategy;
            battleActionObject.ApplyModifiedProperties();
            Undo.SetCurrentGroupName(_addSlotUndoName);
            RefreshSlots();
            slotList.SetSelection(index);
            slotList.ScrollToItem(index);
        }
        
        private void MoveSlot(int fromIndex, int toIndex)
        {
            // Note:  The list has already reordered its indices ~ array follows, then the indices are reset to match
            battleActionObject.Update();
            slotsProperty.MoveArrayElement(fromIndex, toIndex);
            battleActionObject.ApplyModifiedProperties();
            Undo.SetCurrentGroupName(_moveSlotUndoName);
            for (int i = 0; i < slotIndices.Count; i++) { slotIndices[i] = i; }
            slotList.RefreshItems();
            slotsChanged?.Invoke();
        }

        private void DeleteSlot(int index)
        {
            battleActionObject.Update();
            if (index < 0 || index >= slotsProperty.arraySize) { return; }

            List<int> selection = slotList.selectedIndices.Where(selected => selected != index).Select(selected => selected > index ? selected - 1 : selected).ToList(); // stash && shift indices
            
            int arraySize = slotsProperty.arraySize;
            slotsProperty.DeleteArrayElementAtIndex(index);
            if (slotsProperty.arraySize == arraySize) { slotsProperty.DeleteArrayElementAtIndex(index); } // Older serialization nulls a set reference first
            battleActionObject.ApplyModifiedProperties();
            Undo.SetCurrentGroupName(_deleteSlotUndoName);
            RefreshSlots();
            
            slotList.SetSelection(selection);
        }
        #endregion
    }
}

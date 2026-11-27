using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using LowDefMustard.Utils.Editor;

namespace Frankie.Combat.Editor
{
    public class SkillPoolView : VisualElement
    {
        // Const Tunables
        private const string _headerText = "Skill Pool";
        private const string _addTooltip = "Add an entry";
        private const string _removeTooltip = "Remove this entry";
        private const float _weightWidth = 130f;
        internal const float cardWidth = 380f;

        // Classes
        internal const string entryRowClass = "skill-pool-view__entry";
        internal const string addButtonClass = "skill-pool-view__add-button";
        internal const string removeButtonClass = "skill-pool-view__remove-button";

        // State
        private readonly SerializedObject serializedObject;
        private readonly Func<string, Skill> resolveSkill;
        private readonly Action onSkillsChanged;
        private readonly Action<TextField> attachSkillAutocomplete;

        // UI State
        private readonly VisualElement body;
        private int shownEntryCount = -1;

        // Constructor
        public SkillPoolView(SkillPool skillPool, Func<string, Skill> resolveSkill, Action onSkillsChanged, Action<TextField> attachSkillAutocomplete)
        {
            this.resolveSkill = resolveSkill;
            this.onSkillsChanged = onSkillsChanged;
            this.attachSkillAutocomplete = attachSkillAutocomplete;
            if (skillPool == null) { return; }

            name = $"skill-pool-{skillPool.name}";
            serializedObject = new SerializedObject(skillPool);
            style.position = Position.Absolute;
            style.left = 0f;
            style.top = 0f;
            style.width = cardWidth;
            SkillNodeStyle.ApplyCardStyle(this, true);

            Add(SkillNodeStyle.MakeHeader(_headerText, true));
            body = SkillNodeStyle.MakeBody();
            Add(body);
            Add(MakeAddRow());

            RebuildEntries();
            this.TrackSerializedObjectValue(serializedObject, _ => OnPoolChanged()); // e.g. edits in the inspector
        }

        #region UIBuilders
        private VisualElement MakeEntryRow(SerializedProperty entryProperty, int index)
        {
            VisualElement row = SkillNodeStyle.MakeRow();
            row.AddToClassList(entryRowClass);

            TextField skillField = SkillNodeStyle.MakeSkillField(GetSkill(entryProperty.FindPropertyRelative(SkillProbabilityPair.skillReferenceField)));
            attachSkillAutocomplete?.Invoke(skillField);
            SkillNodeStyle.AddSkillDropTarget(skillField, IsAssignable);
            skillField.RegisterValueChangedCallback(changeEvent => OnSkillNameChanged(skillField, index, changeEvent.newValue));
            row.Add(skillField);

            SerializedProperty weightProperty = entryProperty.FindPropertyRelative(SkillProbabilityPair.probabilityWeightField);
            row.Add(new ProbabilityWeightField(weightProperty, null, isEmptyValueSkipped: true) { style = { width = _weightWidth, flexShrink = 0f, marginLeft = 2f } });

            Button removeButton = SkillNodeStyle.MakeSmallButton("-", true);
            removeButton.tooltip = _removeTooltip;
            removeButton.AddToClassList(removeButtonClass);
            removeButton.clicked += () => RemoveEntry(index);
            row.Add(removeButton);
            return row;
        }

        private VisualElement MakeAddRow()
        {
            VisualElement row = SkillNodeStyle.MakeRow();
            row.style.paddingLeft = SkillNodeStyle.nodePadding;
            row.style.paddingRight = SkillNodeStyle.nodePadding;
            row.Add(SkillNodeStyle.MakeFlexibleSpacer());

            Button addButton = SkillNodeStyle.MakeSmallButton("+", true);
            addButton.tooltip = _addTooltip;
            addButton.AddToClassList(addButtonClass);
            addButton.clicked += AddEntry;
            row.Add(addButton);
            return row;
        }
        #endregion
        
        #region Rebuilds
        private void RebuildEntries()
        {
            body.Clear();
            serializedObject.Update();
            SerializedProperty entriesProperty = GetEntriesProperty();
            shownEntryCount = entriesProperty.arraySize;
            for (int i = 0; i < shownEntryCount; i++) { body.Add(MakeEntryRow(entriesProperty.GetArrayElementAtIndex(i), i)); }
        }

        private void OnPoolChanged()
        {
            serializedObject.Update();
            if (GetEntriesProperty().arraySize != shownEntryCount) { RebuildEntries(); onSkillsChanged?.Invoke(); return; }

            // Same entries ~ only skill names can be stale (weights are bound)
            for (int i = 0; i < shownEntryCount; i++)
            {
                var skillField = body[i].Q<TextField>();
                skillField?.SetValueWithoutNotify(GetShownSkillName(GetSkillProperty(i)));
            }
            onSkillsChanged?.Invoke();
        }

        private void OnSkillNameChanged(TextField skillField, int index, string newName)
        {
            if (!SkillNodeStyle.TryResolveTypedSkill(newName, resolveSkill, out Skill skill)) { skillField.SetValueWithoutNotify(GetShownSkillName(GetSkillProperty(index))); return; }
            SetSkill(index, skill);
        }
        #endregion
        
        #region Getters
        private static Skill GetSkill(SerializedProperty skillProperty) => Skill.GetSkillFromName(skillProperty.stringValue);
        private static string GetShownSkillName(SerializedProperty skillProperty) => SkillNodeStyle.GetSkillName(GetSkill(skillProperty)); // Stored name, round trip to ensure blanked if it no longer resolves
        private bool IsAssignable(Skill skill) => SkillNodeStyle.IsAssignableSkill(skill, resolveSkill);
        private SerializedProperty GetEntriesProperty() => serializedObject.FindProperty(SkillPool.skillProbabilityPairsField);
        private SerializedProperty GetSkillProperty(int index) => GetEntriesProperty().GetArrayElementAtIndex(index).FindPropertyRelative(SkillProbabilityPair.skillReferenceField);
        #endregion

        #region Setters
        private void SetSkill(int index, Skill skill)
        {
            serializedObject.Update();
            if (index >= GetEntriesProperty().arraySize) { return; }
            GetSkillProperty(index).stringValue = SkillNodeStyle.GetSkillName(skill);
            serializedObject.ApplyModifiedProperties();
            OnPoolChanged();
        }

        private void AddEntry()
        {
            serializedObject.Update();
            SerializedProperty entriesProperty = GetEntriesProperty();
            int index = entriesProperty.arraySize;
            entriesProperty.arraySize = index + 1; // Copies the last entry
            SerializedProperty newEntry = entriesProperty.GetArrayElementAtIndex(index);
            newEntry.FindPropertyRelative(SkillProbabilityPair.skillReferenceField).stringValue = string.Empty; // ~ reset to a blank one
            newEntry.FindPropertyRelative(SkillProbabilityPair.probabilityWeightField).intValue = 1;
            serializedObject.ApplyModifiedProperties();
            RebuildEntries();
            onSkillsChanged?.Invoke();
        }

        private void RemoveEntry(int index)
        {
            serializedObject.Update();
            SerializedProperty entriesProperty = GetEntriesProperty();
            if (index >= entriesProperty.arraySize) { return; }
            entriesProperty.DeleteArrayElementAtIndex(index);
            serializedObject.ApplyModifiedProperties();
            RebuildEntries();
            onSkillsChanged?.Invoke();
        }
        #endregion
    }
}

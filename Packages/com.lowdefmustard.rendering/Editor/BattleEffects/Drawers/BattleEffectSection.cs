#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace LowDefMustard.Rendering.Editor
{
    public sealed class BattleEffectSection : Foldout
    {
        // Notes:
        //  - Standard foldout for BattleEffect property fields that only shows valid entries (refreshed on any edit)
        //      - fields are keyed by their path below the owner (a direct child:  its name;  deeper, e.g. "count.x")
        //  - Right-click functionality enabled on header to copy / paste the section's settings (BattleEffectClipboard)

        // State
        private readonly SerializedProperty owner;
        private readonly Func<bool> isEnabled;
        private readonly Action<BattleEffectSection> refreshFields;
        private readonly Dictionary<string, PropertyField> fields = new();

        // Constructors
        public BattleEffectSection(SerializedProperty property, Func<bool> isEnabled = null, Action<BattleEffectSection> refreshFields = null)
            : this(property.displayName, property, GetChildren(property), isEnabled, refreshFields, property)
        {
        }

        public BattleEffectSection(string title, SerializedProperty owner, IEnumerable<SerializedProperty> properties, Func<bool> isEnabled = null, Action<BattleEffectSection> refreshFields = null, SerializedProperty foldProperty = null)
        {
            text = title;
            this.owner = owner;
            this.isEnabled = isEnabled;
            this.refreshFields = refreshFields;
            var sectionProperties = BuildSectionProperties(properties);
            InitializeFoldout(foldProperty);
            Refresh();
            if (isEnabled != null || refreshFields != null) { this.TrackPropertyValue(owner, _ => Refresh()); } // Only its owner's edits can change what it shows (w/o rules:  nothing to refresh)
            BattleEffectClipboard.AddMenu(this.Q<Toggle>(className: toggleUssClassName), title, owner, sectionProperties);
        }

        #region PublicMethods
        public bool IsEnabled() => isEnabled != null && isEnabled();

        public void SetLabel(string fieldName, string label, string labelTooltip = null)
        {
            if (!fields.TryGetValue(fieldName, out PropertyField field)) { return; }
            field.label = label;
            if (labelTooltip != null) { field.tooltip = labelTooltip; }
            Label builtLabel = field.Q<Label>(className: BaseField<int>.labelUssClassName); 
            if (builtLabel != null) { builtLabel.text = label; } // After binding need to update built field's label too
        }

        public void SetShown(string fieldName, bool isShown)
        {
            if (fields.TryGetValue(fieldName, out PropertyField field)) { field.style.display = isShown ? DisplayStyle.Flex : DisplayStyle.None; }
        }

        public void TrackAlso(SerializedProperty property)
        {
            // Used to enable property tracking across sibling sections ~ e.g. Shade dependent on Style
            // Force-refresh section when another property updates
            
            var tracker = new VisualElement();
            Add(tracker);
            tracker.TrackPropertyValue(property, _ => Refresh());
            Refresh();
        }

        public static IEnumerable<SerializedProperty> GetChildren(SerializedProperty property)
        {
            SerializedProperty child = property.Copy();
            SerializedProperty end = property.GetEndProperty();
            for (bool hasChild = child.NextVisible(true); hasChild && !SerializedProperty.EqualContents(child, end); hasChild = child.NextVisible(false))
            {
                yield return child.Copy();
            }
        }
        #endregion

        #region PrivateMethods
        private void Refresh()
        {
            // Note:  an array element's path can outlive the element (e.g. the last item deleted / undone away) until the inspector rebuilds
            if (owner.serializedObject.FindProperty(owner.propertyPath) == null) { return; }
            
            refreshFields?.Invoke(this);
            VisualElement label = this.Q(className: Foldout.textUssClassName) ?? this.Q(className: Toggle.textUssClassName);
            if (label != null) { label.style.color = IsEnabled() ? BattleEffectInspectorColours.enabledLabel : StyleKeyword.Null; }
        }

        private List<SerializedProperty> BuildSectionProperties(IEnumerable<SerializedProperty> properties)
        {
            List<SerializedProperty> sectionProperties = properties.Select(property => property.Copy()).ToList();
            foreach (SerializedProperty property in sectionProperties)
            {
                var field = new PropertyField(property);
                string ownerPrefix = owner.propertyPath + ".";
                fields[property.propertyPath.StartsWith(ownerPrefix) ? property.propertyPath[ownerPrefix.Length..] : property.name] = field;
                Add(field);
            }
            return sectionProperties;
        }
        
        private void InitializeFoldout(SerializedProperty foldProperty)
        {
            bool isOpen = isEnabled?.Invoke() ?? foldProperty?.isExpanded ?? true;
            value = isOpen;
            if (foldProperty != null)
            {
                foldProperty.isExpanded = isOpen;
                this.RegisterValueChangedCallback(changeEvent =>
                {
                    if (changeEvent.target != this) { return; }
                    foldProperty.isExpanded = changeEvent.newValue;
                });
            }
        }
        #endregion
    }
}
#endif

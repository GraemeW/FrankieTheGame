using System;
using System.Globalization;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Frankie.Combat.Editor
{
    public sealed class EffectStrategySlotRow : VisualElement
    {
        // Const Tunables
        private const float _indexWidth = 20f;
        private const string _deleteText = "-";
        private const string _deleteTooltip = "Remove this slot";

        // Element References
        private readonly Label indexLabel;
        private readonly ObjectField strategyField;

        // State
        public int slotIndex { get; private set; } = -1;

        // Constructor
        public EffectStrategySlotRow(Action<int> deleteSlot)
        {
            style.flexDirection = FlexDirection.Row;
            style.alignItems = Align.Center;
            style.flexGrow = 1f;

            indexLabel = new Label { style = { width = _indexWidth, flexShrink = 0f, unityTextAlign = TextAnchor.MiddleCenter } };
            strategyField = new ObjectField { objectType = typeof(EffectStrategy), allowSceneObjects = false, style = { flexGrow = 1f, flexShrink = 1f, marginLeft = 0f } };
            var deleteButton = new Button(() => deleteSlot(slotIndex)) { text = _deleteText, tooltip = _deleteTooltip, style = { width = BattleActionWindowStyles.smallButtonWidth, flexShrink = 0f } };
            Add(indexLabel);
            Add(strategyField);
            Add(deleteButton);

            strategyField.RegisterValueChangedCallback(changeEvent => BattleActionWindowStyles.SetMissing(this, changeEvent.newValue == null));
        }

        #region PublicMethods
        public void Bind(SerializedProperty slotProperty, int setSlotIndex)
        {
            slotIndex = setSlotIndex;
            indexLabel.text = slotIndex.ToString(CultureInfo.InvariantCulture);
            strategyField.BindProperty(slotProperty);
            BattleActionWindowStyles.SetMissing(this, slotProperty.objectReferenceValue == null);
        }

        public void UnbindRow()
        {
            slotIndex = -1;
            strategyField.Unbind();
        }
        #endregion
    }
}

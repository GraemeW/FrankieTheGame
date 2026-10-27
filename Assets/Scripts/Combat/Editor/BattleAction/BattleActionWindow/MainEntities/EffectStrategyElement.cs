using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using LowDefMustard.Utils.Editor;

namespace Frankie.Combat.Editor
{
    public sealed class EffectStrategyElement : VisualElement
    {
        // Const Tunables
        private const float _slotLabelWidth = 26f;
        private const string _emptySlotText = "Empty slot ~ assign an effect strategy (or remove the slot)";
        private static readonly string[] _skippedFields = { EffectStrategy.effectGUIDField };

        // Element References
        private readonly AssetNameLabel nameLabel;

        // Constructor
        public EffectStrategyElement(EffectStrategy strategy, string slotLabel, Action changed)
        {
            VisualElement card = BattleActionWindowStyles.CreateCard();
            Add(card);

            nameLabel = new AssetNameLabel(true);
            nameLabel.SetAsset(strategy);
            VisualElement header = BuildHeader(slotLabel, nameLabel);
            card.Add(header);

            if (strategy == null)
            {
                ShowEmptySlot(card);
                return;
            }
            AddStrategyInfo(header, strategy);
            card.Add(BuildFields(strategy, changed));
        }

        #region PublicMethods
        public void RefreshName() => nameLabel.RefreshName();
        #endregion

        #region GUI
        private static VisualElement BuildHeader(string slotLabel, AssetNameLabel setNameLabel)
        {
            var header = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, minHeight = BattleActionWindowStyles.rowHeight } };
            if (!string.IsNullOrEmpty(slotLabel)) { header.Add(new Label(slotLabel) { style = { width = _slotLabelWidth, flexShrink = 0f, unityFontStyleAndWeight = FontStyle.Bold } }); }
            header.Add(setNameLabel);
            return header;
        }

        private static void ShowEmptySlot(VisualElement card)
        {
            card.Add(BattleActionWindowStyles.CreateHint(_emptySlotText));
            BattleActionWindowStyles.SetMissing(card, true);
        }

        private static void AddStrategyInfo(VisualElement header, EffectStrategy strategy)
        {
            header.Add(BattleActionWindowStyles.CreateDimLabel(EffectStrategyCatalog.GetTypeLabel(strategy.GetType())));
            header.Add(BattleActionWindowStyles.CreatePingButton(strategy));
        }

        private static VisualElement BuildFields(EffectStrategy strategy, Action changed)
        {
            var serializedObject = new SerializedObject(strategy);
            VisualElement fields = BattleActionWindowStyles.CreateFields(serializedObject, _skippedFields);
            if (changed != null) { fields.TrackSerializedObjectValue(serializedObject, _ => changed()); }
            return fields;
        }
        #endregion
    }
}

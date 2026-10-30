using UnityEngine;
using UnityEngine.UIElements;
using LowDefMustard.Utils.Editor;

namespace Frankie.Combat.Editor
{
    public sealed class EffectStrategyBrowserRow : VisualElement
    {
        // Element References
        private readonly Label headerLabel;
        private readonly AssetNameLabel nameLabel;
        private readonly Label summaryLabel;

        // Constructor
        public EffectStrategyBrowserRow()
        {
            style.flexDirection = FlexDirection.Row;
            style.alignItems = Align.Center;
            style.flexGrow = 1f;
            style.paddingRight = BattleActionWindowStyles.columnPadding;

            headerLabel = new Label { style = { flexGrow = 1f, unityTextAlign = TextAnchor.MiddleLeft } };
            BattleActionWindowStyles.ApplyTreeHeader(headerLabel, true);
            nameLabel = new AssetNameLabel(false);
            summaryLabel = BattleActionWindowStyles.CreateDimLabel(string.Empty);
            summaryLabel.style.marginLeft = BattleActionWindowStyles.columnPadding;
            Add(headerLabel);
            Add(nameLabel);
            Add(summaryLabel);
        }

        #region PublicMethods
        public void Bind(EffectStrategyCatalogEntry entry)
        {
            headerLabel.text = entry.isHeader ? entry.summary : string.Empty;
            headerLabel.style.display = entry.isHeader ? DisplayStyle.Flex : DisplayStyle.None;
            nameLabel.style.display = entry.isHeader ? DisplayStyle.None : DisplayStyle.Flex;
            summaryLabel.style.display = entry.isHeader ? DisplayStyle.None : DisplayStyle.Flex;
            nameLabel.SetAsset(entry.strategy);
            summaryLabel.text = entry.isHeader ? string.Empty : entry.summary;
        }
        #endregion
    }
}

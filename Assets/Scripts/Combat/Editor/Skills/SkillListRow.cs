using UnityEditor;
using UnityEngine.UIElements;
using LowDefMustard.Utils.Editor;

namespace Frankie.Combat.Editor
{
    public sealed class SkillListRow : VisualElement
    {
        // Const Tunables
        private const string _inSelectedSkillTreeText = "in tree";
        private const string _viewText = "View";
        private const string _viewTooltip = "Open in the Battle Action Editor";
        private const string _nameTooltip = "Click to show the skill in the project window";

        // Element References
        private readonly Label groupHeaderLabel;
        private readonly Label skillNameLabel;
        private readonly Label inSelectedSkillTreeLabel;
        private readonly Button viewButton;

        // State
        private Skill skill;

        // Constructor
        public SkillListRow()
        {
            style.flexDirection = FlexDirection.Row;
            style.alignItems = Align.Center;
            style.flexGrow = 1f;
            style.paddingRight = 2f;

            groupHeaderLabel = new Label { style = { flexGrow = 1f } };
            groupHeaderLabel.AddToClassList(StandardBrownTheme.groupHeaderClass);

            skillNameLabel = new Label { tooltip = _nameTooltip, style = { flexGrow = 1f, flexShrink = 1f, overflow = Overflow.Hidden, textOverflow = TextOverflow.Ellipsis } };
            skillNameLabel.RegisterCallback<ClickEvent>(_ => { if (skill != null) { EditorGUIUtility.PingObject(skill); } });

            inSelectedSkillTreeLabel = new Label(_inSelectedSkillTreeText) { style = { flexShrink = 0f, marginRight = 4f } };
            inSelectedSkillTreeLabel.AddToClassList(StandardBrownTheme.dimClass);

            viewButton = new Button(() => { if (skill != null) { AssetDatabase.OpenAsset(skill); } }) { text = _viewText, tooltip = _viewTooltip, style = { flexShrink = 0f } };

            Add(groupHeaderLabel);
            Add(skillNameLabel);
            Add(inSelectedSkillTreeLabel);
            Add(viewButton);
        }

        #region PublicMethods
        public void Bind(SkillListEntry entry, bool isInSelectedSkillTree)
        {
            skill = entry.skill;
            bool isSkill = !entry.isHeader;

            groupHeaderLabel.text = isSkill ? string.Empty : entry.text;
            skillNameLabel.text = isSkill ? entry.text : string.Empty;
            SetShown(groupHeaderLabel, !isSkill);
            SetShown(skillNameLabel, isSkill);
            SetShown(inSelectedSkillTreeLabel, isSkill && isInSelectedSkillTree);
            SetShown(viewButton, isSkill);
        }
        #endregion

        #region PrivateMethods
        private static void SetShown(VisualElement element, bool isShown) => element.style.display = isShown ? DisplayStyle.Flex : DisplayStyle.None;
        #endregion
    }
}

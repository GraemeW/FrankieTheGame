using System;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Scripting.LifecycleManagement;
using LowDefMustard.Utils.Editor;

namespace Frankie.Combat.Editor
{
    public static class SkillNodeStyle
    {
        // Shared node card look for the Skill Set Editor's canvas (tree branches / pool card)
        //  - Accented (root branch / pool card) vs standard (child branch)

        // Const Tunables
        private const float _nodeCornerRadius = 6f;
        private const float _accentedNodeBorderWidth = 3f;
        private const float _nodeBorderWidth = 2f;
        private const float _smallButtonSize = 20f;
        private const string _skillFieldPlaceholder = "--unset: type or drag-in skill--";
        internal const float nodePadding = 6f;
        internal const float headerHeight = 30f;
        [NoAutoStaticsCleanup] private static readonly Color _nodeBackgroundColour = Color.burlywood * 0.7f;
        [NoAutoStaticsCleanup] private static readonly Color _nodeBorderColour = Color.blanchedAlmond * 0.6f;
        [NoAutoStaticsCleanup] private static readonly Color _nodeHeaderBackgroundColour = Color.peru * 0.4f;
        [NoAutoStaticsCleanup] private static readonly Color _accentedNodeBackgroundColour = Color.saddleBrown * 0.7f;
        [NoAutoStaticsCleanup] private static readonly Color _accentedNodeBorderColour = Color.goldenRod * 0.6f;
        [NoAutoStaticsCleanup] private static readonly Color _accentedHeaderBackgroundColour = Color.gray1 * 0.4f;

        #region UtilityMethods
        public static string GetSkillName(Skill skill) => skill != null ? skill.name : string.Empty;
        public static bool IsAssignableSkill(Skill skill, Func<string, Skill> resolveSkill) => skill != null && ResolveAssignableSkill(skill.name, resolveSkill) == skill; // Drops go through the name field ~ only skills whose name resolves back to them
        
        public static bool TryResolveTypedSkill(string typedName, Func<string, Skill> resolveSkill, out Skill skill)
        {
            // Blank ~ resolves to no skill (clear);  unknown name ~ refused, so the caller keeps the current skill rather than lose it to a typo
            bool isBlank = string.IsNullOrWhiteSpace(typedName);
            skill = isBlank ? null : ResolveAssignableSkill(typedName.Trim(), resolveSkill);
            return isBlank || skill != null;
        }
        
        private static Skill ResolveAssignableSkill(string skillName, Func<string, Skill> resolveSkill)
        {
            // Skill sets hold names ~ the runtime name lookup has to resolve back to the same skill (e.g. addressable)
            Skill skill = resolveSkill?.Invoke(skillName);
            return skill != null && Skill.GetSkillFromName(skill.name) == skill ? skill : null;
        }
        
        public static void AddSkillDropTarget(TextField skillField, Func<Skill, bool> isAssignable)
        {
            // Note:  drop writes the skill's name ~ lands through the field's own change handling (same path as typing)
            skillField.AddManipulator(new StandardObjectDropManipulator<Skill>(isAssignable, droppedSkill => skillField.value = droppedSkill.name));
        }
        #endregion
        
        #region UIBuilders
        public static void ApplyCardStyle(VisualElement card, bool isAccented)
        {
            card.style.backgroundColor = isAccented ? _accentedNodeBackgroundColour : _nodeBackgroundColour;

            Color borderColor = isAccented ? _accentedNodeBorderColour : _nodeBorderColour;
            float borderWidth = isAccented ? _accentedNodeBorderWidth : _nodeBorderWidth;
            card.style.borderTopColor = borderColor;
            card.style.borderBottomColor = borderColor;
            card.style.borderLeftColor = borderColor;
            card.style.borderRightColor = borderColor;
            card.style.borderTopWidth = borderWidth;
            card.style.borderBottomWidth = borderWidth;
            card.style.borderLeftWidth = borderWidth;
            card.style.borderRightWidth = borderWidth;

            card.style.borderTopLeftRadius = _nodeCornerRadius;
            card.style.borderTopRightRadius = _nodeCornerRadius;
            card.style.borderBottomLeftRadius = _nodeCornerRadius;
            card.style.borderBottomRightRadius = _nodeCornerRadius;

            card.style.paddingBottom = nodePadding;
        }

        public static VisualElement MakeHeader(string label, bool isAccented)
        {
            var header = new VisualElement
            {
                style =
                {
                    backgroundColor = isAccented ? _accentedHeaderBackgroundColour : _nodeHeaderBackgroundColour,
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    paddingLeft = nodePadding,
                    paddingRight = nodePadding,
                    marginBottom = 4f,
                    borderTopLeftRadius = _nodeCornerRadius - 2f,
                    borderTopRightRadius = _nodeCornerRadius - 2f,
                    height = headerHeight,
                    flexShrink = 0f
                }
            };
            header.Add(new Label(label));
            return header;
        }

        public static VisualElement MakeBody()
        {
            return new VisualElement
            {
                style =
                {
                    flexGrow = 1,
                    paddingLeft = nodePadding,
                    paddingRight = nodePadding
                }
            };
        }

        public static VisualElement MakeRow()
        {
            return new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    marginBottom = 2f
                }
            };
        }
        
        public static TextField MakeSkillField(Skill currentSkill)
        {
            var skillField = new TextField
            {
                value = currentSkill != null ? currentSkill.name : string.Empty,
                isDelayed = true,
                style = { flexGrow = 1f, flexShrink = 1f, flexBasis = 0f, minWidth = 0f },
                textEdition =
                {
                    placeholder = _skillFieldPlaceholder,
                    hidePlaceholderOnFocus = true
                }
            };

            VisualElement input = skillField.Q(className: TextField.inputUssClassName);
            if (input != null)
            {
                input.style.minWidth = 0f;
                input.style.overflow = Overflow.Hidden;
            }
            return skillField;
        }

        public static Button MakeSmallButton(string text, bool shouldDisplay)
        {
            return new Button
            {
                text = text,
                style =
                {
                    width = _smallButtonSize,
                    marginLeft = 2f,
                    flexShrink = 0f,
                    display = shouldDisplay ? DisplayStyle.Flex : DisplayStyle.None
                }
            };
        }

        public static VisualElement MakeFlexibleSpacer() => new() { style = { flexGrow = 1 } };
        #endregion
    }
}

#if UNITY_EDITOR
using UnityEngine;

namespace LowDefMustard.Rendering.Editor
{
    public static class BattleEffectEntryColours
    {
        // Const Tunables
        public static readonly Color item = Color.cornflowerBlue;
        public static readonly Color activeItem = Color.lightSkyBlue;
        public static readonly Color instance = Color.mediumPurple;
        public static readonly Color activeInstance = Color.plum;
        public static readonly Color hidden = Color.gray3;
        public static readonly Color activeHidden = Color.gray5;
        public static readonly Color overall = Color.sienna;
        public static readonly Color activeOverall = Color.darkGoldenRod;

        // Inspector list row backgrounds
        private const float _rowAlpha = 0.25f;
        private const float _hoveredRowAlpha = 0.4f;
        private const float _selectedRowAlpha = 0.5f;
        private const float _hoveredSelectedRowAlpha = 0.65f;
        private const float _panelAlpha = 0.12f;

        public static Color GetRowColour(bool isInstance, bool isSelected, bool isHovered)
        {
            Color colour = isSelected ? (isInstance ? activeInstance : activeItem) : (isInstance ? instance : item);
            colour.a = isSelected ? (isHovered ? _hoveredSelectedRowAlpha : _selectedRowAlpha) : (isHovered ? _hoveredRowAlpha : _rowAlpha);
            return colour;
        }

        public static Color GetPanelColour(bool isInstance)
        {
            Color colour = isInstance ? instance : item;
            colour.a = _panelAlpha;
            return colour;
        }
    }
}
#endif

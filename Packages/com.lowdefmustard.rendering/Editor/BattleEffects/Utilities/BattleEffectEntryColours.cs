#if UNITY_EDITOR
using UnityEngine;

namespace LowDefMustard.Rendering.Editor
{
    public static class BattleEffectEntryColours
    {
        // Const Tunables
        public static readonly Color item = new(0.35f, 0.55f, 0.85f, 1f);
        public static readonly Color activeItem = new(0.55f, 0.8f, 1f, 1f);
        public static readonly Color instance = new(0.55f, 0.4f, 0.75f, 1f);
        public static readonly Color activeInstance = new(0.8f, 0.6f, 1f, 1f);

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

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace LowDefMustard.Rendering.Editor
{
    public static class BattleEffectInspectorColours
    {
        public static Color enabledLabel { get; } = new(0.45f, 0.85f, 0.45f, 1f);
        private static Color changingInputDark { get; } =  new(0.16f, 0.33f, 0.16f, 1f);
        private static Color changingInputLight { get; } = new(0.66f, 0.86f, 0.66f, 1f);

        public static Color changingInput => EditorGUIUtility.isProSkin ? changingInputDark : changingInputLight;
    }
}
#endif

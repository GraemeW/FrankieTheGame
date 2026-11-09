using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.UIElements;
using LowDefMustard.Utils.Editor;

namespace Frankie.Speech.Editor
{
    internal sealed class DialogueGraphStyleSheets : ScriptableObject
    {
        // Holds the dialogue graph window's stylesheets as this script's default references (no path strings)
        private const string _themeStyleSheetField = nameof(themeStyleSheet);
        private const string _graphStyleSheetField = nameof(graphStyleSheet);

        [SerializeField] private StyleSheet themeStyleSheet;
        [SerializeField] private StyleSheet graphStyleSheet;

        [NoAutoStaticsCleanup] private static DialogueGraphStyleSheets _cachedInstance;

        public static DialogueGraphStyleSheets instance
        {
            get
            {
                if (_cachedInstance == null)
                {
                    _cachedInstance = CreateInstance<DialogueGraphStyleSheets>();
                    _cachedInstance.hideFlags = HideFlags.HideAndDontSave;
                }
                return _cachedInstance;
            }
        }

        public StyleSheet GetThemeStyleSheet() => themeStyleSheet != null ? themeStyleSheet : StandardBrownTheme.GetDefaultReference(this, _themeStyleSheetField);
        public StyleSheet GetGraphStyleSheet() => graphStyleSheet != null ? graphStyleSheet : StandardBrownTheme.GetDefaultReference(this, _graphStyleSheetField);
    }
}

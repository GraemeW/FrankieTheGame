using System;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace LowDefMustard.Core
{
    public static class LDMLog
    {
        // Notes:
        //  - Warning / Error / Exception always emit ~ the category is a tag, not a filter
        //  - No settings asset = every category enabled, no colour
        //  - Message is always the string parameter straight after category
        //      - plz respect && do not deviate: this way C# 10+ interpolated string handler overload can slot in later with no call-site changes (see README)
        //  - [HideInCallstack] needed to keep console double-click on the caller (console menu > Strip logging callstack)
        //  - Internal methods/properties are for test visibility

        // Const Tunables
        internal const string editorConfigKey = "com.lowdefmustard.core.logsettings";

        // State
        private static LDMLogSettings _settings;

        #region Initialization
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        private static void InitializeForEditor() => Reload();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void InitializeForPlayMode() => Reload();

        internal static void Reload()
        {
            UnityEditor.EditorBuildSettings.TryGetConfigObject(editorConfigKey, out _settings);
            if (_settings != null) { _settings.ApplyStackTraceSettings(); }
        }
#endif

        internal static void HandleSettingsEnabled(LDMLogSettings enabledSettings)
        {
#if !UNITY_EDITOR
            if (settings != null) { return; }
            settings = enabledSettings;
            settings.ApplyStackTraceSettings();
#endif
        }

        internal static void HandleSettingsChanged(LDMLogSettings changedSettings)
        {
            if (_settings == null || _settings != changedSettings) { return; }
            _settings.ApplyStackTraceSettings();
        }

        internal static void UseSettings(LDMLogSettings overrideSettings) => _settings = overrideSettings;
        #endregion

        #region PublicMethods
        // Guard for expensive message building
        public static bool IsEnabled(LDMLogCategory category) => _settings == null ? category != LDMLogCategory.None : _settings.IsEnabled(category);

        [HideInCallstack, Conditional("UNITY_EDITOR"), Conditional("UNITY_ENABLE_CHECKS")]
        public static void Info(LDMLogCategory category, string message)
        {
            if (IsEnabled(category)) { Debug.Log(Format(category, message)); }
        }

        [HideInCallstack, Conditional("UNITY_EDITOR"), Conditional("UNITY_ENABLE_CHECKS")]
        public static void Info(LDMLogCategory category, string message, Object context)
        {
            if (IsEnabled(category)) { Debug.Log(Format(category, message), context); }
        }

        [HideInCallstack]
        public static void Important(LDMLogCategory category, string message)
        {
            if (IsEnabled(category)) { Debug.Log(Format(category, message)); }
        }

        [HideInCallstack]
        public static void Important(LDMLogCategory category, string message, Object context)
        {
            if (IsEnabled(category)) { Debug.Log(Format(category, message), context); }
        }

        [HideInCallstack]
        public static void Warning(LDMLogCategory category, string message) => Debug.LogWarning(Format(category, message));

        [HideInCallstack]
        public static void Warning(LDMLogCategory category, string message, Object context) => Debug.LogWarning(Format(category, message), context);

        [HideInCallstack]
        public static void Error(LDMLogCategory category, string message) => Debug.LogError(Format(category, message));

        [HideInCallstack]
        public static void Error(LDMLogCategory category, string message, Object context) => Debug.LogError(Format(category, message), context);

        [HideInCallstack]
        public static void Exception(Exception exception) => Debug.LogException(exception);

        [HideInCallstack]
        public static void Exception(Exception exception, Object context) => Debug.LogException(exception, context);
        #endregion

        #region PrivateMethods
        private static string Format(LDMLogCategory category, string message)
        {
#if UNITY_EDITOR
            if (_settings != null) { return $"<color=#{_settings.GetColorHex(category)}>[{category}]</color> {message}"; }
#endif
            return $"[{category}] {message}";
        }
        #endregion
    }
}

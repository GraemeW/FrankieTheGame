using System;
using System.Collections.Generic;
using UnityEngine;

namespace LowDefMustard.Core
{
    public sealed class LDMLogSettings : ScriptableObject
    {
        // Notes:
        //  - LDMLog uses the settings SO registered via Tools/Low Def Mustard/Log Settings (LDMLogSettingsMenu)
        //  - Internal methods/properties are for test visibility

        // Tunables
        [SerializeField] private List<LDMLogCategoryEntry> categoryEntries = new();
        [SerializeField][Tooltip("Shared by LDMLog.Info + LDMLog.Important")] private StackTraceLogType logStackTrace = StackTraceLogType.None;
        [SerializeField] private StackTraceLogType warningStackTrace = StackTraceLogType.ScriptOnly;
        [SerializeField] private StackTraceLogType errorStackTrace = StackTraceLogType.ScriptOnly;
        [SerializeField] private StackTraceLogType exceptionStackTrace = StackTraceLogType.ScriptOnly;

        // Editor Field References
        public const string categoryEntriesField = nameof(categoryEntries);
        public const string logStackTraceField = nameof(logStackTrace);
        public const string warningStackTraceField = nameof(warningStackTrace);
        public const string errorStackTraceField = nameof(errorStackTrace);
        public const string exceptionStackTraceField = nameof(exceptionStackTrace);

        // Const Tunables
        private const float _defaultColorSaturation = 0.45f;

        // State
        [NonSerialized] private LDMLogCategory enabledMask;
        [NonSerialized] private string[] colorHexes = Array.Empty<string>();
        [NonSerialized] private bool isCacheBuilt;

        #region UnityMethods
        private void OnEnable()
        {
            SyncEntriesToEnum();
            RebuildCache();
            LDMLog.HandleSettingsEnabled(this);
        }

        private void OnValidate()
        {
            SyncEntriesToEnum();
            RebuildCache();
            LDMLog.HandleSettingsChanged(this);
        }
        #endregion

        #region PublicMethods
        public bool IsEnabled(LDMLogCategory category)
        {
            EnsureCache();
            return (enabledMask & category) != 0;
        }

        // First tagged category with an entry wins, in declaration order (no blending)
        public string GetColorHex(LDMLogCategory category)
        {
            EnsureCache();
            for (int i = 0; i < categoryEntries.Count; i++)
            {
                if ((category & categoryEntries[i].category) != 0) { return colorHexes[i]; }
            }
            return "FFFFFF";
        }

        public void ApplyStackTraceSettings()
        {
            Application.SetStackTraceLogType(LogType.Log, logStackTrace);
            Application.SetStackTraceLogType(LogType.Warning, warningStackTrace);
            Application.SetStackTraceLogType(LogType.Error, errorStackTrace);
            Application.SetStackTraceLogType(LogType.Assert, errorStackTrace);
            Application.SetStackTraceLogType(LogType.Exception, exceptionStackTrace);
        }
        #endregion

        #region InternalMethods
        internal IReadOnlyList<LDMLogCategoryEntry> GetCategoryEntries() => categoryEntries;

        internal void SetCategoryEnabled(LDMLogCategory category, bool enabled)
        {
            foreach (LDMLogCategoryEntry entry in categoryEntries)
            {
                if ((category & entry.category) != 0) { entry.enabled = enabled; }
            }
            RebuildCache();
        }

        internal static IReadOnlyList<LDMLogCategory> GetSingleBitCategories()
        {
            var singleBitCategories = new List<LDMLogCategory>();
            foreach (LDMLogCategory category in (LDMLogCategory[])Enum.GetValues(typeof(LDMLogCategory)))
            {
                int raw = (int)category;
                if (raw != 0 && (raw & (raw - 1)) == 0) { singleBitCategories.Add(category); }
            }
            return singleBitCategories;
        }
        
        internal void SyncEntriesToEnum()
        {
            // Add missing categories, drop stale + duplicate entries, sort by bit
            IReadOnlyList<LDMLogCategory> singleBitCategories = GetSingleBitCategories();
            var validCategories = new HashSet<LDMLogCategory>(singleBitCategories);
            var seenCategories = new HashSet<LDMLogCategory>();
            categoryEntries.RemoveAll(entry => entry == null || !validCategories.Contains(entry.category) || !seenCategories.Add(entry.category));

            for (int i = 0; i < singleBitCategories.Count; i++)
            {
                LDMLogCategory category = singleBitCategories[i];
                if (seenCategories.Contains(category)) { continue; }

                Color defaultColor = Color.HSVToRGB((float)i / singleBitCategories.Count, _defaultColorSaturation, 1f);
                categoryEntries.Add(new LDMLogCategoryEntry { category = category, enabled = true, color = defaultColor });
            }
            categoryEntries.Sort((a, b) => ((int)a.category).CompareTo((int)b.category));
            isCacheBuilt = false;
        }
        #endregion

        #region PrivateMethods
        private void EnsureCache() { if (!isCacheBuilt || colorHexes.Length != categoryEntries.Count) { RebuildCache(); } }

        private void RebuildCache()
        {
            enabledMask = LDMLogCategory.None;
            colorHexes = new string[categoryEntries.Count];
            for (int i = 0; i < categoryEntries.Count; i++)
            {
                LDMLogCategoryEntry entry = categoryEntries[i];
                if (entry.enabled) { enabledMask |= entry.category; }
                colorHexes[i] = ColorUtility.ToHtmlStringRGB(entry.color);
            }
            isCacheBuilt = true;
        }
        #endregion
    }
}

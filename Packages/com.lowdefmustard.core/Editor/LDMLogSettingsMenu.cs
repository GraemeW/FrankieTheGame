using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace LowDefMustard.Core.Editor
{
    public static class LDMLogSettingsMenu
    {
        // Const Tunables
        internal const string defaultAssetPath = "Assets/Settings/LDMLogSettings.asset";

        [MenuItem("Tools/Low Def Mustard/Log Settings", false, -100)]
        public static void SelectOrCreateSettings()
        {
            LDMLogSettings settings = GetRegisteredSettings();
            if (settings == null) { settings = AssetDatabase.LoadAssetAtPath<LDMLogSettings>(defaultAssetPath); }
            if (settings == null) { settings = CreateSettingsAsset(); }
            Register(settings);

            Selection.activeObject = settings;
            EditorGUIUtility.PingObject(settings);
        }

        internal static LDMLogSettings GetRegisteredSettings()
        {
            EditorBuildSettings.TryGetConfigObject(LDMLog.editorConfigKey, out LDMLogSettings settings);
            return settings;
        }

        internal static bool IsPreloaded(LDMLogSettings settings) => new List<Object>(PlayerSettings.GetPreloadedAssets()).Contains(settings);

        // Editor config object for the editor, preloaded asset for builds ~ any other LDMLogSettings is dropped from the preload list
        internal static void Register(LDMLogSettings settings)
        {
            EditorBuildSettings.AddConfigObject(LDMLog.editorConfigKey, settings, true);

            var preloadedAssets = new List<Object>(PlayerSettings.GetPreloadedAssets());
            preloadedAssets.RemoveAll(asset => asset is LDMLogSettings && asset != settings);
            if (!preloadedAssets.Contains(settings)) { preloadedAssets.Add(settings); }
            PlayerSettings.SetPreloadedAssets(preloadedAssets.ToArray());

            LDMLog.Reload();
        }

        private static LDMLogSettings CreateSettingsAsset()
        {
            string folder = Path.GetDirectoryName(defaultAssetPath)?.Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(folder)) { AssetDatabase.CreateFolder(Path.GetDirectoryName(folder), Path.GetFileName(folder)); }

            var settings = ScriptableObject.CreateInstance<LDMLogSettings>();
            AssetDatabase.CreateAsset(settings, defaultAssetPath);
            AssetDatabase.SaveAssets();
            return settings;
        }
    }
}

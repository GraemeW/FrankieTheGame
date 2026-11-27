using System.Reflection;
using UnityEditor;
using UnityEditor.AddressableAssets.Build;
using UnityEngine.AddressableAssets;
using LowDefMustard.Core;

namespace LowDefMustard.Utils.Editor
{
    [InitializeOnLoad]
    public static class AddressablesContentBuildReinitializer
    {
        // Why?
        //  - Edit-mode content build clears the catalogue of the edit-mode Addressables instance
        //  - w/ domain reload disabled, the next Play mode reuses it:  initialization then throws (no locators) and nothing loads (e.g. stuck on the bootstrap scene)
        //  - Addressables only flags a fresh instance when leaving Play mode, so:  flag it after each content build
        // This reinitializer will exist until Unity fixes this more fundamentally on their side (resetting the flag themselves)

        // Const
        private const string _reinitializeFieldName = "reinitializeAddressables"; // N.B. _reinitializeFieldName is an internal Addressables field ~ warns if a package update renames it

        static AddressablesContentBuildReinitializer()
        {
            BuildScript.buildCompleted += _ => FlagReinitialization();
        }

        private static void FlagReinitialization()
        {
            FieldInfo reinitializeField = typeof(Addressables).GetField(_reinitializeFieldName, BindingFlags.NonPublic | BindingFlags.Static);
            if (reinitializeField == null || reinitializeField.FieldType != typeof(bool))
            {
                LDMLog.Warning(LDMLogCategory.Utilities, $"Addressables has no '{_reinitializeFieldName}' flag (package update?). Enter Play mode twice after a content build to get a fresh Addressables instance.");
                return;
            }
            reinitializeField.SetValue(null, true);
        }
    }
}

using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;

namespace LowDefMustard.Rendering.Editor
{
    public static class GraphicsWarmupFolders
    {
        // Notes:
        //  - Material folders:  scanned (recursively) by ReconcileShaderCollection for materials w/ no GraphicsWarmup entry
        //  - UI Toolkit material folders:  new materials in these are auto-bound as UIToolkitMaterial entries

        // State
        [NoAutoStaticsCleanup] private static readonly List<string> _materialFolders = new();
        [NoAutoStaticsCleanup] private static readonly List<string> _uiToolkitMaterialFolders = new();

        public static void RegisterMaterialFolder(string folder) => AddUnique(_materialFolders, folder);
        public static void RegisterUIToolkitMaterialFolder(string folder) => AddUnique(_uiToolkitMaterialFolders, folder);

        public static IReadOnlyList<string> GetMaterialFolders() => _materialFolders;
        public static IReadOnlyList<string> GetUIToolkitMaterialFolders() => _uiToolkitMaterialFolders;

        private static void AddUnique(List<string> folders, string folder)
        {
            string trimmedFolder = folder?.TrimEnd('/');
            if (string.IsNullOrEmpty(trimmedFolder) || folders.Contains(trimmedFolder)) { return; }
            folders.Add(trimmedFolder);
        }
    }
}

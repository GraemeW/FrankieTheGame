using Unity.Scripting.LifecycleManagement;

namespace LowDefMustard.Saving.Editor
{
    public static class SaveFileManagerProvider
    {
        // Editor-level access to the static, per-project SaveFileManager implementation
        // Note:  Runtime access should go through the SaveFileManager itself
        [NoAutoStaticsCleanup] public static ISaveFileManagerAdapter current { get; set; } = new NullSaveFileManagerAdapter();
    }
}

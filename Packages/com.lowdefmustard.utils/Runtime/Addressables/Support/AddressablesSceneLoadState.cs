using Unity.Scripting.LifecycleManagement;

namespace LowDefMustard.Utils
{
    public static partial class AddressablesSceneLoadState
    {
        // State
        [AutoStaticsCleanup] public static bool isLoadingScene;
    }
}

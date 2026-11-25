namespace LowDefMustard.Utils
{
    public interface IAddressablesCacheLifecycle
    {
        void StartBuild(); // Async, no-op once started
        void BuildIfEmpty(); // Starts + completes synchronously ~ N.B. NEVER call during a scene load
        bool IsReady();
        void Release(); // Releases load handle, keeps lookups
        void Reset(); // Release + drop lookups ~ next lookup rebuilds
    }
}

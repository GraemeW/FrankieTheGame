namespace LowDefMustard.Utils
{
    public interface IAddressablesCache
    {
        // Notes:
        //  - label == owner class name by default ~ nameof()
        //  - on the contract:
        //      - loose contract until static abstract members in Unity7 w/ CoreCLR + >C#11
        //      - meanwhile, AddressablesCacheOwners checks it by reflection + tests enforce it
        
        // Contract Details:
        //  - one public static property typed IAddressablesCacheLifecycle, backed by an [AutoStaticsCleanup] AddressablesLookupCache<T>
        //  - lookups go through the cache (Get / GetAll), which builds on demand
        //  - subclasses share their base type's cache (e.g. KeyItem : InventoryItem) and declare none of their own
        
        
        // Future-looking (w/ Unity CoreCLR)
        //  - make interface generic + declare the member below -> owners implement IAddressablesCache<TSelf> (e.g. Skill : IAddressablesCache<Skill>)
        //      ```
        //      public interface IAddressablesCache<TSelf> where TSelf : IAddressablesCache<TSelf>
        //      {
        //          static abstract IAddressablesCacheLifecycle addressablesCache { get; }
        //      }
        //      ```
        //  - remove AddressablesCacheInvalidatorTests' contract checks (OwnerTypes_OutsideTestAssemblies_EachExposeOneCache, GetCache_* by property count)
        //  - keep AddressablesLoaderTests.GetCaches_CoversEveryCacheOwner ~ i.e. since the compiler can't check the loader's list
    }
}

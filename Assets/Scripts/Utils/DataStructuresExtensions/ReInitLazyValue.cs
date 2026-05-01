namespace Frankie.Utils
{
    public class ReInitLazyValue<T> : LazyValue<T>
    {
        public ReInitLazyValue(InitializerDelegate setInitializer) : base(setInitializer)
        {
            initializer = setInitializer;
        }

        // Call initialization even if _initialized flag is set
        public override bool ForceInit()
        {
            // Access cachedValue directly (otherwise recursion)
            if (base.ForceInit() || cachedValue != null) return false;
            Initialize();
            return true;
        }
    }
}

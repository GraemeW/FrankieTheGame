namespace LowDefMustard.Utils
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
            if (base.ForceInit() || IsCachedValueStillValid()) return false;
            Initialize();
            return true;
        }

        private bool IsCachedValueStillValid()
        {
            // Deliberately uses .Equals() (a virtual instance method, resolved at runtime against the actual type) rather than != (an operator, resolved at compile time against the generic type parameter T)
            // When T is a UnityEngine.Object-derived type, this lets Unity's overridden Equals correctly report a destroyed-but-non-null C# reference ("fake null") as invalid
            // The `cachedValue != null &&` guard short-circuits before calling .Equals() on an actual null reference, which would otherwise throw
            return cachedValue != null && !cachedValue.Equals(null);
        }
    }
}

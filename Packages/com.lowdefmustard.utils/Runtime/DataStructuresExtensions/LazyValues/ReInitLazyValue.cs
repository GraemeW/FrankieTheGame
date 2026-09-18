namespace LowDefMustard.Utils
{
    public class ReInitLazyValue<T>
    {
        // Usage:
        // ReInitLazyValues are appropriate for reference-type variables - access value via TryGetSafely()
        // For primitives / value-type variables, use the standard LazyValue
        
        private bool isInitialized = false;
        private T cachedValue;
        private readonly InitializerDelegate initializer;

        public delegate T InitializerDelegate();
        
        public ReInitLazyValue(InitializerDelegate setInitializer)
        {
            initializer = setInitializer;
        }
        
        // Access is internal for test visibility only - otherwise preventing (potentially unsafe) direct access - see TryGetSafely()
        internal T value
        {
            get
            {
                ForceInit();
                return cachedValue;
            }
            set
            {
                isInitialized = true;
                cachedValue = value;
            }
        }
        
        #region PublicMethods
        public bool IsCachedValueStillValid()
        {
            // Deliberately uses .Equals() (a virtual instance method, resolved at runtime against the actual type) rather than != (an operator, resolved at compile time against the generic type parameter T)
            // When T is a UnityEngine.Object-derived type, this lets Unity's overridden Equals correctly report a destroyed-but-non-null C# reference ("fake null") as invalid
            // The `cachedValue != null &&` guard short-circuits before calling .Equals() on an actual null reference, which would otherwise throw
            return cachedValue != null && !cachedValue.Equals(null);
        }
        
        public bool ForceInit()
        {
            if (BaseForceInit() || IsCachedValueStillValid()) { return false; }
            Initialize();
            return true;
        }
        
        public bool TryGetSafely(out T passValue, bool allowReInit = true)
        {
            // allowReInit = false skips ForceInit() entirely & reads cachedValue directly
            //  - set to false in e.g. OnDisable/OnDestroy (i.e. when triggering a fresh initializer could be unsafe mid-teardown)
            
            passValue = allowReInit ? value : cachedValue;
            return IsCachedValueStillValid();
        }
        #endregion

        #region PrivateMethods
        private void Initialize()
        {
            cachedValue = initializer();
            isInitialized = true;
        }

        private bool BaseForceInit()
        {
            if (isInitialized) { return false; }
            Initialize(); 
            return true;
        }
        #endregion
    }
}

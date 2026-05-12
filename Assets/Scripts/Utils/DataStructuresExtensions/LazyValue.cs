namespace Frankie.Utils
{
    public class LazyValue<T>
    {
        private bool isInitialized = false;
        protected T cachedValue;
        protected InitializerDelegate initializer;

        public delegate T InitializerDelegate();
        
        public LazyValue(InitializerDelegate setInitializer)
        {
            initializer = setInitializer;
        }
        
        public T value
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
        
        public virtual bool ForceInit()
        {
            if (isInitialized) { return false; }
            Initialize(); 
            return true;
        }

        protected void Initialize()
        {
            cachedValue = initializer();
            isInitialized = true;
        }
    }
}

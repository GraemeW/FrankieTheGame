namespace Frankie.Utils
{
    public class ReInitLazyValue<T> : LazyValue<T>
    {
        public ReInitLazyValue(InitializerDelegate initializer) : base(initializer)
        {
            _initializer = initializer;
        }

        public override void ForceInit()
        {
            base.ForceInit();
            if (_value == null) // Access directly (otherwise recursion)
            {
                Initialize();
            }
        }
    }
}
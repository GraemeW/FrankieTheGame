namespace LowDefMustard.Utils
{
    public interface IObjectProbabilityPair<out T>
    {
        public T value { get; }
        public int weight { get; }
    }
}

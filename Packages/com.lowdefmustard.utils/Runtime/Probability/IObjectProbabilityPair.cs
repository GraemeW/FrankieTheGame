namespace LowDefMustard.Utils
{
    public interface IObjectProbabilityPair<T>
    {
        public T GetObject();

        public int GetProbability();
    }
}

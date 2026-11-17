using System.Collections.Generic;
using System.Linq;

namespace LowDefMustard.Utils.Tests
{
    public sealed class TestProbabilityPair<T> : IObjectProbabilityPair<T>
    {
        // Note:  Plain in-memory pair ~ i.e. never serialized && auto-properties are fine here (real implementers need named backing fields)
        
        public TestProbabilityPair(T value, int weight)
        {
            this.value = value;
            this.weight = weight;
        }

        public T value { get; }
        public int weight { get; }

        public static List<IObjectProbabilityPair<T>> ListOf(params (T value, int weight)[] entries)
        {
            return entries.Select(entry => (IObjectProbabilityPair<T>)new TestProbabilityPair<T>(entry.value, entry.weight)).ToList();
        }
    }
}

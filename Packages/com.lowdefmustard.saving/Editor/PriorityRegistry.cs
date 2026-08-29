using System;
using System.Collections.Generic;

namespace LowDefMustard.Saving.Editor
{
    public class PriorityRegistry<T>
    {
        private readonly List<(Func<T, bool> Match, int Priority)> rules = new();
        public void Register(Func<T, bool> match, int priority) => rules.Add((match, priority));
        public void Unregister(Func<T, bool> match) => rules.RemoveAll(rule => rule.Match == match);
        public int GetPriority(T item)
        {
            foreach (var (match, priority) in rules) { if (match(item)) { return priority; } }
            return int.MaxValue;
        }
    }
}

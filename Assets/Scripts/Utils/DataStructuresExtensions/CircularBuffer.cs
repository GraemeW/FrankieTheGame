using System.Collections.Generic;
using System.Linq;

namespace Frankie.Utils
{
    public class CircularBuffer<T>
    {
        private readonly LinkedList<T> queue;
        private readonly int size;

        public CircularBuffer(int size)
        {
            queue = new LinkedList<T>();
            this.size = size;
        }
        
        public T GetFirstEntry() => queue.First();
        public T GetLastEntry() => queue.Last();
        public int GetCurrentSize() => queue.Count;

        public void Add(T obj)
        {
            if (queue.Count == size) { queue.RemoveLast(); }
            queue.AddFirst(obj);
        }

        public T GetEntryAtPosition(int position)
        {
            if (position > queue.Count) { return default(T); }
            return position == 0 ? queue.First() : queue.ElementAt(position);
        }
        
        public void Clear()
        {
            queue.Clear();
        }
    }
}

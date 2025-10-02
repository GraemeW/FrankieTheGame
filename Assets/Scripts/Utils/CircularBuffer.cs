using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Frankie.Utils
{
    public class CircularBuffer<T>
    {
        LinkedList<T> queue;
        int size;

        public CircularBuffer(int size)
        {
            queue = new LinkedList<T>();
            this.size = size;
        }

        public void Add(T obj)
        {
            if (queue.Count == size)
            {
                queue.RemoveLast();
                queue.AddFirst(obj);
            }
            else
            {
                queue.AddFirst(obj);
            }

        }

        public T GetEntryAtPosition(int position)
        {
            if (position > queue.Count) { return default(T); }
            if (position == 0) { return queue.First(); }

            return queue.ElementAt(position);
        }

        public T GetFirstEntry()
        {
            return queue.First();
        }

        public T GetLastEntry()
        {
            return queue.Last();
        }

        public int GetCurrentSize()
        {
            return queue.Count;
        }

        public void Clear()
        {
            queue.Clear();
        }
    }
}

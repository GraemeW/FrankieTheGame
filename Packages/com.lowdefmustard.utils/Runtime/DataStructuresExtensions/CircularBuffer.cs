using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LowDefMustard.Utils
{
    public class CircularBuffer<T> : IEnumerable<T>
    {
        private readonly T[] buffer;
        private readonly int size;
        private readonly int mask; // size - 1; valid because size is a power of two
        private int head;  // index of the oldest entry
        private int tail;  // index of the next slot to write to
        private int count; // number of valid entries currently stored

        #region PublicMethods
        public CircularBuffer(int size)
        {
            if (size <= 0) { size = 2; }
            if ((size & (size - 1)) != 0)
            {
                Debug.Log($"CircularBuffer size must be a power of two. Got {size}.");
                size = CeilToPowerOf2(size);
                Debug.Log($"Updated size to {size}.");
            }

            buffer = new T[size];
            this.size = size;
            mask = size - 1;
            head = 0;
            tail = 0;
            count = 0;
        }

        public int GetCurrentSize() => count;

        public void Add(T obj)
        {
            if (count == size)
            {
                // Full: overwrite oldest slot (== tail when full), advance both.
                buffer[head] = obj;
                head = (head + 1) & mask;
                tail = (tail + 1) & mask;
            }
            else
            {
                buffer[tail] = obj;
                tail = (tail + 1) & mask;
                count++;
            }
        }
        
        public T GetFirstEntry() => count > 0 ? buffer[MostRecentIndex()] : default(T);
        
        public T GetLastEntry() => count > 0 ? buffer[head] : default(T);
        
        public T GetEntryAtPosition(int position)
        {
            if (position >= count) { return default(T); }
            int index = (MostRecentIndex() - position) & mask;
            return buffer[index];
        }

        public void Clear()
        {
            Array.Clear(buffer, 0, count);
            head = 0;
            tail = 0;
            count = 0;
        }
        
        private int MostRecentIndex() => (tail - 1) & mask;
        #endregion
        
        #region IEnumerableAndSpan
        public IEnumerator<T> GetEnumerator()
        {
            int index = MostRecentIndex();
            for (int i = 0; i < count; i++)
            {
                yield return buffer[index];
                index = (index - 1) & mask;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        
        public Span<T> AsSpan()
        {
            // Span over every currently-valid entry, for fast bulk in-place mutation.
            int validLength = count < size ? count : size;
            return buffer.AsSpan(0, validLength);
        }
        #endregion
        
        #region StaticMethods
        private static int CeilToPowerOf2(int size)
        {
            if ((size & (size - 1)) == 0) { return size; }
            switch (size)
            {
                case <= 1:
                    return 2;
                case > 1073741824:
                    return 1073741824; // highest viable power of 2
            }

            size--;
            size |= size >> 1;
            size |= size >> 2;
            size |= size >> 4;
            size |= size >> 8;
            size |= size >> 16;
            size++;

            return size;
        }
        #endregion
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LowDefMustard.Utils
{
    [Serializable]
    public class EnumKeyedCollection<TEnum, TData> : IEnumKeyedCollection, IEnumerable where TEnum : struct, Enum
    {
        [SerializeField] private List<TData> values = new();
        Type IEnumKeyedCollection.GetEnumType() => typeof(TEnum);
        string IEnumKeyedCollection.GetListName() => nameof(values);
        
        public IEnumerator<(TEnum key, TData value)> GetEnumerator()
        {
            var enumValues = (TEnum[])Enum.GetValues(typeof(TEnum));
            for (int i = 0; i < values.Count; i++)
            {
                if (i >= enumValues.Length) { break; } // index safeguard
                yield return (enumValues[i], values[i]);
            }
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

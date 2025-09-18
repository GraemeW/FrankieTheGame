using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Utils
{
    public interface IObjectProbabilityPair<T>
    {
        public T GetObject();

        public int GetProbability();
    }
}

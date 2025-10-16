using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Frankie.Utils
{
    public static class ProbabilityPairOperation<T>
    {
        public static T GetRandomObject(IEnumerable<IObjectProbabilityPair<T>> objectProbabilityPairs)
        {
            var iObjectProbabilityPairs = objectProbabilityPairs.ToList();
            int probabilityDenominator = iObjectProbabilityPairs.Sum(x => x.GetProbability());
            int randomRoll = Random.Range(0, probabilityDenominator);

            int accumulatingProbability = 0;
            foreach (IObjectProbabilityPair<T> objectProbabilityPair in iObjectProbabilityPairs)
            {
                accumulatingProbability += objectProbabilityPair.GetProbability();
                if (randomRoll < accumulatingProbability)
                {
                    return objectProbabilityPair.GetObject();
                }
            }
            return default(T);
        }
    }
}

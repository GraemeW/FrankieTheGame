using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Frankie.Utils
{
    public static class GetObjectFromProbabilityPairList<T>
    {
        public static T GetRandomObject(IObjectProbabilityPair<T>[] objectProbabilityPairs)
        {
            int probabilityDenominator = objectProbabilityPairs.Sum(x => x.GetProbability());
            int randomRoll = Random.Range(0, probabilityDenominator);

            int accumulatingProbability = 0;
            foreach (IObjectProbabilityPair<T> objectProbabilityPair in objectProbabilityPairs)
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

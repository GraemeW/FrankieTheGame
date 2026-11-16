using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LowDefMustard.Utils
{
    public static class ProbabilityPairExtensions
    {
        #region PublicMethods
        public static bool TryGetRandom<T>(this IEnumerable<IObjectProbabilityPair<T>> objectProbabilityPairs, out T result)
        {
            return TryRoll(GetUsablePairs(objectProbabilityPairs), out result);
        }

        public static bool TryGetRandom<T>(this IEnumerable<IObjectProbabilityPair<T>> objectProbabilityPairs, int count, out List<T> results)
        {
            // Without repeats:  once a value is picked, every entry holding it leaves the draw, so repeated entries only raise its odds
            results = new List<T>();
            List<IObjectProbabilityPair<T>> usablePairs = GetUsablePairs(objectProbabilityPairs);
            while (results.Count < count && TryRoll(usablePairs, out T picked))
            {
                results.Add(picked);
                usablePairs.RemoveAll(pair => EqualityComparer<T>.Default.Equals(pair.value, picked));
            }
            return results.Count > 0;
        }
        #endregion

        #region PrivateMethods
        private static List<IObjectProbabilityPair<T>> GetUsablePairs<T>(IEnumerable<IObjectProbabilityPair<T>> objectProbabilityPairs)
        {
            return objectProbabilityPairs != null ? objectProbabilityPairs.Where(IsUsable).ToList() : new List<IObjectProbabilityPair<T>>();
        }

        private static bool IsUsable<T>(IObjectProbabilityPair<T> objectProbabilityPair)
        {
            if (objectProbabilityPair is not { weight: > 0 }) { return false; }
            T value = objectProbabilityPair.value;
            return value != null && !value.Equals(null); // Equals to catch destroyed Unity objects (!= null misses on generic type T)
        }

        private static bool TryRoll<T>(List<IObjectProbabilityPair<T>> usablePairs, out T result)
        {
            result = default;
            if (usablePairs.Count == 0) { return false; }

            int totalWeight = usablePairs.Sum(pair => pair.weight);
            int randomRoll = Random.Range(0, totalWeight);
            int accumulatingWeight = 0;
            foreach (IObjectProbabilityPair<T> pair in usablePairs)
            {
                accumulatingWeight += pair.weight;
                if (randomRoll >= accumulatingWeight) { continue; }
                
                result = pair.value;
                return true;
            }
            return false;
        }
        #endregion
    }
}

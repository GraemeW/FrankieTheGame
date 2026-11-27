using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LowDefMustard.Utils
{
    public static class ProbabilityPairExtensions
    {
        // Behavioural Notes:
        //  - Entries w/ weight <= 0 never land
        //  - An empty value (null / destroyed Unity object) keeps its weight:  landing on it picks nothing (false)
        //      - To never land on empties, filter them out before rolling

        #region PublicMethods
        public static bool TryGetRandom<T>(this IEnumerable<IObjectProbabilityPair<T>> objectProbabilityPairs, out T result)
        {
            result = default;
            List<IObjectProbabilityPair<T>> weightedPairs = GetWeightedPairs(objectProbabilityPairs);
            if (!TryRoll(weightedPairs, out IObjectProbabilityPair<T> landedPair) || IsEmpty(landedPair.value)) { return false; }

            result = landedPair.value;
            return true;
        }

        public static bool TryGetRandom<T>(this IEnumerable<IObjectProbabilityPair<T>> objectProbabilityPairs, int count, out List<T> results)
        {
            // Without repeats:  once a value is picked, every entry holding it leaves the draw, so repeated entries only raise its odds
            //  - Landing on an empty value picks nothing && takes the empties out of the draw (so up to count values still get picked)
            results = new List<T>();
            List<IObjectProbabilityPair<T>> weightedPairs = GetWeightedPairs(objectProbabilityPairs);
            while (results.Count < count && TryRoll(weightedPairs, out IObjectProbabilityPair<T> landedPair))
            {
                if (IsEmpty(landedPair.value))
                {
                    weightedPairs.RemoveAll(pair => IsEmpty(pair.value));
                    continue;
                }

                T picked = landedPair.value;
                results.Add(picked);
                weightedPairs.RemoveAll(pair => EqualityComparer<T>.Default.Equals(pair.value, picked));
            }
            return results.Count > 0;
        }
        
        internal static bool IsEmpty<T>(T value)
        {
            // Empty = null or a destroyed Unity object ~ Equals catches the latter (== null misses on generic type T)
            return value == null || value.Equals(null);
        }
        #endregion

        #region PrivateMethods
        private static List<IObjectProbabilityPair<T>> GetWeightedPairs<T>(IEnumerable<IObjectProbabilityPair<T>> objectProbabilityPairs)
        {
            return objectProbabilityPairs != null ? objectProbabilityPairs.Where(pair => pair is { weight: > 0 }).ToList() : new List<IObjectProbabilityPair<T>>();
        }
        
        private static bool TryRoll<T>(List<IObjectProbabilityPair<T>> weightedPairs, out IObjectProbabilityPair<T> landedPair)
        {
            landedPair = null;
            if (weightedPairs.Count == 0) { return false; }

            int totalWeight = weightedPairs.Sum(pair => pair.weight);
            int randomRoll = Random.Range(0, totalWeight);
            int accumulatingWeight = 0;
            foreach (IObjectProbabilityPair<T> pair in weightedPairs)
            {
                accumulatingWeight += pair.weight;
                if (randomRoll >= accumulatingWeight) { continue; }

                landedPair = pair;
                return true;
            }
            return false;
        }
        #endregion
    }
}

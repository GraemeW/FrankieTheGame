using System;
using System.Collections;
using UnityEngine;

namespace LowDefMustard.Utils.Tests
{
    public static class TestWait
    {
        // Yields updates until the condition holds or the timeout passes (edit + play mode)
        //  - only times out ~ or, does not fail a test assertion, must follow with the real assertion

        public static IEnumerator Until(Func<bool> condition, float timeoutSeconds = 2f)
        {
            double deadline = Time.realtimeSinceStartupAsDouble + timeoutSeconds;
            while (!condition() && Time.realtimeSinceStartupAsDouble < deadline) { yield return null; }
        }
    }
}

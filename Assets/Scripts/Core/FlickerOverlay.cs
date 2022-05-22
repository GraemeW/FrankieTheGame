using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Core
{
    public class FlickerOverlay : MonoBehaviour
    {
        // Tunables
        [NonReorderable][SerializeField] FlickerEntry[] flickerEntries = null;

        // Static Fixed
        static float MIN_FLICKER_TIME = 0f;
        static float MAX_FLICKER_TIME = 5f;

        // Data Structures
        [System.Serializable]
        public struct FlickerEntry
        {
            public float onTime;
            public float offTime;
        }

        public void FlickerToEnable()
        {
            if (flickerEntries == null || flickerEntries.Length == 0) { return; }

            StartCoroutine(TraverseFlickerEntries(true));
        }

        public void FlickerToDisable()
        {
            if (flickerEntries == null || flickerEntries.Length == 0) { return; }

            StartCoroutine(TraverseFlickerEntries(false));
        }

        IEnumerator TraverseFlickerEntries(bool settleEnable)
        {
            foreach (FlickerEntry flickerEntry in flickerEntries)
            {
                foreach (Transform child in transform) { child.gameObject.SetActive(true); }
                yield return new WaitForSeconds(Mathf.Clamp(flickerEntry.onTime, MIN_FLICKER_TIME, MAX_FLICKER_TIME));
                foreach (Transform child in transform) { child.gameObject.SetActive(false); }
                yield return new WaitForSeconds(Mathf.Clamp(flickerEntry.offTime, MIN_FLICKER_TIME, MAX_FLICKER_TIME));
            }
            foreach (Transform child in transform) { child.gameObject.SetActive(settleEnable); }
        }
    }
}
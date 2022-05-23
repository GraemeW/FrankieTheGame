using Frankie.Saving;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Core
{
    public class FlickerOverlay : MonoBehaviour, ISaveable
    {
        // Tunables
        [NonReorderable][SerializeField] FlickerEntry[] flickerEntries = null;

        // State
        bool childrenEnabled = true;

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
            childrenEnabled = true;
        }

        public void FlickerToDisable()
        {
            if (flickerEntries == null || flickerEntries.Length == 0) { return; }

            StartCoroutine(TraverseFlickerEntries(false));
            childrenEnabled = false;
        }

        public void FlickerToDeletion()
        {
            if (flickerEntries == null || flickerEntries.Length == 0) { return; }

            StartCoroutine(TraverseFlickerEntries(false, true));
        }

        IEnumerator TraverseFlickerEntries(bool settleEnable, bool deleteAfter = false)
        {
            foreach (FlickerEntry flickerEntry in flickerEntries)
            {
                foreach (Transform child in transform) { child.gameObject.SetActive(true); }
                yield return new WaitForSeconds(Mathf.Clamp(flickerEntry.onTime, MIN_FLICKER_TIME, MAX_FLICKER_TIME));
                foreach (Transform child in transform) { child.gameObject.SetActive(false); }
                yield return new WaitForSeconds(Mathf.Clamp(flickerEntry.offTime, MIN_FLICKER_TIME, MAX_FLICKER_TIME));
            }
            foreach (Transform child in transform) { child.gameObject.SetActive(settleEnable); }

            if (deleteAfter) { Destroy(gameObject); }
        }

        // Interface
        public LoadPriority GetLoadPriority()
        {
            return LoadPriority.ObjectProperty;
        }

        public SaveState CaptureState()
        {
            return new SaveState(GetLoadPriority(), childrenEnabled);
        }

        public void RestoreState(SaveState saveState)
        {
            childrenEnabled = (bool)saveState.state;
            foreach (Transform child in transform) { child.gameObject.SetActive(childrenEnabled); }
        }
    }
}
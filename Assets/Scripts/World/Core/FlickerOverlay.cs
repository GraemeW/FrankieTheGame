using System.Collections;
using UnityEngine;
using Frankie.Saving;

namespace Frankie.World
{
    public class FlickerOverlay : MonoBehaviour, ISaveable
    {
        // Tunables
        [SerializeField] private bool enabledOnAwake = false;
        [NonReorderable][SerializeField] private FlickerEntry[] flickerEntries;

        // State
        private bool childrenEnabled = false;

        // Static Fixed
        private const float _minFlickerTime = 0f;
        private const float _maxFlickerTime = 5f;

        // Data Structures
        [System.Serializable]
        public struct FlickerEntry
        {
            public float onTime;
            public float offTime;
        }

        private void Awake()
        {
            if (enabledOnAwake)
            {
                childrenEnabled = true;
                foreach (Transform child in transform) { child.gameObject.SetActive(true); }
            }
            else
            {
                childrenEnabled = false;
                foreach (Transform child in transform) { child.gameObject.SetActive(false); }
            }
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
                yield return new WaitForSeconds(Mathf.Clamp(flickerEntry.onTime, _minFlickerTime, _maxFlickerTime));
                foreach (Transform child in transform) { child.gameObject.SetActive(false); }
                yield return new WaitForSeconds(Mathf.Clamp(flickerEntry.offTime, _minFlickerTime, _maxFlickerTime));
            }
            foreach (Transform child in transform) { child.gameObject.SetActive(settleEnable); }

            if (deleteAfter) { Destroy(gameObject); }
        }

        // Interface
        public LoadPriority GetLoadPriority() => LoadPriority.ObjectProperty;

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

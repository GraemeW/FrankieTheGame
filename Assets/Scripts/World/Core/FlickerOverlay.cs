using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frankie.Saving;

namespace Frankie.World
{
    public class FlickerOverlay : MonoBehaviour, ISaveable
    {
        // Tunables
        [SerializeField] private bool enabledOnAwake = false;
        [NonReorderable][SerializeField] private List<FlickerEntry> flickerEntries = new();

        // State
        private bool childrenEnabled = false;

        // Static Fixed
        private const float _minFlickerTime = 0f;
        private const float _maxFlickerTime = 5f;

        #region DataStructures
        [System.Serializable]
        public struct FlickerEntry
        {
            public float onTime;
            public float offTime;
        }
        #endregion

        #region UnityMethods
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
        #endregion

        #region PublicMethods
        public void FlickerToEnable()
        {
            if (flickerEntries.Count == 0) { return; }
            StartCoroutine(TraverseFlickerEntries(true));
            childrenEnabled = true;
        }

        public void FlickerToDisable()
        {
            if (flickerEntries.Count == 0) { return; }
            StartCoroutine(TraverseFlickerEntries(false));
            childrenEnabled = false;
        }

        public void FlickerToDeletion()
        {
            if (flickerEntries.Count == 0) { return; }
            StartCoroutine(TraverseFlickerEntries(false, true));
        }
        #endregion

        #region PrivateMethods
        private IEnumerator TraverseFlickerEntries(bool settleEnable, bool deleteAfter = false)
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
        #endregion

        #region SaveInterface
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
        #endregion
    }
}

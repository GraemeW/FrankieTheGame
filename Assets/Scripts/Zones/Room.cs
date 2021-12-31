using Frankie.Saving;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.ZoneManagement
{
    public class Room : MonoBehaviour, ISaveable
    {
        // Tunables
        [SerializeField] bool disableOnStart = false;

        // State
        bool stateSetBySave = false;

        private void Start()
        {
            if (disableOnStart && !stateSetBySave) { gameObject.SetActive(false); }
        }

        public LoadPriority GetLoadPriority()
        {
            return LoadPriority.ObjectProperty;
        }

        SaveState ISaveable.CaptureState()
        {
            SaveState saveState = new SaveState(GetLoadPriority(), gameObject.activeSelf);
            return saveState;
        }

        void ISaveable.RestoreState(SaveState saveState)
        {
            if (saveState == null) { return; }

            bool roomEnabled = (bool)saveState.GetState();
            gameObject.SetActive(roomEnabled);

            if (saveState != null) { stateSetBySave = true; }
        }
    }
}

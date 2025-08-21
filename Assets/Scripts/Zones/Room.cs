using Frankie.Saving;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.ZoneManagement
{
    public class Room : MonoBehaviour, ISaveable
    {
        // Tunables
        [SerializeField] bool disableOnStart = true;

        // State
        bool stateSetBySave = false;
        bool stateSetByZoneHandler = false;

        private void Start()
        {
            if (disableOnStart && !stateSetBySave && !stateSetByZoneHandler) { gameObject.SetActive(false); }
        }

        public void FlagStateSetByZoneHandler() { stateSetByZoneHandler = true; }

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

            bool roomEnabled = (bool)saveState.GetState(typeof(bool));
            gameObject.SetActive(roomEnabled);
            stateSetBySave = true;
        }
    }
}

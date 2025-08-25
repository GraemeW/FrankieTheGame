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
        bool isRoomActive = true;
        bool roomSetByOtherEntity = false;

        #region UnityMethods
        private void Start()
        {
            if (disableOnStart && !roomSetByOtherEntity) { ToggleRoom(false, false); }
        }
        #endregion

        #region PublicMethods
        public void ToggleRoom(bool enable, bool roomSetByOtherEntity)
        {
            isRoomActive = enable;
            foreach (Transform child in transform)
            {
                if (child.TryGetComponent(out Door door)) { door.ToggleDoor(!enable); }
                else { child.gameObject.SetActive(enable); }
            }

            this.roomSetByOtherEntity = roomSetByOtherEntity;
        }
        #endregion

        #region InterfaceMethods
        public LoadPriority GetLoadPriority()
        {
            return LoadPriority.ObjectProperty;
        }

        SaveState ISaveable.CaptureState()
        {
            SaveState saveState = new SaveState(GetLoadPriority(), isRoomActive);
            return saveState;
        }

        void ISaveable.RestoreState(SaveState saveState)
        {
            if (saveState == null) { return; }

            bool roomEnabled = (bool)saveState.GetState(typeof(bool));
            ToggleRoom(roomEnabled, true);
        }
        #endregion
    }
}

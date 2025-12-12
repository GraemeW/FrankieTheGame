using UnityEngine;
using Frankie.Saving;
using Frankie.Utils;

namespace Frankie.ZoneManagement
{
    public class Room : MonoBehaviour, ISaveable
    {
        // Tunables
        [SerializeField] private bool disableOnStart = true;

        // State
        private bool isRoomActive = true;
        private LazyValue<bool> isRoomInitialized;

        #region UnityMethods
        private void Awake()
        {
            isRoomInitialized = new LazyValue<bool>(() => false);
        }

        private void Start()
        {
            isRoomInitialized.ForceInit();
            if (disableOnStart && !isRoomInitialized.value) { ToggleRoom(false, false); }
        }
        #endregion

        #region PublicMethods
        public void ToggleRoom(bool enable, bool roomSetByOtherEntity)
        {
            Debug.Log($"Toggling {gameObject.name} room to {enable}");

            isRoomActive = enable;
            foreach (Transform child in transform)
            {
                if (child.TryGetComponent(out Door door)) { door.ToggleDoor(enable); }
                else { child.gameObject.SetActive(enable); }
            }

            isRoomInitialized.value = roomSetByOtherEntity;
        }
        #endregion

        #region InterfaceMethods
        public LoadPriority GetLoadPriority() => LoadPriority.ObjectProperty;

        SaveState ISaveable.CaptureState()
        {
            var saveState = new SaveState(GetLoadPriority(), isRoomActive);
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

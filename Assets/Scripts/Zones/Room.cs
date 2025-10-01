using Frankie.Saving;
using Frankie.Utils;
using UnityEngine;

namespace Frankie.ZoneManagement
{
    public class Room : MonoBehaviour, ISaveable
    {
        // Tunables
        [SerializeField] bool disableOnStart = true;

        // State
        bool isRoomActive = true;
        LazyValue<bool> isRoomInitialized;

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
            UnityEngine.Debug.Log($"Toggling {gameObject.name} room to {enable}");

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

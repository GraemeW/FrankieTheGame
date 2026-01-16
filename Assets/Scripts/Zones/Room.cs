using UnityEngine;
using Frankie.Saving;
using Frankie.Sound;
using Frankie.Utils;

namespace Frankie.ZoneManagement
{
    [RequireComponent(typeof(BackgroundMusicOverride))]
    public class Room : MonoBehaviour, ISaveable
    {
        // Tunables
        [SerializeField] private bool disableOnStart = true;
        
        // Cached References
        private BackgroundMusicOverride backgroundMusicOverride;

        // State
        private bool isRoomActive = true;
        private LazyValue<bool> isRoomInitialized;

        #region UnityMethods
        private void Awake()
        {
            InitializeRoom();
        }

        private void Start()
        {
            isRoomInitialized.ForceInit();
            if (disableOnStart && !isRoomInitialized.value) { ToggleRoom(false, false); }
        }

        private void InitializeRoom()
        {
            backgroundMusicOverride = GetComponent<BackgroundMusicOverride>();
            isRoomInitialized = new LazyValue<bool>(() => false);
        }
        #endregion

        #region PublicMethods
        public void ToggleRoom(bool enable, bool roomSetByOtherEntity)
        {
            Debug.Log($"Toggling {gameObject.name} room to {enable}");

            isRoomActive = enable;
            isRoomInitialized.value = roomSetByOtherEntity;
            
            foreach (Transform child in transform)
            {
                if (child.TryGetComponent(out Door door)) { door.ToggleDoor(enable); }
                else { child.gameObject.SetActive(enable); }
            }
            
            if (backgroundMusicOverride.HasAudioOverride()) { backgroundMusicOverride.TriggerOverride(enable); }
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
            
            InitializeRoom();
            ToggleRoom(roomEnabled, true);
        }
        #endregion
    }
}

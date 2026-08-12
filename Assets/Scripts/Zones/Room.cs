using UnityEngine;
using LowDefMustard.Saving;
using LowDefMustard.Utils;
using Frankie.Sound;
using Frankie.Utils;

namespace Frankie.ZoneManagement
{
    [RequireComponent(typeof(BackgroundMusicOverride))]
    public class Room : MonoBehaviour, ISaveable<bool>
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
            bool debugForceEnable = false;
#if UNITY_EDITOR
            debugForceEnable = FrankieDebugger.ShouldEnableAllRooms();
#endif
            
            if (disableOnStart && !isRoomInitialized.value && !debugForceEnable) { ToggleRoom(false, false); }
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
        
        public SaveState CaptureState() => ManualGetStateFromData(isRoomActive);
        
        public void RestoreState(SaveState saveState)
        {
            if (saveState == null) { return; }
            TryManualGetDataFromState(saveState, out bool roomEnabled);
            
            InitializeRoom();
            ToggleRoom(roomEnabled, true);
        }
        
        public SaveState ManualGetStateFromData(bool data) => new(GetLoadPriority(), data);
        
        public bool TryManualGetDataFromState(SaveState saveState, out bool value)
        {
            if (saveState != null && saveState.TryGetState(out value)) { return true; }
            value = isRoomActive;
            return true;
        }
        #endregion
    }
}

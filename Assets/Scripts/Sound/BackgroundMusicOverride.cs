using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Scripting.LifecycleManagement;
using LowDefMustard.Saving;

namespace Frankie.Sound
{
    public partial class BackgroundMusicOverride : MonoBehaviour, ISaveable<bool>
    {
        // Tunables
        [SerializeField] private AudioClip audioClip;
        [SerializeField] [Tooltip("All overrides take priority over Zone music, Room setting: 1, others >")] private int priority = 0;

        // State
        private bool queueTriggerInStart = false;
        private bool isOverrideActive = false;

        // Static
        [AutoStaticsCleanup] private static BackgroundMusicOverride _currentBackgroundMusicOverride;
        [AutoStaticsCleanup] private static readonly List<BackgroundMusicOverride> _backgroundMusicOverrides = new();

        #region StaticMethods
        private static void SubscribeOverride(BackgroundMusicOverride backgroundMusicOverride)
        {
            if (_backgroundMusicOverrides.Contains(backgroundMusicOverride)) { return; }
            _backgroundMusicOverrides.Add(backgroundMusicOverride);
        }

        private static void UnsubscribeOverride(BackgroundMusicOverride backgroundMusicOverride)
        {
            if (_currentBackgroundMusicOverride == backgroundMusicOverride)
            {
                backgroundMusicOverride.TriggerOverride(false);
            }
            _backgroundMusicOverrides.Remove(backgroundMusicOverride);
        }

        private static bool TryGetHighestPriorityActiveOverride(out BackgroundMusicOverride highestPriorityOverride)
        {
            highestPriorityOverride = null;
            foreach (BackgroundMusicOverride backgroundMusicOverride in _backgroundMusicOverrides.Where(backgroundMusicOverride => backgroundMusicOverride.isOverrideActive))
            {
                if (highestPriorityOverride == null || backgroundMusicOverride.priority > highestPriorityOverride.priority)
                {
                    highestPriorityOverride = backgroundMusicOverride;
                }
            }
            return highestPriorityOverride != null;
        }

        public static bool TryGetOverrideAudio(out AudioClip audioClip)
        {
            audioClip = null;
            for (int i =  0; i < _backgroundMusicOverrides.Count; i++)
            {
                if (!TryGetHighestPriorityActiveOverride(out BackgroundMusicOverride backgroundMusicOverride)) { return false; }

                if (backgroundMusicOverride.HasAudioOverride())
                {
                    audioClip = backgroundMusicOverride.GetAudioClip();
                    return true;
                }
                
                // Invalid Configuration -- Disable and iterate until we get something
                backgroundMusicOverride.TriggerOverride(false);
            }
            return false;
        }
        #endregion
        
        #region UnityMethods
        private void Awake() => SubscribeOverride(this);
        private void OnDestroy() => UnsubscribeOverride(this);

        private void Start()
        {
            if (!queueTriggerInStart) { return; }
            queueTriggerInStart = false;
            
            TriggerOverride(true);
        }
        #endregion

        #region PublicMethods
        public bool HasAudioOverride() => audioClip != null;

        public void ToggleOverride() // Callable via Unity Events
        {
            TriggerOverride(!isOverrideActive);
        }
        
        public void TriggerOverride(bool enable)  // Callable via Unity Events
        {
            if (!CoreAudio.DoesBackgroundMusicExist() || audioClip == null) { return; }
            if (enable && _currentBackgroundMusicOverride == this) { return; }

            if (enable)
            {
                isOverrideActive = true;
                if (_currentBackgroundMusicOverride != null && priority < _currentBackgroundMusicOverride.GetPriority()) { return; }
                
                if (CoreAudio.OverrideBackgroundMusic(audioClip))
                {
                    _currentBackgroundMusicOverride = this;
                }
            }
            else
            {
                isOverrideActive = false;
                if (_currentBackgroundMusicOverride != this) { return; }
                
                _currentBackgroundMusicOverride = null;
                if (TryGetHighestPriorityActiveOverride(out BackgroundMusicOverride nextUpMusicOverride))
                {
                    nextUpMusicOverride.TriggerOverride(true);
                }
                else
                {
                    CoreAudio.StopOverrideBackgroundMusic();
                }
            }
        }
        #endregion

        #region PrivateMethods
        private AudioClip GetAudioClip() => audioClip;
        private int GetPriority() => priority;
        #endregion

        #region SaveSystem
        public LoadPriority GetLoadPriority() => LoadPriority.ObjectProperty; 
        public SaveState CaptureState() => ManualGetStateFromData(isOverrideActive);
        public void RestoreState(SaveState saveState)
        { 
            TryManualGetDataFromState(saveState, out queueTriggerInStart);
        }
        
        public SaveState ManualGetStateFromData(bool data) => new(GetLoadPriority(), data);
        
        public bool TryManualGetDataFromState(SaveState saveState, out bool value)
        {
            if (saveState != null && saveState.TryGetState(out value)) { return true; }
            value = isOverrideActive;
            return true;
        }
        #endregion
    }
}

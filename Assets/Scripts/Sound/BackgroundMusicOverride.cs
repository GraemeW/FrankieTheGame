using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Frankie.Saving;

namespace Frankie.Sound
{
    public class BackgroundMusicOverride : MonoBehaviour, ISaveable<bool>
    {
        // Tunables
        [SerializeField] private AudioClip audioClip;
        [SerializeField] [Tooltip("All overrides take priority over Zone music, Room setting: 1, others >")] private int priority = 0;

        // State
        private bool queueTriggerInStart = false;
        private bool isOverrideActive = false;

        // Static
        private static BackgroundMusicOverride _currentBackgroundMusicOverride;
        private static readonly List<BackgroundMusicOverride> _backgroundMusicOverrides = new();

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

        public static bool TryGetHighestPriorityActiveOverride(out BackgroundMusicOverride highestPriorityOverride)
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
        #endregion
        
        #region UnityMethods
        private void Awake() => SubscribeOverride(this);
        private void OnDestroy() => UnsubscribeOverride(this);

        private void Start()
        {
            if (!queueTriggerInStart) { return; }
            queueTriggerInStart = false;
            
            BackgroundMusic backgroundMusic = BackgroundMusic.FindBackgroundMusic();
            if (backgroundMusic == null) { return; }
            
            TriggerOverride(true, backgroundMusic);
        }
        #endregion

        #region UtilityMethods
        public bool HasAudioOverride() => audioClip != null;
        public AudioClip GetAudioClip() => audioClip;
        public void TriggerOverride(bool enable) // Callable via Unity Events
        {
            BackgroundMusic backgroundMusic = BackgroundMusic.FindBackgroundMusic();
            if (backgroundMusic == null) { return; }
            TriggerOverride(enable, backgroundMusic);
        }

        public void ToggleOverride() // Callable via Unity Events
        {
            TriggerOverride(!isOverrideActive);
        }

        private int GetPriority() => priority;
        private void TriggerOverride(bool enable, BackgroundMusic backgroundMusic)
        {
            if (backgroundMusic == null || audioClip == null) { return; }
            if (enable && _currentBackgroundMusicOverride == this) { return; }

            if (enable)
            {
                isOverrideActive = true;
                if (_currentBackgroundMusicOverride != null && priority < _currentBackgroundMusicOverride.GetPriority()) { return; }
                
                if (backgroundMusic.OverrideMusic(audioClip))
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
                    backgroundMusic.StopOverrideMusic();
                }
            }
        }
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

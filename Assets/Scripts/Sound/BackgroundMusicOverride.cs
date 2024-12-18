using Frankie.Saving;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Sound
{
    public class BackgroundMusicOverride : MonoBehaviour, ISaveable
    {
        // Tunables
        [SerializeField] AudioClip audioClip = null;

        // State
        bool queueTriggerInStart = false;
        bool triggered = false;

        // Static
        static List<BackgroundMusicOverride> backgroundMusicOverrides = new List<BackgroundMusicOverride>();

        #region StaticMethods
        public static void SubscribeOverride(BackgroundMusicOverride backgroundMusicOverride)
        {
            if (backgroundMusicOverrides.Contains(backgroundMusicOverride)) { return; }

            backgroundMusicOverrides.Add(backgroundMusicOverride);
        }

        public static void UnsubscribeOverride(BackgroundMusicOverride backgroundMusicOverride)
        {
            backgroundMusicOverrides.Remove(backgroundMusicOverride);
        }

        public static void UnflagAllOverrides()
        {
            foreach (BackgroundMusicOverride backgroundMusicOverride in backgroundMusicOverrides)
            {
                backgroundMusicOverride.UnflagOverride();
            }
        }
        #endregion


        #region UnityMethods
        private void Awake() => SubscribeOverride(this);
        private void OnDestroy() => UnsubscribeOverride(this);

        private void Start()
        {
            if (queueTriggerInStart)
            {
                TriggerOverride(true);
                queueTriggerInStart = false;
            }
        }
        #endregion

        #region UtilityMethods
        public void TriggerOverride(bool enable) // Default behavior, callable via Unity Events
        {
            TriggerOverride(enable, false);
        }

        public void UnflagOverride() => triggered = false;

        private void TriggerOverride(bool enable, bool calledInRestore)
        {
            if (audioClip == null) { return; }

            UnflagAllOverrides(); // Only one override allowed at a time, no history / hierarchy currently possible
            GameObject backgroundMusicObject = GameObject.FindGameObjectWithTag("BackgroundMusic");
            if (backgroundMusicObject == null) { return; }

            if (backgroundMusicObject.TryGetComponent(out BackgroundMusic backgroundMusic))
            {
                if (enable)
                {
                    backgroundMusic.OverrideMusic(audioClip, calledInRestore);
                    triggered = true;
                }
                else
                {
                    backgroundMusic.StopOverrideMusic();
                    triggered = false;
                }
            }
        }
        #endregion

        #region SaveSystem
        public LoadPriority GetLoadPriority()
        {
            return LoadPriority.ObjectProperty;
        }

        public SaveState CaptureState()
        {
            return new SaveState(GetLoadPriority(), triggered);
        }

        public void RestoreState(SaveState state)
        {
            if ((bool)state.GetState(typeof(bool))) { queueTriggerInStart = true; }
        }
        #endregion
    }
}

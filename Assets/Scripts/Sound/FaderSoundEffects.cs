using UnityEngine;
using UnityEngine.Audio;
using Frankie.ZoneManagement;

namespace Frankie.Sound
{
    public class FaderSoundEffects : SoundEffects
    {
        // Tunables
        [SerializeField] private Fader fader;
        [SerializeField] private AudioClip battleEntryAudioClip;
        
        protected override void OnEnable()
        {
            base.OnEnable();
            fader.fadingIn += HandleFadeIn;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            fader.fadingIn -= HandleFadeIn;
        }

        protected override void LinkToAudioMixer()
        {
            if (CoreAudio.TryGetFaderAudioMixer(out AudioMixerGroup audioMixerGroup))
            {
                audioSource.outputAudioMixerGroup = audioMixerGroup;
            }
        }

        private void HandleFadeIn(TransitionType transitionType)
        {
            if (transitionType is TransitionType.BattleNeutral or TransitionType.BattleGood or TransitionType.BattleBad)
            {
                StartBattleEntrySoundEffect();
            }
        }

        private void StartBattleEntrySoundEffect()
        {
            PlayClip(battleEntryAudioClip);
        }
    }
}

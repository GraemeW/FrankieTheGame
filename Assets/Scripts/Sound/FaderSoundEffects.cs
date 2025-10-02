using UnityEngine;
using Frankie.ZoneManagement;

namespace Frankie.Sound
{
    public class FaderSoundEffects : SoundEffects
    {
        // Tunables
        [SerializeField] Fader fader = null;
        [SerializeField] AudioClip battleEntryAudioClip = null;

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

        protected override void Update()
        {
            base.Update();
        }

        private void HandleFadeIn(TransitionType transitionType)
        {
            if (transitionType == TransitionType.BattleNeutral || transitionType == TransitionType.BattleGood || transitionType == TransitionType.BattleBad)
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

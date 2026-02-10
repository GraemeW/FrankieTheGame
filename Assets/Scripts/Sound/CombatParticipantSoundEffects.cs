using System.Linq;
using UnityEngine;
using Frankie.Combat;

namespace Frankie.Sound
{
    public class CombatParticipantSoundEffects : SoundEffects
    {
        // Tunables
        [Header("Audio Source Hookups")]
        [SerializeField] private AudioSource[] audioSources;
        [Header("CombatParticipant Reference")]
        [SerializeField] private CombatParticipant combatParticipant;
        [Header("AudioClip Hookups")]
        [SerializeField] private AudioClip decreaseHPAudioClip;
        [SerializeField] private AudioClip missedHitAudioClip;
        [SerializeField] private AudioClip increaseHPAudioClip;
        [SerializeField] private AudioClip deadAudioClip;
        [SerializeField] private AudioClip decreaseAPAudioClip;
        [SerializeField] private AudioClip increaseAPAudioClip;
        [SerializeField] private AudioClip increaseStatAudioClip;
        [SerializeField] private AudioClip decreaseStatAudioClip;
        [SerializeField] private AudioClip getHelpAudioClip;
        [SerializeField] private AudioClip actionDequeuedAudioClip;
        
        protected override void OnEnable()
        {
            base.OnEnable();
            combatParticipant.SubscribeToStateUpdates(HandleCombatParticipantState);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            combatParticipant.UnsubscribeToStateUpdates(HandleCombatParticipantState);
        }

        protected override void SetAudioSource(AudioClip audioClip = null)
        {
            if (audioSources == null || audioSources.Length == 0) { base.SetAudioSource(audioClip); }
            else
            {
                // Avoid duplicate simultaneous clip plays (impact is otherwise LOUD)
                if (audioClip != null && audioSources.Any(source => source.isPlaying && source.clip == audioClip))
                {
                    audioSource = null;
                    return;
                }
                audioSource = audioSources.FirstOrDefault(source => !source.isPlaying);
            }
        }

        private void HandleCombatParticipantState(StateAlteredInfo stateAlteredInfo)
        {
            switch (stateAlteredInfo.stateAlteredType)
            {
                case StateAlteredType.DecreaseHP:
                    PlayClip(decreaseHPAudioClip);
                    break;
                case StateAlteredType.IncreaseHP:
                    PlayClip(increaseHPAudioClip);
                    break;
                case StateAlteredType.IncreaseAP:
                    PlayClip(increaseAPAudioClip);
                    break;
                case StateAlteredType.DecreaseAP:
                    PlayClip(decreaseAPAudioClip);
                    break;
                case StateAlteredType.AdjustHPNonSpecific:
                case StateAlteredType.AdjustAPNonSpecific:
                    break;
                case StateAlteredType.Dead:
                    PlayClipAfterDestroy(deadAudioClip);
                    break;
                case StateAlteredType.Resurrected:
                    break;
                case StateAlteredType.StatusEffectApplied:
                    PersistentStatus persistentStatus = stateAlteredInfo.persistentStatus;
                    if (persistentStatus == null) { break; }
                    PlayClip(persistentStatus.IsIncrease() ? increaseStatAudioClip : decreaseStatAudioClip);
                    break;
                case StateAlteredType.BaseStateEffectApplied:
                    break;
                case StateAlteredType.CooldownSet:
                    break;
                case StateAlteredType.CooldownExpired:
                    break;
                case StateAlteredType.HitMiss:
                    PlayClip(missedHitAudioClip);
                    break;
                case StateAlteredType.HitCrit:
                    break;
                case StateAlteredType.FriendFound:
                    PlayClip(getHelpAudioClip);
                    break;
                case StateAlteredType.FriendIgnored:
                    PlayClip(missedHitAudioClip);
                    break;
                case StateAlteredType.ActionDequeued:
                    PlayClip(actionDequeuedAudioClip);
                    break;
            }
        }
    }
}

using UnityEngine;
using Frankie.Combat;

namespace Frankie.Sound
{
    public class CombatParticipantSoundEffects : SoundEffects
    {
        [SerializeField] private CombatParticipant combatParticipant;
        [SerializeField] private AudioClip decreaseHPAudioClip;
        [SerializeField] private AudioClip increaseHPAudioClip;
        [SerializeField] private AudioClip deadAudioClip;
        [SerializeField] private AudioClip decreaseAPAudioClip;
        [SerializeField] private AudioClip increaseAPAudioClip;
        [SerializeField] private AudioClip increaseStatAudioClip;
        [SerializeField] private AudioClip decreaseStatAudioClip;

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
                    break;
                case StateAlteredType.HitCrit:
                    break;
                case StateAlteredType.AdjustHPNonSpecific:
                default:
                    break;
            }
        }
    }
}

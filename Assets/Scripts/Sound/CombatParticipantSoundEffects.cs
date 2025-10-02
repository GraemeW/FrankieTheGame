using UnityEngine;
using Frankie.Combat;

namespace Frankie.Sound
{
    public class CombatParticipantSoundEffects : SoundEffects
    {
        [SerializeField] CombatParticipant combatParticipant = null;
        [SerializeField] AudioClip decreaseHPAudioClip = null;
        [SerializeField] AudioClip increaseHPAudioClip = null;
        [SerializeField] AudioClip deadAudioClip = null;
        [SerializeField] AudioClip decreaseAPAudioClip = null;
        [SerializeField] AudioClip increaseAPAudioClip = null;

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

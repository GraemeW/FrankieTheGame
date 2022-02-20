using Frankie.Combat;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
            combatParticipant.stateAltered += HandleCombatParticipantState;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            combatParticipant.stateAltered -= HandleCombatParticipantState;
        }

        private void HandleCombatParticipantState(CombatParticipant combatParticipant, StateAlteredData stateAlteredData)
        {
            switch (stateAlteredData.stateAlteredType)
            {
                case StateAlteredType.DecreaseHP:
                    PlayClip(decreaseHPAudioClip);
                    break;
                case StateAlteredType.IncreaseHP:
                    PlayClip(increaseHPAudioClip);
                    break;
                case StateAlteredType.IncreaseAP:
                    PlayClipAfterDestroy(increaseAPAudioClip);
                    break;
                case StateAlteredType.DecreaseAP:
                    PlayClipAfterDestroy(decreaseAPAudioClip);
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

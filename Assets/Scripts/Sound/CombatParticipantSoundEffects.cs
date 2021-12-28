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
        [SerializeField] AudioClip restoreAPClip = null;

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
            if (stateAlteredData.stateAlteredType == StateAlteredType.DecreaseHP)
            {
                PlayClip(decreaseHPAudioClip);
            }
            else if (stateAlteredData.stateAlteredType == StateAlteredType.IncreaseHP)
            {
                PlayClip(increaseHPAudioClip);
            }
            else if (stateAlteredData.stateAlteredType == StateAlteredType.Dead)
            {
                PlayClipAfterDestroy(deadAudioClip);
            }
            else if (stateAlteredData.stateAlteredType == StateAlteredType.IncreaseAP)
            {
                PlayClipAfterDestroy(restoreAPClip);
            }
        }
    }
}

using Frankie.Combat;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Frankie.Control
{
    [System.Serializable]
    public class InteractionEventWithCombatParticipant : UnityEvent<PlayerStateHandler, CombatParticipant>
    {
    }
}
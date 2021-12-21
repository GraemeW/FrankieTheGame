using Frankie.Combat;
using Frankie.Stats;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Control.Specialization
{
    public class WorldPartyLeaderSetter : MonoBehaviour
    {
        public void SetPartyLeader(PlayerStateHandler playerStateHandler, CombatParticipant character)
        {
            playerStateHandler.GetParty().SetPartyLeader(character);
        }
    }
}
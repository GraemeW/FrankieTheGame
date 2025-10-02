using UnityEngine;
using Frankie.Stats;

namespace Frankie.Control.Specialization
{
    public class WorldPartyInterface : MonoBehaviour
    {
        // Tunables
        [SerializeField] CharacterProperties characterProperties = null;

        // Methods -- Called via Unity Events
        public void AddToParty(PlayerStateMachine playerStateMachine)
        {
            if (playerStateMachine.TryGetComponent(out Party party))
            {
                party.AddToParty(characterProperties);
            }
        }

        public void AddToPartyAssist(PlayerStateMachine playerStateMachine)
        {
            if (playerStateMachine.TryGetComponent(out PartyAssist partyAssist))
            {
                partyAssist.AddToParty(characterProperties);
            }
        }

        public void RemoveFromParty(PlayerStateMachine playerStateMachine)
        {
            if (playerStateMachine.TryGetComponent(out Party party))
            {
                party.RemoveFromParty(characterProperties);
            }
        }

        public void RemoveFromPartyAssist(PlayerStateMachine playerStateMachine)
        {
            if (playerStateMachine.TryGetComponent(out PartyAssist partyAssist))
            {
                partyAssist.RemoveFromParty(characterProperties);
            }
        }
    }
}

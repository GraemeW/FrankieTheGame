using UnityEngine;
using Frankie.Control;
using Frankie.Stats;

namespace Frankie.World
{
    public class WorldPartyInterface : MonoBehaviour
    {
        // Tunables
        [SerializeField] private CharacterProperties characterProperties;

        #region PublicMethods
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
        #endregion
    }
}

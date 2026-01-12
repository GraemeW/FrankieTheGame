using Frankie.Combat;
using Frankie.Core;
using Frankie.Stats;
using UnityEngine;

namespace Frankie.Control
{
    public class PlayerMenuHandler : MonoBehaviour
    {
        [SerializeField][Tooltip("false for GameOver screen")] private bool destroyPlayerOnStart = true;

        private void Start()
        {
            HandlePlayerExistence(true);
        }
        
        private void OnDestroy()
        {
            HandlePlayerExistence(false);
        }
        
        private void HandlePlayerExistence(bool isStart)
        {
            PlayerStateMachine playerStateMachine = Player.FindPlayerStateMachine();
            if (playerStateMachine == null) return;

            if (destroyPlayerOnStart)
            {
                if (isStart) { Destroy(playerStateMachine.gameObject); }
                return;
            }

            HealParty(playerStateMachine);
            playerStateMachine.EnterWorld();
            LockPlayer(playerStateMachine, isStart);
        }

        private void HealParty(PlayerStateMachine playerStateMachine)
        {
            if (playerStateMachine == null) { return; }
            Party party = playerStateMachine.GetParty();
            if (party == null) { return; }
            
            foreach (BaseStats member in party.GetParty())
            {
                if (member.TryGetComponent(out CombatParticipant combatParticipant))
                {
                    combatParticipant.Revive(false);
                }
            }
        }
        
        private void LockPlayer(PlayerStateMachine playerStateMachine, bool enable)
        {
            if (playerStateMachine == null) { return; }
            
            if (enable)
            {
                // Lock menus, but allow player movement
                playerStateMachine.EnterCutscene(true, true);
                if (playerStateMachine.TryGetComponent(out PlayerMover playerMover))
                {
                    playerMover.SetLookDirection(Vector2.down);
                }
            }
            else
            {
                playerStateMachine.EnterWorld();
            }
        }
    }
}

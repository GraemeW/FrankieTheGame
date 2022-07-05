using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Control
{
    public class PlayerStateOverrideToCutscene : MonoBehaviour
    {
        // Cached Reference
        PlayerStateMachine playerStateMachine = null;

    private void OnEnable()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player.TryGetComponent(out PlayerStateMachine playerStateMachine))
            {
                this.playerStateMachine = playerStateMachine;
                playerStateMachine.EnterCutscene();

            }
        }

        private void OnDisable()
        {
            if (playerStateMachine == null) { return; }

            playerStateMachine.EnterWorld();
        }
    }

}

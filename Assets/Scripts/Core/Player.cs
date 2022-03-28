using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frankie.Stats;
using Frankie.Control;

namespace Frankie.Core
{
    [RequireComponent(typeof(PlayerStateMachine))]
    [RequireComponent(typeof(PartyCombatConduit))]

    public class Player : MonoBehaviour
    {
        // Cached References
        PlayerStateMachine playerStateHandler = null;
        PartyCombatConduit partyCombatConduit = null;

        private void Awake()
        {
            VerifySingleton();
            playerStateHandler = GetComponent<PlayerStateMachine>();
        }

        private void OnEnable()
        {
            playerStateHandler.playerStateChanged += HandlePlayerStateChanged;
        }

        private void OnDisable()
        {
            playerStateHandler.playerStateChanged -= HandlePlayerStateChanged;
        }

        private void VerifySingleton()
        {
            // Singleton through standard approach -- do not use persistent object spawner for player
            int numberOfPlayers = FindObjectsOfType<Player>().Length;
            if (numberOfPlayers > 1)
            {
                gameObject.SetActive(false);
                Destroy(gameObject);
            }
            else
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        private void HandlePlayerStateChanged(PlayerStateType playerState)
        {
            // Any player scene change when party is completely wiped out -> shift to game over
            // Will naturally call on combat end during transition
            if (partyCombatConduit == null) { return; }

            if (!partyCombatConduit.IsAnyMemberAlive())
            {
                SavingWrapper.LoadGameOverScene();
            }
        }
    }
}
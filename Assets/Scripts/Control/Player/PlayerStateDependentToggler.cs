using Frankie.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Control
{
    public class PlayerStateDependentToggler : MonoBehaviour
    {
        // Tunables
        [SerializeField] [Tooltip("Default behavior is disable for all other states")] List<PlayerStateType> playerStateForEnable = new List<PlayerStateType>();

        // Cached References
        GameObject player = null;
        ReInitLazyValue<PlayerStateHandler> playerStateHandler = null;

        private void Awake()
        {
            playerStateHandler = new ReInitLazyValue<PlayerStateHandler>(SetupPlayerStateHandler);
        }

        private void Start()
        {
            playerStateHandler.ForceInit();
        }

        private void OnEnable()
        {
            playerStateHandler.value.playerStateChanged += HandlePlayerStateChanged;
        }

        private void OnDisable()
        {
            playerStateHandler.value.playerStateChanged -= HandlePlayerStateChanged;
        }

        private PlayerStateHandler SetupPlayerStateHandler()
        {
            if (player == null) { player = GameObject.FindGameObjectWithTag("Player"); }
            return player?.GetComponent<PlayerStateHandler>();
        }

        private void HandlePlayerStateChanged(PlayerStateType playerState)
        {
            if (playerStateForEnable == null || playerStateForEnable.Count == 0) { return; }

            if (playerStateForEnable.Contains(playerState))
            {
                foreach (Transform child in transform)
                {
                    child.gameObject.SetActive(true);
                }
            }
            else
            {
                foreach (Transform child in transform)
                {
                    child.gameObject.SetActive(false);
                }
            }
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frankie.Core;
using Frankie.Utils;

namespace Frankie.Control
{
    public class PlayerStateDependentToggler : MonoBehaviour
    {
        // Tunables
        [SerializeField][Tooltip("Default behavior is disable for all other states")] List<PlayerStateType> playerStateForEnable = new List<PlayerStateType>();

        // Cached References
        ReInitLazyValue<PlayerStateMachine> playerStateMachine = null;

        #region UnityMethods
        private void Awake()
        {
            playerStateMachine = new ReInitLazyValue<PlayerStateMachine>(Player.FindPlayerStateMachine);
        }

        private void Start()
        {
            playerStateMachine.ForceInit();
        }

        private void OnEnable()
        {
            playerStateMachine.value.playerStateChanged += HandlePlayerStateChanged;
        }

        private void OnDisable()
        {
            playerStateMachine.value.playerStateChanged -= HandlePlayerStateChanged;
        }
        #endregion

        #region PrivateMethods
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
        #endregion
    }
}

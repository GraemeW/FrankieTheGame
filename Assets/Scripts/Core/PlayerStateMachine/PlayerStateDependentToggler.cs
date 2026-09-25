using System.Collections.Generic;
using UnityEngine;
using LowDefMustard.Utils;

namespace Frankie.Core
{
    public class PlayerStateDependentToggler : MonoBehaviour
    {
        // Tunables
        [SerializeField][Tooltip("Default behavior is disable for all other states")] private List<PlayerStateType> playerStateForEnable = new();

        // Cached References
        private ReInitLazyValue<PlayerStateMachine> playerStateMachine;

        #region UnityMethods
        private void Awake()
        {
            playerStateMachine = new ReInitLazyValue<PlayerStateMachine>(Player.FindPlayerStateMachine);
        }

        private void Start()
        {
            playerStateMachine ??= new ReInitLazyValue<PlayerStateMachine>(Player.FindPlayerStateMachine);
            playerStateMachine.ForceInit();
        }

        private void OnEnable()
        {
            if (playerStateMachine.TryGetSafely(out PlayerStateMachine playerStateMachineInstance)) { playerStateMachineInstance.playerStateChanged += HandlePlayerStateChanged; }
        }

        private void OnDisable()
        {
            if (playerStateMachine.TryGetSafely(out PlayerStateMachine playerStateMachineInstance, allowReInit: false)) { playerStateMachineInstance.playerStateChanged -= HandlePlayerStateChanged; }
        }
        #endregion

        #region PrivateMethods
        private void HandlePlayerStateChanged(PlayerStateType playerState, IPlayerStateContext playerStateContext)
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

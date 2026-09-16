using UnityEngine;
using LowDefMustard.Utils;

namespace Frankie.Core
{
    public class PlayerStateOverrideToCutscene : MonoBehaviour
    {
        // Cached Reference
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
            if (playerStateMachine.TryGetSafely(out PlayerStateMachine playerStateMachineInstance)) { playerStateMachineInstance.EnterCutscene(); }
        }

        private void OnDisable()
        {
            if (playerStateMachine.TryGetSafely(out PlayerStateMachine playerStateMachineInstance)) { playerStateMachineInstance.EnterWorld(); }
        }
        #endregion
    }
}

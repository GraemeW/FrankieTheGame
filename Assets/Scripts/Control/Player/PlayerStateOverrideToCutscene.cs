using UnityEngine;
using Frankie.Core;
using Frankie.Utils;

namespace Frankie.Control
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
            playerStateMachine.ForceInit();
        }

        private void OnEnable()
        {
            playerStateMachine.value?.EnterCutscene();
        }

        private void OnDisable()
        {
            playerStateMachine.value?.EnterWorld();
        }
        #endregion
    }
}

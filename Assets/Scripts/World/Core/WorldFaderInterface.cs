using UnityEngine;
using Frankie.Control;
using Frankie.ZoneManagement;

namespace Frankie.World
{
    public class WorldFaderInterface : MonoBehaviour
    {
        // Tunables
        [SerializeField] private float blipFadeHoldSeconds = 1.0f;

        // State
        private Coroutine activeFade;
        
        #region UnityMethods
        private void OnDisable()
        {
            if (activeFade != null) { StopCoroutine(activeFade); }
        }
        #endregion

        #region PublicMethods
        public void StartBlipFade(PlayerStateMachine playerStateMachine)
        {
            var faderEventTriggers = new FaderEventTriggers(_ => playerStateMachine.EnterCutscene(true), null, playerStateMachine.EnterWorld, null);
            Fader.StartBlipFade(blipFadeHoldSeconds, faderEventTriggers);
        }
        #endregion
    }
}

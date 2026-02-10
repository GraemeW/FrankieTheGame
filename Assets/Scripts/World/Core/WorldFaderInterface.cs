using System.Collections;
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
            if (activeFade != null) { StopCoroutine(activeFade); }
            activeFade = StartCoroutine(BlipFade(playerStateMachine));
        }
        #endregion

        #region PrivateMethods
        private IEnumerator BlipFade(PlayerStateMachine playerStateMachine)
        {
            Fader fader = Fader.FindFader();
            if (fader == null) { yield break; }
            
            if (playerStateMachine != null) { playerStateMachine.EnterCutscene(true);}
            yield return fader.BlipFade(blipFadeHoldSeconds);
            if (playerStateMachine != null) { playerStateMachine.EnterWorld(); }
        }
        #endregion
    }
}

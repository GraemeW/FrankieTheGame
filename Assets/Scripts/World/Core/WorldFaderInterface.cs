using System;
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
        
        private void OnDisable()
        {
            if (activeFade != null) { StopCoroutine(activeFade); }
        }

        public void StartBlipFade(PlayerStateMachine playerStateMachine)
        {
            if (activeFade != null) { StopCoroutine(activeFade); }
            activeFade = StartCoroutine(BlipFade(playerStateMachine));
        }

        private IEnumerator BlipFade(PlayerStateMachine playerStateMachine)
        {
            Fader fader = Fader.FindFader();
            if (fader == null) { yield break; }
            
            if (playerStateMachine != null) { playerStateMachine.EnterCutscene(true);}
            yield return fader.BlipFade(blipFadeHoldSeconds);
            if (playerStateMachine != null) { playerStateMachine.EnterWorld(); }
        }
    }
}

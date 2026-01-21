using System.Collections;
using UnityEngine;
using Frankie.Control;
using Frankie.ZoneManagement;

namespace Frankie.World
{
    public class WorldFaderInterface : MonoBehaviour
    {
        [SerializeField] private float blipFadeHoldSeconds = 1.0f;
        
        public void StartBlipFade(PlayerStateMachine playerStateMachine)
        {
            StartCoroutine(BlipFade(playerStateMachine));
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

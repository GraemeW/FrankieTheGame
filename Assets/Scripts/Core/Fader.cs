using Frankie.Control;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Frankie.Core
{
    public class Fader : MonoBehaviour
    {
        // Tunables
        [Header("Linked Assets")]
        [SerializeField] Canvas battleCanvas = null;
        [SerializeField] Image nodeEntry = null; // TO IMPLEMENT -- SCENE CHANGE FADING
        [SerializeField] Image goodBattleEntry = null;
        [SerializeField] Image badBattleEntry = null;
        [SerializeField] Image neutralBattleEntry = null;
        [SerializeField] Image battleComplete = null;
        [Header("Fader Properties")]
        [SerializeField] float fadeInTimer = 2.0f;
        [SerializeField] float fadeOutTimer = 1.0f;

        // State
        Image currentTransition = null;
        bool fading = false;

        // Events
        public event Action battleCanvasEnabled;
        public event Action battleCanvasDisabled;

        private void Start()
        {
            ResetOverlays();
        }

        public void UpdateFadeState(TransitionType transitionType)
        {
            if (fading == false)
            {
                StartCoroutine(Fade(transitionType));
            }
        }

        private IEnumerator Fade(TransitionType transitionType)
        {
            yield return QueueFadeEntry(transitionType);
            
            if (transitionType == TransitionType.Zone) 
            { 
                // TODO:  Implement fade handling for scene transitions
                yield break; 
            }
            else if (transitionType == TransitionType.BattleComplete)
            {
                battleCanvas.gameObject.SetActive(false);
                if (battleCanvasDisabled != null)
                {
                    battleCanvasDisabled.Invoke();
                }
            }
            else
            {
                battleCanvas.gameObject.SetActive(true);
                if (battleCanvasEnabled != null)
                {
                    battleCanvasEnabled.Invoke();
                }
            }
            yield return QueueFadeExit();
        }

        private IEnumerator QueueFadeEntry(TransitionType transitionType)
        {
            fading = true;
            if (transitionType == TransitionType.BattleGood) 
            { 
                goodBattleEntry.gameObject.SetActive(true);
                currentTransition = goodBattleEntry;
            }
            else if (transitionType == TransitionType.BattleBad) 
            { 
                badBattleEntry.gameObject.SetActive(true);
                currentTransition = badBattleEntry;
            }
            else if (transitionType == TransitionType.BattleNeutral) 
            { 
                neutralBattleEntry.gameObject.SetActive(true);
                currentTransition = neutralBattleEntry;
            }
            else if (transitionType == TransitionType.BattleComplete)
            {
                battleComplete.gameObject.SetActive(true);
                currentTransition = battleComplete;
            }
            if (currentTransition == null) { fading = false; yield break; }

            currentTransition.CrossFadeAlpha(0f, 0f, true);
            currentTransition.CrossFadeAlpha(1, fadeInTimer, false);
            yield return new WaitForSeconds(fadeInTimer);
        }

        IEnumerator QueueFadeExit()
        {
            currentTransition.CrossFadeAlpha(0, fadeOutTimer, false);
            yield return new WaitForSeconds(fadeOutTimer);
            currentTransition.gameObject.SetActive(false);
            currentTransition = null;
            fading = false;
        }

        private void ResetOverlays()
        {
            battleCanvas.gameObject.SetActive(false);
            goodBattleEntry.gameObject.SetActive(false);
            badBattleEntry.gameObject.SetActive(false);
            neutralBattleEntry.gameObject.SetActive(false);
        }
    }
}
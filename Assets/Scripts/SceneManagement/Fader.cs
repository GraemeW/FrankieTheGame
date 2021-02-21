using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Frankie.SceneManagement
{
    public class Fader : MonoBehaviour
    {
        // Tunables
        [Header("Linked Assets")]
        [SerializeField] GameObject battleUIPrefab = null;
        [SerializeField] Image nodeEntry = null; // TODO: IMPLEMENT -- SCENE CHANGE FADING
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
        bool fadedInOnZoneTransition = false;
        GameObject battleUI = null;

        // Events
        public event Action<bool> battleUIStateChanged;

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
                // No special handling during fade -- managed by Zone scripts
            }
            else if (transitionType == TransitionType.BattleComplete)
            {
                battleUI.gameObject.SetActive(false);
                if (battleUIStateChanged != null)
                {
                    battleUIStateChanged.Invoke(false);
                }
                Destroy(battleUI.gameObject);
                battleUI = null;
            }
            else
            {
                if (battleUI == null) { battleUI = Instantiate(battleUIPrefab); }
                battleUI.gameObject.SetActive(true);
                if (battleUIStateChanged != null)
                {
                    battleUIStateChanged.Invoke(true);
                }
            }
            yield return QueueFadeExit(transitionType);
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
            else if (transitionType == TransitionType.Zone)
            {
                if (fadedInOnZoneTransition) { yield break; }
                nodeEntry.gameObject.SetActive(true);
                currentTransition = nodeEntry;
            }
            if (currentTransition == null) { fading = false; yield break; }

            currentTransition.CrossFadeAlpha(0f, 0f, true);
            currentTransition.CrossFadeAlpha(1, fadeInTimer, false);
            yield return new WaitForSeconds(fadeInTimer);
        }

        IEnumerator QueueFadeExit(TransitionType transitionType)
        {
            if (transitionType == TransitionType.Zone)
            {
                if (!fadedInOnZoneTransition)
                {
                    fadedInOnZoneTransition = true;
                    yield break; 
                }
            }

            currentTransition.CrossFadeAlpha(0, fadeOutTimer, false);
            yield return new WaitForSeconds(fadeOutTimer);
            currentTransition.gameObject.SetActive(false);
            currentTransition = null;
            fading = false;
        }

        private void ResetOverlays()
        {
            if (battleUI != null) { battleUI.gameObject.SetActive(false); }
            goodBattleEntry.gameObject.SetActive(false);
            badBattleEntry.gameObject.SetActive(false);
            neutralBattleEntry.gameObject.SetActive(false);
            nodeEntry.gameObject.SetActive(false);
        }

        public void FadeOutImmediate()
        {
            nodeEntry.gameObject.SetActive(true);
            currentTransition = nodeEntry;
            currentTransition.CrossFadeAlpha(1, 0f, true);
            fadedInOnZoneTransition = true;
        }
    }
}
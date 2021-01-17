using Frankie.Control;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Frankie.Core
{
    public class FaderTransition : MonoBehaviour
    {
        // Tunables
        [SerializeField] Canvas battleCanvas = null;
        [SerializeField] Image nodeEntry = null; // TO IMPLEMENT -- SCENE CHANGE FADING
        [SerializeField] Image goodBattleEntry = null;
        [SerializeField] Image badBattleEntry = null;
        [SerializeField] Image neutralBattleEntry = null;

        // Cached References
        PlayerController playerController = null;

        // State
        Image currentTransition = null;
        float faderTimeouts = 2.0f; // over-ride from player controller
        bool fading = false;

        private void Awake()
        {
            playerController = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>();
        }

        private void Start()
        {
            ResetOverlays();
            faderTimeouts = playerController.GetFaderTimeouts();
        }

        private void Update()
        {
            if (playerController.GetPlayerState() == PlayerController.PlayerState.inTransition && fading == false)
            {
                QueueFadeEntry(playerController.GetTransitionType());
            }

            if (fading == true && playerController.GetPlayerState() == PlayerController.PlayerState.inBattle)
            {
                battleCanvas.gameObject.SetActive(true);
                StartCoroutine(QueueFadeExit());
            }

            if (fading == true && playerController.GetPlayerState() == PlayerController.PlayerState.inWorld)
            {
                QueueFadeExit();
                StartCoroutine(QueueFadeExit());
            }
        }

        private void QueueFadeEntry(TransitionType transitionType)
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
            if (currentTransition == null) { return; }

            currentTransition.CrossFadeAlpha(0f, 0f, true);
            currentTransition.CrossFadeAlpha(1, faderTimeouts, false);
        }

        IEnumerator QueueFadeExit()
        {
            fading = false;
            currentTransition.CrossFadeAlpha(0, faderTimeouts, false);
            yield return new WaitForSeconds(faderTimeouts);
            currentTransition.gameObject.SetActive(false);
            currentTransition = null;
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
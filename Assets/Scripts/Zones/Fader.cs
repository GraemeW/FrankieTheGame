using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Frankie.Core;

namespace Frankie.ZoneManagement
{
    public class Fader : MonoBehaviour
    {
        // Tunables
        [Header("Linked Assets")]
        [SerializeField] GameObject battleUIPrefab = null;
        [SerializeField] Image nodeEntry = null;
        [SerializeField] Image goodBattleEntry = null;
        [SerializeField] Image badBattleEntry = null;
        [SerializeField] Image neutralBattleEntry = null;
        [SerializeField] Image battleComplete = null;
        [Header("Fader Properties")]
        [SerializeField] float fadeInTimer = 2.0f;
        [SerializeField] float fadeOutTimer = 1.0f;
        [SerializeField] float zoneFadeTimerMultiplier = 0.25f;

        // State
        Image currentTransition = null;
        bool fading = false;
        GameObject battleUI = null;

        // Cached References
        SceneLoader sceneLoader = null;
        SavingWrapper savingWrapper = null;

        // Events
        public event Action<TransitionType> fadingIn;
        public event Action<bool> battleUIStateChanged;
        public event Action fadingOut;

        // Static
        static string GLOBAL_SHADER_PHASE_REFERENCE = "_Phase";

        private void Awake()
        {
            sceneLoader = FindObjectOfType<SceneLoader>();
            savingWrapper = FindObjectOfType<SavingWrapper>();
        }

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

        public void UpdateFadeState(TransitionType transitionType, Zone nextZone)
        {
            if (fading == false)
            {
                StartCoroutine(Fade(transitionType, nextZone));
            }
        }

        public void UpdateFadeStateImmediate()
        {
            if (fading == false)
            {
                StartCoroutine(FadeImmediate());
            }
        }

        private IEnumerator Fade(TransitionType transitionType)
        {
            yield return QueueFadeEntry(transitionType);
            
            if (transitionType == TransitionType.BattleComplete)
            {
                battleUI.gameObject.SetActive(false);
                if (battleUIStateChanged != null)
                {
                    battleUIStateChanged.Invoke(false);
                }
                Destroy(battleUI.gameObject);
                battleUI = null;
            }
            else if (transitionType == TransitionType.BattleGood || transitionType == TransitionType.BattleNeutral || transitionType == TransitionType.BattleBad)
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

        private IEnumerator Fade(TransitionType transitionType, Zone zone)
        {
            if (transitionType == TransitionType.Zone)
            {
                yield return QueueFadeEntry(transitionType);

                savingWrapper.SaveSession(); // Save world state
                yield return sceneLoader.LoadNewSceneAsync(zone);

                savingWrapper.LoadSession(); // Load world state
                if (fadingOut != null)
                {
                    fadingOut.Invoke();
                }
                yield return QueueFadeExit(transitionType);
                savingWrapper.SaveSession();
            }
            yield break;
        }

        private IEnumerator FadeImmediate()
        {
            fading = true;
            nodeEntry.gameObject.SetActive(true);
            currentTransition = nodeEntry;
            currentTransition.CrossFadeAlpha(1, 0f, true);
            yield return QueueFadeExit(TransitionType.Zone);
        }

        private IEnumerator QueueFadeEntry(TransitionType transitionType)
        {
            fading = true;
            if (transitionType == TransitionType.BattleGood) 
            { 
                goodBattleEntry.gameObject.SetActive(true);
                currentTransition = goodBattleEntry;
                currentTransition.material.SetFloat(GLOBAL_SHADER_PHASE_REFERENCE, Time.time);
            }
            else if (transitionType == TransitionType.BattleBad) 
            { 
                badBattleEntry.gameObject.SetActive(true);
                currentTransition = badBattleEntry;
                currentTransition.material.SetFloat(GLOBAL_SHADER_PHASE_REFERENCE, Time.time);
            }
            else if (transitionType == TransitionType.BattleNeutral) 
            { 
                neutralBattleEntry.gameObject.SetActive(true);
                currentTransition = neutralBattleEntry;
                currentTransition.material.SetFloat(GLOBAL_SHADER_PHASE_REFERENCE, Time.time);
            }
            else if (transitionType == TransitionType.BattleComplete)
            {
                battleComplete.gameObject.SetActive(true);
                currentTransition = battleComplete;
            }
            else if (transitionType == TransitionType.Zone)
            {
                nodeEntry.gameObject.SetActive(true);
                currentTransition = nodeEntry;
            }
            if (currentTransition == null) { fading = false; yield break; }

            currentTransition.CrossFadeAlpha(0f, 0f, true);
            currentTransition.CrossFadeAlpha(1, GetFadeTime(true, transitionType), false);
            if (fadingIn != null)
            {
                fadingIn.Invoke(transitionType);
            }
            yield return new WaitForSeconds(GetFadeTime(true, transitionType));
        }

        private IEnumerator QueueFadeExit(TransitionType transitionType)
        {
            currentTransition.CrossFadeAlpha(0, GetFadeTime(false, transitionType), false);
            yield return new WaitForSeconds(GetFadeTime(false, transitionType));
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

        private float GetFadeTime(bool isFadeIn, TransitionType transitionType)
        {
            float fadeTime = 1.0f;
            if (isFadeIn) { fadeTime *= fadeInTimer; }
            else { fadeTime *= fadeOutTimer; }
            if (transitionType == TransitionType.Zone) { fadeTime *= zoneFadeTimerMultiplier; }

            return fadeTime;
        }
    }
}
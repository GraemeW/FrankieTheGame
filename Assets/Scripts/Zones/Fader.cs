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
        Action initiateBattleCallback = null;

        // Cached References
        SceneLoader sceneLoader = null;
        SavingWrapper savingWrapper = null;

        // Events
        public event Action<TransitionType> fadingIn;
        public event Action fadingPeak;
        public event Action fadingOut;

        #region UnityMethods
        private void Awake()
        {
            sceneLoader = FindAnyObjectByType<SceneLoader>();
            savingWrapper = FindAnyObjectByType<SavingWrapper>();
        }

        private void Start()
        {
            ResetOverlays();
        }
        #endregion

        #region PublicMethods
        public bool IsFading() => fading;
        public void QueueInitiateBattleCallback(Action initiateBattleCallback) => this.initiateBattleCallback = initiateBattleCallback;

        public void UpdateFadeState(TransitionType transitionType, Zone nextZone)
        {
            // Non-IEnumerator Type for Scene Transitions:
            // Coroutine needs to exist on an object that will persist between scenes
            if (fading == false)
            {
                StartCoroutine(Fade(transitionType, nextZone));
            }
        }

        public void UpdateFadeStateImmediate()
        {
            // Coroutine needs to exist on an object that will persist between scenes
            if (fading == false)
            {
                StartCoroutine(FadeImmediate());
            }
        }

        public IEnumerator QueueFadeEntry(TransitionType transitionType)
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
                nodeEntry.gameObject.SetActive(true);
                currentTransition = nodeEntry;
            }
            if (currentTransition == null) { fading = false; yield break; }

            currentTransition.CrossFadeAlpha(0f, 0f, true);
            currentTransition.CrossFadeAlpha(1, GetFadeTime(true, transitionType), false);
            fadingIn?.Invoke(transitionType);
            yield return new WaitForSeconds(GetFadeTime(true, transitionType));
            fadingPeak?.Invoke();

            if (transitionType == TransitionType.BattleComplete)
            {
                battleUI.gameObject.SetActive(false);
                Destroy(battleUI.gameObject);
                battleUI = null;
            }
        }

        public IEnumerator QueueFadeExit(TransitionType transitionType)
        {
            if (transitionType == TransitionType.BattleGood || transitionType == TransitionType.BattleNeutral || transitionType == TransitionType.BattleBad)
            {
                if (battleUI == null) { battleUI = Instantiate(battleUIPrefab); }
                battleUI.gameObject.SetActive(true);
                if (initiateBattleCallback != null)
                {
                    initiateBattleCallback.Invoke();
                    initiateBattleCallback = null;
                }
            }

            currentTransition.CrossFadeAlpha(0, GetFadeTime(false, transitionType), false);
            yield return new WaitForSeconds(GetFadeTime(false, transitionType));
            currentTransition.gameObject.SetActive(false);
            currentTransition = null;
            fading = false;
        }
        #endregion

        #region PrivateMethods
        private IEnumerator Fade(TransitionType transitionType, Zone zone)
        {
            if (transitionType == TransitionType.Zone)
            {
                yield return QueueFadeEntry(transitionType);

                savingWrapper.SaveSession(); // Save world state
                yield return sceneLoader.LoadNewSceneAsync(zone);

                savingWrapper.LoadSession(); // Load world state
                fadingOut?.Invoke();

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

        private void ResetOverlays()
        {
            battleUI?.gameObject.SetActive(false);
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
        #endregion
    }
}
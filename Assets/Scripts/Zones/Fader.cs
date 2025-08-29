using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Frankie.Core;
using Frankie.Utils;

namespace Frankie.ZoneManagement
{
    public class Fader : MonoBehaviour
    {
        // Tunables
        [Header("Linked Assets")]
        [SerializeField] GameObject battleUIPrefab = null;
        [SerializeField] ShaderPropertySetter nodeEntry = null;
        [SerializeField] ShaderPropertySetter goodBattleEntry = null;
        [SerializeField] ShaderPropertySetter badBattleEntry = null;
        [SerializeField] ShaderPropertySetter neutralBattleEntry = null;
        [SerializeField] ShaderPropertySetter battleComplete = null;
        [Header("Fader Properties")]
        [SerializeField] float fadeInTimer = 2.0f;
        [SerializeField] float fadeOutTimer = 1.0f;
        [SerializeField] float zoneFadeTimerMultiplier = 0.25f;

        // State
        ShaderPropertySetter currentTransition = null;
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

            switch (transitionType)
            {
                case TransitionType.Zone:
                    nodeEntry.gameObject.SetActive(true);
                    currentTransition = nodeEntry;
                    break;
                case TransitionType.BattleGood:
                    goodBattleEntry.gameObject.SetActive(true);
                    currentTransition = goodBattleEntry;
                    break;
                case TransitionType.BattleBad:
                    badBattleEntry.gameObject.SetActive(true);
                    currentTransition = badBattleEntry;
                    break;
                case TransitionType.BattleNeutral:
                    neutralBattleEntry.gameObject.SetActive(true);
                    currentTransition = neutralBattleEntry;
                    break;
                case TransitionType.BattleComplete:
                    battleComplete.gameObject.SetActive(true);
                    currentTransition = battleComplete;
                    break;
                case TransitionType.None:
                default:
                    fading = false;
                    yield break;
            }

            AlphaFadeIn(transitionType);
            yield return new WaitForSeconds(GetFadeTime(true, transitionType));
            fadingPeak?.Invoke();

            if (transitionType == TransitionType.BattleComplete)
            {
                battleUI.gameObject.SetActive(false);
                Destroy(battleUI.gameObject);
                battleUI = null;
            }
        }

        private void AlphaFadeIn(TransitionType transitionType)
        {
            switch (transitionType)
            {
                case TransitionType.BattleGood:
                    goodBattleEntry.SetFadeTime(GetFadeTime(true, transitionType));
                    break;
                case TransitionType.BattleBad:
                    badBattleEntry.SetFadeTime(GetFadeTime(true, transitionType));
                    break;
                case TransitionType.BattleNeutral:
                    neutralBattleEntry.SetFadeTime(GetFadeTime(true, transitionType));
                    break;
                case TransitionType.Zone:
                case TransitionType.BattleComplete:
                    currentTransition.GetImage().CrossFadeAlpha(0f, 0f, true);
                    currentTransition.GetImage().CrossFadeAlpha(1, GetFadeTime(true, transitionType), false);
                    break;
                default:
                case TransitionType.None:
                    return;
            }
            fadingIn?.Invoke(transitionType);
        }

        private void AlphaFadeOut(TransitionType transitionType)
        {
            Image currentTransitionImage = currentTransition.GetImage();
            if (currentTransitionImage != null)
            {
                currentTransitionImage.CrossFadeAlpha(0, GetFadeTime(false, transitionType), false);
            }
            if (transitionType != TransitionType.Zone) { fadingOut?.Invoke(); } // invoked separately for zone transitions
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

            AlphaFadeOut(transitionType);
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

            Image currentTransitionImage = currentTransition.GetImage();
            if (currentTransitionImage != null) { currentTransitionImage.CrossFadeAlpha(1, 0f, true); }
            yield return QueueFadeExit(TransitionType.Zone);
        }

        private void ResetOverlays()
        {
            battleUI?.gameObject.SetActive(false);
            nodeEntry?.gameObject.SetActive(false);
            goodBattleEntry?.gameObject.SetActive(false);
            badBattleEntry?.gameObject.SetActive(false);
            neutralBattleEntry?.gameObject.SetActive(false);
            battleComplete?.gameObject.SetActive(false);
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
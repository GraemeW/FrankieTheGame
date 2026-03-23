using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Frankie.Core;
using Frankie.Rendering;

namespace Frankie.ZoneManagement
{
    [RequireComponent(typeof(BattleEntryShaderControl))]
    public class Fader : MonoBehaviour
    {
        // Tunables
        [Header("Linked Assets")]
        [SerializeField] private Image nodeEntry;
        [SerializeField] private Image battleComplete;
        [Header("Fader Properties")]
        [SerializeField] private float fadeInTimer = 2.0f;
        [SerializeField] private float fadeOutTimer = 1.0f;
        [SerializeField] private float zoneFadeTimerMultiplier = 0.25f;

        // Static State
        private static Fader _activeFader;
        
        // State
        private bool fading;
        private Coroutine activeFade;
        private Image currentTransitionImage;

        // Events
        public event Action<TransitionType> fadingIn;
        
        // Cached References
        private BattleEntryShaderControl battleEntryShaderControl;

        #region StaticFind
        private const string _faderTag = "Fader";
        private static Fader FindFader()
        {
            var faderGameObject = GameObject.FindGameObjectWithTag(_faderTag);
            return faderGameObject != null ? faderGameObject.GetComponent<Fader>() : null;
        }

        public static bool StartStandardFade(TransitionType transitionType, FaderEventTriggers faderEventTriggers, bool overrideActiveFade = true)
        {
            if (_activeFader == null) { _activeFader = FindFader(); }
            if (_activeFader == null) { return false; }
            if (!overrideActiveFade && _activeFader.IsFading()) { return false; }
            if (transitionType == TransitionType.Zone) { Debug.Log("Notice:  Use StartZoneFade() instead!"); return false; }
            
            _activeFader.InitiateStandardFadeCoroutine(transitionType, faderEventTriggers);
            return true;
        }

        public static bool StartBlipFade(float holdSeconds, FaderEventTriggers faderEventTriggers)
        {
            if (_activeFader == null) { _activeFader = FindFader(); }
            if (_activeFader == null) { return false; }
            if (_activeFader.IsFading()) { return false; }
            
            _activeFader.InitiateBlipFadeCoroutine(holdSeconds, faderEventTriggers);
            return true;
        }

        public static bool StartZoneFade(Zone nextZone, FaderEventTriggers faderEventTriggers, bool saveSession = true, bool overrideActiveFade = true)
        {
            if (_activeFader == null) { _activeFader = FindFader(); }
            if (_activeFader == null) { return false; }
            if (!overrideActiveFade && _activeFader.IsFading()) { return false; }
            
            _activeFader.InitiateZoneFadeCoroutine(nextZone, faderEventTriggers, saveSession);
            return true;
        }

        public static bool StartQuickZoneFade()
        {
            if (_activeFader == null) { _activeFader = FindFader(); }
            if (_activeFader == null) { return false; }
            
            _activeFader.InitiateQuickZoneFadeCoroutine();
            return true;
        }
        #endregion

        #region UnityMethods
        private void Awake()
        {
            battleEntryShaderControl = GetComponent<BattleEntryShaderControl>();
            
            // Fader is included in PersistentObjects and thus a singleton by standard implementation
            // So:  establish fader in static state for public method calls
            _activeFader = this;
        }

        private void Start()
        {
            ResetOverlays();
        }

        private void OnDisable()
        {
            if (activeFade != null) { StopCoroutine(activeFade); }
        }
        #endregion

        #region Getters
        private bool IsFading() => fading;
        
        private float GetFadeTime(bool isFadeIn, TransitionType transitionType)
        {
            float fadeTime = 1.0f;
            if (isFadeIn) { fadeTime *= fadeInTimer; }
            else { fadeTime *= fadeOutTimer; }
            if (transitionType == TransitionType.Zone) { fadeTime *= zoneFadeTimerMultiplier; }

            return fadeTime;
        }
        #endregion
        
        #region CoroutineInitiators
        private void InitiateStandardFadeCoroutine(TransitionType transitionType, FaderEventTriggers faderEventTriggers)
        {
            if (activeFade != null) { StopCoroutine(activeFade); }
            activeFade = StartCoroutine(StandardFade(transitionType, faderEventTriggers));
        }
        
        private void InitiateZoneFadeCoroutine(Zone nextZone, FaderEventTriggers faderEventTriggers, bool saveSession = true)
        {
            if (activeFade != null) { StopCoroutine(activeFade); }
            activeFade = StartCoroutine(ZoneFade(nextZone, faderEventTriggers, saveSession));
        }

        private void InitiateQuickZoneFadeCoroutine()
        {
            if (activeFade != null) { StopCoroutine(activeFade); }
            activeFade = StartCoroutine(QuickFade());
        }

        private void InitiateBlipFadeCoroutine(float holdSeconds, FaderEventTriggers faderEventTriggers)
        {
            if (activeFade != null) { StopCoroutine(activeFade); }
            activeFade = StartCoroutine(BlipFade(holdSeconds, faderEventTriggers));
        }
        #endregion

        #region Coroutines
        private IEnumerator StandardFade(TransitionType transitionType, FaderEventTriggers faderEventTriggers)
        {
            yield return QueueFadeEntry(transitionType, faderEventTriggers.onFadeIn, faderEventTriggers.onFadePeak);
            yield return QueueFadeExit(transitionType, faderEventTriggers.onFadeOut, faderEventTriggers.onFadeComplete);
        }
        
        private IEnumerator ZoneFade(Zone zone, FaderEventTriggers faderEventTriggers, bool saveSession = true)
        {
            fading = true;
            yield return QueueFadeEntry(TransitionType.Zone, faderEventTriggers.onFadeIn, faderEventTriggers.onFadePeak);
            if (saveSession) { SavingWrapper.SaveSession(); }
            yield return SceneLoader.LoadNewSceneAsync(zone);

            if (saveSession) { SavingWrapper.LoadSession(); }

            yield return QueueFadeExit(TransitionType.Zone, faderEventTriggers.onFadeOut, faderEventTriggers.onFadeComplete);
            if (saveSession) { SavingWrapper.SaveSession(); }
        }

        private IEnumerator QuickFade()
        {
            fading = true;
            nodeEntry.gameObject.SetActive(true);
            currentTransitionImage = nodeEntry;

            if (currentTransitionImage != null) { currentTransitionImage.CrossFadeAlpha(1, 0f, true); }
            yield return QueueFadeExit(TransitionType.Zone, null, null);
        }

        private IEnumerator BlipFade(float holdSeconds, FaderEventTriggers faderEventTriggers)
        {
            // Re-use Zone-based fading (black screen)
            fading = true;
            yield return QueueFadeEntry(TransitionType.Zone, faderEventTriggers.onFadeIn, faderEventTriggers.onFadePeak);
            yield return new WaitForSeconds(holdSeconds);
            yield return QueueFadeExit(TransitionType.Zone, faderEventTriggers.onFadeOut, faderEventTriggers.onFadeComplete);
        }
        
        private IEnumerator QueueFadeEntry(TransitionType transitionType, Action<TransitionType> onFadeIn, Action onFadePeak)
        {
            switch (transitionType)
            {
                case TransitionType.Zone:
                    nodeEntry.gameObject.SetActive(true);
                    currentTransitionImage = nodeEntry;
                    break;
                case TransitionType.BattleComplete:
                    battleComplete.gameObject.SetActive(true);
                    currentTransitionImage = battleComplete;
                    break;
                case TransitionType.BattleGood:
                case TransitionType.BattleBad:
                case TransitionType.BattleNeutral:
                    break;
                case TransitionType.None:
                default:
                    fading = false;
                    yield break;
            }

            AlphaFadeIn(transitionType);
            fadingIn?.Invoke(transitionType);
            onFadeIn?.Invoke(transitionType);
            yield return new WaitForSeconds(GetFadeTime(true, transitionType));
            onFadePeak?.Invoke();
        }

        private IEnumerator QueueFadeExit(TransitionType transitionType, Action onFadeOut, Action onFadeComplete)
        {
            // Note:  order of operations for alpha fading slightly different on zone fades
            if (transitionType == TransitionType.Zone) { onFadeOut?.Invoke(); }
            AlphaFadeOut(transitionType);
            if (transitionType != TransitionType.Zone) { onFadeOut?.Invoke(); }
            
            yield return new WaitForSeconds(GetFadeTime(false, transitionType));
            CleanUpTransitionBlends(transitionType);
            fading = false;
            onFadeComplete?.Invoke();
        }
        #endregion

        #region AlphaBlends
        private void ResetOverlays()
        {
            nodeEntry?.gameObject.SetActive(false);
            battleComplete?.gameObject.SetActive(false);
            battleEntryShaderControl?.EndFade();
        }
        
        private void AlphaFadeIn(TransitionType transitionType)
        {
            switch (transitionType)
            {
                case TransitionType.BattleGood:
                case TransitionType.BattleBad:
                case TransitionType.BattleNeutral:
                    battleEntryShaderControl.SetBattleEntryParameters(transitionType, GetFadeTime(true, transitionType), GetFadeTime(false, transitionType));
                    battleEntryShaderControl.StartFadeIn();
                    break;
                case TransitionType.Zone:
                case TransitionType.BattleComplete:
                    currentTransitionImage.CrossFadeAlpha(0f, 0f, true);
                    currentTransitionImage.CrossFadeAlpha(1, GetFadeTime(true, transitionType), false);
                    break;
                default:
                case TransitionType.None:
                    return;
            }
        }

        private void AlphaFadeOut(TransitionType transitionType)
        {
            switch (transitionType)
            {
                case TransitionType.BattleGood:
                case TransitionType.BattleBad:
                case TransitionType.BattleNeutral:
                    battleEntryShaderControl.StartFadeOut();
                    break;
                case TransitionType.Zone:
                case TransitionType.BattleComplete:
                    currentTransitionImage.CrossFadeAlpha(0, GetFadeTime(false, transitionType), false);
                    break;
                case TransitionType.None:
                default:
                    return;
            }
        }
        private void CleanUpTransitionBlends(TransitionType transitionType)
        {
            switch (transitionType)
            {
                case TransitionType.BattleGood:
                case TransitionType.BattleBad:
                case TransitionType.BattleNeutral:
                    battleEntryShaderControl.EndFade();
                    break;
                case TransitionType.Zone:
                case TransitionType.BattleComplete:
                    currentTransitionImage?.gameObject.SetActive(false);
                    break;
                case TransitionType.None:
                default:
                    return;
            }
            currentTransitionImage = null;
        }
        #endregion
    }
}

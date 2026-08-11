using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Frankie.ZoneManagement
{
    public abstract class FaderBase<T> : MonoBehaviour where T : struct, Enum
    {
        // Tunables
        [Header("Linked Assets")]
        [SerializeField] protected Image nodeEntry;
        [Header("Fader Properties")]
        [SerializeField] private float fadeInTimer = 2.0f;
        [SerializeField] private float fadeOutTimer = 1.0f;
        [SerializeField] private float zoneFadeTimerMultiplier = 0.25f;

        // Const
        private const string _faderTag = "Fader";
        
        // Static State
        private static FaderBase<T> _activeFader;
        
        // State
        protected bool fading;
        protected Image currentTransitionImage;
        private Coroutine activeFade;

        // Events
        public event Action<T> fadingIn;

        #region StaticCallers
        public static bool StartStandardFade(T transitionType, FaderEventTriggers<T> faderEventTriggers)
        {
            if (_activeFader == null) { _activeFader = TryFindFader(); }
            if (_activeFader == null || _activeFader.IsFading()) { return false; }
            
            _activeFader.InitiateStandardFadeCoroutine(transitionType, faderEventTriggers);
            return true;
        }
        
        public static bool StartBlipFade(float holdSeconds, FaderEventTriggers<T> faderEventTriggers)
        {
            if (_activeFader == null) { _activeFader = TryFindFader(); }
            if (_activeFader == null || _activeFader.IsFading()) { return false; }
            
            _activeFader.InitiateBlipFadeCoroutine(holdSeconds, faderEventTriggers);
            return true;
        }

        public static bool StartSceneLoadFade(Zone nextZone, bool saveSession = true)
        {
            var faderEventTriggers = new FaderEventTriggers<T>();
            return StartSceneLoadFade(nextZone, faderEventTriggers, saveSession);
        }

        public static bool StartSceneLoadFade(Zone nextZone, FaderEventTriggers<T> faderEventTriggers, bool saveSession = true)
        {
            if (_activeFader == null) { _activeFader = TryFindFader(); }
            if (_activeFader == null || _activeFader.IsFading()) { return false; }
            if (_activeFader is not Fader fader) { return false; }
            
            _activeFader.InitiateSceneLoadFadeCoroutine(nextZone, faderEventTriggers, saveSession);
            return true;
        }

        public static bool StartQuickSceneLoadFade()
        {
            if (_activeFader == null) { _activeFader = TryFindFader(); }
            if (_activeFader == null) { return false; }
            
            _activeFader.InitiateSceneLoadZoneFadeCoroutine();
            return true;
        }

        private static FaderBase<T> TryFindFader()
        {
            var faderGameObject = GameObject.FindGameObjectWithTag(_faderTag);
            return faderGameObject != null ? faderGameObject.GetComponent<FaderBase<T>>() : null;
        }
        #endregion

        #region UnityMethods
        private void OnEnable()
        {
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
        protected virtual bool IsSceneLoadFade(T transitionType) => true;
        
        protected float GetFadeTime(bool isFadeIn, T transitionType)
        {
            float fadeTime = 1.0f;
            if (isFadeIn) { fadeTime *= fadeInTimer; }
            else { fadeTime *= fadeOutTimer; }
            if (IsSceneLoadFade(transitionType)) { fadeTime *= zoneFadeTimerMultiplier; }

            return fadeTime;
        }
        #endregion
        
        #region CoroutineInitiators
        private void InitiateStandardFadeCoroutine(T transitionType, FaderEventTriggers<T> faderEventTriggers)
        {
            if (activeFade != null) { StopCoroutine(activeFade); }
            activeFade = StartCoroutine(StandardFade(transitionType, faderEventTriggers));
        }
        
        private void InitiateSceneLoadFadeCoroutine(Zone nextZone, FaderEventTriggers<T> faderEventTriggers, bool saveSession = true)
        {
            if (activeFade != null) { StopCoroutine(activeFade); }
            activeFade = StartCoroutine(ZoneFade(nextZone, faderEventTriggers, saveSession));
        }

        private void InitiateSceneLoadZoneFadeCoroutine()
        {
            if (activeFade != null) { StopCoroutine(activeFade); }
            activeFade = StartCoroutine(QuickFade());
        }

        private void InitiateBlipFadeCoroutine(float holdSeconds, FaderEventTriggers<T> faderEventTriggers)
        {
            if (activeFade != null) { StopCoroutine(activeFade); }
            activeFade = StartCoroutine(BlipFade(holdSeconds, faderEventTriggers));
        }
        #endregion

        #region Coroutines
        protected abstract T GetSceneLoadTransitionType();
        protected abstract void TriggerSave();
        protected abstract void TriggerLoad();
        
        protected virtual bool PreFadeSetup(T transitionType)
        {
            nodeEntry.gameObject.SetActive(true);
            currentTransitionImage = nodeEntry;
            return true;
        }
        
        private IEnumerator StandardFade(T transitionType, FaderEventTriggers<T> faderEventTriggers)
        {
            yield return QueueFadeEntry(transitionType, faderEventTriggers.onFadeIn, faderEventTriggers.onFadePeak);
            yield return QueueFadeExit(transitionType, faderEventTriggers.onFadeOut, faderEventTriggers.onFadeComplete);
        }
        
        private IEnumerator ZoneFade(Zone zone, FaderEventTriggers<T> faderEventTriggers, bool shouldSaveSession = true)
        {
            fading = true;
            yield return QueueFadeEntry(GetSceneLoadTransitionType(), faderEventTriggers.onFadeIn, faderEventTriggers.onFadePeak);
            if (shouldSaveSession) { TriggerSave(); }
            
            yield return SceneLoader.LoadNewSceneAsync(zone);
            
            if (shouldSaveSession) { TriggerLoad(); }
            yield return QueueFadeExit(GetSceneLoadTransitionType(), faderEventTriggers.onFadeOut, faderEventTriggers.onFadeComplete);
            if (shouldSaveSession) { TriggerSave(); }
        }

        private IEnumerator QuickFade()
        {
            fading = true;
            nodeEntry.gameObject.SetActive(true);
            currentTransitionImage = nodeEntry;

            if (currentTransitionImage != null) { currentTransitionImage.CrossFadeAlpha(1, 0f, true); }
            yield return QueueFadeExit(GetSceneLoadTransitionType(), null, null);
        }

        private IEnumerator BlipFade(float holdSeconds, FaderEventTriggers<T> faderEventTriggers)
        {
            // Re-use Zone-based fading (black screen)
            fading = true;
            yield return QueueFadeEntry(GetSceneLoadTransitionType(), faderEventTriggers.onFadeIn, faderEventTriggers.onFadePeak);
            yield return new WaitForSeconds(holdSeconds);
            yield return QueueFadeExit(GetSceneLoadTransitionType(), faderEventTriggers.onFadeOut, faderEventTriggers.onFadeComplete);
        }
        
        private IEnumerator QueueFadeEntry(T transitionType, Action<T> onFadeIn, Action onFadePeak)
        {
            if (!PreFadeSetup(transitionType)) { yield break; }

            AlphaFadeIn(transitionType);
            fadingIn?.Invoke(transitionType);
            onFadeIn?.Invoke(transitionType);
            yield return new WaitForSeconds(GetFadeTime(true, transitionType));
            onFadePeak?.Invoke();
        }
        
        private IEnumerator QueueFadeExit(T transitionType, Action onFadeOut, Action onFadeComplete)
        {
            // Note:  order of operations for alpha fading slightly different on zone fades
            if (IsSceneLoadFade(transitionType)) { onFadeOut?.Invoke(); }
            AlphaFadeOut(transitionType);
            if (!IsSceneLoadFade(transitionType)) { onFadeOut?.Invoke(); }
            
            yield return new WaitForSeconds(GetFadeTime(false, transitionType));
            CleanUpTransitionBlends(transitionType);
            fading = false;
            onFadeComplete?.Invoke();
        }
        #endregion

        #region AlphaBlends
        protected virtual void ResetOverlays()
        {
            nodeEntry?.gameObject.SetActive(false);
        }

        protected virtual bool IsSkipFade(T transitionType) => false;
        protected virtual bool TransitionUsesStandaloneFadeControl(T transitionType) => false;

        protected virtual void TriggerStandaloneFadeIn(T transitionType) { } 
        
        protected virtual void TriggerStandaloneFadeOut(T transitionType) { }
        
        protected virtual void TriggerStandaloneFadeCleanup() { }
        
        private void AlphaFadeIn(T transitionType)
        {
            if (IsSkipFade(transitionType)) { return; }
            if (TransitionUsesStandaloneFadeControl(transitionType)) { TriggerStandaloneFadeIn(transitionType); return; }
            
            currentTransitionImage.CrossFadeAlpha(0f, 0f, true);
            currentTransitionImage.CrossFadeAlpha(1, GetFadeTime(true, transitionType), false);
        }

        private void AlphaFadeOut(T transitionType)
        {
            if (IsSkipFade(transitionType)) { return; }
            if (TransitionUsesStandaloneFadeControl(transitionType)) { TriggerStandaloneFadeOut(transitionType); return; }
            
            currentTransitionImage.CrossFadeAlpha(0, GetFadeTime(false, transitionType), false);
        }
        private void CleanUpTransitionBlends(T transitionType)
        {
            if (IsSkipFade(transitionType)) { return; }
            if (TransitionUsesStandaloneFadeControl(transitionType)) { TriggerStandaloneFadeCleanup(); }
            else { currentTransitionImage?.gameObject.SetActive(false); }
            
            currentTransitionImage = null;
        }
        #endregion
    }
}

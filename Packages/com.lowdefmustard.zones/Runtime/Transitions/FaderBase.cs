using System;
using System.Collections;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using LowDefMustard.Utils;

namespace LowDefMustard.Zones
{
    public abstract partial class FaderBase<TTransitionType> : MonoBehaviour where TTransitionType : struct, Enum
    {
        // Note:  Internal fields/methods for test visibility
        // Note:  View-agnostic ~ the fade overlay(s) are owned by the implementer (see SetOverlayAlpha / ResetOverlays)
        
        // Tunables
        [Header("Hookups")]
        [SerializeField] private SceneLoaderBase sceneLoader;
        [Header("Fader Properties")]
        [SerializeField] internal float fadeInTimer = 2.0f;
        [SerializeField] internal float fadeOutTimer = 1.0f;
        [SerializeField] internal float zoneFadeTimerMultiplier = 0.25f;

        // Const
        private const string _faderTag = "Fader";
        
        // Static State
        [AutoStaticsCleanup] internal static FaderBase<TTransitionType> activeFader;
        
        // State
        protected bool fading;
        private Coroutine activeFade;
        
        // Cached References
        private ReInitLazyValue<SceneLoaderBase> activeSceneLoader;

        // Events
        public event Action<TTransitionType> fadingIn;

        #region StaticCallers
        public static bool StartStandardFade(TTransitionType transitionType, FaderEventTriggers<TTransitionType> faderEventTriggers)
        {
            if (activeFader == null) { activeFader = TryFindFader(); }
            if (activeFader == null || activeFader.IsFading()) { return false; }
            
            activeFader.InitiateStandardFadeCoroutine(transitionType, faderEventTriggers);
            return true;
        }
        
        public static bool StartBlipFade(float holdSeconds, FaderEventTriggers<TTransitionType> faderEventTriggers)
        {
            if (activeFader == null) { activeFader = TryFindFader(); }
            if (activeFader == null || activeFader.IsFading()) { return false; }
            
            activeFader.InitiateBlipFadeCoroutine(holdSeconds, faderEventTriggers);
            return true;
        }

        public static bool StartSceneLoadFade(Zone nextZone, bool saveSession = true)
        {
            var faderEventTriggers = new FaderEventTriggers<TTransitionType>();
            return StartSceneLoadFade(nextZone, faderEventTriggers, saveSession);
        }

        public static bool StartSceneLoadFade(Zone nextZone, FaderEventTriggers<TTransitionType> faderEventTriggers, bool saveSession = true)
        {
            if (activeFader == null) { activeFader = TryFindFader(); }
            if (activeFader == null || activeFader.IsFading()) { return false; }
            
            activeFader.InitiateSceneLoadFadeCoroutine(nextZone, faderEventTriggers, saveSession);
            return true;
        }

        public static bool StartQuickSceneLoadFade()
        {
            if (activeFader == null) { activeFader = TryFindFader(); }
            if (activeFader == null) { return false; }
            
            activeFader.InitiateSceneLoadZoneFadeCoroutine();
            return true;
        }

        private static FaderBase<TTransitionType> TryFindFader()
        {
            var faderGameObject = GameObject.FindGameObjectWithTag(_faderTag);
            return faderGameObject != null ? faderGameObject.GetComponent<FaderBase<TTransitionType>>() : null;
        }
        #endregion

        #region UnityMethods
        protected virtual void Awake()
        {
            activeSceneLoader ??= new ReInitLazyValue<SceneLoaderBase>(FindSceneLoader);
        }

        protected virtual void Start()
        {
            ResetOverlays();
            activeSceneLoader.ForceInit();
        }
        
        protected virtual void OnEnable()
        {
            // Fader is included in PersistentObjects and thus a singleton by standard implementation
            // So:  establish fader in static state for public method calls
            activeFader = this;
            TrySubscribeToSceneLoader(true);
        }

        protected virtual void OnDisable()
        {
            StopActiveFade();
            TrySubscribeToSceneLoader(false);
        }

        // Ideally, just hook up the scene loader directly in persistent objects - if for some reason this doesn't work, fallback to finder
        private SceneLoaderBase FindSceneLoader() => sceneLoader != null ? sceneLoader : SceneLoaderBase.FindSceneLoader();

        private void TrySubscribeToSceneLoader(bool enable)
        {
            activeSceneLoader ??= new ReInitLazyValue<SceneLoaderBase>(FindSceneLoader);
            if (!activeSceneLoader.TryGetSafely(out SceneLoaderBase sceneLoaderInstance, allowReInit: enable)) { return; }

            sceneLoaderInstance.sceneLoadFadeProvider -= InitiateSceneLoadFadeCoroutine;
            if (enable) { sceneLoaderInstance.sceneLoadFadeProvider += InitiateSceneLoadFadeCoroutine; }
        }
        #endregion

        #region Getters
        private bool IsFading() => fading;
        protected virtual bool IsSceneLoadFade(TTransitionType transitionType) => true;
        
        protected float GetFadeTime(bool isFadeIn, TTransitionType transitionType)
        {
            float fadeTime = 1.0f;
            if (isFadeIn) { fadeTime *= fadeInTimer; }
            else { fadeTime *= fadeOutTimer; }
            if (IsSceneLoadFade(transitionType)) { fadeTime *= zoneFadeTimerMultiplier; }

            return fadeTime;
        }
        #endregion
        
        #region CoroutineInitiators
        private void InitiateStandardFadeCoroutine(TTransitionType transitionType, FaderEventTriggers<TTransitionType> faderEventTriggers)
        {
            StopActiveFade();
            activeFade = StartCoroutine(StandardFade(transitionType, faderEventTriggers));
        }

        private void InitiateSceneLoadFadeCoroutine(Zone nextZone, bool saveSession = true)
        {
            var faderEventTriggers = new FaderEventTriggers<TTransitionType>();
            InitiateSceneLoadFadeCoroutine(nextZone, faderEventTriggers, saveSession);
        }
        
        private void InitiateSceneLoadFadeCoroutine(Zone nextZone, FaderEventTriggers<TTransitionType> faderEventTriggers, bool saveSession = true)
        {
            StopActiveFade();
            activeFade = StartCoroutine(ZoneFade(nextZone, faderEventTriggers, saveSession));
        }

        private void InitiateSceneLoadZoneFadeCoroutine()
        {
            StopActiveFade();
            activeFade = StartCoroutine(QuickFade());
        }

        private void InitiateBlipFadeCoroutine(float holdSeconds, FaderEventTriggers<TTransitionType> faderEventTriggers)
        {
            StopActiveFade();
            activeFade = StartCoroutine(BlipFade(holdSeconds, faderEventTriggers));
        }

        private void StopActiveFade()
        {
            if (activeFade != null) { StopCoroutine(activeFade); }
            activeFade = null;
            if (!fading) { return; }

            // Note:  An interrupted fade never reaches its own cleanup ~ without this its overlay would stay up (and keep blocking input)
            fading = false;
            ResetOverlays();
        }
        #endregion

        #region Coroutines
        protected abstract TTransitionType GetSceneLoadTransitionType();
        protected abstract void TriggerSave();
        protected abstract void TriggerLoad();
        
        protected virtual bool PreFadeSetup(TTransitionType transitionType) => true;
        
        private IEnumerator StandardFade(TTransitionType transitionType, FaderEventTriggers<TTransitionType> faderEventTriggers)
        {
            fading = true;
            yield return QueueFadeEntry(transitionType, faderEventTriggers.onFadeIn, faderEventTriggers.onFadePeak);
            yield return QueueFadeExit(transitionType, faderEventTriggers.onFadeOut, faderEventTriggers.onFadeComplete);
        }
        
        private IEnumerator ZoneFade(Zone zone, FaderEventTriggers<TTransitionType> faderEventTriggers, bool shouldSaveSession = true)
        {
            fading = true;
            yield return QueueFadeEntry(GetSceneLoadTransitionType(), faderEventTriggers.onFadeIn, faderEventTriggers.onFadePeak);
            if (shouldSaveSession) { TriggerSave(); }
            
            yield return SceneLoaderBase.LoadNewSceneAsync(zone);
            
            if (shouldSaveSession) { TriggerLoad(); }
            yield return QueueFadeExit(GetSceneLoadTransitionType(), faderEventTriggers.onFadeOut, faderEventTriggers.onFadeComplete);
            if (shouldSaveSession) { TriggerSave(); }
        }

        private IEnumerator QuickFade()
        {
            fading = true;
            SetOverlayAlpha(GetSceneLoadTransitionType(), 1f);
            yield return QueueFadeExit(GetSceneLoadTransitionType(), null, null);
        }

        private IEnumerator BlipFade(float holdSeconds, FaderEventTriggers<TTransitionType> faderEventTriggers)
        {
            // Re-use Zone-based fading (black screen)
            fading = true;
            yield return QueueFadeEntry(GetSceneLoadTransitionType(), faderEventTriggers.onFadeIn, faderEventTriggers.onFadePeak);
            yield return new WaitForSeconds(holdSeconds);
            yield return QueueFadeExit(GetSceneLoadTransitionType(), faderEventTriggers.onFadeOut, faderEventTriggers.onFadeComplete);
        }
        
        private IEnumerator QueueFadeEntry(TTransitionType transitionType, Action<TTransitionType> onFadeIn, Action onFadePeak)
        {
            if (!PreFadeSetup(transitionType)) { yield break; }

            if (UsesStandaloneFade(transitionType)) { TriggerStandaloneFadeIn(transitionType); }
            fadingIn?.Invoke(transitionType);
            onFadeIn?.Invoke(transitionType);
            yield return FadeOverlay(transitionType, 0f, 1f, GetFadeTime(true, transitionType));
            onFadePeak?.Invoke();
        }
        
        private IEnumerator QueueFadeExit(TTransitionType transitionType, Action onFadeOut, Action onFadeComplete)
        {
            // Note:  order of operations for alpha fading slightly different on zone fades
            if (IsSceneLoadFade(transitionType)) { onFadeOut?.Invoke(); }
            if (UsesStandaloneFade(transitionType)) { TriggerStandaloneFadeOut(transitionType); }
            if (!IsSceneLoadFade(transitionType)) { onFadeOut?.Invoke(); }
            
            yield return FadeOverlay(transitionType, 1f, 0f, GetFadeTime(false, transitionType));
            if (UsesStandaloneFade(transitionType)) { TriggerStandaloneFadeCleanup(); }
            fading = false;
            onFadeComplete?.Invoke();
        }
        #endregion

        #region AlphaBlends
        protected abstract void SetOverlayAlpha(TTransitionType transitionType, float alpha); // Alpha 0 ~ overlay hidden
        protected abstract void ResetOverlays(); // Hide every overlay + end any standalone fade

        protected virtual bool IsSkipFade(TTransitionType transitionType) => false;
        protected virtual bool TransitionUsesStandaloneFadeControl(TTransitionType transitionType) => false;

        protected virtual void TriggerStandaloneFadeIn(TTransitionType transitionType) { } 
        
        protected virtual void TriggerStandaloneFadeOut(TTransitionType transitionType) { }
        
        protected virtual void TriggerStandaloneFadeCleanup() { }

        private bool UsesStandaloneFade(TTransitionType transitionType) => !IsSkipFade(transitionType) && TransitionUsesStandaloneFadeControl(transitionType);
        private bool UsesOverlayFade(TTransitionType transitionType) => !IsSkipFade(transitionType) && !TransitionUsesStandaloneFadeControl(transitionType);

        private IEnumerator FadeOverlay(TTransitionType transitionType, float startAlpha, float endAlpha, float fadeTime)
        {
            // Note:  Overlay alpha is stepped here (i.e. rather than handed to the view)
            //  - A. this way the fade shares a clock with the fade sequence
            //  - B. in-line with this, we're tracking fade time here even with a StandaloneFadeControl (in lieu callbacks) to ensure Fader is always the master timer
            
            bool usesOverlayFade = UsesOverlayFade(transitionType);
            float elapsedTime = 0f;
            while (elapsedTime < fadeTime)
            {
                if (usesOverlayFade) { SetOverlayAlpha(transitionType, Mathf.Lerp(startAlpha, endAlpha, elapsedTime / fadeTime)); }
                yield return null;
                elapsedTime += Time.deltaTime;
            }
            if (usesOverlayFade) { SetOverlayAlpha(transitionType, endAlpha); }
        }
        #endregion
    }
}

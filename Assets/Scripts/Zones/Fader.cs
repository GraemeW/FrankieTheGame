using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Frankie.Core;
using Frankie.Rendering;
using Frankie.Utils;

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

        // State
        private bool fading = false;
        private Coroutine activeFade;
        private Image currentTransitionImage;
        private Action initiateBattleCallback;

        // Cached References
        private ReInitLazyValue<SceneLoader> sceneLoader;
        private BattleEntryShaderControl battleEntryShaderControl;

        // Events
        public event Action<TransitionType> fadingIn;
        public event Action fadingPeak;
        public event Action fadingOut;

        #region StaticFind
        private const string _faderTag = "Fader";
        public static Fader FindFader()
        {
            var faderGameObject = GameObject.FindGameObjectWithTag(_faderTag);
            return faderGameObject != null ? faderGameObject.GetComponent<Fader>() : null;
        }

        #endregion

        #region UnityMethods
        private void Awake()
        {
            sceneLoader = new ReInitLazyValue<SceneLoader>(SceneLoader.FindSceneLoader);
            battleEntryShaderControl = GetComponent<BattleEntryShaderControl>();
        }

        private void Start()
        {
            sceneLoader.ForceInit();
            ResetOverlays();
        }

        private void OnDisable()
        {
            if (activeFade != null) { StopCoroutine(activeFade); }
        }
        #endregion

        #region PublicMethods
        public bool IsFading() => fading;

        public IEnumerator BlipFade(float holdSeconds)
        {
            if (fading) { yield break; }
            
            // Re-use Zone-based fading (black screen)
            yield return QueueFadeEntry(TransitionType.Zone);
            yield return new WaitForSeconds(holdSeconds);
            yield return QueueFadeExit(TransitionType.Zone);
        }
        
        public void UpdateFadeState(TransitionType transitionType, Zone nextZone, bool saveSession = true)
        {
            // Non-IEnumerator Type for Scene Transitions:
            // Coroutine needs to exist on an object that will persist between scenes
            if (activeFade != null) { StopCoroutine(activeFade); }
            activeFade = StartCoroutine(ZoneFade(transitionType, nextZone, saveSession));
        }

        public void UpdateFadeStateImmediate()
        {
            // Coroutine needs to exist on an object that will persist between scenes
            if (activeFade != null) { StopCoroutine(activeFade); }
            activeFade = StartCoroutine(FadeImmediate());
        }

        public IEnumerator QueueFadeEntry(TransitionType transitionType)
        {
            fading = true;

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
            yield return new WaitForSeconds(GetFadeTime(true, transitionType));
            fadingPeak?.Invoke();
        }

        public IEnumerator QueueFadeExit(TransitionType transitionType)
        {
            AlphaFadeOut(transitionType);
            yield return new WaitForSeconds(GetFadeTime(false, transitionType));
            EndFade(transitionType);
        }
        #endregion

        #region PrivateMethods
        private IEnumerator ZoneFade(TransitionType transitionType, Zone zone, bool saveSession = true)
        {
            if (transitionType != TransitionType.Zone) yield break;
            
            yield return QueueFadeEntry(transitionType);
            if (saveSession) { SavingWrapper.SaveSession(); }
            yield return sceneLoader.value.LoadNewSceneAsync(zone);

            if (saveSession) { SavingWrapper.LoadSession(); }
            fadingOut?.Invoke();

            yield return QueueFadeExit(transitionType);
            if (saveSession) { SavingWrapper.SaveSession(); }
        }

        private IEnumerator FadeImmediate()
        {
            fading = true;
            nodeEntry.gameObject.SetActive(true);
            currentTransitionImage = nodeEntry;

            if (currentTransitionImage != null) { currentTransitionImage.CrossFadeAlpha(1, 0f, true); }
            yield return QueueFadeExit(TransitionType.Zone);
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
            fadingIn?.Invoke(transitionType);
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
            if (transitionType != TransitionType.Zone) { fadingOut?.Invoke(); } // invoked separately for zone transitions
        }
        private void EndFade(TransitionType transitionType)
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
            fading = false;
        }

        private void ResetOverlays()
        {
            nodeEntry?.gameObject.SetActive(false);
            battleComplete?.gameObject.SetActive(false);
            battleEntryShaderControl?.EndFade();
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

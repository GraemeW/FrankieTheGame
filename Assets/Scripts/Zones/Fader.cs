using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Frankie.Core;
using Frankie.Utils;
using UnityEditorInternal;

namespace Frankie.ZoneManagement
{
    [RequireComponent(typeof(BattleEntryShaderControl))]
    public class Fader : MonoBehaviour
    {
        // Tunables
        [Header("Linked Assets")]
        [SerializeField] GameObject battleUIPrefab = null;
        [SerializeField] Image nodeEntry = null;
        [SerializeField] Image battleComplete = null;
        [Header("Fader Properties")]
        [SerializeField] float fadeInTimer = 2.0f;
        [SerializeField] float fadeOutTimer = 1.0f;
        [SerializeField] float zoneFadeTimerMultiplier = 0.25f;

        // State
        Image currentTransitionImage = null;
        bool fading = false;
        GameObject battleUI = null;
        Action initiateBattleCallback = null;

        // Cached References
        SceneLoader sceneLoader = null;
        SavingWrapper savingWrapper = null;
        BattleEntryShaderControl battleEntryShaderControl = null;

        // Events
        public event Action<TransitionType> fadingIn;
        public event Action fadingPeak;
        public event Action fadingOut;

        #region UnityMethods
        private void Awake()
        {
            sceneLoader = FindAnyObjectByType<SceneLoader>();
            savingWrapper = FindAnyObjectByType<SavingWrapper>();
            battleEntryShaderControl = GetComponent<BattleEntryShaderControl>();
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

            AlphaFadeOut(transitionType);
            yield return new WaitForSeconds(GetFadeTime(false, transitionType));
            EndFade(transitionType);
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
            this.currentTransitionImage = nodeEntry;

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
            battleUI?.gameObject.SetActive(false);
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

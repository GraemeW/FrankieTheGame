using System.Collections;
using UnityEngine;
using LowDefMustard.Control;
using LowDefMustard.Zones;
using Frankie.Menu.UI;
using Frankie.Zones;
using Frankie.Utils.Addressables;

namespace Frankie.Control
{
    public class SplashMenuController : BaseController
    {
        // Tunables
        [Header("Scene Parameters")]
        [SerializeField] private float splashDelayTime = 3.0f;
        [SerializeField] private SplashScreen splashScreen;

        // State
        private Coroutine splashSequence;
        private bool kickedOffNextScene = false;

        // Cached References
        private PlayerInput playerInput;

        #region UnityMethods
        private void Awake()
        {
            playerInput = new PlayerInput();
            
            if (!VerifyUnique()) { return; }
            
            playerInput.Menu.Execute.performed += _ => SkipSplash();
            playerInput.Menu.Cancel.performed += _ => SkipSplash();
        }
        
        private void Start()
        {
            RunSplashSequence();
        }

        private void OnEnable()
        {
            playerInput.Menu.Enable();
        }

        private void OnDisable()
        {
            playerInput.Menu.Disable();
            StopSplashSequence();
        }
        #endregion

        #region SplashLoading
        private void RunSplashSequence()
        {
            StopSplashSequence();
            if (kickedOffNextScene) { return; }
            splashSequence = StartCoroutine(SplashSequence());
        }

        private void StopSplashSequence()
        {
            if (splashSequence == null) { return; }
            StopCoroutine(splashSequence);
            splashSequence = null;
        }

        private IEnumerator SplashSequence()
        {
            AddressablesLoader.StartAllCacheBuilds(); // Safety check ~ POR path is the AddressablesLoader singleton's Awake (no-op if already started)
            while (splashScreen != null)
            {
                if (splashScreen.IsShowingLastStage()) { yield return new WaitUntil(AddressablesLoader.AreAllCachesReady); }
                if (!splashScreen.TryShowNextStage()) { break; }
                yield return new WaitForSeconds(splashDelayTime);
            }
            if (splashScreen == null) { yield return new WaitUntil(AddressablesLoader.AreAllCachesReady); }
            splashSequence = null;
            KickOffNextScene();
        }

        private void KickOffNextScene()
        {
            if (kickedOffNextScene) { return; }
            
            kickedOffNextScene = true;
            float fadeOutSeconds = splashScreen != null ? splashScreen.GetFadeSeconds() : 0f; // Let the last stage fade out before the scene swaps
            SceneLoader.QueueScene(SceneQueueType.Start, new SceneQueueData(null, fadeOutSeconds, false));
        }
        #endregion

        #region InputHandling

        protected override void OnNoReceiversIdentified() => this.StandardOnNoReceiversIdentified();

        private void SkipSplash()
        {
            RunSplashSequence();
            HandleUserInput(ControllerInputType.Execute);
        }

        private void HandleUserInput(ControllerInputType controllerInputType) => TriggerGlobalInput(controllerInputType);
        #endregion
    }
}

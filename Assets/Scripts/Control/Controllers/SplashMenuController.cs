using System;
using UnityEngine;
using Frankie.ZoneManagement;

namespace Frankie.Control
{
    public class SplashMenuController : MonoBehaviour, IStandardPlayerInputCaller
    {
        // Tunables
        [Header("Scene Parameters")]
        [SerializeField] private float splashDelayTime = 3.0f;
        [SerializeField] private float splashRampTime = 0.7f;
        [SerializeField] private CanvasGroup[] splashObjects;

        // State
        private int currentSplashIndex = -1;
        private float timeSinceSplashLoaded;
        private CanvasGroup rampUpCanvasGroup;
        private CanvasGroup rampDownCanvasGroup;
        private bool kickedOffNextScene = false;

        // Cached References
        private SceneLoader sceneLoader;
        private PlayerInput playerInput;

        public event Action<PlayerInputType> globalInput;

        #region UnityMethods
        private void Awake()
        {
            playerInput = new PlayerInput();

            VerifyUnique();

            playerInput.Menu.Execute.performed += _ => SkipSplash();
            playerInput.Menu.Cancel.performed += _ => SkipSplash();
        }

        public void VerifyUnique()
        {
            var splashMenuControllers = FindObjectsByType<SplashMenuController>(FindObjectsSortMode.None);
            if (splashMenuControllers.Length > 1)
            {
                Destroy(gameObject);
            }
        }

        private void OnEnable()
        {
            playerInput.Menu.Enable();
        }

        private void OnDisable()
        {
            playerInput.Menu.Disable();
        }

        private void Start()
        {
            ClearSplashObjects();
            LoadNextSplash(currentSplashIndex);
        }

        private void Update()
        {
            timeSinceSplashLoaded += Time.deltaTime;
            if (timeSinceSplashLoaded >= splashDelayTime)
            {
                currentSplashIndex++;
                if (!kickedOffNextScene) { LoadNextSplash(currentSplashIndex); }
            }
            
            RampSplashAlphas();
        }
        #endregion

        #region SplashLoading
        private void ClearSplashObjects()
        {
            foreach (CanvasGroup splashObject in splashObjects)
            {
                splashObject.gameObject.SetActive(false);
            }
        }
        
        private void LoadNextSplash(int splashIndex)
        {
            int nextSplashIndex = splashIndex + 1;
            if (nextSplashIndex < 0) { return; }
            
            if (nextSplashIndex >= splashObjects.Length)
            {
                rampDownCanvasGroup = rampUpCanvasGroup;
                rampUpCanvasGroup = null;
                KickOffNextScene();
                return;
            }
            
            if (splashIndex >= 0) { rampDownCanvasGroup = splashObjects[splashIndex]; }
            
            rampUpCanvasGroup = splashObjects[nextSplashIndex];
            rampUpCanvasGroup.gameObject.SetActive(true);
            rampUpCanvasGroup.alpha = 0.0f;
            timeSinceSplashLoaded = 0;
        }

        private void RampSplashAlphas()
        {
            if (rampUpCanvasGroup != null && rampUpCanvasGroup.alpha < 1.0f)
            {
                rampUpCanvasGroup.alpha = Mathf.Min(timeSinceSplashLoaded / splashRampTime, 1.0f);
            }

            if (rampDownCanvasGroup != null && rampDownCanvasGroup.alpha > 0.0f)
            {
                rampDownCanvasGroup.alpha = Mathf.Max(1.0f - timeSinceSplashLoaded / splashRampTime, 0.0f);
            }
        }

        private void KickOffNextScene()
        {
            if (kickedOffNextScene) { return; }
            
            kickedOffNextScene = true;
            sceneLoader = SceneLoader.FindSceneLoader();
            if (sceneLoader == null) { return; }
            sceneLoader.QueueStartScreen();
        }
        #endregion

        #region InputHandling
        private void SkipSplash()
        {
            currentSplashIndex++;
            LoadNextSplash(currentSplashIndex);
            HandleUserInput(PlayerInputType.Execute);
        }

        private void HandleUserInput(PlayerInputType playerInputType)
        {
            globalInput?.Invoke(playerInputType);
        }
        #endregion
    }
}

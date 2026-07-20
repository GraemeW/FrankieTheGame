using UnityEngine;
using Frankie.ZoneManagement;

namespace Frankie.Control
{
    public class SplashMenuController : BaseController
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
        private PlayerInput playerInput;

        #region UnityMethods
        private void Awake()
        {
            playerInput = new PlayerInput();
            
            if (!VerifyUnique()) { return; }
            
            playerInput.Menu.Execute.performed += _ => SkipSplash();
            playerInput.Menu.Cancel.performed += _ => SkipSplash();
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
            
            SceneLoader.QueueScene(SceneQueueType.Start, new SceneQueueData(false));
        }
        #endregion

        #region InputHandling
        private void SkipSplash()
        {
            currentSplashIndex++;
            LoadNextSplash(currentSplashIndex);
            HandleUserInput(ControllerInputType.Execute);
        }

        private void HandleUserInput(ControllerInputType controllerInputType) => TriggerGlobalInput(controllerInputType);
        #endregion
    }
}

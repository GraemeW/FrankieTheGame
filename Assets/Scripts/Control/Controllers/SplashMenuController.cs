using Frankie.ZoneManagement;
using System;
using UnityEngine;

namespace Frankie.Control
{
    public class SplashMenuController : MonoBehaviour, IStandardPlayerInputCaller
    {
        // Tunables
        [Header("Scene Parameters")]
        [SerializeField] float splashDelayTime = 15.0f;

        // State
        Coroutine skipSplashRoutine = null;

        // Cached References
        SceneLoader sceneLoader = null;
        PlayerInput playerInput = null;

        public event Action<PlayerInputType> globalInput;

        private void Awake()
        {
            playerInput = new PlayerInput();

            VerifyUnique();

            playerInput.Menu.Execute.performed += context => SkipSplash();
            playerInput.Menu.Cancel.performed += context => SkipSplash();
            playerInput.Menu.Skip.performed += context => SkipSplash();
        }

        public void VerifyUnique()
        {
            SplashMenuController[] splashMenuControllers = FindObjectsByType<SplashMenuController>(FindObjectsSortMode.None);
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
            sceneLoader = GameObject.FindGameObjectWithTag("SceneLoader")?.GetComponent<SceneLoader>();
                // SceneLoader is a persistent object, thus can only be found after Awake -- so find in Start
            if (sceneLoader != null)
            {
                skipSplashRoutine = StartCoroutine(sceneLoader.SplashDelayToLoad(splashDelayTime));
            }
        }

        private void SkipSplash()
        {
            if (skipSplashRoutine != null) { StopCoroutine(skipSplashRoutine); skipSplashRoutine = null; }
            sceneLoader.QueueStartScreen();
            globalInput?.Invoke(PlayerInputType.Execute);
        }

        public PlayerInputType NavigationVectorToInputTypeTemplate(Vector2 navigationVector)
        {
            // Not evaluated -> IStandardPlayerInputCallerExtension
            return PlayerInputType.DefaultNone;
        }
    }

}

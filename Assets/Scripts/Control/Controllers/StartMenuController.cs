using Frankie.ZoneManagement;
using Frankie.Speech.UI;
using System;
using UnityEngine;

namespace Frankie.Control
{
    public class StartMenuController : MonoBehaviour, IStandardPlayerInputCaller
    {
        // Tunables
        [Header("Links and Prefabs")]
        [SerializeField] Canvas startCanvas = null;
        [SerializeField] GameObject optionsPrefab = null;

        // Cached References
        SceneLoader sceneLoader = null;
        PlayerInput playerInput = null;

        // Events
        public event Action<PlayerInputType> globalInput;

        private void Awake()
        {
            playerInput = new PlayerInput();
            playerInput.Menu.Navigate.performed += context => ParseDirectionalInput(context.ReadValue<Vector2>());
            playerInput.Menu.Execute.performed += context => HandleUserInput(PlayerInputType.Execute);
            playerInput.Menu.Cancel.performed += context => HandleUserInput(PlayerInputType.Cancel);
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
            sceneLoader = GameObject.FindGameObjectWithTag("SceneLoader").GetComponent<SceneLoader>();
            // SceneLoader is a persistent object, thus can only be found after Awake -- so find in Start
        }

        private void ParseDirectionalInput(Vector2 directionalInput)
        {
            PlayerInputType playerInputType = this.NavigationVectorToInputType(directionalInput);
            HandleUserInput(playerInputType);
        }

        private void HandleUserInput(PlayerInputType playerInputType)
        {
            if (globalInput != null)
            {
                globalInput.Invoke(playerInputType);
            }
        }

        public void NewGame()
        {
            sceneLoader.QueueNewGame();
        }

        public void LoadOptions()
        {
            GameObject optionsObject = Instantiate(optionsPrefab, startCanvas.transform);
            OptionsMenu menuOptions = optionsObject.GetComponent<OptionsMenu>();
            menuOptions.SetGlobalCallbacks(this);
        }

        public void ExitGame()
        {
            Application.Quit();
        }

        public PlayerInputType NavigationVectorToInputTypeTemplate(Vector2 navigationVector)
        {
            // Not evaluated -> IStandardPlayerInputCallerExtension
            return PlayerInputType.DefaultNone;
        }
    }
}
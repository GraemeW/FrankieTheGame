using Frankie.ZoneManagement;
using Frankie.Menu.UI;
using System;
using UnityEngine;
using Frankie.Core;

namespace Frankie.Control
{
    public class StartMenuController : MonoBehaviour, IStandardPlayerInputCaller
    {
        // Tunables
        [Header("Links and Prefabs")]
        [SerializeField] Canvas startCanvas = null;
        [SerializeField] OptionsMenu optionsPrefab = null;
        [SerializeField] LoadGameMenu loadGamePrefab = null;

        // Cached References
        PlayerInput playerInput = null;
        SavingWrapper savingWrapper = null;

        // Events
        public event Action<PlayerInputType> globalInput;

        private void Awake()
        {
            playerInput = new PlayerInput();

            VerifyUnique();

            playerInput.Menu.Navigate.performed += context => ParseDirectionalInput(context.ReadValue<Vector2>());
            playerInput.Menu.Execute.performed += context => HandleUserInput(PlayerInputType.Execute);
            playerInput.Menu.Cancel.performed += context => HandleUserInput(PlayerInputType.Cancel);
        }

        public void VerifyUnique()
        {
            StartMenuController[] startMenuControllers = FindObjectsOfType<StartMenuController>();
            if (startMenuControllers.Length > 1)
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
            // SavingWrapper is a persistent object, thus can only be found after Awake -- so find in Start
            savingWrapper = GameObject.FindGameObjectWithTag("Saver").GetComponent<SavingWrapper>();
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

        public void LoadGame() // Called via Unity Events
        {
            LoadGameMenu loadGameMenu = Instantiate(loadGamePrefab, startCanvas.transform);
            loadGameMenu.SetGlobalInputHandler(this);
        }

        public void Continue() // Called via Unity Events
        {
            savingWrapper.Continue();
        }

        public void LoadOptions() // Called via Unity Events
        {
            OptionsMenu menuOptions = Instantiate(optionsPrefab, startCanvas.transform);
            menuOptions.SetGlobalInputHandler(this);
        }

        public void ExitGame() // Called via Unity Events
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
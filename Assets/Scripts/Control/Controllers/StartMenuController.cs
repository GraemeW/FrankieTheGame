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
        [SerializeField] StartMenu startMenu = null;

        // Cached References
        PlayerInput playerInput = null;

        // Events
        public event Action<PlayerInputType> globalInput;

        private void Awake()
        {
            playerInput = new PlayerInput();

            VerifyUnique();

            playerInput.Menu.Navigate.performed += context => ParseDirectionalInput(context.ReadValue<Vector2>());
            playerInput.Menu.Execute.performed += context => HandleUserInput(PlayerInputType.Execute);
            playerInput.Menu.Cancel.performed += context => HandleUserInput(PlayerInputType.Cancel);
            playerInput.Menu.Cancel.performed += context => HandleUserInput(PlayerInputType.Option);
        }

        public void VerifyUnique()
        {
            StartMenuController[] startMenuControllers = FindObjectsOfType<StartMenuController>();
            if (startMenuControllers.Length > 1)
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            startMenu.Setup(this, startCanvas);
        }

        private void OnEnable()
        {
            playerInput.Menu.Enable();
        }

        private void OnDisable()
        {
            playerInput.Menu.Disable();
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

        public PlayerInputType NavigationVectorToInputTypeTemplate(Vector2 navigationVector)
        {
            // Not evaluated -> IStandardPlayerInputCallerExtension
            return PlayerInputType.DefaultNone;
        }
    }
}
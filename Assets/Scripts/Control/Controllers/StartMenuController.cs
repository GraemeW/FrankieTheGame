using System;
using UnityEngine;
using Frankie.Menu.UI;

namespace Frankie.Control
{
    public class StartMenuController : MonoBehaviour, IStandardPlayerInputCaller
    {
        // Tunables
        [Header("Links and Prefabs")]
        [SerializeField] private Canvas startCanvas;
        [SerializeField] private StartMenu startMenu;

        // Cached References
        private PlayerInput playerInput;

        // Events
        public event Action<PlayerInputType> globalInput;

        private void Awake()
        {
            playerInput = new PlayerInput();

            VerifyUnique();

            playerInput.Menu.Navigate.performed += context => ParseDirectionalInput(context.ReadValue<Vector2>());
            playerInput.Menu.Execute.performed += _ => HandleUserInput(PlayerInputType.Execute);
            playerInput.Menu.Cancel.performed += _ => HandleUserInput(PlayerInputType.Cancel);
            playerInput.Menu.Option.performed += _ => HandleUserInput(PlayerInputType.Option);
        }

        public void VerifyUnique()
        {
            var startMenuControllers = FindObjectsByType<StartMenuController>(FindObjectsSortMode.None);
            if (startMenuControllers.Length > 1)
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            startMenu.Setup(startCanvas);
            startMenu.TakeControl(this, startMenu, null);
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
            PlayerInputType playerInputType = IStandardPlayerInputCaller.NavigationVectorToInputType(directionalInput);
            HandleUserInput(playerInputType);
        }

        private void HandleUserInput(PlayerInputType playerInputType)
        {
            globalInput?.Invoke(playerInputType);
        }

        public PlayerInputType NavigationVectorToInputTypeTemplate(Vector2 navigationVector)
        {
            // Not evaluated -> IStandardPlayerInputCallerExtension
            return PlayerInputType.DefaultNone;
        }
    }
}
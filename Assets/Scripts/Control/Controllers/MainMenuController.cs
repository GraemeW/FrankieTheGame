using UnityEngine;
using Frankie.Menu.UI;
using UnityEngine.Serialization;

namespace Frankie.Control
{
    public class MainMenuController : BaseController
    {
        // Tunables
        [Header("Links and Prefabs")]
        [SerializeField] private Canvas startCanvas;
        [FormerlySerializedAs("startMenu")] [SerializeField] private Launcher launcher;

        // State
        private ControllerInputType currentDirectionalInput = ControllerInputType.DefaultNone;
        
        // Cached References
        private PlayerInput playerInput;
        
        // Lifecycle Overrides -- Prevent Polling to Self-Destruct
        protected override bool HasListeners() => true;
        protected override bool HasBeenActivated() => true;

        private void Awake()
        {
            playerInput = new PlayerInput();
            
            if (!VerifyUnique()) { return; }
            
            playerInput.Menu.Navigate.performed += context => ParseDirectionalInput(context.ReadValue<Vector2>());
            playerInput.Menu.Navigate.canceled += _ => ParseDirectionalInput(Vector2.zero);
            playerInput.Menu.Execute.performed += _ => HandleUserInput(ControllerInputType.Execute);
            playerInput.Menu.Cancel.performed += _ => HandleUserInput(ControllerInputType.Cancel);
            playerInput.Menu.Option.performed += _ => HandleUserInput(ControllerInputType.Option);
        }

        private void Start()
        {
            launcher.Setup(startCanvas);
            launcher.TakeControl(this, launcher, null);
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
            if (!BaseController.ParseDirectionalInput(directionalInput, currentDirectionalInput, out ControllerInputType newControllerInputType)) { return; }
            currentDirectionalInput = newControllerInputType;
            HandleUserInput(newControllerInputType);
        }

        private void HandleUserInput(ControllerInputType controllerInputType) => TriggerGlobalInput(controllerInputType);
    }
}

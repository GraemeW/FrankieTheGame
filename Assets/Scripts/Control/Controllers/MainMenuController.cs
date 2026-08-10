using UnityEngine;
using LowDefMustard.Control;
using Frankie.Menu.UI;
using Frankie.Utils.UI;

namespace Frankie.Control
{
    public class MainMenuController : BaseController
    {
        // Tunables
        [Header("Links and Prefabs")]
        [SerializeField] private Canvas startCanvas;
        [SerializeField] private UIBoxBase uiBoxReceiver;

        // State
        private ControllerInputType currentDirectionalInput = ControllerInputType.DefaultNone;
        
        // Cached References
        private PlayerInput playerInput;

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
            if (uiBoxReceiver == null) { return; }
            if (uiBoxReceiver is Launcher launcher) { launcher.Setup(startCanvas); }
            
            if (uiBoxReceiver is IInputReceiver inputReceiver) { AddInputReceiver(inputReceiver, null); }
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
        protected override void OnNoReceiversIdentified() => this.StandardOnNoReceiversIdentified();
    }
}

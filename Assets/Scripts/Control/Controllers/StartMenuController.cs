using System;
using Frankie.Combat;
using UnityEngine;
using Frankie.Core;
using Frankie.Menu.UI;
using Frankie.Stats;

namespace Frankie.Control
{
    public class StartMenuController : MonoBehaviour, IStandardPlayerInputCaller
    {
        // Tunables
        [Header("Start Menu Tunables")] 
        [SerializeField][Tooltip("false for GameOver screen")] private bool destroyPlayerOnStart = true;
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
            StartMenuController[] startMenuControllers = FindObjectsByType<StartMenuController>(FindObjectsSortMode.None);
            if (startMenuControllers.Length > 1)
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            HandlePlayerExistence(true);
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

        private void OnDestroy()
        {
            HandlePlayerExistence(false);
        }

        private void HandlePlayerExistence(bool isStart)
        {
            PlayerStateMachine playerStateMachine = Player.FindPlayerStateMachine();
            if (playerStateMachine == null) return;

            if (destroyPlayerOnStart)
            {
                if (isStart) { Destroy(playerStateMachine.gameObject); }
                return;
            }

            HealParty(playerStateMachine);
            LockPlayer(playerStateMachine, isStart);
        }

        private void HealParty(PlayerStateMachine playerStateMachine)
        {
            if (playerStateMachine == null) { return; }
            Party party = playerStateMachine.GetParty();
            if (party == null) { return; }
            
            foreach (BaseStats member in party.GetParty())
            {
                if (member.TryGetComponent(out CombatParticipant combatParticipant))
                {
                    combatParticipant.Revive(false);
                }
            }
        }
        
        private void LockPlayer(PlayerStateMachine playerStateMachine, bool enable)
        {
            if (playerStateMachine == null) { return; }
            
            if (enable)
            {
                // Lock menus, but allow player movement
                playerStateMachine.EnterCutscene(true, true);
                if (playerStateMachine.TryGetComponent(out PlayerMover playerMover))
                {
                    playerMover.SetLookDirection(Vector2.down);
                }
            }
            else
            {
                playerStateMachine.EnterWorld();
            }
        }

        private void ParseDirectionalInput(Vector2 directionalInput)
        {
            PlayerInputType playerInputType = this.NavigationVectorToInputType(directionalInput);
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
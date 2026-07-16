using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using Frankie.Core;
using Frankie.Stats;

namespace Frankie.Control
{
    [RequireComponent(typeof(PlayerStateMachine))]
    [RequireComponent(typeof(PlayerMover))]
    [RequireComponent(typeof(Party))]
    public class PlayerController : BaseController
    {
        // Data Types
        [Serializable]
        public struct CursorMapping
        {
            public CursorType type;
            public Texture2D texture;
            public Vector2 hotspot;
        }

        // Tunables
        [Header("Interaction")]
        [SerializeField] private CursorMapping[] cursorMappings;
        [SerializeField] private LayerMask raycastInteractionLayers;
        [SerializeField] private float raycastMouseDistance = 10.0f;
        [SerializeField] private float raycastRadius = 0.1f;
        [SerializeField] private float interactionDistance = 0.5f;
        
        // State
        private ControllerInputType currentDirectionalInput = ControllerInputType.DefaultNone;
        private bool allowComponentInteraction = true;
        private bool inTransition = false;

        // Cached References
        private PlayerInput playerInput;
        private PlayerStateMachine playerStateMachine;
        private PlayerMover playerMover;
        private Transform interactionCentrePoint;
        
        // Lifecycle Overrides -- Prevent Polling to Self-Destruct
        protected override bool HasListeners() => true;
        protected override bool HasBeenActivated() => true;
        
        #region Static
        private static Vector2 GetMouseRay()
        {
            return Camera.main != null ? Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue()) : Vector2.zero;
        }
        #endregion

        #region UnityMethods
        private void Awake()
        {
            playerInput = new PlayerInput();
            interactionCentrePoint = transform; // Initialize for safety, but overridden by Party updates
            playerMover = GetComponent<PlayerMover>();
            playerStateMachine = GetComponent<PlayerStateMachine>();
            
            if (!VerifyUnique()) { return; }

            playerInput.Player.Navigate.performed += context => playerMover.ParseMovement(context.ReadValue<Vector2>());
            playerInput.Player.Navigate.canceled += _ => playerMover.ParseMovement(Vector2.zero);
            playerInput.Player.Navigate.performed += context => ParseDirectionalInput(context.ReadValue<Vector2>());
            playerInput.Player.Navigate.canceled += _ => ParseDirectionalInput(Vector2.zero);
            playerInput.Player.Pointer.performed += _ => InteractWithComponentManual(ControllerInputType.DefaultNone);
            playerInput.Player.Execute.performed += _ => HandleUserInput(ControllerInputType.Execute);
            playerInput.Player.Cancel.performed += _ => HandleUserInput(ControllerInputType.Cancel);
            playerInput.Player.Option.performed += _ => HandleUserInput(ControllerInputType.Option);
            playerInput.Player.Escape.performed += _  => HandleUserInput(ControllerInputType.Escape);
        }
        
        private void OnEnable()
        {
            playerInput.Player.Enable();
            playerStateMachine.playerStateChanged += ParsePlayerStateChange;
            if (TryGetComponent(out Party party)) { party.SubscribeToMembersAlteredUpdates(true, HandlePartyUpdate); }
        }

        private void OnDisable()
        {
            playerInput.Player.Disable();
            playerStateMachine.playerStateChanged -= ParsePlayerStateChange;
            if (TryGetComponent(out Party party)) { party.SubscribeToMembersAlteredUpdates(false, HandlePartyUpdate); }
        }
        #endregion
        
        #region Getters
        public float GetInteractionDistance() => interactionDistance;
        public PlayerMover GetPlayerMover() => playerMover;
        #endregion
        
        #region Interfaces
        public RaycastHit2D PlayerCastToObject(Vector3 objectPosition)
        {
            Vector2 castDirection = objectPosition - interactionCentrePoint.position;
            RaycastHit2D closestHit = Physics2D.CircleCast(interactionCentrePoint.position, raycastRadius, castDirection, interactionDistance, raycastInteractionLayers);
            return closestHit.collider == null ? new RaycastHit2D() : closestHit;
        }

        public Vector2 GetInteractionPosition() => interactionCentrePoint != null ? interactionCentrePoint.position : Vector2.zero;
        
        private void SetCursor(CursorType type)
        {
            CursorMapping mapping = GetCursorMapping(type);
            Cursor.SetCursor(mapping.texture, mapping.hotspot, CursorMode.Auto);
        }

        private CursorMapping GetCursorMapping(CursorType type)
        {
            foreach (CursorMapping cursorMapping in cursorMappings)
            {
                if (cursorMapping.type == type)
                {
                    return cursorMapping;
                }
            }
            return cursorMappings[0];
        }
        #endregion
        
        #region PrivateMethods
        private void HandlePartyUpdate(PartyAlteredData partyAlteredData)
        {
            if (partyAlteredData == null) { return; }

            Transform newInteractionCentrePoint = partyAlteredData.GetPartyLeaderInteractionCentrePoint();
            if (newInteractionCentrePoint == null) { return; }
            
            interactionCentrePoint = newInteractionCentrePoint;
        }
        
        private void ParsePlayerStateChange(PlayerStateType playerStateType, IPlayerStateContext playerStateContext)
        {
            SetCursor(CursorType.None);
            allowComponentInteraction = false;
            inTransition = false;

            switch (playerStateType)
            {
                case PlayerStateType.InWorld:
                    allowComponentInteraction = true;
                    break;
                case PlayerStateType.InTransition:
                    inTransition = true;
                    break;
            }
        }

        private void ParseDirectionalInput(Vector2 directionalInput)
        {
            if (!BaseController.ParseDirectionalInput(directionalInput, currentDirectionalInput, out ControllerInputType newControllerInputType)) { return; }
            currentDirectionalInput = newControllerInputType;
            HandleUserInput(newControllerInputType);
        }

        private void HandleUserInput(ControllerInputType controllerInputType)
        {
            if (inTransition) { return; }
            if (InteractWithGlobals(controllerInputType)) { return; }
            
            if (allowComponentInteraction)
            {
                if (InteractWithComponent(controllerInputType)) { return; }
                if (InteractWithComponentManual(controllerInputType)) { return; }
            }
            if (InteractWithMenusOptions(controllerInputType)) { return; }
            SetCursor(CursorType.None);
        }

        private bool InteractWithGlobals(ControllerInputType controllerInputType)
        {
            if (!HasGlobalInput()) { return false; }
            TriggerGlobalInput(controllerInputType);
            return true;
        }

        private bool InteractWithComponent(ControllerInputType controllerInputType)
        {
            RaycastHit2D hitInfo = RaycastToMouseLocation();
            if (hitInfo.collider == null) { return false; }
            
            foreach (IRaycastable raycastable in hitInfo.transform.GetComponentsInChildren<IRaycastable>())
            {
                if (!raycastable.HandleRaycast(playerStateMachine, this, controllerInputType, ControllerInputType.Execute)) { continue; }
                SetCursor(raycastable.GetCursorType());
                return true;
            }
            return false;
        }

        private bool InteractWithComponentManual(ControllerInputType controllerInputType)
        {
            if (controllerInputType != ControllerInputType.Execute) { return false; }
            
            RaycastHit2D hitInfo = RaycastFromPlayerInLookDirection();
            return hitInfo.collider != null 
                   && hitInfo.transform.GetComponentsInChildren<IRaycastable>().Any(raycastable => raycastable.HandleRaycast(playerStateMachine, this, controllerInputType, ControllerInputType.Execute));
        }

        private bool InteractWithMenusOptions(ControllerInputType controllerInputType)
        {
            switch (controllerInputType)
            {
                case ControllerInputType.Option:
                    playerStateMachine.EnterWorldOptions();
                    return true;
                case ControllerInputType.Escape:
                    playerStateMachine.EnterEscapeMenu();
                    return true;
                default:
                    return false;
            }
        }

        private RaycastHit2D RaycastToMouseLocation()
        {
            RaycastHit2D closestHit = Physics2D.CircleCast(GetMouseRay(), raycastRadius, Vector2.zero, raycastMouseDistance,raycastInteractionLayers);
            return closestHit.collider == null ? new RaycastHit2D() : closestHit; // pass an empty hit
        }

        private RaycastHit2D RaycastFromPlayerInLookDirection()
        {
            RaycastHit2D closestHit = Physics2D.CircleCast(interactionCentrePoint.position, raycastRadius, playerMover.GetLookDirection(), interactionDistance,raycastInteractionLayers);
            return closestHit.collider == null ? new RaycastHit2D() : closestHit; // pass an empty hit
        }
        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Frankie.Control
{
    [RequireComponent(typeof(PlayerMover))]
    [RequireComponent(typeof(PlayerStateMachine))]
    public class PlayerController : MonoBehaviour, IStandardPlayerInputCaller
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
        [SerializeField] private float raycastRadius = 0.1f;
        [SerializeField] private float interactionDistance = 0.5f;
        [SerializeField] private Transform interactionCenterPoint;

        // State
        private bool allowComponentInteraction = true;
        private bool inTransition = false;

        // Cached References
        private PlayerInput playerInput;
        private PlayerMover playerMover;
        private PlayerStateMachine playerStateMachine;

        // Static
        private const string _tagInteractable = "Interactable";

        // Events
        public event Action<PlayerInputType> globalInput;

        #region UnityMethods
        private void Awake()
        {
            playerMover = GetComponent<PlayerMover>();
            playerStateMachine = GetComponent<PlayerStateMachine>();
            playerInput = new PlayerInput();

            VerifyUnique();

            playerInput.Player.Navigate.performed += context => playerMover.ParseMovement(context.ReadValue<Vector2>());
            playerInput.Player.Navigate.canceled += _ => playerMover.ParseMovement(Vector2.zero);

            playerInput.Player.Navigate.performed += context => ParseDirectionalInput(context.ReadValue<Vector2>());
            playerInput.Player.Pointer.performed += _ => InteractWithComponentManual(PlayerInputType.DefaultNone);
            playerInput.Player.Execute.performed += _ => HandleUserInput(PlayerInputType.Execute);
            playerInput.Player.Cancel.performed += _ => HandleUserInput(PlayerInputType.Cancel);
            playerInput.Player.Option.performed += _ => HandleUserInput(PlayerInputType.Option);
            playerInput.Player.Skip.performed += _ => HandleUserInput(PlayerInputType.Skip);
        }
        
        private void OnEnable()
        {
            playerStateMachine.playerStateChanged += ParsePlayerStateChange;
            playerInput.Player.Enable();
        }

        private void OnDisable()
        {
            playerStateMachine.playerStateChanged -= ParsePlayerStateChange;
            playerInput.Player.Disable();
        }
        #endregion
        
        #region Getters
        public float GetInteractionDistance() => interactionDistance;
        public PlayerMover GetPlayerMover() => playerMover;
        #endregion
        
        #region Interfaces
        public RaycastHit2D PlayerCastToObject(Vector3 objectPosition)
        {
            Vector2 castDirection = objectPosition - interactionCenterPoint.position;
            float castDistance = Vector2.Distance(objectPosition, interactionCenterPoint.position); // TODO:  Refactor to avoid square root here
            RaycastHit2D[] hits = Physics2D.CircleCastAll(interactionCenterPoint.position, raycastRadius, castDirection, castDistance);

            List<RaycastHit2D> sortedInteractableHits = hits.Where(x => x.collider.transform.gameObject.CompareTag(_tagInteractable)).OrderBy(x => x.distance).ToList();
            if (sortedInteractableHits.Count == 0) { return new RaycastHit2D(); } // pass an empty hit
            return sortedInteractableHits[0];
        }

        public Vector2 GetInteractionPosition()
        {
            if (interactionCenterPoint != null)
            {
                return interactionCenterPoint.position;
            }
            return Vector2.zero;
        }
        
        public void VerifyUnique()
        {
            PlayerController[] playerControllers = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
            if (playerControllers.Length > 1)
            {
                Destroy(gameObject);
            }
        }
        
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

        private static Vector2 GetMouseRay()
        {
            return Camera.main != null ? Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue()) : Vector2.zero;
        }

        public PlayerInputType NavigationVectorToInputTypeTemplate(Vector2 navigationVector)
        {
            // Not evaluated -> IStandardPlayerInputCallerExtension
            return PlayerInputType.DefaultNone;
        }
        #endregion
        
        #region PrivateMethods
        private void ParsePlayerStateChange(PlayerStateType playerStateType)
        {
            SetCursor(CursorType.None);
            allowComponentInteraction = false;
            inTransition = false;

            switch (playerStateType)
            {
                case PlayerStateType.inWorld:
                    allowComponentInteraction = true;
                    break;
                case PlayerStateType.inTransition:
                    inTransition = true;
                    break;
            }
        }

        private void ParseDirectionalInput(Vector2 directionalInput)
        {
            PlayerInputType playerInputType = this.NavigationVectorToInputType(directionalInput);
            HandleUserInput(playerInputType);
        }

        private void HandleUserInput(PlayerInputType playerInputType)
        {
            if (inTransition) { return; }

            if (InteractWithGlobals(playerInputType)) return;
            if (allowComponentInteraction)
            {
                if (InteractWithComponent(playerInputType)) return;
                if (InteractWithComponentManual(playerInputType)) return;
            }
            if (InteractWithMenusOptions(playerInputType)) return;
            SetCursor(CursorType.None);
        }

        private bool InteractWithGlobals(PlayerInputType playerInputType)
        {
            if (globalInput == null) { return false; }
            globalInput.Invoke(playerInputType);
            return true;
        }

        private bool InteractWithComponent(PlayerInputType playerInputType)
        {
            RaycastHit2D hitInfo = RaycastToMouseLocation();
            if (hitInfo.collider == null) { return false; }

            IRaycastable[] raycastables = hitInfo.transform.GetComponentsInChildren<IRaycastable>();
            if (raycastables != null)
            {
                foreach (IRaycastable raycastable in raycastables)
                {
                    if (raycastable.HandleRaycast(playerStateMachine, this, playerInputType, PlayerInputType.Execute))
                    {
                        SetCursor(raycastable.GetCursorType());
                        return true;
                    }
                }
            }
            return false;
        }

        private bool InteractWithComponentManual(PlayerInputType playerInputType)
        {
            if (playerInputType != PlayerInputType.Execute) return false;
            
            RaycastHit2D hitInfo = RaycastFromPlayerInLookDirection();
            if (hitInfo.collider == null) { return false; }

            var raycastables = hitInfo.transform.GetComponentsInChildren<IRaycastable>();
            return raycastables != null && raycastables.Any(raycastable => raycastable.HandleRaycast(playerStateMachine, this, playerInputType, PlayerInputType.Execute));
        }

        private bool InteractWithMenusOptions(PlayerInputType playerInputType)
        {
            switch (playerInputType)
            {
                case PlayerInputType.Option:
                    playerStateMachine.EnterWorldOptions();
                    return true;
                case PlayerInputType.Cancel:
                    playerStateMachine.EnterEscapeMenu();
                    return true;
                default:
                    return false;
            }
        }

        private RaycastHit2D RaycastToMouseLocation()
        {
            RaycastHit2D[] hits = Physics2D.CircleCastAll(GetMouseRay(), raycastRadius, Vector2.zero);
            RaycastHit2D[] nonPlayerHits = hits.Where(x => !x.collider.transform.gameObject.CompareTag("Player")).ToArray(); 
            return nonPlayerHits.Length == 0 ? new RaycastHit2D() : nonPlayerHits[0]; // pass an empty hit
        }

        private RaycastHit2D RaycastFromPlayerInLookDirection()
        {
            RaycastHit2D[] hits = Physics2D.CircleCastAll(interactionCenterPoint.position, raycastRadius, playerMover.GetLookDirection());

            RaycastHit2D[] nonPlayerHits = hits.Where(x => !x.collider.transform.gameObject.CompareTag("Player")).ToArray();
            return nonPlayerHits.Length == 0 ? new RaycastHit2D() : nonPlayerHits[0]; // pass an empty hit
        }
        #endregion
    }
}

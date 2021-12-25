using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Frankie.Core;
using UnityEngine.InputSystem;

namespace Frankie.Control
{
    [RequireComponent(typeof(PlayerMover))]
    [RequireComponent(typeof(PlayerStateHandler))]
    public class PlayerController : MonoBehaviour, IStandardPlayerInputCaller
    {
        // Data Types
        [System.Serializable]
        public struct CursorMapping
        {
            public CursorType type;
            public Texture2D texture;
            public Vector2 hotspot;
        }

        // Tunables
        [Header("Interaction")]
        [SerializeField] CursorMapping[] cursorMappings = null;
        [SerializeField] float raycastRadius = 0.1f;
        [SerializeField] float interactionDistance = 0.5f;
        [SerializeField] Transform interactionCenterPoint = null;

        // Cached References
        PlayerInput playerInput = null;
        PlayerMover playerMover = null;
        PlayerStateHandler playerStateHandler = null;

        // Static
        string STATIC_TAG_INTERACTABLE = "Interactable";

        // Events
        public event Action<PlayerInputType> globalInput;

        // Public functions
        public float GetInteractionDistance()
        {
            return interactionDistance;
        }

        public RaycastHit2D PlayerCastToObject(Vector3 objectPosition)
        {
            Vector2 castDirection = objectPosition - interactionCenterPoint.position;
            float castDistance = Vector2.Distance(objectPosition, interactionCenterPoint.position); // TODO:  Refactor to avoid square root here
            RaycastHit2D[] hits = Physics2D.CircleCastAll(interactionCenterPoint.position, raycastRadius, castDirection, castDistance);

            List<RaycastHit2D> sortedInteractableHits = hits.Where(x => x.collider.transform.gameObject.CompareTag(STATIC_TAG_INTERACTABLE)).OrderBy(x => x.distance).ToList();
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

        public PlayerMover GetPlayerMover()
        {
            return playerMover;
        }

        // Internal functions

        private void Awake()
        {
            playerMover = GetComponent<PlayerMover>();
            playerStateHandler = GetComponent<PlayerStateHandler>();
            playerInput = new PlayerInput();

            VerifyUnique();

            playerInput.Player.Navigate.performed += context => playerMover.ParseMovement(context.ReadValue<Vector2>());
            playerInput.Player.Navigate.canceled += context => playerMover.ParseMovement(Vector2.zero);

            playerInput.Player.Navigate.performed += context => ParseDirectionalInput(context.ReadValue<Vector2>());
            playerInput.Player.Pointer.performed += context => HandleMouseMovement(PlayerInputType.DefaultNone);
            playerInput.Player.Execute.performed += context => HandleUserInput(PlayerInputType.Execute);
            playerInput.Player.Cancel.performed += context => HandleUserInput(PlayerInputType.Cancel);
            playerInput.Player.Option.performed += context => HandleUserInput(PlayerInputType.Option);
            playerInput.Player.Skip.performed += context => HandleUserInput(PlayerInputType.Skip);
        }

        public void VerifyUnique()
        {
            PlayerController[] playerControllers = FindObjectsOfType<PlayerController>();
            if (playerControllers.Length > 1)
            {
                Destroy(gameObject);
            }
        }

        private void OnEnable()
        {
            playerStateHandler.playerStateChanged += ResetCursor;
            playerInput.Player.Enable();
        }

        private void OnDisable()
        {
            playerStateHandler.playerStateChanged -= ResetCursor;
            playerInput.Player.Disable();
        }

        private void ResetCursor(PlayerState playerState)
        {
            SetCursor(CursorType.None);
        }

        private void ParseDirectionalInput(Vector2 directionalInput)
        {
            PlayerInputType playerInputType = this.NavigationVectorToInputType(directionalInput);
            HandleUserInput(playerInputType);
        }

        private void HandleUserInput(PlayerInputType playerInputType)
        {
            if (playerStateHandler.GetPlayerState() == PlayerState.inTransition) { return; }

            if (playerStateHandler.GetPlayerState() == PlayerState.inWorld)
            {
                if (InteractWithGlobals(playerInputType)) return;
                if (InteractWithComponent(playerInputType)) return;
                if (InteractWithComponentManual(playerInputType)) return;
                if (InteractWithMenusOptions(playerInputType)) return;
                SetCursor(CursorType.None);
            }
            else if (playerStateHandler.GetPlayerState() == PlayerState.inMenus)
            {
                if (InteractWithGlobals(playerInputType)) return;
                if (InteractWithMenusOptions(playerInputType)) return;
            }
        }

        private void HandleMouseMovement(PlayerInputType playerInputType)
        {
            if (InteractWithComponentManual(playerInputType)) return;
        }

        private bool InteractWithGlobals(PlayerInputType playerInputType)
        {
            if (globalInput != null)
            {
                globalInput.Invoke(playerInputType);
                return true;
            }
            return false;
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
                    if (raycastable.HandleRaycast(playerStateHandler, this, playerInputType, PlayerInputType.Execute))
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
            if (playerInputType == PlayerInputType.Execute)
            {
                RaycastHit2D hitInfo = RaycastFromPlayerInLookDirection();
                if (hitInfo.collider == null) { return false; }

                IRaycastable[] raycastables = hitInfo.transform.GetComponentsInChildren<IRaycastable>();
                if (raycastables != null)
                {
                    foreach (IRaycastable raycastable in raycastables)
                    {
                        if (raycastable.HandleRaycast(playerStateHandler, this, playerInputType, PlayerInputType.Execute))
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
            return false;
        }

        private bool InteractWithMenusOptions(PlayerInputType playerInputType)
        {
            if (playerInputType == PlayerInputType.Option)
            {
                playerStateHandler.EnterWorldOptions();
                return true;
            }
            else if (playerStateHandler.GetPlayerState() == PlayerState.inWorld && playerInputType == PlayerInputType.Cancel)
            {
                playerStateHandler.EnterEscapeMenu();
                return true;
            }
            return false;
        }

        private RaycastHit2D RaycastToMouseLocation()
        {
            RaycastHit2D[] hits = Physics2D.CircleCastAll(GetMouseRay(), raycastRadius, Vector2.zero);
            RaycastHit2D[] nonPlayerHits = hits.Where(x => !x.collider.transform.gameObject.CompareTag("Player")).ToArray(); 
            if (nonPlayerHits == null || nonPlayerHits.Length == 0) { return new RaycastHit2D(); } // pass an empty hit
            return nonPlayerHits[0];
        }

        private RaycastHit2D RaycastFromPlayerInLookDirection()
        {
            RaycastHit2D[] hits = Physics2D.CircleCastAll(interactionCenterPoint.position, raycastRadius, playerMover.GetLookDirection());

            RaycastHit2D[] nonPlayerHits = hits.Where(x => !x.collider.transform.gameObject.CompareTag("Player")).ToArray();
            if (nonPlayerHits == null || nonPlayerHits.Length == 0) { return new RaycastHit2D(); } // pass an empty hit
            return nonPlayerHits[0];
        }

        // Mouse / Cursor Handling
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
            return Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        }

        public PlayerInputType NavigationVectorToInputTypeTemplate(Vector2 navigationVector)
        {
            // Not evaluated -> IStandardPlayerInputCallerExtension
            return PlayerInputType.DefaultNone;
        }
    }
}

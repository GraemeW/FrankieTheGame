using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Frankie.Core;

namespace Frankie.Control
{
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
        [SerializeField] KeyCode interactUp = KeyCode.W;
        [SerializeField] KeyCode interactLeft = KeyCode.A;
        [SerializeField] KeyCode interactRight = KeyCode.D;
        [SerializeField] KeyCode interactDown = KeyCode.S;
        [SerializeField] string interactSkipButton = "Fire1";
        [SerializeField] string interactInspectButton = "Fire2";
        [SerializeField] KeyCode interactInspectKey = KeyCode.E;
        [SerializeField] KeyCode interactOptionKey = KeyCode.Tab;
        [SerializeField] string interactCancelButton = "Cancel";
        [SerializeField] CursorMapping[] cursorMappings = null;
        [SerializeField] float raycastRadius = 0.1f;
        [SerializeField] float interactionDistance = 0.5f;
        [SerializeField] Transform interactionCenterPoint = null;

        // Cached References
        PlayerMover playerMover = null;
        PlayerStateHandler playerStateHandler = null;

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
            float castDistance = Vector2.Distance(objectPosition, interactionCenterPoint.position);
            RaycastHit2D[] hits = Physics2D.CircleCastAll(interactionCenterPoint.position, raycastRadius, castDirection, castDistance);

            List<RaycastHit2D> sortedInteractableHits = hits.Where(x => x.collider.transform.gameObject.CompareTag("Interactable")).OrderBy(x => x.distance).ToList();
            if (sortedInteractableHits.Count == 0) { return new RaycastHit2D(); } // pass an empty hit
            return sortedInteractableHits[0];
        }

        public Vector2 GetInteractionPosition()
        {
            return interactionCenterPoint.position;
        }

        public PlayerMover GetPlayerMover()
        {
            return playerMover;
        }

        public PlayerInputType GetPlayerInput()
        {
            PlayerInputType input = PlayerInputType.DefaultNone;

            if (Input.GetKeyDown(interactUp))
            {
                input = PlayerInputType.NavigateUp;
            }
            else if (Input.GetKeyDown(interactLeft))
            {
                input = PlayerInputType.NavigateLeft;
            }
            else if (Input.GetKeyDown(interactRight))
            {
                input = PlayerInputType.NavigateRight;
            }
            else if (Input.GetKeyDown(interactDown))
            {
                input = PlayerInputType.NavigateDown;
            }
            else if (Input.GetKeyDown(interactInspectKey) || Input.GetButtonDown(interactInspectButton) || Input.GetButtonDown(interactSkipButton))
            {
                input = PlayerInputType.Execute;
            }
            else if (Input.GetButtonDown(interactCancelButton))
            {
                input = PlayerInputType.Cancel;
            }
            else if (Input.GetKeyDown(interactOptionKey))
            {
                input = PlayerInputType.Option;
            }

            return input;
        }

        // Internal functions

        private void Awake()
        {
            playerMover = GetComponent<PlayerMover>();
            playerStateHandler = GetComponent<PlayerStateHandler>();
        }

        private void OnEnable()
        {
            playerStateHandler.playerStateChanged += ResetCursor;
        }

        private void OnDisable()
        {
            playerStateHandler.playerStateChanged -= ResetCursor;
        }

        private void ResetCursor()
        {
            SetCursor(CursorType.None);
        }

        private void Update()
        {
            if (playerStateHandler.GetPlayerState() == PlayerState.inTransition) { return; }

            if (playerStateHandler.GetPlayerState() == PlayerState.inWorld)
            {
                PlayerInputType playerInputType = GetPlayerInput();
                if (InteractWithGlobals(playerInputType)) return;
                if (InteractWithComponent(playerInputType)) return;
                if (InteractWithComponentManual(playerInputType)) return;
                if (InteractWithOptions(playerInputType)) return;
                SetCursor(CursorType.None);
            }
            else if (playerStateHandler.GetPlayerState() == PlayerState.inOptions)
            {
                PlayerInputType playerInputType = GetPlayerInput();
                if (InteractWithGlobals(playerInputType)) return;
                if (InteractWithOptions(playerInputType)) return;
            }
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

        private bool InteractWithOptions(PlayerInputType playerInputType)
        {
            if (playerInputType == PlayerInputType.Option)
            {
                playerStateHandler.EnterWorldOptions();
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
            return Camera.main.ScreenToWorldPoint(Input.mousePosition);
        }
    }
}

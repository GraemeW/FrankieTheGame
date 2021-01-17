using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Frankie.Core;

namespace Frankie.Control
{
    public class PlayerController : MonoBehaviour
    {
        // Data Types
        [System.Serializable]
        public struct CursorMapping
        {
            public CursorType type;
            public Texture2D texture;
            public Vector2 hotspot;
        }

        [System.Serializable]
        public enum PlayerState
        {
            inWorld,
            inTransition,
            inBattle
        }

        // Tunables
        [Header("Interaction")]
        [SerializeField] CursorMapping[] cursorMappings = null;
        [SerializeField] float raycastRadius = 0.1f;
        [SerializeField] float interactionDistance = 0.5f;
        [SerializeField] Transform interactionCenterPoint = null;
        [Header("Movement")]
        [SerializeField] float movementSpeed = 1.0f;
        [SerializeField] float speedMoveThreshold = 0.05f;
        [Header("CombatFader")]
        [SerializeField] float faderTimeouts = 2.0f;

        // State
        float inputHorizontal;
        float inputVertical;
        Vector2 lookDirection = new Vector2();
        float currentSpeed = 0;
        PlayerState playerState = PlayerState.inWorld;
        TransitionType transitionType = TransitionType.None;

        // Cached References
        Rigidbody2D playerRigidbody2D = null;
        Animator animator = null;

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

        public void SetLookDirection(Vector2 lookDirection)
        {
            this.lookDirection = lookDirection;
        }

        public Vector2 GetInteractionPosition()
        {
            return interactionCenterPoint.position;
        }

        public void EnterCombat(NPCController enemy, TransitionType transitionType)
        {
            StartCoroutine(QueueFaders(transitionType));
        }

        public PlayerState GetPlayerState()
        {
            return playerState;
        }

        public TransitionType GetTransitionType()
        {
            return transitionType;
        }

        public float GetFaderTimeouts()
        {
            return faderTimeouts;
        }

        // Internal functions
        static float Sign(float number)
        {
            return number < 0 ? -1 : (number > 0 ? 1 : 0);
        }

        private void Awake()
        {
            animator = GetComponent<Animator>();
            playerRigidbody2D = GetComponent<Rigidbody2D>();
        }

        private void Start()
        {
            SetLookDirection(Vector2.down); // Initialize look direction to avoid wonky
        }

        private void Update()
        {
            inputHorizontal = Input.GetAxis("Horizontal");
            inputVertical = Input.GetAxis("Vertical");
            if (InteractWithComponent()) return;
            SetCursor(CursorType.None);
        }

        private void FixedUpdate()
        {
            InteractWithMovement();
        }

        private void InteractWithMovement()
        {
            SetMovementParameters();
            UpdateAnimator();
            if (currentSpeed > speedMoveThreshold)
            {
                MovePlayer();
            }
        }

        private bool InteractWithComponent()
        {
            RaycastHit2D hitInfo = RaycastToMouseLocation();
            if (hitInfo.collider == null) { return false; }

            IRaycastable[] raycastables = hitInfo.transform.GetComponentsInChildren<IRaycastable>();
            if (raycastables != null)
            {
                foreach (IRaycastable raycastable in raycastables)
                {
                    if (raycastable.HandleRaycast(this, "Fire2", "Fire1"))
                    {
                        SetCursor(raycastable.GetCursorType());
                        return true;
                    }
                }
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

        private void SetMovementParameters()
        {
            Vector2 move = new Vector2(inputHorizontal, inputVertical);
            if (!Mathf.Approximately(move.x, 0.0f) || !Mathf.Approximately(move.y, 0.0f))
            {
                lookDirection.Set(move.x, move.y);
                lookDirection.Normalize();
            }
            currentSpeed = move.magnitude;
        }

        private void MovePlayer()
        {
            Vector2 position = playerRigidbody2D.position;
            position.x = position.x + movementSpeed * Sign(inputHorizontal) * Time.deltaTime;
            position.y = position.y + movementSpeed * Sign(inputVertical) * Time.deltaTime;
            playerRigidbody2D.MovePosition(position);
        }

        private void UpdateAnimator()
        {
            animator.SetFloat("Speed", currentSpeed);
            animator.SetFloat("xLook", lookDirection.x);
            animator.SetFloat("yLook", lookDirection.y);
        }

        private IEnumerator QueueFaders(TransitionType transitionType)
        {
            playerState = PlayerState.inTransition;
            this.transitionType = transitionType;
            yield return new WaitForSeconds(faderTimeouts);
            playerState = PlayerState.inBattle;
            this.transitionType = TransitionType.None;
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

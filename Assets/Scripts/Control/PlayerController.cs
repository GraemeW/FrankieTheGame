using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

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

        // Tunables
        [Header("Interaction")]
        [SerializeField] CursorMapping[] cursorMappings = null;
        [SerializeField] float raycastRadius = 0.1f;
        [SerializeField] float interactionDistance = 0.5f;
        [Header("Movement")]
        [SerializeField] float movementSpeed = 1.0f;
        [SerializeField] float speedMoveThreshold = 0.05f;

        // State
        float inputHorizontal;
        float inputVertical;
        Vector2 lookDirection = new Vector2();
        float currentSpeed;

        // Cached References
        Rigidbody2D playerRigidbody2D = null;
        Animator animator = null;

        // Public functions
        public float GetInteractionDistance()
        {
            return interactionDistance;
        }

        public RaycastHit2D PlayerCastToObject(Vector2 objectPosition)
        {
            RaycastHit2D[] hits = Physics2D.LinecastAll(transform.position, objectPosition);
            List<RaycastHit2D> sortedNonPlayerHits = hits.Where(x => !x.transform.gameObject.CompareTag("Player")).OrderBy(x => x.distance).ToList();
            return sortedNonPlayerHits[0];
        }

        // Internal functions
        static float Sign(float number)
        {
            return number < 0 ? -1 : (number > 0 ? 1 : 0);
        }


        private void Start()
        {
            animator = GetComponent<Animator>();
            playerRigidbody2D = GetComponent<Rigidbody2D>();
            lookDirection = Vector2.down; // Initialize look direction to avoid wonky
        }

        private void Update()
        {
            inputHorizontal = Input.GetAxis("Horizontal");
            inputVertical = Input.GetAxis("Vertical");
            InteractWithComponent();
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
            if (!hitInfo) { return false; }

            IRaycastable[] raycastables = hitInfo.transform.GetComponents<IRaycastable>();
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
            // No need to sort multiple hits since colliders will not overlap in 2D -- 1 collider / cast
            RaycastHit2D hitInfo = Physics2D.CircleCast(GetMouseRay(), raycastRadius, Vector2.zero);
            return hitInfo;
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

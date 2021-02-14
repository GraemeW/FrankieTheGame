using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Frankie.Core;
using Frankie.Combat;
using Frankie.Stats;

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
        [SerializeField] Transform interactionCenterPoint = null;
        [Header("Movement")]
        [SerializeField] float movementSpeed = 1.0f;
        [SerializeField] float speedMoveThreshold = 0.05f;
        [Header("Battle Handlers")]
        [SerializeField] GameObject battleControllerPrefab = null;

        // State
        float inputHorizontal;
        float inputVertical;
        Vector2 lookDirection = new Vector2();
        float currentSpeed = 0;
        PlayerState playerState = PlayerState.inWorld;
        TransitionType transitionType = TransitionType.None;
        BattleController battleController = null;

        // Cached References
        Rigidbody2D playerRigidbody2D = null;
        Party party = null;

        // Events
        public event Action<string> globalInput;
        public event Action playerStateChanged;

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

        public void EnterCombat(List<CombatParticipant> enemies, TransitionType transitionType)
        {
            // TODO:  Concept of 'pre-battle' where enemies can pile on ++ count up list of enemies -> transfer to battle
            this.transitionType = transitionType;

            GameObject battleControllerInstance = Instantiate(battleControllerPrefab);
            battleController = battleControllerInstance.GetComponent<BattleController>();
            battleController.Setup(enemies, transitionType);

            Fader fader = FindObjectOfType<Fader>();
            fader.UpdateFadeState(transitionType);

            battleController.battleStateChanged += HandleCombatComplete;

            playerState = PlayerState.inBattle;
            if (playerStateChanged != null)
            {
                playerStateChanged.Invoke();
            }
        }

        public void HandleCombatComplete(BattleState battleState)
        {
            if (battleState != BattleState.Complete) { return; }
            battleController.battleStateChanged -= HandleCombatComplete;

            Fader fader = FindObjectOfType<Fader>();
            fader.battleCanvasStateChanged += ExitCombat;
            transitionType = TransitionType.BattleComplete;
            fader.UpdateFadeState(transitionType);
        }

        public void ExitCombat(bool isBattleCanvasEnabled)
        {
            if (!isBattleCanvasEnabled)
            {
                // TODO:  Handling for party death

                FindObjectOfType<Fader>().battleCanvasStateChanged -= ExitCombat;
                Destroy(battleController.gameObject);
                battleController = null;

                playerState = PlayerState.inWorld;
                if (playerStateChanged != null)
                {
                    playerStateChanged.Invoke();
                }
            }
        }

        public void EnterDialogue()
        {
            // TODO:  refactor to pull out dialogue controller as spawned entity
        }

        public void ExitDialogue()
        {

        }

        public PlayerState GetPlayerState()
        {
            return playerState;
        }

        public TransitionType GetTransitionType()
        {
            return transitionType;
        }

        // Internal functions
        static float Sign(float number)
        {
            return number < 0 ? -1 : (number > 0 ? 1 : 0);
        }

        private void Awake()
        {
            party = GetComponent<Party>();
            playerRigidbody2D = GetComponent<Rigidbody2D>();
        }

        private void Start()
        {
            SetLookDirection(Vector2.down); // Initialize look direction to avoid wonky
        }

        private void Update()
        {
            if (playerState == PlayerState.inWorld)
            {
                KillRogueControllers();

                inputHorizontal = Input.GetAxis("Horizontal");
                inputVertical = Input.GetAxis("Vertical");
                if (InteractWithGlobals()) return;
                if (InteractWithComponent()) return;
                if (InteractWithComponentManual()) return;
                SetCursor(CursorType.None);
            }
            // Some level of input now also handled by dialogueBox && extensions -- I don't love this, think of a nicer way to handle
            // Maybe fold into playerconversant (generalize to dialogue controller) && centralize interaction there?
        }

        private void FixedUpdate()
        {
            if (playerState == PlayerState.inWorld)
            {
                InteractWithMovement();
            }
        }

        private void KillRogueControllers()
        {
            if (battleController != null)
            {
                HandleCombatComplete(BattleState.Complete);
            }
            // TODO:  same for dialogue, once implemented
        }

        // TODO:  Implement new unity input system
        private void InteractWithMovement()
        {
            SetMovementParameters();
            party.UpdatePartyAnimation(currentSpeed, lookDirection.x, lookDirection.y);
            if (currentSpeed > speedMoveThreshold)
            {
                MovePlayer();
            }
        }

        private bool InteractWithGlobals()
        {
            if (globalInput != null)
            {
                SetCursor(CursorType.None);
                globalInput.Invoke("Fire1");
                return true;
            }
            return false;
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

        private bool InteractWithComponentManual()
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                RaycastHit2D hitInfo = RaycastFromPlayerInLookDirection();
                if (hitInfo.collider == null) { return false; }

                IRaycastable[] raycastables = hitInfo.transform.GetComponentsInChildren<IRaycastable>();
                if (raycastables != null)
                {
                    foreach (IRaycastable raycastable in raycastables)
                    {
                        if (raycastable.HandleRaycast(this, KeyCode.E))
                        {
                            return true;
                        }
                    }
                }
                return false;
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
            RaycastHit2D[] hits = Physics2D.CircleCastAll(interactionCenterPoint.position, raycastRadius, lookDirection);
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

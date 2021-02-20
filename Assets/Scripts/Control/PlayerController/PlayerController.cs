using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Frankie.Core;
using Frankie.Combat;
using Frankie.Stats;
using Frankie.Speech;

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
        [SerializeField] string interactSkipButton = "Fire1";
        [SerializeField] string interactInspectButton = "Fire2";
        [SerializeField] KeyCode interactInspectKey = KeyCode.E;
        [SerializeField] string interactCancelButton = "Cancel";
        [SerializeField] CursorMapping[] cursorMappings = null;
        [SerializeField] float raycastRadius = 0.1f;
        [SerializeField] float interactionDistance = 0.5f;
        [SerializeField] Transform interactionCenterPoint = null;
        [Header("Movement")]
        [SerializeField] float movementSpeed = 1.0f;
        [SerializeField] float speedMoveThreshold = 0.05f;
        [Header("Other Controller Prefabs")]
        [SerializeField] GameObject battleControllerPrefab = null;
        [SerializeField] GameObject dialogueControllerPrefab = null;
        [Header("Messages")]
        [SerializeField] string messageCannotFight = "You are wounded and cannot fight.";

        // State
        float inputHorizontal;
        float inputVertical;
        Vector2 lookDirection = new Vector2();
        float currentSpeed = 0;
        PlayerState playerState = PlayerState.inWorld;
        TransitionType transitionType = TransitionType.None;
        BattleController battleController = null;
        DialogueController dialogueController = null;

        // Cached References
        Rigidbody2D playerRigidbody2D = null;
        Party party = null;
        WorldCanvas worldCanvas = null;

        // Events
        public event Action<PlayerInputType> globalInput;
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
            if (!party.IsAnyMemberAlive()) { OpenSimpleDialogue(messageCannotFight); return; }

            // TODO:  Concept of 'pre-battle' where enemies can pile on ++ count up list of enemies -> transfer to battle
            this.transitionType = transitionType;

            battleController = GetUniqueBattleController();
            battleController.Setup(enemies, transitionType);

            Fader fader = FindObjectOfType<Fader>();
            fader.UpdateFadeState(transitionType);

            battleController.battleStateChanged += HandleCombatComplete;

            SetPlayerState(PlayerState.inBattle);
        }

        public void HandleCombatComplete(BattleState battleState)
        {
            if (battleState != BattleState.Complete) { return; }
            battleController.battleStateChanged -= HandleCombatComplete;

            Fader fader = FindObjectOfType<Fader>();
            fader.battleUIStateChanged += ExitCombat;
            transitionType = TransitionType.BattleComplete;
            fader.UpdateFadeState(transitionType);
        }

        public void ExitCombat(bool isBattleCanvasEnabled)
        {
            if (!isBattleCanvasEnabled)
            {
                // TODO:  Handling for party death
                FindObjectOfType<Fader>().battleUIStateChanged -= ExitCombat;
                Destroy(battleController.gameObject);
                battleController = null;

                if (playerState == PlayerState.inBattle)
                {
                    SetPlayerState(PlayerState.inWorld);
                }
            }
        }

        public void EnterDialogue(AIConversant newConversant, Dialogue newDialogue)
        {
            GameObject dialogueControllerObject = Instantiate(dialogueControllerPrefab);
            dialogueController = dialogueControllerObject.GetComponent<DialogueController>();
            dialogueController.Setup(worldCanvas, this, party);
            dialogueController.InitiateConversation(newConversant, newDialogue);

            SetPlayerState(PlayerState.inDialogue);
        }

        public void OpenSimpleDialogue(string message)
        {
            dialogueController = GetUniqueDialogueController();
            dialogueController.Setup(worldCanvas, this, party);
            dialogueController.InitiateSimpleMessage(message);

            SetPlayerState(PlayerState.inDialogue);
        }

        public void ExitDialogue()
        {
            dialogueController = null;
            if (playerState == PlayerState.inDialogue)
            {
                SetPlayerState(PlayerState.inWorld);
            }
        }

        public void SetPlayerState(PlayerState playerState)
        {
            this.playerState = playerState;
            SetCursor(CursorType.None);
            if (playerStateChanged != null)
            {
                playerStateChanged.Invoke();
            }
        }

        public PlayerState GetPlayerState()
        {
            return playerState;
        }

        public TransitionType GetTransitionType()
        {
            return transitionType;
        }

        public PlayerInputType GetPlayerInput()
        {
            // TODO:  Implement new unity input system
            PlayerInputType input = PlayerInputType.DefaultNone;

            if (Input.GetKeyDown(interactInspectKey) || Input.GetButtonDown(interactInspectButton))
            {
                input = PlayerInputType.Execute;
            }
            else if (Input.GetButtonDown(interactCancelButton))
            {
                input = PlayerInputType.Cancel;
            }
            return input;
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
            worldCanvas = GameObject.FindGameObjectWithTag("WorldCanvas").GetComponent<WorldCanvas>();
        }

        private void Start()
        {
            SetLookDirection(Vector2.down); // Initialize look direction to avoid wonky
        }

        private void Update()
        {
            KillRogueControllers(playerState);

            if (playerState == PlayerState.inWorld)
            {
                inputHorizontal = Input.GetAxis("Horizontal");
                inputVertical = Input.GetAxis("Vertical");
                if (InteractWithGlobals()) return;
                if (InteractWithComponent()) return;
                if (InteractWithComponentManual()) return;
                SetCursor(CursorType.None);
            }
        }

        private void FixedUpdate()
        {
            if (playerState == PlayerState.inWorld)
            {
                InteractWithMovement();
            }
        }

        private DialogueController GetUniqueDialogueController()
        {
            if (dialogueController != null) { return dialogueController; }

            GameObject dialogueControllerObject = GameObject.FindGameObjectWithTag("DialogueController");
            DialogueController existingDialogueController = null;
            if (dialogueControllerObject != null)
            {
                existingDialogueController = dialogueControllerObject.GetComponent<DialogueController>();
            }
            
            if (existingDialogueController == null)
            {
                GameObject newDialogueControllerObject = Instantiate(dialogueControllerPrefab);
                dialogueController = newDialogueControllerObject.GetComponent<DialogueController>();
            }
            else
            {
                dialogueController = existingDialogueController;
            }

            return dialogueController;
        }

        private BattleController GetUniqueBattleController()
        {
            if (battleController != null) { return battleController; }

            GameObject battleControllerObject = GameObject.FindGameObjectWithTag("BattleController");
            BattleController existingBattleControllerController = null;
            if (battleControllerObject != null)
            {
                existingBattleControllerController = battleControllerObject.GetComponent<BattleController>();
            }

            if (existingBattleControllerController == null)
            {
                GameObject newBattleControllerObject = Instantiate(battleControllerPrefab);
                battleController = newBattleControllerObject.GetComponent<BattleController>();
            }
            else
            {
                battleController = existingBattleControllerController;
            }

            return battleController;
        }

        private void KillRogueControllers(PlayerState playerState)
        {
            if (playerState == PlayerState.inWorld)
            {
                if (battleController != null)
                {
                    HandleCombatComplete(BattleState.Complete);
                }
                if (dialogueController != null)
                {
                    ExitDialogue();
                }
            }
            else if (playerState == PlayerState.inBattle)
            {
                if (dialogueController != null)
                {
                    ExitDialogue();
                }
            }
            else if (playerState == PlayerState.inDialogue)
            {
                if (battleController != null)
                {
                    HandleCombatComplete(BattleState.Complete);
                }
            }
        }

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
            if (Input.GetButtonDown(interactSkipButton) || Input.GetKeyDown(interactInspectKey))
            {
                if (globalInput != null)
                {
                    globalInput.Invoke(PlayerInputType.Execute);
                    return true;
                }
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
                    if (raycastable.HandleRaycast(this, interactInspectButton, interactSkipButton))
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
            if (Input.GetKeyDown(interactInspectKey))
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

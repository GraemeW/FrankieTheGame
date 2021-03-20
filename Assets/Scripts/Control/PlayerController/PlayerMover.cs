using Frankie.Stats;
using UnityEngine;
using Frankie.Saving;
using Frankie.Core;

namespace Frankie.Control
{
    [RequireComponent(typeof(PlayerController))]
    public class PlayerMover : MonoBehaviour, ISaveable
    {
        // Tunables
        [SerializeField] float movementSpeed = 1.0f;
        [SerializeField] float speedMoveThreshold = 0.05f;

        // State
        float inputHorizontal;
        float inputVertical;
        Vector2 lookDirection = new Vector2();
        float currentSpeed = 0;

        // Cached References
        PlayerStateHandler playerStateHandler = null;
        Rigidbody2D playerRigidbody2D = null;
        Party party = null;

        static float Sign(float number)
        {
            return number < 0 ? -1 : (number > 0 ? 1 : 0);
        }

        private void Awake()
        {
            playerRigidbody2D = GetComponent<Rigidbody2D>();
            playerStateHandler = GetComponent<PlayerStateHandler>();
            party = GetComponent<Party>();
        }

        private void Start()
        {
            SetLookDirection(Vector2.down); // Initialize look direction to avoid wonky
        }

        private void Update()
        {
            inputHorizontal = Input.GetAxis("Horizontal");
            inputVertical = Input.GetAxis("Vertical");
        }

        private void FixedUpdate()
        {
            if (playerStateHandler.GetPlayerState() == PlayerState.inWorld)
            {
                InteractWithMovement();
            }
        }

        public void SetLookDirection(Vector2 lookDirection)
        {
            this.lookDirection = lookDirection;
        }

        public Vector2 GetLookDirection()
        {
            return lookDirection;
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

        // Save State
        [System.Serializable]
        struct MoverSaveData
        {
            public SerializableVector2 position;
        }

        public object CaptureState()
        {
            MoverSaveData data = new MoverSaveData
            {
                position = new SerializableVector2(transform.position)
            };
            return data;
        }

        public void RestoreState(object state)
        {
            MoverSaveData data = (MoverSaveData)state;
            transform.position = data.position.ToVector();
        }
    }

}

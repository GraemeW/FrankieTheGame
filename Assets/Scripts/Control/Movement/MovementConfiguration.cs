using UnityEngine;

namespace Frankie.Control
{
    [CreateAssetMenu(fileName = "New Movement Configuration", menuName = "Characters/New Movement Configuration", order = 1)]
    public class MovementConfiguration : ScriptableObject
    {
        // Tunables
        [field: SerializeField] public MovementStyle movementStyle { get; private set; } = MovementStyle.Walk;
        [field: SerializeField] public bool usingPathFinding { get; private set; } = false;
        [field: SerializeField] public float baseMovementSpeed { get; private set; } = 1.0f;
        [field: SerializeField] public int targetMovementHistoryLength { get; private set; } = 15;
        [field: SerializeField] public float warpDelay { get; private set; } = 1.25f;
        [field: SerializeField] public float warpPostTargetDelay { get; private set; } = 0.25f;
        [field: SerializeField] public float warpPathfindingTravelDistance { get; private set; } = 2f;

        // Call for no target (e.g. input movement)
        public bool MoveToTarget(Mover mover, float deltaTime, out Vector2 newPosition)
        {
            if (mover == null)
            {
                newPosition = Vector2.zero;
                return false; 
            }
            
            newPosition = GetNewPosition(mover.GetCurrentPosition(), mover.GetCurrentSpeed(), mover.GetLookDirection(), deltaTime);
            switch (movementStyle)
            {
                case MovementStyle.Warp:
                    // Skip delay, since based on input
                    mover.transform.position = newPosition;
                    break;
                case MovementStyle.Walk:
                default:
                    mover.MoveRigidBody(newPosition);
                    break;
            }
            mover.ResetTimeSinceLastMove();
            return true;
        }

        // Call for explicit target
        public bool MoveToTarget(Mover mover, Vector2 target, float deltaTime, out Vector2 newPosition)
        {
            if (mover == null) { newPosition = Vector2.zero; return false; }

            newPosition = movementStyle switch
            {
                MovementStyle.Warp => target,
                _ => GetNewPosition(mover.GetCurrentPosition(), mover.GetCurrentSpeed(), mover.GetLookDirection(), deltaTime)
            };

            if (movementStyle == MovementStyle.Warp)
            {
                if (mover.GetTimeSinceLastMove() < warpDelay) { return false; }
                // Called on the mover itself since we need a Mono to delay via coroutine or timer
                mover.QueueDelayedMoveExecution(newPosition, warpPostTargetDelay);
                mover.ResetTimeSinceLastMove();
                return true;
            }

            ExecuteMove(mover, newPosition);
            return true;
        }

        public void ExecuteMove(Mover mover, Vector2 newPosition)
        {
            switch (movementStyle)
            {
                case MovementStyle.Warp:
                    mover.transform.position = newPosition;
                    break;
                case MovementStyle.Walk:
                default:
                    mover.MoveRigidBody(newPosition);
                    break;
            }
            mover.ResetTimeSinceLastMove();
        }

        private static Vector2 GetNewPosition(Vector2 position, float speed, Vector2 lookDirection, float deltaTime)
        {
            return new Vector2(
                position.x + speed * Mover.SignFloored(lookDirection.x) * deltaTime, 
                position.y + speed * Mover.SignFloored(lookDirection.y) * deltaTime);
        }
    }
}
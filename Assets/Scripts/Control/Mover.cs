using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frankie.Saving;

namespace Frankie.Control
{
    public class Mover : MonoBehaviour, ISaveable
    {
        // Tunables
        [SerializeField] protected float movementSpeed = 1.0f;

        // State
        protected Vector2 lookDirection = new Vector2();
        protected float currentSpeed = 0;

        // Cached References
        protected Rigidbody2D playerRigidbody2D = null;

        protected static float Sign(float number)
        {
            return number < 0 ? -1 : (number > 0 ? 1 : 0);
        }

        protected virtual void Awake()
        {
            playerRigidbody2D = GetComponent<Rigidbody2D>();
        }

        protected virtual void Start()
        {
            SetLookDirection(Vector2.down); // Initialize look direction to avoid wonky
        }

        public void SetLookDirection(Vector2 lookDirection)
        {
            this.lookDirection = lookDirection;
        }

        public Vector2 GetLookDirection()
        {
            return lookDirection;
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

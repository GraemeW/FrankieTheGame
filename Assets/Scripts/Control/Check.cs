using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Frankie.Control
{
    [RequireComponent(typeof(Collider2D))]
    public class Check : MonoBehaviour, IRaycastable
    {
        // Tunables
        [SerializeField] protected bool overrideDefaultInteractionDistance = false;
        [SerializeField] protected float interactionDistance = 0.3f;

        // Events
        public InteractionEvent checkInteraction;

        // Data Structures
        [System.Serializable]
        public class InteractionEvent : UnityEvent<PlayerController>
        {
        }

        // Raycastable Interface Implementation
        public virtual CursorType GetCursorType()
        {
            return CursorType.Check;
        }

        public virtual bool HandleRaycast(PlayerController callingController, string interactButtonOne = "Fire1", string interactButtonTwo = "Fire2")
        {
            if (!this.CheckDistance(gameObject, transform.position, callingController,
                overrideDefaultInteractionDistance, interactionDistance))
            {
                return false;
            }

            if (Input.GetButtonDown(interactButtonOne))
            {
                if (checkInteraction != null)
                {
                    checkInteraction.Invoke(callingController);
                }
            }
            return true;
        }

        public virtual bool HandleRaycast(PlayerController callingController, KeyCode interactKeyOne = KeyCode.E, KeyCode interactKeyTwo = KeyCode.Return)
        {
            if (!this.CheckDistance(gameObject, transform.position, callingController,
                overrideDefaultInteractionDistance, interactionDistance))
            {
                return false;
            }

            if (Input.GetKeyDown(interactKeyOne))
            {
                if (checkInteraction != null)
                {
                    checkInteraction.Invoke(callingController);
                }

            }
            return true;
        }

        bool IRaycastable.CheckDistanceTemplate()
        {
            // Not evaluated -> IRaycastableExtension
            return false;
        }
    }
}
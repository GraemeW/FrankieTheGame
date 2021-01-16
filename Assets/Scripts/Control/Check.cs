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
        [SerializeField] bool overrideDefaultInteractionDistance = false;
        [SerializeField] float interactionDistance = 0.3f;

        // Events
        public InteractionEvent checkInteraction;

        // Data Structures
        [System.Serializable]
        public class InteractionEvent : UnityEvent<PlayerController>
        {
        }

        // Raycastable Interface Implementation
        public CursorType GetCursorType()
        {
            return CursorType.Check;
        }

        public bool HandleRaycast(PlayerController callingController, string interactButtonOne = "Fire1", string interactButtonTwo = "Fire2")
        {
            if (!this.CheckDistance(gameObject, transform.position, callingController,
                overrideDefaultInteractionDistance, interactionDistance))
            {
                return false;
            }

            if (Input.GetButtonDown(interactButtonOne))
            {
                checkInteraction.Invoke(callingController);
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
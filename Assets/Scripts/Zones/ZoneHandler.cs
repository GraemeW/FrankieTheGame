using Frankie.Control;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Frankie.Zone
{
    public class ZoneHandler : MonoBehaviour, IRaycastable
    {
        // Tunables 
        [SerializeField] ZoneNode zoneNode = null;
        [SerializeField] bool overrideDefaultInteractionDistance = false;
        [SerializeField] float interactionDistance = 0.3f;
        [SerializeField] Transform warpPosition = null;

        // Events
        public UnityEvent zoneInteraction;

        // Public Functions
        public ZoneNode GetZoneNode()
        {
            return zoneNode;
        }

        public Transform GetWarpPosition()
        {
            return warpPosition;
        }

        // Private Functions
        private ZoneNode SelectRandomNodeFromChildren()
        {
            if (zoneNode == null || zoneNode.GetChildren() == null) { return null; }

            int nodeIndex = UnityEngine.Random.Range(0, zoneNode.GetChildren().Count);
            return Zone.GetFromName(zoneNode.GetZoneName()).GetNodeFromID(zoneNode.GetChildren()[nodeIndex]);
        }

        private void WarpPlayerToNextNode(PlayerController callingController)
        {
            ZoneNode nextNode = SelectRandomNodeFromChildren();
            if (nextNode == null) { return; }

            ZoneHandler[] availableZoneHandlers = FindObjectsOfType<ZoneHandler>();
            foreach (ZoneHandler zoneHandler in availableZoneHandlers)
            {
                if (nextNode == zoneHandler.GetZoneNode())
                {
                    if (warpPosition != null) 
                    { 
                        callingController.transform.position = zoneHandler.GetWarpPosition().position;
                        Vector2 lookDirection = zoneHandler.GetWarpPosition().position - zoneHandler.transform.position;
                        lookDirection.Normalize();
                        callingController.SetLookDirection(lookDirection);
                    }
                    else { callingController.transform.position = zoneHandler.transform.position; }

                    if (zoneInteraction != null)
                    {
                        zoneInteraction.Invoke();
                    }
                }
            }
        }

        // IRaycastable Implementation
        public CursorType GetCursorType()
        {
            return CursorType.Zone;
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
                WarpPlayerToNextNode(callingController);
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

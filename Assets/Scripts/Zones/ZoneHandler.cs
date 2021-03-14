using Frankie.Control;
using UnityEngine;
using UnityEngine.Events;
using Frankie.SceneManagement;
using System;

namespace Frankie.Zone
{
    public class ZoneHandler : MonoBehaviour, IRaycastable
    {
        // Tunables 
        [SerializeField] ZoneNode zoneNode = null;
        [SerializeField] bool overrideDefaultInteractionDistance = false;
        [SerializeField] float interactionDistance = 0.3f;
        [SerializeField] Transform warpPosition = null;
        [SerializeField] float delayToDestroyAfterSceneLoading = 0.1f;
        [Header("Game Object (Dis)Enablement")]
        [SerializeField] bool disableOnExit = true;
        [SerializeField] GameObject roomParent = null;

        // State
        bool inTransitToNextScene = false;
        ZoneNode queuedZoneNode = null;

        // Cached References
        Fader fader = null;

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

        public void EnableRoomParent(bool enable)
        {
            if (roomParent != null)
            {
                roomParent.SetActive(enable);
            }
        }

        // Static functions
        public static ZoneNode SelectNodeFromIDs(string zoneID, string nodeID)
        {
            return Zone.GetFromName(zoneID).GetNodeFromID(nodeID);
        }

        // Private Functions
        private void WarpPlayerToNextNode(PlayerController callingController)
        {
            ZoneNode nextNode = SetUpNextNode(callingController);
            if (nextNode == null) { return; }

            if (!inTransitToNextScene) // On scene transition, called via fader (sceneTransitionComplete)
            {
                MoveToNextNode(callingController, nextNode);
            }
        }

        private ZoneNode SetUpNextNode(PlayerController callingController)
        {
            ZoneNode nextNode = null;
            if (zoneNode.HasSceneReference())
            {
                callingController.SetPlayerState(PlayerState.inTransition);
                SetZoneHandlerToPersistOnSceneTransition();

                Tuple<string, string> zoneIDNodeIDPair = zoneNode.GetSceneReferenceNodePair();
                SceneReference sceneReference = Zone.GetFromName(zoneIDNodeIDPair.Item1).GetSceneReference();
                nextNode = ZoneHandler.SelectNodeFromIDs(zoneIDNodeIDPair.Item1, zoneIDNodeIDPair.Item2);

                TransitionToNextScene(nextNode, sceneReference);
                callingController.SetPlayerState(PlayerState.inWorld);
            }
            else
            {
                nextNode = SelectRandomNodeFromChildren();
            }
            return nextNode;
        }

        private void SetZoneHandlerToPersistOnSceneTransition()
        {
            inTransitToNextScene = true;
            gameObject.transform.parent = null;
            DontDestroyOnLoad(gameObject);
        }

        private void TransitionToNextScene(ZoneNode nextNode, SceneReference sceneReference)
        {
            if (fader == null) { fader = FindObjectOfType<Fader>(); }
            queuedZoneNode = nextNode;
            fader.sceneTransitionComplete += QueuedMoveToNextNode;
            fader.UpdateFadeState(TransitionType.Zone, sceneReference);
        }

        private void MoveToNextNode(PlayerController callingController, ZoneNode nextNode)
        {
            ZoneHandler[] availableZoneHandlers = FindObjectsOfType<ZoneHandler>();
            foreach (ZoneHandler zoneHandler in availableZoneHandlers)
            {
                if (nextNode == zoneHandler.GetZoneNode())
                {
                    if (GetWarpPosition() != null)
                    {
                        callingController.transform.position = zoneHandler.GetWarpPosition().position;
                        Vector2 lookDirection = zoneHandler.GetWarpPosition().position - zoneHandler.transform.position;
                        lookDirection.Normalize();
                        callingController.GetPlayerMover().SetLookDirection(lookDirection);
                    }
                    else { callingController.transform.position = zoneHandler.transform.position; }

                    ToggleParentGameObjects(zoneHandler);
                    if (zoneInteraction != null) // Unity event, linked via Unity Editor
                    {
                        zoneInteraction.Invoke();
                    }

                    if (inTransitToNextScene) { RemoveZoneHandler(); }
                    break; // Node transport complete, no need to complete loop
                }
            }
            if (inTransitToNextScene) { RemoveZoneHandler(); }
        }

        private void RemoveZoneHandler()
        {
            if (fader == null) { fader = FindObjectOfType<Fader>(); }
            fader.sceneTransitionComplete -= QueuedMoveToNextNode;
            Destroy(gameObject, delayToDestroyAfterSceneLoading);
        }

        private void QueuedMoveToNextNode()
        {
            if (queuedZoneNode == null)
            {
                RemoveZoneHandler();
                return; 
            }
            PlayerController playerController = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>();
            MoveToNextNode(playerController, queuedZoneNode);
        }

        private ZoneNode SelectRandomNodeFromChildren()
        {
            if (zoneNode == null || zoneNode.GetChildren() == null) { return null; }

            int nodeIndex = UnityEngine.Random.Range(0, zoneNode.GetChildren().Count);
            return Zone.GetFromName(zoneNode.GetZoneName()).GetNodeFromID(zoneNode.GetChildren()[nodeIndex]);
        }

        private void ToggleParentGameObjects(ZoneHandler nextZoneHandler)
        {
            if (disableOnExit)
            {
                EnableRoomParent(false);
            }
            nextZoneHandler.EnableRoomParent(true);
        }

        // IRaycastable Implementation
        public CursorType GetCursorType()
        {
            return CursorType.Zone;
        }

        public bool HandleRaycast(PlayerController callingController, PlayerInputType inputType, PlayerInputType matchType)
        {
            if (!this.CheckDistance(gameObject, transform.position, callingController, 
                overrideDefaultInteractionDistance, interactionDistance)) 
            { 
                return false; 
            }

            if (inputType == matchType)
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

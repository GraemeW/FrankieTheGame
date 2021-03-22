using Frankie.Control;
using UnityEngine;
using UnityEngine.Events;
using System;
using Frankie.Core;
using System.Collections.Generic;

namespace Frankie.ZoneManagement
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
        [Header("Warp Properties")]
        [SerializeField] bool randomizeChoice = true;
        [SerializeField] string choiceMessage = "Where do you want to go?";

        // State
        bool inTransitToNextScene = false;
        ZoneNode queuedZoneNode = null;
        PlayerStateHandler currentPlayerStateHandler = null;
        PlayerController currentPlayerController = null;

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
        private void WarpPlayerToNextNode(PlayerStateHandler playerStateHandler, PlayerController playerController)
        {
            ZoneNode nextNode = SetUpNextNode(playerStateHandler);
            if (nextNode == null) { return; }

            if (!inTransitToNextScene) // On scene transition, called via fader (sceneTransitionComplete)
            {
                MoveToNextNode(playerController, nextNode);
            }
        }

        private void WarpPlayerToNode(string nodeID)
        {
            ZoneNode nextNode = SetUpNextNode(nodeID);
            if (nextNode == null) { return; }

            if (!inTransitToNextScene) // On scene transition, called via fader (sceneTransitionComplete)
            {
                MoveToNextNode(currentPlayerController, nextNode);
            }
            currentPlayerStateHandler = null;
            currentPlayerController = null;
        }

        private ZoneNode SetUpNextNode(PlayerStateHandler playerStateHandler)
        {
            ZoneNode nextNode = SelectRandomNodeFromChildren();
            if (nextNode == null) { return null; }

            nextNode = GetNodeOnSceneTransition(playerStateHandler, nextNode);
            return nextNode;
        }

        private ZoneNode SetUpNextNode(string nodeID)
        {
            ZoneNode nextNode = Zone.GetFromName(zoneNode.GetZoneName()).GetNodeFromID(nodeID);
            if (nextNode == null) { return null; }

            nextNode = GetNodeOnSceneTransition(currentPlayerStateHandler, nextNode);
            return nextNode;
        }

        private ZoneNode GetNodeOnSceneTransition(PlayerStateHandler playerStateHandler, ZoneNode nextNode)
        {
            if (nextNode.HasSceneReference())
            {
                playerStateHandler.SetPlayerState(PlayerState.inTransition);
                SetZoneHandlerToPersistOnSceneTransition();

                ZoneIDNodeIDPair zoneIDNodeIDPair = nextNode.GetZoneReferenceNodeReferencePair();
                Zone nextZone = Zone.GetFromName(zoneIDNodeIDPair.zoneID);
                nextNode = ZoneHandler.SelectNodeFromIDs(zoneIDNodeIDPair.zoneID, zoneIDNodeIDPair.nodeID);

                TransitionToNextScene(nextZone, nextNode);
                playerStateHandler.SetPlayerState(PlayerState.inWorld);
            }

            return nextNode;
        }

        private void SetZoneHandlerToPersistOnSceneTransition()
        {
            inTransitToNextScene = true;
            gameObject.transform.parent = null;
            DontDestroyOnLoad(gameObject);
        }

        private void TransitionToNextScene(Zone nextZone, ZoneNode nextNode)
        {
            if (fader == null) { fader = FindObjectOfType<Fader>(); }
            queuedZoneNode = nextNode;
            fader.fadingOut += QueuedMoveToNextNode;
            fader.UpdateFadeState(TransitionType.Zone, nextZone);
        }

        private void MoveToNextNode(PlayerController playerController, ZoneNode nextNode)
        {
            ZoneHandler[] availableZoneHandlers = FindObjectsOfType<ZoneHandler>();
            foreach (ZoneHandler zoneHandler in availableZoneHandlers)
            {
                if (nextNode == zoneHandler.GetZoneNode())
                {
                    if (GetWarpPosition() != null)
                    {
                        playerController.transform.position = zoneHandler.GetWarpPosition().position;
                        Vector2 lookDirection = zoneHandler.GetWarpPosition().position - zoneHandler.transform.position;
                        lookDirection.Normalize();
                        playerController.GetPlayerMover().SetLookDirection(lookDirection);
                    }
                    else { playerController.transform.position = zoneHandler.transform.position; }

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
            fader.fadingOut -= QueuedMoveToNextNode;
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

        private bool? IsSimpleWarp()
        {
            if (zoneNode == null || zoneNode.GetChildren() == null) { return null; }

            if (zoneNode.GetChildren().Count == 1 || randomizeChoice)
            {
                return true;
            }
            return false;
        }

        private void SetUpCurrentReferences(PlayerStateHandler playerStateHandler, PlayerController playerController)
        {
            currentPlayerStateHandler = playerStateHandler;
            currentPlayerController = playerController;
        }

        private List<ChoiceActionPair> GetChoiceActionPairs()
        {
            List<ChoiceActionPair> choiceActionPairs = new List<ChoiceActionPair>();
            foreach (string childNode in zoneNode.GetChildren())
            {
                ChoiceActionPair choiceActionPair = new ChoiceActionPair(childNode, WarpPlayerToNode, childNode);
                choiceActionPairs.Add(choiceActionPair);
            }
            return choiceActionPairs;
        }

        // IRaycastable Implementation
        public CursorType GetCursorType()
        {
            return CursorType.Zone;
        }

        public bool HandleRaycast(PlayerStateHandler playerStateHandler, PlayerController playerController, PlayerInputType inputType, PlayerInputType matchType)
        {
            if (!this.CheckDistance(gameObject, transform.position, playerController, 
                overrideDefaultInteractionDistance, interactionDistance)) 
            { 
                return false; 
            }

            if (inputType == matchType)
            {
                if (IsSimpleWarp() == true)
                {
                    WarpPlayerToNextNode(playerStateHandler, playerController);
                }
                else if (IsSimpleWarp() == false)
                {
                    SetUpCurrentReferences(playerStateHandler, playerController);
                    playerStateHandler.OpenSimpleChoiceDialogue(choiceMessage, GetChoiceActionPairs());
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

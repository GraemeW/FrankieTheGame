using Frankie.Control;
using UnityEngine;
using UnityEngine.Events;
using System;
using Frankie.Core;
using System.Collections.Generic;
using Frankie.Saving;
using Frankie.Utils;

namespace Frankie.ZoneManagement
{
    public class ZoneHandler : MonoBehaviour, IRaycastable, ISaveable
    {
        // Tunables 
        [SerializeField] ZoneNode zoneNode = null;
        [SerializeField] bool overrideDefaultInteractionDistance = false;
        [SerializeField] float interactionDistance = 0.3f;
        [SerializeField] Transform warpPosition = null;
        [SerializeField] float delayToDestroyAfterSceneLoading = 0.1f;
        [Header("Game Object (Dis)Enablement")]
        [SerializeField] GameObject roomParent = null;
        [SerializeField] bool disableOnStart = false;
        [SerializeField] bool disableOnExit = true;
        [Header("Warp Properties")]
        [SerializeField] bool randomizeChoice = true;
        [SerializeField] string choiceMessage = "Where do you want to go?";

        // State
        bool roomParentSetBySave = false;
        bool inTransitToNextScene = false;
        ZoneNode queuedZoneNode = null;
        PlayerStateHandler currentPlayerStateHandler = null;
        PlayerController currentPlayerController = null;

        // Cached References
        Fader fader = null;

        // Events
        public UnityEvent<bool> zoneInteraction;

        // Public Functions
        public ZoneNode GetZoneNode()
        {
            return zoneNode;
        }

        public Transform GetWarpPosition()
        {
            return warpPosition;
        }

        public bool EnableRoomParent(bool enable)
        {
            if (roomParent != null)
            {
                roomParent.SetActive(enable);
                return true;
            }
            return false;
        }

        // Static functions
        public static ZoneNode SelectNodeFromIDs(string zoneID, string nodeID)
        {
            return Zone.GetFromName(zoneID).GetNodeFromID(nodeID);
        }

        // Private Functions
        private void Start()
        {
            if (disableOnStart && !roomParentSetBySave) { EnableRoomParent(false); }
        }

        private void WarpPlayerToNextNode()
        {
            ZoneNode nextNode = SetUpNextNode();
            if (nextNode == null) { return; }

            if (!inTransitToNextScene) // On scene transition, called via fader (sceneTransitionComplete)
            {
                MoveToNextNode(nextNode);
            }
        }

        private void WarpPlayerToNode(string nodeID)
        {
            ZoneNode nextNode = SetUpNextNode(nodeID);
            if (nextNode == null) { return; }

            if (!inTransitToNextScene) // On scene transition, called via fader (sceneTransitionComplete)
            {
                MoveToNextNode(nextNode);
            }
        }

        private ZoneNode SetUpNextNode()
        {
            ZoneNode nextNode = SelectRandomNodeFromChildren();
            if (nextNode == null) { return null; }

            nextNode = GetNodeOnSceneTransition(nextNode);
            return nextNode;
        }

        private ZoneNode SetUpNextNode(string nodeID)
        {
            ZoneNode nextNode = Zone.GetFromName(zoneNode.GetZoneName()).GetNodeFromID(nodeID);
            if (nextNode == null) { return null; }

            nextNode = GetNodeOnSceneTransition(nextNode);
            return nextNode;
        }

        private ZoneNode GetNodeOnSceneTransition(ZoneNode nextNode)
        {
            if (nextNode.HasSceneReference())
            {
                currentPlayerStateHandler.SetPlayerState(PlayerState.inTransition);
                SetZoneHandlerToPersistOnSceneTransition();

                ZoneIDNodeIDPair zoneIDNodeIDPair = nextNode.GetZoneReferenceNodeReferencePair();
                Zone nextZone = Zone.GetFromName(zoneIDNodeIDPair.zoneID);
                nextNode = ZoneHandler.SelectNodeFromIDs(zoneIDNodeIDPair.zoneID, zoneIDNodeIDPair.nodeID);

                TransitionToNextScene(nextZone, nextNode); // NOTE:  Exit inTransition done on queued move
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

        private void MoveToNextNode(ZoneNode nextNode)
        {
            ZoneHandler[] availableZoneHandlers = FindObjectsOfType<ZoneHandler>();
            foreach (ZoneHandler zoneHandler in availableZoneHandlers)
            {
                if (nextNode == zoneHandler.GetZoneNode())
                {
                    if (GetWarpPosition() != null)
                    {
                        currentPlayerController.transform.position = zoneHandler.GetWarpPosition().position;
                        Vector2 lookDirection = zoneHandler.GetWarpPosition().position - zoneHandler.transform.position;
                        lookDirection.Normalize();
                        currentPlayerController.GetPlayerMover().SetLookDirection(lookDirection);
                        currentPlayerController.GetPlayerMover().ResetHistory(zoneHandler.GetWarpPosition().position);
                    }
                    else 
                    { 
                        currentPlayerController.transform.position = zoneHandler.transform.position;
                        currentPlayerController.GetPlayerMover().ResetHistory(zoneHandler.transform.position);
                    }

                    ToggleParentGameObjects(zoneHandler);
                    OnZoneInteraction();
                    queuedZoneNode = null;

                    break; // Node transport complete, no need to complete loop
                }
            }

            // Failsafe -- no movement
            ExitMove();
        }

        private void OnZoneInteraction()
        {
            if (zoneInteraction != null)
            {
                if (queuedZoneNode == null)
                {
                    zoneInteraction.Invoke(false);
                }
                else
                {
                    zoneInteraction.Invoke(true);
                }
            }
        }

        private void ExitMove()
        {
            currentPlayerStateHandler.SetPlayerState(PlayerState.inWorld);
            if (inTransitToNextScene) { RemoveZoneHandler(); }
            SetUpCurrentReferences(null, null);
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
            MoveToNextNode(queuedZoneNode);
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
                SetUpCurrentReferences(playerStateHandler, playerController);
                if (IsSimpleWarp() == true)
                {
                    WarpPlayerToNextNode();
                }
                else if (IsSimpleWarp() == false)
                {
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

        public LoadPriority GetLoadPriority()
        {
            return LoadPriority.ObjectProperty;
        }

        SaveState ISaveable.CaptureState()
        {
            bool isActive = true;
            if (roomParent != null)
            {
                isActive = roomParent.activeSelf;
            }
            SaveState saveState = new SaveState(GetLoadPriority(), isActive);
            return saveState;
        }

        void ISaveable.RestoreState(SaveState saveState)
        {
            if (EnableRoomParent((bool)saveState.GetState())) { roomParentSetBySave = true; }
        }
    }

}

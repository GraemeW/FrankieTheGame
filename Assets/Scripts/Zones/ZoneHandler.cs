using Frankie.Control;
using UnityEngine;
using UnityEngine.Events;
using System;
using Frankie.Core;
using System.Collections.Generic;
using Frankie.Saving;
using Frankie.Utils;
using System.Text.RegularExpressions;

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
        [SerializeField] Room roomParent = null;
        [SerializeField] bool disableOnExit = true;
        [Header("Warp Properties")]
        [SerializeField] bool randomizeChoice = true;
        [SerializeField] string choiceMessage = "Where do you want to go?";

        // State
        bool inTransitToNextScene = false;
        string queuedZoneNodeID = null;
        PlayerStateHandler currentPlayerStateHandler = null;
        PlayerController currentPlayerController = null;

        // Cached References
        Fader fader = null;

        // Events
        public UnityEvent zoneInteraction;

        #region PublicMethods
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
                roomParent.gameObject.SetActive(enable);
                return true;
            }
            return false;
        }
        #endregion

        #region StaticMethods
        public static ZoneNode SelectNodeFromIDs(string zoneID, string nodeID)
        {
            return Zone.GetFromName(zoneID).GetNodeFromID(nodeID);
        }

        public static string GetStaticZoneNamePretty(string zoneName)
        {
            return Regex.Replace(zoneName, "([a-z])_?([A-Z])", "$1 $2");
        }

        private static List<ZoneHandler> FindAllZoneHandlersInScene()
        {
            List<ZoneHandler> zoneHandlers = new List<ZoneHandler>();
            // Find visible handlers
            zoneHandlers.AddRange(FindObjectsOfType<ZoneHandler>());

            // Find invisible handlers
            CheckWithToggleChildren[] toggleableGameObjects = FindObjectsOfType<CheckWithToggleChildren>();
            foreach (CheckWithToggleChildren toggleableGameObject in toggleableGameObjects)
            {
                foreach (ZoneHandler hiddenZoneHandler in toggleableGameObject.GetComponentsInChildren<ZoneHandler>(true))
                {
                    if (zoneHandlers.Contains(hiddenZoneHandler)) { continue; }
                    zoneHandlers.Add(hiddenZoneHandler);
                }
            }
            return zoneHandlers;
        }
        #endregion

        #region PrivateMethods
        private void WarpPlayerToNextNode()
        {
            ZoneNode nextNode = SetUpNextNode();
            if (nextNode == null) { return; }

            if (!inTransitToNextScene) // On scene transition, called via fader (sceneTransitionComplete)
            {
                MoveToNextNode(nextNode.GetNodeID());
            }
        }

        private void WarpPlayerToNode(string nodeID)
        {
            ZoneNode nextNode = SetUpNextNode(nodeID);
            if (nextNode == null) { return; }

            if (!inTransitToNextScene) // On scene transition, called via fader (sceneTransitionComplete)
            {
                MoveToNextNode(nodeID);
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
                currentPlayerStateHandler.EnterZoneTransition();
                SetZoneHandlerToPersistOnSceneTransition();

                ZoneNodePair zoneNodePair = nextNode.GetZoneReferenceNodeReferencePair();

                TransitionToNextScene(zoneNodePair.zone, zoneNodePair.zoneNode); // NOTE:  Exit inTransition done on queued move
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
            if (fader == null) { return; }

            queuedZoneNodeID = nextNode.GetNodeID();
            fader.fadingOut += QueuedMoveToNextNode;
            fader.UpdateFadeState(TransitionType.Zone, nextZone);
        }

        private void MoveToNextNode(string nextNodeID)
        {
            foreach (ZoneHandler zoneHandler in FindAllZoneHandlersInScene())
            {
                if (nextNodeID == zoneHandler.GetZoneNode().GetNodeID())
                {
                    Vector3 warpPosition = zoneHandler.GetWarpPosition().position;
                    if (warpPosition != null)
                    {
                        currentPlayerController.gameObject.transform.position = warpPosition;
                        Vector2 lookDirection = warpPosition - zoneHandler.transform.position;
                        lookDirection.Normalize();
                        currentPlayerController.GetPlayerMover().SetLookDirection(lookDirection);
                        currentPlayerController.GetPlayerMover().ResetHistory(warpPosition);
                    }
                    else 
                    { 
                        currentPlayerController.gameObject.transform.position = zoneHandler.transform.position;
                        currentPlayerController.GetPlayerMover().ResetHistory(zoneHandler.transform.position);
                    }

                    ToggleParentGameObjects(zoneHandler);
                    OnZoneInteraction();
                    queuedZoneNodeID = null;

                    break; // Node transport complete, no need to complete loop
                }
            }

            // Failsafe -- no movement
            ExitMove();
        }

        private void OnZoneInteraction()
        {
            zoneInteraction?.Invoke();
        }

        private void ExitMove()
        {
            currentPlayerStateHandler.SetZoneTransitionStatus(true);
            currentPlayerStateHandler.EnterWorld();
            if (inTransitToNextScene) { RemoveZoneHandler(); }
            SetUpCurrentReferences(null, null);
        }

        private void RemoveZoneHandler()
        {
            if (fader == null) { fader = FindObjectOfType<Fader>(); }
            if (fader == null) { return; }

            fader.fadingOut -= QueuedMoveToNextNode;
            Destroy(gameObject, delayToDestroyAfterSceneLoading);
        }

        private void QueuedMoveToNextNode()
        {
            if (queuedZoneNodeID == null)
            {
                RemoveZoneHandler();
                return; 
            }
            MoveToNextNode(queuedZoneNodeID);
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

        private List<ChoiceActionPair> GetZoneNameZoneNodePairs()
        {
            List<ChoiceActionPair> choiceActionPairs = new List<ChoiceActionPair>();
            foreach (string childNode in zoneNode.GetChildren())
            {
                string childNodeNamePretty = GetStaticZoneNamePretty(childNode);
                ChoiceActionPair choiceActionPair = new ChoiceActionPair(childNodeNamePretty, () => WarpPlayerToNode(childNode));
                choiceActionPairs.Add(choiceActionPair);
            }
            return choiceActionPairs;
        }
        #endregion

        #region Interfaces
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
                    playerStateHandler.EnterDialogue(choiceMessage, GetZoneNameZoneNodePairs());
                }
            }
            return true;
        }

        bool IRaycastable.CheckDistanceTemplate()
        {
            // Not evaluated -> IRaycastableExtension
            return false;
        }
        #endregion
    }
}
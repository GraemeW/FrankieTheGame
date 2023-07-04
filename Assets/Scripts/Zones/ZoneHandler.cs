using Frankie.Control;
using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using Frankie.Utils;
using System.Text.RegularExpressions;
using Frankie.Core;

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
        PlayerStateMachine playerStateMachine = null;
        PlayerController playerController = null;

        // Cached References
        Fader fader = null;

        // Events
        public UnityEvent zoneInteraction;

        // Static State
        static List<ZoneHandler> activeZoneHandlers = new List<ZoneHandler>();

        #region UnityMethods
        private void Awake()
        {
            AddToActiveZoneHandlers(this);
        }

        private void OnDestroy()
        {
            RemoveFromActiveZoneHandlers(this);
        }
        #endregion

        #region StaticMethods
        private static void AddToActiveZoneHandlers(ZoneHandler zoneHandler)
        {
            if (activeZoneHandlers.Contains(zoneHandler)) { return; }

            activeZoneHandlers.Add(zoneHandler);
        }

        private static void RemoveFromActiveZoneHandlers(ZoneHandler zoneHandler)
        {
            activeZoneHandlers.Remove(zoneHandler);
        }

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
            return activeZoneHandlers;
        }
        #endregion

        #region PublicMethods
        public ZoneNode GetZoneNode() => zoneNode;

        public Transform GetWarpPosition() => warpPosition;

        public bool EnableRoomParent(bool enable)
        {
            if (roomParent != null)
            {
                roomParent.gameObject.SetActive(enable);
                return true;
            }
            return false;
        }

        public void AttemptToWarpPlayer(PlayerStateMachine playerStateMachine) // Callable by Unity Events
        {
            if (playerStateMachine.TryGetComponent(out PlayerController playerController))
            {
                AttemptToWarpPlayer(playerStateMachine, playerController);
            }
        }
        #endregion

        #region PrivateMethods
        private void SetUpCurrentReferences(PlayerStateMachine playerStateMachine, PlayerController playerController)
        {
            this.playerStateMachine = playerStateMachine;
            this.playerController = playerController;
        }

        private void WarpPlayerToNextNode(PlayerStateMachine playerStateMachine)
        {
            ZoneNode nextNode = SetUpNextNode(playerStateMachine);
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

        private ZoneNode SetUpNextNode(PlayerStateMachine playerStateMachine)
        {
            ZoneNode nextNode = SelectRandomNodeFromChildren(playerStateMachine);
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
                playerStateMachine.EnterZoneTransition();
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
            if (fader == null) { fader = FindAnyObjectByType<Fader>(); }
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
                        playerController.gameObject.transform.position = warpPosition;
                        Vector2 lookDirection = warpPosition - zoneHandler.transform.position;
                        lookDirection.Normalize();
                        playerController.GetPlayerMover().SetLookDirection(lookDirection);
                        playerController.GetPlayerMover().ResetHistory(warpPosition);
                    }
                    else 
                    { 
                        playerController.gameObject.transform.position = zoneHandler.transform.position;
                        playerController.GetPlayerMover().ResetHistory(zoneHandler.transform.position);
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
            playerStateMachine.SetZoneTransitionStatus(true);
            playerStateMachine.EnterWorld();
            if (inTransitToNextScene) { RemoveZoneHandler(); }
            SetUpCurrentReferences(null, null);
        }

        private void RemoveZoneHandler()
        {
            if (fader == null) { fader = FindAnyObjectByType<Fader>(); }
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

        private ZoneNode SelectRandomNodeFromChildren(PlayerStateMachine playerStateMachine)
        {
            List<string> childNodeOptions = GetFilteredZoneNodes(playerStateMachine);
            if (zoneNode == null || childNodeOptions == null || childNodeOptions.Count == 0) { return null; }

            int nodeIndex = UnityEngine.Random.Range(0, childNodeOptions.Count);
            return Zone.GetFromName(zoneNode.GetZoneName()).GetNodeFromID(childNodeOptions[nodeIndex]);
        }

        private void ToggleParentGameObjects(ZoneHandler nextZoneHandler)
        {
            if (disableOnExit)
            {
                EnableRoomParent(false);
            }
            nextZoneHandler.EnableRoomParent(true);
        }

        private bool? IsSimpleWarp(PlayerStateMachine playerStateMachine)
        {
            if (zoneNode == null || zoneNode.GetChildren() == null) { return null; }

            if (GetFilteredZoneNodes(playerStateMachine).Count == 1 || randomizeChoice)
            {
                return true;
            }
            return false;
        }

        private List<string> GetFilteredZoneNodes(PlayerStateMachine playerStateMachine)
        {
            List<string> filteredZoneNodes = new List<string>();

            Zone zone = Zone.GetFromName(zoneNode.GetZoneName());
            if (zone == null) { return filteredZoneNodes; }

            foreach (string zoneNodeID in zoneNode.GetChildren())
            {
                if (IsZoneNodeAvailable(playerStateMachine, zone, zoneNodeID))
                {
                    filteredZoneNodes.Add(zoneNodeID);
                }
            }
            return filteredZoneNodes;
        }

        private bool IsZoneNodeAvailable(PlayerStateMachine playerStateMachine, Zone zone, string zoneNodeID)
        {
            ZoneNode candidateNode = zone.GetNodeFromID(zoneNodeID);
            if (candidateNode == null) { return false; }

            return candidateNode.CheckCondition(GetEvaluators(playerStateMachine));
        }

        private IEnumerable<IPredicateEvaluator> GetEvaluators(PlayerStateMachine playerStateMachine)
        {
            return playerStateMachine.GetComponentsInChildren<IPredicateEvaluator>();
        }

        private List<ChoiceActionPair> GetZoneNameZoneNodePairs(PlayerStateMachine playerStateHandler)
        {
            List<ChoiceActionPair> choiceActionPairs = new List<ChoiceActionPair>();
            foreach (string childNode in GetFilteredZoneNodes(playerStateHandler))
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

        public bool HandleRaycast(PlayerStateMachine playerStateHandler, PlayerController playerController, PlayerInputType inputType, PlayerInputType matchType)
        {
            if (!this.CheckDistance(gameObject, transform.position, playerController, 
                overrideDefaultInteractionDistance, interactionDistance)) 
            { 
                return false; 
            }

            if (inputType == matchType)
            {
                AttemptToWarpPlayer(playerStateHandler, playerController);
            }
            return true;
        }

        private void AttemptToWarpPlayer(PlayerStateMachine playerStateHandler, PlayerController playerController)
        {
            SetUpCurrentReferences(playerStateHandler, playerController);

            bool? isSimpleWarp = IsSimpleWarp(playerStateHandler);
            if (isSimpleWarp == null) { return; }

            if (isSimpleWarp == true)
            {
                WarpPlayerToNextNode(playerStateHandler);
            }
            else if (isSimpleWarp == false)
            {
                playerStateHandler.EnterDialogue(choiceMessage, GetZoneNameZoneNodePairs(playerStateHandler));
            }
        }

        bool IRaycastable.CheckDistanceTemplate()
        {
            // Not evaluated -> IRaycastableExtension
            return false;
        }
        #endregion
    }
}
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
        [SerializeField][Tooltip("If left null, will attempt to auto-populate from parent")] Room roomParent = null;
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

        public static ZoneNode SelectNodeFromIDs(string zoneID, string nodeID) => Zone.GetFromName(zoneID).GetNodeFromID(nodeID);

        public static string GetStaticZoneNamePretty(string zoneName) => Regex.Replace(zoneName, "([a-z])_?([A-Z])", "$1 $2");

        private static List<ZoneHandler> FindAllZoneHandlersInScene() => activeZoneHandlers;
        #endregion

        #region PublicMethods
        public ZoneNode GetZoneNode() => zoneNode;

        public Transform GetWarpPosition() => warpPosition;

        public void ToggleRoomParent(bool enable)
        {
            if (roomParent == null)
            {
                GameObject parentObject = gameObject.transform.parent?.gameObject;
                if (parentObject != null) { parentObject.TryGetComponent(out roomParent); }
            }

            if (roomParent != null) { roomParent.ToggleRoom(enable, true);}
        }

        public void AttemptToWarpPlayer(PlayerStateMachine playerStateMachine) // Callable by Unity Events
        {
            if (playerStateMachine.TryGetComponent(out PlayerController playerController))
            {
                AttemptToWarpPlayer(playerStateMachine, playerController);
            }
        }
        #endregion

        #region WarpMethods
        private void SetUpCurrentReferences(PlayerStateMachine playerStateMachine, PlayerController playerController)
        {
            this.playerStateMachine = playerStateMachine;
            this.playerController = playerController;
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

        private void AttemptToWarpPlayer(PlayerStateMachine playerStateHandler, PlayerController playerController)
        {
            SetUpCurrentReferences(playerStateHandler, playerController);
            StartViableSceneTransition(zoneNode); // If scene setup on entry node, immediately kick off next scene load

            bool? isSimpleWarp = IsSimpleWarp(playerStateHandler);
            if (isSimpleWarp == null) { return; }

            if (isSimpleWarp == true)
            {
                string nextNodeID = SelectRandomChildNodeID(playerStateMachine);
                WarpPlayerToSpecificNode(nextNodeID);
            }
            else if (isSimpleWarp == false)
            {
                playerStateHandler.EnterDialogue(choiceMessage, GetZoneNameZoneNodePairs(playerStateHandler));
            }
        }

        private List<ChoiceActionPair> GetZoneNameZoneNodePairs(PlayerStateMachine playerStateHandler)
        {
            List<ChoiceActionPair> choiceActionPairs = new List<ChoiceActionPair>();
            foreach (string childNode in GetFilteredZoneNodes(playerStateHandler))
            {
                string childNodeNamePretty = GetStaticZoneNamePretty(childNode);
                ChoiceActionPair choiceActionPair = new ChoiceActionPair(childNodeNamePretty, () => WarpPlayerToSpecificNode(childNode));
                choiceActionPairs.Add(choiceActionPair);
            }
            return choiceActionPairs;
        }

        private void WarpPlayerToSpecificNode(string nodeID)
        {
            ZoneNode nextNode = Zone.GetFromName(zoneNode.GetZoneName()).GetNodeFromID(nodeID);
            if (nextNode == null) { return; }

            StartViableSceneTransition(nextNode); // If scene setup on next node, likewise, kick off next scene load 

            if (!inTransitToNextScene) // On scene transition, called via fader (sceneTransitionComplete)
            {
                ExecuteWarpToNode(nextNode.GetNodeID());
            }
        }

        private string SelectRandomChildNodeID(PlayerStateMachine playerStateMachine)
        {
            List<string> childNodeOptions = GetFilteredZoneNodes(playerStateMachine);
            if (zoneNode == null || childNodeOptions == null || childNodeOptions.Count == 0) { return null; }

            int nodeIndex = UnityEngine.Random.Range(0, childNodeOptions.Count);
            return childNodeOptions[nodeIndex];
        }
        #endregion

        #region NodeTraversalMethods
        private void ExecuteWarpToNode(string nextNodeID)
        {
            foreach (ZoneHandler zoneHandler in FindAllZoneHandlersInScene())
            {
                if (nextNodeID == zoneHandler.GetZoneNode()?.GetNodeID())
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

                    SwapActiveRoomParents(zoneHandler);
                    zoneInteraction?.Invoke();
                    queuedZoneNodeID = null;

                    break; // Node transport complete, no need to complete loop
                }
            }

            // Failsafe -- no movement
            ExitMove();
        }

        private void ExitMove()
        {
            playerStateMachine.SetZoneTransitionStatus(true);
            playerStateMachine.EnterWorld();
            if (inTransitToNextScene) { RemoveZoneHandler(); }
            SetUpCurrentReferences(null, null);
        }

        private void SwapActiveRoomParents(ZoneHandler nextZoneHandler)
        {
            DisableCurrentRoomParent();
            nextZoneHandler.ToggleRoomParent(true);
        }

        private void DisableCurrentRoomParent()
        {
            if (disableOnExit) { ToggleRoomParent(false); }
        }
        #endregion

        #region SceneTransitionMethods
        private void StartViableSceneTransition(ZoneNode zoneNode)
        {
            if (zoneNode == null) { return; }

            if (zoneNode.HasSceneReference())
            {
                playerStateMachine.EnterZoneTransition();
                SetZoneHandlerToPersistOnSceneTransition();

                ZoneNodePair zoneNodePair = zoneNode.GetZoneReferenceNodeReferencePair();

                TransitionToNextScene(zoneNodePair.zone, zoneNodePair.zoneNode); // NOTE:  Exit inTransition done on queued move
            }
        }

        private void SetZoneHandlerToPersistOnSceneTransition()
        {
            inTransitToNextScene = true;
            gameObject.transform.parent = null;
            DontDestroyOnLoad(gameObject);
        }

        private void QueuedMoveToNextNode()
        {
            if (queuedZoneNodeID == null)
            {
                RemoveZoneHandler();
                return; 
            }
            ExecuteWarpToNode(queuedZoneNodeID);
        }

        private void TransitionToNextScene(Zone nextZone, ZoneNode nextNode)
        {
            if (fader == null) { fader = FindAnyObjectByType<Fader>(); }
            if (fader == null) { return; }

            queuedZoneNodeID = nextNode.GetNodeID();
            fader.fadingOut += QueuedMoveToNextNode;
            fader.fadingPeak += DisableCurrentRoomParent; // Required for save state
            fader.UpdateFadeState(TransitionType.Zone, nextZone);
        }

        private void RemoveZoneHandler()
        {
            if (fader == null) { fader = FindAnyObjectByType<Fader>(); }
            if (fader == null) { return; }

            fader.fadingOut -= QueuedMoveToNextNode;
            fader.fadingPeak -= DisableCurrentRoomParent;
            Destroy(gameObject, delayToDestroyAfterSceneLoading);
        }
        #endregion

        #region NodeFilteringMethods
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

        bool IRaycastable.CheckDistanceTemplate()
        {
            // Not evaluated -> IRaycastableExtension
            return false;
        }
        #endregion
    }
}
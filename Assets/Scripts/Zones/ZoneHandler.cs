using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Events;
using Frankie.Core;
using Frankie.Utils;
using Frankie.Control;

namespace Frankie.ZoneManagement
{
    public class ZoneHandler : MonoBehaviour, IRaycastable
    {
        // Tunables 
        [SerializeField] private ZoneNode zoneNode;
        [SerializeField] private bool overrideDefaultInteractionDistance = false;
        [SerializeField] private float interactionDistance = 0.3f;
        [SerializeField] private Transform warpPosition;
        [SerializeField] private float delayToDestroyAfterSceneLoading = 0.1f;
        [Header("Game Object (Dis)Enablement")]
        [SerializeField] [Tooltip("If left null, will attempt to auto-populate from parent")] private Room roomParent;
        [SerializeField] private bool disableOnExit = true;
        [Header("Warp Properties")]
        [SerializeField] private bool randomizeChoice = true;
        [SerializeField] private string choiceMessage = "Where do you want to go?";

        // State
        private bool inTransitToNextScene = false;
        private string queuedZoneNodeID;
        private PlayerStateMachine playerStateMachine;
        private PlayerController playerController;

        // Cached References
        private Fader fader;

        // Events
        public UnityEvent zoneInteraction;

        // Static State
        private static readonly List<ZoneHandler> _activeZoneHandlers = new();

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
            if (_activeZoneHandlers.Contains(zoneHandler)) { return; }
            _activeZoneHandlers.Add(zoneHandler);
        }

        private static void RemoveFromActiveZoneHandlers(ZoneHandler zoneHandler)
        {
            _activeZoneHandlers.Remove(zoneHandler);
        }
        
        private static string GetStaticZoneNamePretty(string zoneName) => Regex.Replace(zoneName, "([a-z])_?([A-Z])", "$1 $2");
        private static List<ZoneHandler> FindAllZoneHandlersInScene() => _activeZoneHandlers;
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

            if (roomParent != null) { roomParent.ToggleRoom(enable, true); }
        }

        public void AttemptToWarpPlayer(PlayerStateMachine passPlayerStateMachine) // Callable by Unity Events
        {
            if (!passPlayerStateMachine.TryGetComponent(out PlayerController passPlayerController)) { return; }
            AttemptToWarpPlayer(passPlayerStateMachine, passPlayerController);
        }
        #endregion

        #region WarpMethods
        private void SetUpCurrentReferences(PlayerStateMachine setPlayerStateMachine, PlayerController setPlayerController)
        {
            playerStateMachine = setPlayerStateMachine;
            playerController = setPlayerController;
        }

        private bool? IsSimpleWarp(PlayerStateMachine passPlayerStateMachine)
        {
            if (zoneNode == null || zoneNode.GetChildren() == null) { return null; }
            return GetFilteredZoneNodes(passPlayerStateMachine).Count == 1 || randomizeChoice;
        }

        private void AttemptToWarpPlayer(PlayerStateMachine setPlayerStateHandler, PlayerController setPlayerController)
        {
            SetUpCurrentReferences(setPlayerStateHandler, setPlayerController);
            StartViableSceneTransition(zoneNode); // If scene setup on entry node, immediately kick off next scene load

            bool? isSimpleWarp = IsSimpleWarp(setPlayerStateHandler);
            switch (isSimpleWarp)
            {
                case null:
                    return;
                case true:
                {
                    string nextNodeID = SelectRandomChildNodeID(playerStateMachine);
                    WarpPlayerToSpecificNode(nextNodeID);
                    break;
                }
                case false:
                {
                    setPlayerStateHandler.EnterDialogue(choiceMessage, GetZoneNameZoneNodePairs(setPlayerStateHandler));
                    break;
                }
            }
        }

        private List<ChoiceActionPair> GetZoneNameZoneNodePairs(PlayerStateMachine playerStateHandler)
        {
            var choiceActionPairs = new List<ChoiceActionPair>();
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

            // If scene setup on next node, likewise, kick off next scene load
            StartViableSceneTransition(nextNode);  

            // On scene transition, called via fader (sceneTransitionComplete)
            if (!inTransitToNextScene) { ExecuteWarpToNode(nextNode.GetNodeID()); } 
        }

        private string SelectRandomChildNodeID(PlayerStateMachine passPlayerStateMachine)
        {
            List<string> childNodeOptions = GetFilteredZoneNodes(passPlayerStateMachine);
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
                if (nextNodeID != zoneHandler.GetZoneNode()?.GetNodeID()) continue;
                
                Transform warpTransform = zoneHandler.GetWarpPosition();
                if (warpTransform != null)
                {
                    playerController.gameObject.transform.position = warpTransform.position;
                    Vector2 lookDirection = warpTransform.position - zoneHandler.transform.position;
                    lookDirection.Normalize();
                    playerController.GetPlayerMover().SetLookDirection(lookDirection);
                    playerController.GetPlayerMover().ResetHistory(warpTransform.position);
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
        private void StartViableSceneTransition(ZoneNode startZoneNode)
        {
            if (startZoneNode == null) { return; }
            if (!startZoneNode.HasLinkedSceneReference()) { return; }
            
            playerStateMachine.EnterZoneTransition();
            SetZoneHandlerToPersistOnSceneTransition();

            ZoneNode linkedZoneNode = startZoneNode.GetLinkedZoneNode();
            Zone linkedZone = linkedZoneNode.GetZone();

            TransitionToNextScene(linkedZone, linkedZoneNode); // NOTE:  Exit inTransition done on queued move
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
            if (fader == null) { fader = Fader.FindFader(); }
            if (fader == null) { return; }

            queuedZoneNodeID = nextNode.GetNodeID();
            fader.fadingOut += QueuedMoveToNextNode;
            fader.fadingPeak += DisableCurrentRoomParent; // Required for save state
            fader.UpdateFadeState(TransitionType.Zone, nextZone);
        }

        private void RemoveZoneHandler()
        {
            if (fader == null) { fader = Fader.FindFader(); }
            if (fader == null) { return; }

            fader.fadingOut -= QueuedMoveToNextNode;
            fader.fadingPeak -= DisableCurrentRoomParent;
            Destroy(gameObject, delayToDestroyAfterSceneLoading);
        }
        #endregion

        #region NodeFilteringMethods
        private List<string> GetFilteredZoneNodes(PlayerStateMachine passPlayerStateMachine)
        {
            var filteredZoneNodes = new List<string>();
            Zone zone = Zone.GetFromName(zoneNode.GetZoneName());
            if (zone == null) { return filteredZoneNodes; }

            filteredZoneNodes.AddRange(zoneNode.GetChildren().Where(zoneNodeID => IsZoneNodeAvailable(passPlayerStateMachine, zone, zoneNodeID)));
            return filteredZoneNodes;
        }

        private bool IsZoneNodeAvailable(PlayerStateMachine passPlayerStateMachine, Zone zone, string zoneNodeID)
        {
            ZoneNode candidateNode = zone.GetNodeFromID(zoneNodeID);
            return candidateNode != null && candidateNode.CheckCondition(GetEvaluators(passPlayerStateMachine));
        }

        private IEnumerable<IPredicateEvaluator> GetEvaluators(PlayerStateMachine passPlayerStateMachine)
        {
            return passPlayerStateMachine.GetComponentsInChildren<IPredicateEvaluator>();
        }
        #endregion

        #region Interfaces
        public CursorType GetCursorType() => CursorType.Zone;

        public bool HandleRaycast(PlayerStateMachine playerStateHandler, PlayerController playerController, PlayerInputType inputType, PlayerInputType matchType)
        {
            if (!IRaycastable.CheckDistance(gameObject, transform.position, playerController,
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
        #endregion
    }
}

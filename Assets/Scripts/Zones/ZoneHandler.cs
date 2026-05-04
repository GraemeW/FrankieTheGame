using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using Frankie.Core.Predicates;
using Frankie.Utils;
using Frankie.Control;
using Frankie.Utils.Localization;

namespace Frankie.ZoneManagement
{
    [ExecuteInEditMode]
    public class ZoneHandler : MonoBehaviour, IRaycastable, ILocalizable
    {
        // Localization Properties
        public LocalizationTableType localizationTableType { get; } = LocalizationTableType.Zones;
        
        // Tunables 
        [SerializeField] private ZoneNode zoneNode;
        [SerializeField] private bool overrideDefaultInteractionDistance = false;
        [SerializeField] private float interactionDistance = 0.3f;
        [SerializeField] private Transform warpTransform;
        [SerializeField] private float delayToDestroyAfterSceneLoading = 0.1f;
        [Header("Game Object (Dis)Enablement")]
        [SerializeField] [Tooltip("If left null, will attempt to auto-populate from parent")] private Room roomParent;
        [SerializeField] private bool disableOnExit = true;
        [Header("Warp Properties")]
        [SerializeField] private bool randomizeChoice = true;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.Zones, true)] private LocalizedString localizedChoiceMessage;

        // State
        private bool inTransitToNextScene = false;
        private string queuedZoneNodeID;
        private PlayerStateMachine cachedPlayerStateMachine;
        private PlayerController cachedPlayerController;

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
            ILocalizable.TriggerOnDestroy(this);
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
        private static List<ZoneHandler> FindAllZoneHandlersInScene() => _activeZoneHandlers;
        #endregion

        #region PublicMethods
        public ZoneNode GetZoneNode() => zoneNode;
        private bool HasWarpPosition() => warpTransform != null;
        public Vector3 GetWarpPosition() => warpTransform != null ? warpTransform.position : transform.position;

        private void ToggleRoomParent(bool enable)
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
            cachedPlayerStateMachine = setPlayerStateMachine;
            cachedPlayerController = setPlayerController;
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
                    string nextNodeID = SelectRandomChildNodeID(cachedPlayerStateMachine);
                    WarpPlayerToSpecificNode(nextNodeID);
                    break;
                }
                case false:
                {
                    setPlayerStateHandler.EnterDialogue(localizedChoiceMessage.GetSafeLocalizedString(), GetZoneNameZoneNodePairs(setPlayerStateHandler));
                    break;
                }
            }
        }

        private List<ChoiceActionPair> GetZoneNameZoneNodePairs(PlayerStateMachine playerStateHandler)
        {
            var choiceActionPairs = new List<ChoiceActionPair>();
            foreach ((string childNodeID, ZoneNode childNode) in GetFilteredZoneNodes(playerStateHandler))
            {
                if (childNodeID == null || string.IsNullOrEmpty(childNodeID) || childNode == null) { continue; }
                var choiceActionPair = new ChoiceActionPair(childNode.GetDisplayName(), () => WarpPlayerToSpecificNode(childNodeID));
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
            List<(string, ZoneNode)> childNodeOptions = GetFilteredZoneNodes(passPlayerStateMachine);
            if (zoneNode == null || childNodeOptions.Count == 0) { return null; }

            int nodeIndex = UnityEngine.Random.Range(0, childNodeOptions.Count);
            var (childNodeID, _) = childNodeOptions[nodeIndex];
            return childNodeID;
        }
        #endregion

        #region NodeTraversalMethods
        private void ExecuteWarpToNode(string nextNodeID)
        {
            foreach (ZoneHandler zoneHandler in FindAllZoneHandlersInScene())
            {
                if (nextNodeID != zoneHandler.GetZoneNode()?.GetNodeID()) { continue; }
                
                Vector3 warpPosition = zoneHandler.GetWarpPosition();
                cachedPlayerController.gameObject.transform.position = warpPosition;
                
                if (zoneHandler.HasWarpPosition())
                {
                    Vector2 lookDirection = warpPosition - zoneHandler.transform.position;
                    lookDirection.Normalize();
                    cachedPlayerController.GetPlayerMover().SetLookDirection(lookDirection);
                }
                cachedPlayerController.GetPlayerMover().ResetHistory(warpPosition);

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
            cachedPlayerStateMachine.SetZoneTransitionStatus(true);
            cachedPlayerStateMachine.EnterWorld();
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

            ZoneNode linkedZoneNode = startZoneNode.GetLinkedZoneNode();
            if (linkedZoneNode == null) { return; }
            Zone linkedZone = linkedZoneNode.GetZone();
            if (linkedZone == null) { return; }
            
            // NOTE:  Exit inTransition done on queued move
            if (TransitionToNextScene(linkedZone, linkedZoneNode))
            {
                cachedPlayerStateMachine.EnterZoneTransition();
            }
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

        private bool TransitionToNextScene(Zone nextZone, ZoneNode nextNode)
        {
            queuedZoneNodeID = nextNode.GetNodeID();
            var faderEventTriggers = new FaderEventTriggers(null, HandleFadingPeak, QueuedMoveToNextNode, null);
            return Fader.StartZoneFade(nextZone, faderEventTriggers, true);
        }

        private void RemoveZoneHandler()
        {
            Destroy(gameObject, delayToDestroyAfterSceneLoading);
        }

        private void HandleFadingPeak()
        {
            DisableCurrentRoomParent(); // Required for save state
            SetZoneHandlerToPersistOnSceneTransition(); // Call after Disable to avoid room popping off of parent
        }
        
        private void SetZoneHandlerToPersistOnSceneTransition()
        {
            inTransitToNextScene = true;
            gameObject.transform.parent = null;
            DontDestroyOnLoad(gameObject);
        }
        #endregion

        #region NodeFilteringMethods
        private List<(string, ZoneNode)> GetFilteredZoneNodes(PlayerStateMachine passPlayerStateMachine)
        {
            var filteredZoneNodes = new List<(string, ZoneNode)>();
            Zone zone = Zone.GetFromName(zoneNode.GetZoneName());
            if (zone == null) { return filteredZoneNodes; }

            foreach (string candidateNodeID in zoneNode.GetChildren())
            {
                if (!IsZoneNodeAvailable(passPlayerStateMachine, zone, candidateNodeID, out ZoneNode candidateNode)) { continue; }
                filteredZoneNodes.Add((candidateNodeID, candidateNode));
            }
            return filteredZoneNodes;
        }

        private bool IsZoneNodeAvailable(PlayerStateMachine passPlayerStateMachine, Zone zone, string zoneNodeID, out ZoneNode candidateNode)
        {
            candidateNode = zone.GetNodeFromID(zoneNodeID);
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
            if (!IRaycastable.CheckDistance(gameObject, transform.position, playerController, overrideDefaultInteractionDistance, interactionDistance))
            {
                return false;
            }

            if (inputType == matchType)
            {
                AttemptToWarpPlayer(playerStateHandler, playerController);
            }
            return true;
        }
        
        public List<TableEntryReference> GetLocalizationEntries()
        {
            return new List<TableEntryReference>
            {
                localizedChoiceMessage.TableEntryReference
            };
        }
        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using LowDefMustard.Utils;
using LowDefMustard.Localization;
using Frankie.Core;
using Frankie.Control;
using Frankie.Rendering;

namespace Frankie.World
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(NPCMover))]
    [RequireComponent(typeof(Animator))]
    public class WorldSubwayRider : MonoBehaviour, ICheckDynamic, ILocalizable
    {
        // Tunables
        [SerializeField][SimpleLocalizedString(LocalizationTableType.ChecksWorldObjects, true)] private LocalizedString localizedRideMessage;
        [SerializeField] private Transform conductorsRoot;
        [SerializeField] private Transform followTarget;
        [SerializeField] private List<SubwayRide> subwayRides = new();
        [SerializeField] private List<WorldSubwayRider> sisterRidersToDisable = new();
        
        // Localization Properties
        public LocalizationTableType localizationTableType { get; } = LocalizationTableType.ChecksWorldObjects;
        
        // State
        private bool active = true;
        private Action handleRideEndDelegate;

        // Cached References
        private NPCMover npcMover;
        private Animator animator;
        private CameraController cameraController;

        #region UnityMethods
        private void Awake()
        {
            npcMover = GetComponent<NPCMover>();
            animator = GetComponent<Animator>();
        }

        private void OnEnable()
        {
            active = true;
        }
        
        protected void OnDestroy()
        {
            ILocalizable.TriggerOnDestroy(this);
        }
        #endregion

        #region InterfaceMethods
        public string GetMessage() => localizedRideMessage.GetSafeLocalizedString();
        public List<ChoiceActionPair> GetChoiceActionPairs(PlayerStateMachine playerStateMachine)
        {
            var rideOptions = new List<ChoiceActionPair>();
            if (subwayRides.Count == 0 || !active) { return rideOptions; }

            rideOptions.AddRange(from subwayRide in subwayRides where subwayRide.zoneHandler != null && subwayRide.path != null select new ChoiceActionPair(subwayRide.localizedRideName.GetSafeLocalizedString(), () => StartRide(playerStateMachine, subwayRide)));
            return rideOptions;
        }
        
        public List<TableEntryReference> GetLocalizationEntries()
        {
            List<TableEntryReference> localizationEntries = new() { localizedRideMessage.TableEntryReference };
            localizationEntries.AddRange(from subwayRide in subwayRides where subwayRide?.localizedRideName is { IsEmpty: false } select subwayRide.localizedRideName.TableEntryReference);
            return localizationEntries;
        }
        #endregion

        #region UtilityMethods
        private void DisableAllConductors()
        {
            foreach (Transform childTransform in conductorsRoot)
            {
                childTransform.gameObject.SetActive(false);
            }
        }

        private void DisableSisterRides()
        {
            if (sisterRidersToDisable is not { Count: > 0 }) { return; }
            foreach (WorldSubwayRider sisterRider in sisterRidersToDisable)
            {
                sisterRider.gameObject.SetActive(false);
            }
        }

        private void StartRide(PlayerStateMachine playerStateMachine, SubwayRide subwayRide)
        {
            if (subwayRide == null || subwayRide.zoneHandler == null || subwayRide.path == null) { return; }

            var interactionEvent = new InteractionEvent();
            interactionEvent.AddListener((_) => HandleRideStart(subwayRide, playerStateMachine));
            playerStateMachine.SetPostDialogueCallbackActions(interactionEvent);
        }

        private void HandleRideStart(SubwayRide subwayRide, PlayerStateMachine playerStateMachine)
        {
            cameraController = CameraController.GetCameraController();
            if (cameraController == null) { return; }
            
            DisableAllConductors();
            DisableSisterRides();
            
            cameraController.OverrideCameraFollower(animator, followTarget == null ? transform : followTarget); // Pass camera control to train
            subwayRide.zoneHandler.AttemptToWarpPlayer(playerStateMachine); // Warp player -- must be called after camera on train to avoid camera jump
            
            npcMover.SetPatrolPath(subwayRide.path);
            handleRideEndDelegate = () => HandleRideEnd(playerStateMachine);
            npcMover.arrivedAtFinalWaypoint += handleRideEndDelegate;
            
            playerStateMachine.EnterCutscene(false); // Remove player control -- Call this after warping player, or ZoneHandler will force exit cutscene
        }
        
        private void HandleRideEnd(PlayerStateMachine playerStateMachine)
        {
            if (cameraController == null) { CameraController.GetCameraController(); }

            npcMover.arrivedAtFinalWaypoint -= handleRideEndDelegate;
            cameraController.RefreshDefaultCameras();
            playerStateMachine.EnterWorld();

            active = false; // de-activate (cannot ride back on same train, need to leave/rejoin subway)
        }
        #endregion
    }
}

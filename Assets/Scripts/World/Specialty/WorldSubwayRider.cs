using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Frankie.Core;
using Frankie.Control;
using Frankie.Utils;

namespace Frankie.World
{
    [RequireComponent(typeof(NPCMover))]
    [RequireComponent(typeof(Animator))]
    public class WorldSubwayRider : MonoBehaviour, ICheckDynamic
    {
        // Tunables
        [SerializeField] private string message = "Where do you want to ride?";
        [SerializeField] private GameObject conductorToggleObject;
        [SerializeField] private Transform followTarget;
        [SerializeField] private SubwayRide[] subwayRides;
        [SerializeField] private WorldSubwayRider[] sisterRidersToDisable;

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
        #endregion

        #region InterfaceMethods
        public string GetMessage() => message;
        public List<ChoiceActionPair> GetChoiceActionPairs(PlayerStateMachine playerStateMachine)
        {
            var rideOptions = new List<ChoiceActionPair>();
            if (subwayRides == null || subwayRides.Length == 0 || !active) { return rideOptions; }

            rideOptions.AddRange(from subwayRide in subwayRides where subwayRide.zoneHandler != null && subwayRide.path != null select new ChoiceActionPair(subwayRide.rideName, () => StartRide(playerStateMachine, subwayRide)));
            return rideOptions;
        }
        #endregion

        #region UtilityMethods
        public void ToggleConductor(bool enable)
        {
            if (conductorToggleObject == null) { return; }

            conductorToggleObject.SetActive(enable);
        }

        private void StartRide(PlayerStateMachine playerStateMachine, SubwayRide subwayRide)
        {
            if (subwayRide == null || subwayRide.zoneHandler == null || subwayRide.path == null) { return; }

            var interactionEvent = new InteractionEvent();
            interactionEvent.AddListener((_) => HandleRideStart(subwayRide, playerStateMachine));
            playerStateMachine.SetPostDialogueCallbackActions(interactionEvent);
            Debug.Log("Butts");
        }

        private void HandleRideStart(SubwayRide subwayRide, PlayerStateMachine playerStateMachine)
        {
            // Check for camera controller
            cameraController = CameraController.GetCameraController();
            if (cameraController == null) { return; }
            
            // Disable Stuff
            ToggleConductor(false);
            if (sisterRidersToDisable is { Length: > 0 })
            {
                foreach (WorldSubwayRider sisterRider in sisterRidersToDisable)
                {
                    sisterRider.gameObject.SetActive(false);
                }
            }

            // Pass camera control to train
            cameraController.OverrideCameraFollower(animator, followTarget == null ? transform : followTarget);

            // Warp player -- must be called after camera on train to avoid camera jump
            subwayRide.zoneHandler.AttemptToWarpPlayer(playerStateMachine);

            // Start to move Train && set up delegate to handle end of ride
            npcMover.SetPatrolPath(subwayRide.path);
            handleRideEndDelegate = () => HandleRideEnd(playerStateMachine);
            npcMover.arrivedAtFinalWaypoint += handleRideEndDelegate;

            // Remove player control -- Call this after warping player, or ZoneHandler will force exit cutscene
            playerStateMachine.EnterCutscene(false);
        }
        
        private void HandleRideEnd(PlayerStateMachine playerStateMachine)
        {
            if (cameraController == null) { CameraController.GetCameraController(); }
            UnityEngine.Debug.Log("So we're here now");

            npcMover.arrivedAtFinalWaypoint -= handleRideEndDelegate;
            cameraController.RefreshDefaultCameras();
            playerStateMachine.EnterWorld();

            active = false; // de-activate (cannot ride back on same train, need to leave/rejoin subway)
        }
        #endregion
    }
}

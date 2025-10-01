using Frankie.Core;
using Frankie.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Frankie.Control.Specialization
{
    [RequireComponent(typeof(NPCMover))]
    [RequireComponent(typeof(Animator))]
    public class WorldSubwayRider : MonoBehaviour, ICheckDynamic
    {
        // Tunables
        [SerializeField] string message = "Where do you want to ride?";
        [SerializeField] GameObject conductorToggleObject = null;
        [SerializeField] Transform followTarget = null;
        [SerializeField] SubwayRide[] subwayRides = null;
        [SerializeField] WorldSubwayRider[] sisterRidersToDisable = null;

        // State
        bool active = true;
        PlayerStateMachine playerStateMachine = null;
        CameraController cameraController = null;

        // Cached References
        NPCMover npcMover = null;
        Animator animator = null;

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
            List<ChoiceActionPair> rideOptions = new List<ChoiceActionPair>();
            if (subwayRides == null || subwayRides.Length == 0) { return rideOptions; }
            if (!active) { return rideOptions; }

            foreach (SubwayRide subwayRide in subwayRides)
            {
                if (subwayRide.zoneHandler == null || subwayRide.path == null) { continue; }

                ChoiceActionPair choiceActionPair = new ChoiceActionPair(subwayRide.rideName, () => StartRide(playerStateMachine, subwayRide));
                rideOptions.Add(choiceActionPair);
            }
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

            this.playerStateMachine = playerStateMachine;

            InteractionEvent interactionEvent = new InteractionEvent();
            interactionEvent.AddListener((playerStateMachine) => HandleRideStart(subwayRide));
            playerStateMachine.SetPostDialogueCallbackActions(interactionEvent);
        }

        private void HandleRideStart(SubwayRide subwayRide)
        {
            // Check for camera controller
            cameraController = CameraController.GetCameraController();
            if (cameraController == null) { return; }

            // Disable Stuff
            ToggleConductor(false);
            if (sisterRidersToDisable != null && sisterRidersToDisable.Length > 0)
            {
                foreach (WorldSubwayRider sisterRider in sisterRidersToDisable)
                {
                    sisterRider.gameObject.SetActive(false);
                }
            }

            // Pass camera control to train
            if (followTarget == null)
            {
                cameraController.OverrideCameraFollower(animator, transform);
            }
            else
            {
                cameraController.OverrideCameraFollower(animator, followTarget);
            }


            // Warp player -- must be called after camera on train to avoid camera jump
            subwayRide.zoneHandler.AttemptToWarpPlayer(playerStateMachine);

            // Start to move Train
            npcMover.SetPatrolPath(subwayRide.path);
            npcMover.arrivedAtFinalWaypoint += HandleRideEnd;

            // Remove player control -- Call this after warping player, or ZoneHandler will force exit cutscene
            playerStateMachine.EnterCutscene(false);
        }

        private void HandleRideEnd()
        {
            if (playerStateMachine == null) { playerStateMachine = Player.FindPlayerStateMachine(); }
            if (cameraController == null) { CameraController.GetCameraController(); }

            npcMover.arrivedAtFinalWaypoint -= HandleRideEnd;
            cameraController.RefreshDefaultCameras();
            playerStateMachine.EnterWorld();

            active = false; // de-activate (cannot ride back on same train, need to leave/rejoin subway)
        }
        #endregion
    }
}

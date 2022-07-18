using Cinemachine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Frankie.Stats;
using Frankie.Utils;

namespace Frankie.Core
{
    public class CameraController : MonoBehaviour
    {
        // State
        CinemachineStateDrivenCamera stateCamera = null;
        List<CinemachineVirtualCamera> virtualCameras = new List<CinemachineVirtualCamera>();

        // Cached References
        GameObject playerGameObject = null;
        ReInitLazyValue<Player> player;
        ReInitLazyValue<Party> party;

        #region StaticMethods
        public static CameraController GetCameraController()
        {
            GameObject mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            if (mainCamera == null) { return null; }
            GameObject cameraContainer = mainCamera.transform.parent.gameObject; // Structure of cameras is:  [CameraController (container) -> MainCamera, StateCameras, etc.]
            if (cameraContainer == null) { return null; }

            return cameraContainer.GetComponent<CameraController>();
        }
        #endregion

        #region UnityMethods
        private void Awake()
        {
            stateCamera = GetComponentInChildren<CinemachineStateDrivenCamera>();
            virtualCameras = GetComponentsInChildren<CinemachineVirtualCamera>().ToList();

            player = new ReInitLazyValue<Player>(SetupPlayerReference);
            party = new ReInitLazyValue<Party>(SetupPartyReference);
        }

        private void OnEnable()
        {
            if (party.value != null) { party.value.partyUpdated += RefreshDefaultCameras; }   
        }

        private void OnDisable()
        {
            if (party.value != null) { party.value.partyUpdated -= RefreshDefaultCameras; }
        }

        private void Start()
        {
            player.ForceInit();
            party.ForceInit();
            RefreshDefaultCameras();
        }
        #endregion

        #region PublicMethods
        public void RefreshDefaultCameras()
        {
            if (party.value != null)
            {
                SetUpStateDrivenCamera(party.value.GetLeadCharacterAnimator());
            }

            if (player.value != null)
            {
                SetUpVirtualCameraFollowers(player.value.transform);
            }
        }

        public void OverrideCameraFollower(Animator animator, Transform target)
        {
            UpdateStateAnimator(animator);
            SetUpVirtualCameraFollowers(target);
        }
        #endregion

        #region PrivateMethods
        private Player SetupPlayerReference()
        {
            if (playerGameObject == null) { playerGameObject = GameObject.FindGameObjectWithTag("Player"); }
            return playerGameObject?.GetComponent<Player>();
        }

        private Party SetupPartyReference()
        {
            if (playerGameObject == null) { playerGameObject = GameObject.FindGameObjectWithTag("Player"); }
            return playerGameObject?.GetComponent<Party>();
        }

        private void SetUpStateDrivenCamera(Animator animator)
        {
            if (animator == null) { return; }
            
            UpdateStateAnimator(animator);
        }

        private void SetUpVirtualCameraFollowers(Transform target)
        {
            if (target == null) { return; }

            foreach (CinemachineVirtualCamera virtualCamera in virtualCameras)
            {
                virtualCamera.Follow = target;
            }
        }

        private void UpdateStateAnimator(Animator characterAnimator)
        {
            stateCamera.m_AnimatedTarget = characterAnimator;
        }
        #endregion
    }

}

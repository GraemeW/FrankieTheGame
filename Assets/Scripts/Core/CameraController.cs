using Cinemachine;
using UnityEngine;
using Frankie.Stats;
using Frankie.Utils;
using Frankie.Settings;

namespace Frankie.Core
{
    public class CameraController : MonoBehaviour
    {
        // Tunables
        [Header("Hookups")]
        [SerializeField] CinemachineStateDrivenCamera stateCamera = null;
        [SerializeField] CinemachineVirtualCamera activeCamera = null;
        [SerializeField] CinemachineVirtualCamera idleCamera = null;
        [Header("Camera Parameters")]
        [SerializeField] float defaultActiveOrthoSize = 3.6f;
        [SerializeField] float defaultIdleOrthoSize = 1.8f;

        // Cached References
        GameObject playerGameObject = null;
        ReInitLazyValue<Player> player;
        ReInitLazyValue<Party> party;

        // State
        private float currentActiveOrthoSize = 3.6f;
        private float currentIdleOrthoSize = 1.8f;

        #region Static

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
            player = new ReInitLazyValue<Player>(SetupPlayerReference);
            party = new ReInitLazyValue<Party>(SetupPartyReference);
        }

        private void OnEnable()
        {
            if (party.value != null) { party.value.partyUpdated += RefreshDefaultCameras; }
            DisplayResolutions.resolutionUpdated += UpdateCameraOrthoSizes;
        }

        private void OnDisable()
        {
            if (party.value != null) { party.value.partyUpdated -= RefreshDefaultCameras; }
            DisplayResolutions.resolutionUpdated -= UpdateCameraOrthoSizes;
        }

        private void Start()
        {
            player.ForceInit();
            party.ForceInit();
            RefreshDefaultCameras();
            UpdateCameraOrthoSizes(DisplayResolutions.GetResolutionScaler());
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

            activeCamera.Follow = target;
            idleCamera.Follow = target;
        }

        private void UpdateCameraOrthoSizes(ResolutionScaler resolutionScaler)
        {
            currentActiveOrthoSize = (defaultActiveOrthoSize * (float)resolutionScaler.numerator / (float)resolutionScaler.denominator) / (float)resolutionScaler.cameraScaling;
            currentIdleOrthoSize = (defaultIdleOrthoSize * (float)resolutionScaler.numerator / (float)resolutionScaler.denominator) / (float)resolutionScaler.cameraScaling;

            if (activeCamera != null) { activeCamera.m_Lens.OrthographicSize = currentActiveOrthoSize; }
            if (idleCamera != null) { idleCamera.m_Lens.OrthographicSize = currentIdleOrthoSize; }
        }

        private void UpdateStateAnimator(Animator characterAnimator)
        {
            stateCamera.m_AnimatedTarget = characterAnimator;
        }
        #endregion
    }
}

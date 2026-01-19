using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Cinemachine;
using Frankie.Stats;
using Frankie.Utils;
using Frankie.Rendering;

namespace Frankie.Core
{
    public class CameraController : MonoBehaviour
    {
        // Tunables
        [Header("Hookups")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private Camera spawnAssistCamera;
        [SerializeField] private CinemachineStateDrivenCamera stateCamera;
        [SerializeField] private CinemachineVirtualCamera activeCamera;
        [SerializeField] private CinemachineVirtualCamera idleCamera;
        [Header("Camera Parameters")]
        [SerializeField] private float defaultActiveOrthoSize = 3.6f;
        [SerializeField] private float defaultIdleOrthoSize = 1.8f;

        // Cached References
        private ReInitLazyValue<Player> player;
        private ReInitLazyValue<Party> party;

        // State
        private bool usingPixelPerfectCamera = false; // Default:  Not using due to many jank
        private float currentActiveOrthoSize = 3.6f;
        private float currentIdleOrthoSize = 1.8f;
        
        // Events
        public event Action<float> activeOrthoSizeUpdated;

        #region Static
        private const string _mainCameraTag = "MainCamera";
        private const string _cameraControllerTag = "CameraController";
        public static CameraController GetCameraController()
        {
            var cameraControllerObject = GameObject.FindGameObjectWithTag(_cameraControllerTag);
            return cameraControllerObject != null ? cameraControllerObject.GetComponent<CameraController>() : null;
        }
        #endregion

        #region UnityMethods
        private void Awake()
        {
            player = new ReInitLazyValue<Player>(Player.FindPlayer);
            party = new ReInitLazyValue<Party>(SetupPartyReference);

            if (TryGetComponent(out PixelPerfectCamera pixelPerfectCamera))
            {
                if (pixelPerfectCamera.isActiveAndEnabled) { usingPixelPerfectCamera = true; }
            }
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
            SetupOverlayCameras();
        }
        #endregion

        #region PublicMethods
        public float GetActiveOrthoSize() => currentActiveOrthoSize;
        
        public void RefreshDefaultCameras()
        {
            if (party.value != null) { SetUpStateDrivenCamera(party.value.GetLeadCharacterAnimator()); }
            if (player.value != null) { SetUpVirtualCameraFollowers(player.value.transform); }
        }

        public void OverrideCameraFollower(Animator animator, Transform target)
        {
            UpdateStateAnimator(animator);
            SetUpVirtualCameraFollowers(target);
        }
        #endregion

        #region PrivateMethods
        private Party SetupPartyReference()
        {
            GameObject playerObject = Player.FindPlayerObject();
            return playerObject != null ? playerObject.GetComponent<Party>() : null;
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

        private void SetupOverlayCameras()
        {
            if (mainCamera != null && spawnAssistCamera != null)
            {
                UniversalAdditionalCameraData cameraData = mainCamera.GetUniversalAdditionalCameraData();
                cameraData.cameraStack.Clear();
                cameraData.cameraStack.Add(spawnAssistCamera);
            }
        }

        private void UpdateCameraOrthoSizes(ResolutionScaler resolutionScaler, int cameraScaling)
        {
            if (usingPixelPerfectCamera) { return; }

            currentActiveOrthoSize = (defaultActiveOrthoSize * resolutionScaler.numerator / resolutionScaler.denominator) / cameraScaling;
            currentIdleOrthoSize = (defaultIdleOrthoSize * resolutionScaler.numerator / resolutionScaler.denominator) / cameraScaling;

            if (activeCamera != null) { activeCamera.m_Lens.OrthographicSize = currentActiveOrthoSize; }
            if (idleCamera != null) { idleCamera.m_Lens.OrthographicSize = currentIdleOrthoSize; }
            
            activeOrthoSizeUpdated?.Invoke(currentActiveOrthoSize);
        }

        private void UpdateStateAnimator(Animator characterAnimator)
        {
            stateCamera.m_AnimatedTarget = characterAnimator;
        }
        #endregion
    }
}

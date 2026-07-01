using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Unity.Cinemachine;
using Frankie.Stats;
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
        [SerializeField] private CinemachineCamera activeCamera;
        [SerializeField] private CinemachineCamera idleCamera;
        [Header("Camera Parameters")]
        [SerializeField] private float defaultActiveOrthoSize = 3.6f;
        [SerializeField] private float defaultIdleOrthoSize = 1.8f;

        // Cached References
        private GameObject partyLeader;
        private Animator partyLeaderAnimator;

        // State
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
        private void OnEnable()
        {
            if (SubscribeToParty(true, out Party party))
            {
                GameObject partyLeaderObject = party.GetPartyLeaderObject();
                SetUpVirtualCameraFollowers(partyLeaderObject);
                SetUpStateDrivenCamera(partyLeaderObject.GetComponent<Animator>());
            }
            DisplayResolutions.resolutionUpdated += UpdateCameraOrthoSizes;
        }

        private void OnDisable()
        {
            SubscribeToParty(false, out Party _);
            DisplayResolutions.resolutionUpdated -= UpdateCameraOrthoSizes;
        }

        private bool SubscribeToParty(bool enable, out Party party)
        {
            party = null;
            GameObject playerObject = Player.FindPlayerObject();
            if (playerObject == null) { return false; }
            
            if (!playerObject.TryGetComponent(out party)) { return false; }
            party.SubscribeToMembersAlteredUpdates(enable, HandlePartyLeaderAnnouncements);
            return true;
        }

        private void Start()
        {
            RefreshDefaultCameras();
            SetupOverlayCameras();
        }
        #endregion

        #region PublicMethods
        public float GetActiveOrthoSize() => currentActiveOrthoSize;
        
        public void RefreshDefaultCameras(List<BaseStats> _ = null)
        {
            SetUpVirtualCameraFollowers(partyLeader);
            SetUpStateDrivenCamera(partyLeaderAnimator);
        }

        public void OverrideCameraFollower(Animator animator, Transform target)
        {
            SetUpVirtualCameraFollowers(target);
            SetUpStateDrivenCamera(animator);
        }
        #endregion

        #region PrivateMethods
        private void SetUpStateDrivenCamera(Animator animator)
        {
            if (animator == null) { return; }
            stateCamera.AnimatedTarget = animator;
        }

        private void SetUpVirtualCameraFollowers(GameObject targetObject)
        {
            if (targetObject == null) { return; }
            activeCamera.Follow = targetObject.transform;
            idleCamera.Follow = targetObject.transform;
        }

        private void SetUpVirtualCameraFollowers(Transform target)
        {
            if (target == null) { return; }
            activeCamera.Follow = target;
            idleCamera.Follow = target;
        }

        private void SetupOverlayCameras()
        {
            if (mainCamera == null || spawnAssistCamera == null) { return; }
            
            UniversalAdditionalCameraData cameraData = mainCamera.GetUniversalAdditionalCameraData();
            cameraData.cameraStack.Clear();
            cameraData.cameraStack.Add(spawnAssistCamera);
        }

        private void UpdateCameraOrthoSizes(ResolutionScaler resolutionScaler, int cameraScaling)
        {
            currentActiveOrthoSize = (defaultActiveOrthoSize * resolutionScaler.numerator / resolutionScaler.denominator) / cameraScaling;
            currentIdleOrthoSize = (defaultIdleOrthoSize * resolutionScaler.numerator / resolutionScaler.denominator) / cameraScaling;

            if (activeCamera != null) { activeCamera.Lens.OrthographicSize = currentActiveOrthoSize; }
            if (idleCamera != null) { idleCamera.Lens.OrthographicSize = currentIdleOrthoSize; }
            
            activeOrthoSizeUpdated?.Invoke(currentActiveOrthoSize);
        }

        private void HandlePartyLeaderAnnouncements(PartyAlteredData partyAlteredData)
        {
            if (!partyAlteredData.isPartyLeaderDataSet) { return; }
            SetUpVirtualCameraFollowers(partyAlteredData.GetPartyLeaderObject());
            SetUpStateDrivenCamera(partyAlteredData.partyLeaderAnimator);
        }
        #endregion
    }
}

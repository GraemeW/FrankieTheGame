using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Unity.Cinemachine;

namespace LowDefMustard.Rendering
{
    public abstract class CameraControllerBase : MonoBehaviour
    {
        // Notes:
        //  - Cinemachine active / idle cameras follow a default target (e.g. a party leader), driven by its animator via the state-driven camera
        //  - Subclasses find the default target + call SetDefaultFollowTarget whenever it changes

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
        private GameObject defaultFollowTarget;
        private Animator defaultFollowTargetAnimator;

        // State
        private bool isDefaultFollowTargetSet = false;
        private float currentActiveOrthoSize = 3.6f;
        private float currentIdleOrthoSize = 1.8f;

        // Events
        public event Action<float> activeOrthoSizeUpdated;

        #region Static
        private const string _cameraControllerTag = "CameraController";
        public static CameraControllerBase GetCameraController()
        {
            var cameraControllerObject = GameObject.FindGameObjectWithTag(_cameraControllerTag);
            return cameraControllerObject != null ? cameraControllerObject.GetComponent<CameraControllerBase>() : null;
        }
        #endregion

        #region AbstractMethods
        protected abstract GameObject FindDefaultFollowTarget();
        protected abstract void SubscribeToDefaultFollowTarget(bool subscribe);
        #endregion

        #region UnityMethods
        private void OnEnable()
        {
            SubscribeToDefaultFollowTarget(true);
            SetDefaultFollowTarget(FindDefaultFollowTarget());
            
            DisplayResolutions.resolutionUpdated -= UpdateCameraOrthoSizes;
            DisplayResolutions.resolutionUpdated += UpdateCameraOrthoSizes;
        }

        private void OnDisable()
        {
            SubscribeToDefaultFollowTarget(false);
            DisplayResolutions.resolutionUpdated -= UpdateCameraOrthoSizes;
        }

        private void Start()
        {
            if (!isDefaultFollowTargetSet) { SetDefaultFollowTarget(FindDefaultFollowTarget()); }
            RefreshDefaultCameras();
            SetupOverlayCameras();
        }
        #endregion

        #region PublicMethods
        public float GetActiveOrthoSize() => currentActiveOrthoSize;

        public void RefreshDefaultCameras()
        {
            SetUpVirtualCameraFollowers(defaultFollowTarget != null ? defaultFollowTarget.transform : null);
            SetUpStateDrivenCamera(defaultFollowTargetAnimator);
        }

        public void OverrideCameraFollower(Animator animator, Transform target)
        {
            SetUpVirtualCameraFollowers(target);
            SetUpStateDrivenCamera(animator);
        }
        #endregion

        #region ProtectedMethods
        protected void SetDefaultFollowTarget(GameObject target)
        {
            if (target == null) { return; }

            isDefaultFollowTargetSet = true;
            defaultFollowTarget = target;
            defaultFollowTargetAnimator = target.GetComponent<Animator>();
            RefreshDefaultCameras();
        }
        #endregion

        #region PrivateMethods
        private void SetUpStateDrivenCamera(Animator animator)
        {
            if (animator == null) { return; }
            stateCamera.AnimatedTarget = animator;
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

        private void UpdateCameraOrthoSizes(ResolutionScaler resolutionScaler, float cameraScaling)
        {
            currentActiveOrthoSize = (defaultActiveOrthoSize * resolutionScaler.numerator / resolutionScaler.denominator) / cameraScaling;
            currentIdleOrthoSize = (defaultIdleOrthoSize * resolutionScaler.numerator / resolutionScaler.denominator) / cameraScaling;

            if (activeCamera != null) { activeCamera.Lens.OrthographicSize = currentActiveOrthoSize; }
            if (idleCamera != null) { idleCamera.Lens.OrthographicSize = currentIdleOrthoSize; }

            activeOrthoSizeUpdated?.Invoke(currentActiveOrthoSize);
        }
        #endregion
    }
}

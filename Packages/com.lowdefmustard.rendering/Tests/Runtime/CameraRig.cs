#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Unity.Cinemachine;
using LowDefMustard.Utils.Tests;

namespace LowDefMustard.Rendering.Tests
{
    internal sealed class CameraRig
    {
        // Notes:
        //  - Built inactive so every hookup is in place before OnEnable (call Activate, then yield a frame for Start)
        //  - Mirrors a typical camera prefab:
        //      - main + overlay spawn-assist camera
        //      - a state-driven camera w/ active + idle children
        //      - a target w/ an animator

        public GameObject root { get; }
        public TestCameraController controller { get; }
        public Camera mainCamera { get; }
        public Camera spawnAssistCamera { get; }
        public CinemachineStateDrivenCamera stateCamera { get; }
        public CinemachineCamera activeCamera { get; }
        public CinemachineCamera idleCamera { get; }
        public GameObject target { get; }

        public CameraRig()
        {
            root = new GameObject("_TEMP_CameraRig");
            root.SetActive(false);
            controller = root.AddComponent<TestCameraController>();

            mainCamera = AddChild<Camera>("Main Camera", root.transform);
            spawnAssistCamera = AddChild<Camera>("Spawn Assist Camera", root.transform);
            spawnAssistCamera.GetUniversalAdditionalCameraData().renderType = CameraRenderType.Overlay;
            stateCamera = AddChild<CinemachineStateDrivenCamera>("State Driven Camera", root.transform);
            activeCamera = AddChild<CinemachineCamera>("VCam Active", stateCamera.transform);
            idleCamera = AddChild<CinemachineCamera>("VCam Idle", stateCamera.transform);

            target = CreateTarget("Target");
            controller.fallbackTarget = target;

            SerializedFieldInjector.Set(controller, CameraControllerBase.mainCameraField, mainCamera);
            SerializedFieldInjector.Set(controller, CameraControllerBase.spawnAssistCameraField, spawnAssistCamera);
            SerializedFieldInjector.Set(controller, CameraControllerBase.stateCameraField, stateCamera);
            SerializedFieldInjector.Set(controller, CameraControllerBase.activeCameraField, activeCamera);
            SerializedFieldInjector.Set(controller, CameraControllerBase.idleCameraField, idleCamera);
        }

        public void Activate() => root.SetActive(true);

        public static GameObject CreateTarget(string name)
        {
            var targetObject = new GameObject($"_TEMP_{name}");
            targetObject.AddComponent<Animator>();
            return targetObject;
        }

        public void Destroy()
        {
            Object.Destroy(root);
            Object.Destroy(target);
        }

        private static T AddChild<T>(string name, Transform parent) where T : Component
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child.AddComponent<T>();
        }
    }
}
#endif

using UnityEngine;
using Frankie.Utils;

namespace Frankie.Core
{
    [RequireComponent(typeof(Camera))]
    public class CameraSpawnAssist : MonoBehaviour
    {
        [SerializeField][Range(1f, 2f)] private float orthoSpawnViewMultiplier = 1.2f;
        
        // Cached References
        private Camera spawnAssistCamera;
        private ReInitLazyValue<CameraController> playerCameraController;
        
        private void Awake()
        {
            spawnAssistCamera = GetComponent<Camera>();
            playerCameraController = new ReInitLazyValue<CameraController>(CameraController.GetCameraController);
        }

        private void Start()
        {
            playerCameraController.ForceInit();
            
            if (playerCameraController.value == null) { return; }
            HandleOrthoSizeUpdated(playerCameraController.value.GetActiveOrthoSize());
        }

        private void OnEnable()
        {
            playerCameraController.ForceInit();
            UnityEngine.Debug.Log(playerCameraController.value != null);
            if (playerCameraController.value != null) { playerCameraController.value.activeOrthoSizeUpdated += HandleOrthoSizeUpdated; }
        }

        private void OnDisable()
        {
            if (playerCameraController.value != null) { playerCameraController.value.activeOrthoSizeUpdated -= HandleOrthoSizeUpdated; }
        }

        private void HandleOrthoSizeUpdated(float newOrthoSize)
        {
            spawnAssistCamera.orthographicSize = newOrthoSize * orthoSpawnViewMultiplier;
        }
    }
}

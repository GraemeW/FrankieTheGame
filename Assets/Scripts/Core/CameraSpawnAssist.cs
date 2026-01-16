using UnityEngine;

namespace Frankie.Core
{
    [RequireComponent(typeof(Camera))]
    public class CameraSpawnAssist : MonoBehaviour
    {
        [Header("HookUps")] 
        [SerializeField] private CameraController cameraController;
        [Header("Parameters")]
        [SerializeField][Range(1f, 2f)] private float orthoSpawnViewMultiplier = 1.25f;
        
        // Cached References
        private Camera spawnAssistCamera;
        
        private void Awake()
        {
            spawnAssistCamera = GetComponent<Camera>();
        }

        private void Start()
        {
            if (cameraController == null) { return; }
            HandleOrthoSizeUpdated(cameraController.GetActiveOrthoSize());
        }

        private void OnEnable()
        {
            if (cameraController != null) { cameraController.activeOrthoSizeUpdated += HandleOrthoSizeUpdated; }
        }

        private void OnDisable()
        {
            if (cameraController != null) { cameraController.activeOrthoSizeUpdated -= HandleOrthoSizeUpdated; }
        }

        private void HandleOrthoSizeUpdated(float newOrthoSize)
        {
            spawnAssistCamera.orthographicSize = newOrthoSize * orthoSpawnViewMultiplier;
        }
    }
}

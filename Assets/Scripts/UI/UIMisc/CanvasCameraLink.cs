using UnityEngine;
using UnityEngine.SceneManagement;

namespace Frankie.Utils.UI
{
    [RequireComponent(typeof(Canvas))]
    public class CanvasCameraLink : MonoBehaviour
    {
        // Tunables
        [SerializeField] private CanvasSortingOverlayType canvasSortingOverlayType;

        // STATIC
        private const string _sortingLayerFaderOverlay = "FaderOverlay";
        private const string _sortingLayerBattleOverlay = "BattleOverlay";

        // Cached References
        private Canvas canvas;

        private void Awake()
        {
            canvas = GetComponent<Canvas>();
        }

        private void OnEnable()
        {
            if (canvas == null) { GetComponent<Canvas>(); }
            SceneManager.activeSceneChanged += SetupCamera;
            SetupCamera();
        }

        private void OnDisable()
        {
            SceneManager.activeSceneChanged -= SetupCamera;
        }

        private void SetupCamera(Scene lastScene, Scene newScene)
        {
            SetupCamera();
        }

        private void SetupCamera()
        {
            if (canvas == null) { return; }
            
            canvas.worldCamera = Camera.main;
            switch (canvasSortingOverlayType)
            {
                case CanvasSortingOverlayType.FaderOverlay:
                    canvas.sortingLayerName = _sortingLayerFaderOverlay;
                    break;
                case CanvasSortingOverlayType.BattleOverlay:
                    canvas.sortingLayerName = _sortingLayerBattleOverlay;
                    break;
            }
        }
    }
}

using UnityEngine;
using UnityEngine.UIElements;

namespace Frankie.Utils.UI
{
    [RequireComponent(typeof(PanelRenderer))]
    public sealed class CameraSpacePanel : MonoBehaviour
    {
        // For rendering a UI panel in world space (fitted to the main camera) -> required for UI Toolkit elements to be captured by camera effects (battle entry twirl, fader, etc.) 
        //  - Runs on a runtime copy of the PanelRenderer's PanelSettings ~ the shared asset is untouched
        //  - UI Toolkit has no screen-space-camera mode and raises no camera / view-changed event, so the fit is re-checked every LateUpdate
        // Alt approaches to this pending, either a) use a back-drop filter overlay for the URP battle entry (TBD viability) or b) wait for UI Toolkit camera space support (on the roadmap, no date)

        // Tunables
        [SerializeField] private int sortingOrder = 20;
        [SerializeField] private float planeDistance = 100f;
        [SerializeField][Tooltip("Panel pixels per world unit - must match to PanelSettings' own pixels-per-unit")][Min(1f)] private float panelPixelsPerUnit = 100f;

        // Static/Const
        private const string _sortingLayerBattleOverlay = "BattleOverlay";
        
        // State
        private PanelSettings overlayPanelSettings;
        private PanelSettings worldPanelSettings;
        private float fittedAspect;
        private float fittedViewHeight;

        // Cached References
        private PanelRenderer panelRenderer;

        #region UnityMethods
        private void Awake()
        {
            panelRenderer = GetComponent<PanelRenderer>();
        }

        private void OnEnable()
        {
            overlayPanelSettings = panelRenderer.panelSettings; // Cache hooked-up panel renderer settings
            
            // Dupe a copy for worldPanelSettings
            if (worldPanelSettings == null)
            {
                worldPanelSettings = Instantiate(overlayPanelSettings);
                worldPanelSettings.renderMode = PanelRenderMode.WorldSpace;
            }

            // Set panelRenderer to worldPanelSettings && override relevant settings
            panelRenderer.panelSettings = worldPanelSettings;
            panelRenderer.worldSpaceSizeMode = WorldSpaceSizeMode.Fixed;
            panelRenderer.sortingLayerName = _sortingLayerBattleOverlay;
            panelRenderer.sortingOrder = sortingOrder;
            fittedAspect = 0f; // Force a full fit
            fittedViewHeight = 0f;
            FitToCamera();
        }

        private void OnDisable()
        {
            panelRenderer.panelSettings = overlayPanelSettings;
        }

        private void OnDestroy()
        {
            if (worldPanelSettings != null) { Destroy(worldPanelSettings); }
        }

        private void LateUpdate()
        {
            FitToCamera();
        }
        #endregion

        #region PrivateMethods
        private void FitToCamera()
        {
            Camera fitCamera = Camera.main;
            if (fitCamera == null) { return; }

            FitToView(fitCamera);
            FollowCamera(fitCamera.transform);
        }

        // Panel layout matches the overlay panel:  reference height, width following the camera aspect, scaled to fill the view
        private void FitToView(Camera fitCamera)
        {
            float viewHeight = fitCamera.orthographic ? 2f * fitCamera.orthographicSize : 2f * planeDistance * Mathf.Tan(0.5f * fitCamera.fieldOfView * Mathf.Deg2Rad);
            if (Mathf.Approximately(fitCamera.aspect, fittedAspect) && Mathf.Approximately(viewHeight, fittedViewHeight)) { return; }
            fittedAspect = fitCamera.aspect;
            fittedViewHeight = viewHeight;

            float panelHeight = worldPanelSettings.referenceResolution.y;
            panelRenderer.worldSpaceSize = new Vector2(panelHeight * fittedAspect, panelHeight);

            float fitScale = viewHeight * panelPixelsPerUnit / panelHeight;
            transform.localScale = Vector3.one; // Note:  Parent scale is unknown - resolve the world scale via lossyScale
            Vector3 parentScale = transform.lossyScale;
            transform.localScale = new Vector3(SafeDivide(fitScale, parentScale.x), SafeDivide(fitScale, parentScale.y), SafeDivide(fitScale, parentScale.z));
        }

        private void FollowCamera(Transform cameraTransform)
        {
            Vector3 fitPosition = cameraTransform.position + cameraTransform.forward * planeDistance;
            if (transform.position == fitPosition && transform.rotation == cameraTransform.rotation) { return; }
            transform.SetPositionAndRotation(fitPosition, cameraTransform.rotation);
        }

        private static float SafeDivide(float value, float divisor) => Mathf.Approximately(divisor, 0f) ? value : value / divisor;
        #endregion
    }
}

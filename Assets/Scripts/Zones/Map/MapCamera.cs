using UnityEngine;
using Frankie.Core;

namespace Frankie.ZoneManagement
{
    public class MapCamera : MonoBehaviour
    {
        // Tunables
        [SerializeField] private Camera subCamera;
        [SerializeField] private RenderTexture mapRenderTexture;
        [SerializeField] private bool subscribeToSceneEvents;

        #region UnityMethods
        private void Awake()
        {
            subCamera.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            SubscribeToSceneLoader(true);
        }

        private void OnDisable()
        {
            SubscribeToSceneLoader(false);
        }
        #endregion

        #region PrivateMethods
        private void SubscribeToSceneLoader(bool enable)
        {
            if (!subscribeToSceneEvents) { return; }

            if (enable)
            {
                SceneLoader.leavingZone += UpdateMap;
                SceneLoader.zoneUpdated += UpdateMap;
            }
            else
            {
                SceneLoader.leavingZone -= UpdateMap;
                SceneLoader.zoneUpdated -= UpdateMap;
            }
        }

        private void UpdateMap(Zone zone)
        {
            if (zone == null || !zone.ShouldUpdateMap()) { return; }

            subCamera.targetTexture = mapRenderTexture; // Enable/disable target texture otherwise Camera's OnDisable will force a final black frame
            subCamera.gameObject.SetActive(true);
            SetupPlayerFollow();
            subCamera.Render();
            subCamera.targetTexture = null;
            subCamera.gameObject.SetActive(false);
        }
        
        public void UpdateMap()
        {
            Zone zone = SceneLoader.GetCurrentZone();
            UpdateMap(zone);
        }

        private void SetupPlayerFollow()
        {
            GameObject playerObject = Player.FindPlayerObject();
            if (playerObject == null) { return; }

            Transform playerTransform = playerObject.transform;
            var newCameraPosition = new Vector3(playerTransform.position.x, playerTransform.position.y, subCamera.transform.position.z);
            subCamera.transform.position = newCameraPosition;
        }
        #endregion
    }
}

using UnityEngine;
using Frankie.Core;
using Frankie.Stats;

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
            TrackToPartyLeaderPosition();
            subCamera.Render();
            subCamera.targetTexture = null;
            subCamera.gameObject.SetActive(false);
        }
        
        public void UpdateMap()
        {
            Zone zone = SceneLoader.GetCurrentZone();
            UpdateMap(zone);
        }

        private void TrackToPartyLeaderPosition()
        {
            GameObject playerObject = Player.FindPlayerObject();
            if (playerObject == null || !playerObject.TryGetComponent(out Party party)) { return; }
            if (!party.TryGetPartyLeaderPosition(out Vector2 partyLeaderPosition)) { return; }
            
            var newCameraPosition = new Vector3(partyLeaderPosition.x, partyLeaderPosition.y, subCamera.transform.position.z);
            subCamera.transform.position = newCameraPosition;
        }
        #endregion
    }
}

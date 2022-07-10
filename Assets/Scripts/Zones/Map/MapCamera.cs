using Frankie.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

namespace Frankie.ZoneManagement
{
    public class MapCamera : MonoBehaviour
    {
        // Tunables
        [SerializeField] Camera subCamera = null;
        [SerializeField] RenderTexture mapRenderTexture = null;
        [SerializeField] bool subscribeToSceneEvents = false;

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
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) { return; }

            Transform playerTransform = player.transform;
            Vector3 newCameraPosition = new Vector3(playerTransform.position.x, playerTransform.position.y, subCamera.transform.position.z);
            subCamera.transform.position = newCameraPosition;
        }
    }
}


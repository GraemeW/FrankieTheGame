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

        // Cached References
        SceneLoader sceneLoader = null;

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

            SetupSceneLoader();

            if (enable)
            {
                sceneLoader.leavingZone += UpdateMap;
                sceneLoader.zoneUpdated += UpdateMap;
            }
            else
            {
                sceneLoader.leavingZone -= UpdateMap;
                sceneLoader.zoneUpdated -= UpdateMap;
            }
        }

        private void SetupSceneLoader()
        {
            if (sceneLoader == null)
            {
                GameObject sceneLoaderObject = GameObject.FindGameObjectWithTag("SceneLoader");
                if (sceneLoaderObject == null) { return; }

                sceneLoader = sceneLoaderObject.GetComponent<SceneLoader>();
            }
        }

        private void UpdateMap(Zone zone)
        {
            if (!zone.ShouldUpdateMap()) { return; }

            subCamera.targetTexture = mapRenderTexture; // Enable/disable target texture otherwise Camera's OnDisable will force a final black frame
            subCamera.gameObject.SetActive(true);
            SetupPlayerFollow();
            subCamera.Render();
            subCamera.targetTexture = null;
            subCamera.gameObject.SetActive(false);
        }
        
        public void UpdateMap()
        {
            SetupSceneLoader();

            Zone zone = sceneLoader.GetCurrentZone();
            UpdateMap(zone);
        }

        private void SetupPlayerFollow()
        {
            Transform playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
            Vector3 newCameraPosition = new Vector3(playerTransform.position.x, playerTransform.position.y, subCamera.transform.position.z);
            subCamera.transform.position = newCameraPosition;
        }
    }
}


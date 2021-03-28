using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Frankie.ZoneManagement
{
    public class SceneLoader : MonoBehaviour
    {
        // Tunables
        [Header("Core Scene Listing")]
        [SerializeField] Zone splashScreen = null;
        [SerializeField] Zone startScreen = null;
        [SerializeField] Zone newGame = null;

        // State
        Zone currentZone = null;
        int currentSceneIndex = 0;

        // Events
        public event Action<Zone> zoneUpdated;

        private void Awake()
        {
            currentZone = Zone.GetFromSceneReference(SceneManager.GetActiveScene().name);
        }

        public Zone GetCurrentZone()
        {
            return currentZone;
        }

        public IEnumerator SplashDelayToLoad(float splashDelayTime)
        {
            yield return new WaitForSeconds(splashDelayTime);
            yield return LoadStartScreen();
        }

        public void QueueStartScreen()
        {
            StartCoroutine(LoadStartScreen());
        }

        public void QueueNewGame()
        {
            StartCoroutine(LoadNewGame());
        }

        private IEnumerator LoadStartScreen()
        {
            yield return SceneManager.LoadSceneAsync(startScreen.GetSceneReference().SceneName);
            SetCurrentZone(startScreen);
        }

        private IEnumerator LoadNewGame()
        {
            yield return SceneManager.LoadSceneAsync(newGame.GetSceneReference().SceneName);
            SetCurrentZone(newGame);
        }

        public IEnumerator LoadNewSceneAsync(Zone zone)
        {
            yield return SceneManager.LoadSceneAsync(zone.GetSceneReference().SceneName);
            SetCurrentZone(zone);
        }

        public void ExitGame()
        {
            Application.Quit();
        }

        private void SetCurrentZone(Zone zone)
        {
            currentZone = zone;
            currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
            if (zoneUpdated != null)
            {
                zoneUpdated.Invoke(currentZone);
            }
        }

        public void SetCurrentZoneToCurrentScene()
        {
            SetCurrentZone(Zone.GetFromSceneReference(SceneManager.GetActiveScene().name));
        }
    }
}
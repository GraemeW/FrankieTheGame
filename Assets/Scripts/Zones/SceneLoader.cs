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
        Zone lastZone = null;
        Zone currentZone = null;

        // Events
        public event Action<Zone> leavingZone;
        public event Action<Zone> zoneUpdated;

        public Zone GetCurrentZone()
        {
            if (currentZone == null) { currentZone = Zone.GetFromSceneReference(SceneManager.GetActiveScene().name); }
            return currentZone;
        }

        public IEnumerator SplashDelayToLoad(float splashDelayTime)
        {
            yield return new WaitForSeconds(splashDelayTime);
            yield return LoadStartScreen();
        }

        public void QueueSplashScreen()
        {
            StartCoroutine(LoadSplashScreen());
        }

        public void QueueStartScreen()
        {
            StartCoroutine(LoadStartScreen());
        }

        public void QueueNewGame()
        {
            StartCoroutine(LoadNewGame());
        }

        private IEnumerator LoadSplashScreen()
        {
            yield return SceneManager.LoadSceneAsync(splashScreen.GetSceneReference().SceneName);
            SetCurrentZone(splashScreen);
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
            SetLastZone();
            yield return SceneManager.LoadSceneAsync(zone.GetSceneReference().SceneName);
            SetCurrentZone(zone);
        }

        public void ExitGame()
        {
            Application.Quit();
        }

        private void SetLastZone()
        {
            lastZone = currentZone;
            leavingZone?.Invoke(lastZone);
        }

        private void SetCurrentZone(Zone zone)
        {
            currentZone = zone;
            zoneUpdated?.Invoke(currentZone);
        }

        public void SetCurrentZoneToCurrentScene()
        {
            SetCurrentZone(Zone.GetFromSceneReference(SceneManager.GetActiveScene().name));
        }
    }
}
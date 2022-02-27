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
        [SerializeField] Zone gameOverScreen = null;
        [SerializeField] Zone gameWinScreen = null;
        [SerializeField] Zone newGame = null;

        // State
        static Zone lastZone = null;
        static Zone currentZone = null;

        // Events
        public static event Action<Zone> leavingZone;
        public static event Action<Zone> zoneUpdated;

        #region StaticMethods
        public static Zone GetCurrentZone()
        {
            if (currentZone == null) { currentZone = Zone.GetFromSceneReference(SceneManager.GetActiveScene().name); }
            return currentZone;
        }
        #endregion

        #region PublicMethods
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

        public void QueueGameOverScreen()
        {
            StartCoroutine(LoadGameOverScreen());
        }

        public void QueueGameWinScreen()
        {
            StartCoroutine(LoadGameWinScreen());
        }

        public void QueueNewGame()
        {
            StartCoroutine(LoadNewGame());
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

        public void SetCurrentZoneToCurrentScene()
        {
            SetCurrentZone(Zone.GetFromSceneReference(SceneManager.GetActiveScene().name));
        }
        #endregion

        #region PrivateMethods
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

        private IEnumerator LoadGameOverScreen()
        {
            yield return SceneManager.LoadSceneAsync(gameOverScreen.GetSceneReference().SceneName);
            SetCurrentZone(gameOverScreen);
        }

        private IEnumerator LoadGameWinScreen()
        {
            yield return SceneManager.LoadSceneAsync(gameWinScreen.GetSceneReference().SceneName);
            SetCurrentZone(gameWinScreen);
        }

        private IEnumerator LoadNewGame()
        {
            yield return SceneManager.LoadSceneAsync(newGame.GetSceneReference().SceneName);
            SetCurrentZone(newGame);
        }

        private static void SetLastZone()
        {
            lastZone = currentZone;
            leavingZone?.Invoke(lastZone);
        }

        private static void SetCurrentZone(Zone zone)
        {
            currentZone = zone;
            zoneUpdated?.Invoke(currentZone);
        }
        #endregion
    }
}
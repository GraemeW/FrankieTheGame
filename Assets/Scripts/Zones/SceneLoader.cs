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
        [SerializeField] private Zone splashScreen;
        [SerializeField] private Zone startScreen;
        [SerializeField] private Zone gameOverScreen;
        [SerializeField] private Zone gameWinScreen;
        [SerializeField] private Zone newGame;

        // State
        private static Zone _lastZone;
        private static Zone _currentZone;

        // Events
        public static event Action<Zone> leavingZone;
        public static event Action<Zone> zoneUpdated;

        #region StaticMethods
        private const string _sceneLoaderTag = "SceneLoader";
        public static SceneLoader FindSceneLoader()
        {
            var sceneLoaderGameObject = GameObject.FindGameObjectWithTag(_sceneLoaderTag);
            return sceneLoaderGameObject != null ? sceneLoaderGameObject.GetComponent<SceneLoader>() : null;
        }

        public static Zone GetCurrentZone()
        {
            if (_currentZone == null) { _currentZone = Zone.GetFromSceneReference(SceneManager.GetActiveScene().name); }
            return _currentZone;
        }
        #endregion

        #region PublicMethods
        public Zone GetGameWinZone() => gameWinScreen;
        public Zone GetGameOverZone() => gameOverScreen;

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

        public void QueueNewGame(Action newGameLoadedCallback, Zone newGameZoneOverride = null)
        {
            StartCoroutine(LoadNewGame(newGameLoadedCallback, newGameZoneOverride));
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

        private IEnumerator LoadNewGame(Action newGameLoadedCallback, Zone newGameZoneOverride = null)
        {
            if (newGameZoneOverride == null) { newGameZoneOverride = newGame; }
            if (newGameZoneOverride == null) { yield break; }
            
            yield return SceneManager.LoadSceneAsync(newGameZoneOverride.GetSceneReference().SceneName);
            SetCurrentZone(newGame);
            newGameLoadedCallback?.Invoke();
        }

        private static void SetLastZone()
        {
            _lastZone = _currentZone;
            leavingZone?.Invoke(_lastZone);
        }

        private static void SetCurrentZone(Zone zone)
        {
            _currentZone = zone;
            zoneUpdated?.Invoke(_currentZone);
        }
        #endregion
    }
}

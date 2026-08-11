using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LowDefMustard.Zones
{
    public class SceneLoader : MonoBehaviour
    {
        // Tunables
        [Header("Core Scene Listing")]
        [SerializeField] private Zone splashScreen;
        [SerializeField] private Zone startScreen;
        [SerializeField] private Zone namingScreen;
        [SerializeField] private Zone newGame;
        [SerializeField] private Zone gameOverScreen;
        [SerializeField] private Zone gameWinScreen;

        // Static State
        private static SceneLoader _activeSceneLoader;
        private static bool _isCurrentlyLoading = false;
        private static Zone _lastZone;
        private static Zone _currentZone;

        // External Hooks
        public static Func<Zone> DemoZoneOverrideProvider;
        public Action<Zone, bool> sceneLoadFadeProvider;
        
        // Events
        public static event Action<Zone> leavingZone;
        public static event Action<Zone> zoneUpdated;

        #region StaticFind
        private const string _sceneLoaderTag = "SceneLoader";
        public static SceneLoader FindSceneLoader()
        {
            var sceneLoaderGameObject = GameObject.FindGameObjectWithTag(_sceneLoaderTag);
            return sceneLoaderGameObject != null ? sceneLoaderGameObject.GetComponent<SceneLoader>() : null;
        }
        #endregion

        #region UnityMethods
        private void Awake()
        {
            // SceneLoader is included in PersistentObjects and thus a singleton by standard implementation
            // So:  establish sceneLoader in static state for public method calls
            _activeSceneLoader = this;
        }
        #endregion
        
        #region GettersSetters
        public static Zone GetCurrentZone()
        {
            if (_currentZone == null) { _currentZone = Zone.GetFromSceneReference(SceneManager.GetActiveScene().name); }
            return _currentZone;
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
        
        public static void SetCurrentZoneToCurrentScene()
        {
            SetCurrentZone(Zone.GetFromSceneReference(SceneManager.GetActiveScene().name));
        }
        #endregion

        #region PublicMethods
        public static IEnumerator LoadNewSceneAsync(Zone zone)
        {
            if (_isCurrentlyLoading) { yield break; }
            
            _isCurrentlyLoading = true;
            SetLastZone();
            yield return SceneManager.LoadSceneAsync(zone.GetSceneReference().SceneName);
            SetCurrentZone(zone);
            _isCurrentlyLoading = false;
        }

        public static void QueueScene(SceneQueueType sceneQueueType, SceneQueueData sceneQueueData)
        {
            if (_isCurrentlyLoading) { return; }
            
            if (_activeSceneLoader == null) { _activeSceneLoader = FindSceneLoader(); }
            if (_activeSceneLoader == null) { return; }
            _activeSceneLoader.StartLoadScene(sceneQueueType, sceneQueueData);
        }

        public static void QueueDelayedDestroy(IList<GameObject> entries)
        {
            if (_activeSceneLoader == null) { _activeSceneLoader = FindSceneLoader(); }
            if (_activeSceneLoader == null) { return; }
            _activeSceneLoader.StartDelayedDestroy(entries);
        }

        public static void ExitGame()
        {
            Application.Quit();
        }
        #endregion

        #region PrivateMethods
        private void StartLoadScene(SceneQueueType sceneQueueType, SceneQueueData sceneQueueData)
        {
            Zone zone = ReconcileZone(sceneQueueType);
            if (zone == null) { return; }

            if (sceneQueueData.useFader && sceneLoadFadeProvider != null)
            {
                // Standard Behaviour:  Load to GameOver scene while skipping session saving
                // From GameOver scene only player will be present, and we can save session to carry over player exp, etc.
                bool saveSession = sceneQueueType != SceneQueueType.GameOver;
                sceneLoadFadeProvider.Invoke(zone, saveSession);
            }
            else
            {
                StartCoroutine(LoadScene(zone, sceneQueueData.delayTime, sceneQueueData.sceneLoadedCallback));
            }
        }

        private Zone ReconcileZone(SceneQueueType sceneQueueType)
        {
            Zone zone = null;
            if (sceneQueueType == SceneQueueType.New)
            {
                zone = DemoZoneOverrideProvider?.Invoke();
                if (zone != null) { return zone; }
            }
            
            return sceneQueueType switch
            {
                SceneQueueType.Splash => splashScreen,
                SceneQueueType.Start => startScreen,
                SceneQueueType.Naming => namingScreen,
                SceneQueueType.New => newGame,
                SceneQueueType.GameOver => gameOverScreen,
                SceneQueueType.GameWin => gameWinScreen,
                _ => zone
            };
        }

        private void StartDelayedDestroy(IList<GameObject> entries)
        {
            StartCoroutine(DelayedDestroy(entries));
        }
        
        private static IEnumerator LoadScene(Zone zone, float delayTime, Action sceneLoadedCallback)
        {
            if (zone == null) { yield break; }

            _isCurrentlyLoading = true;
            yield return new WaitForSeconds(delayTime);
            yield return SceneManager.LoadSceneAsync(zone.GetSceneReference().SceneName);
            SetCurrentZone(zone);
            _isCurrentlyLoading = false;
            sceneLoadedCallback?.Invoke();
        }

        private static IEnumerator DelayedDestroy(IList<GameObject> entries)
        {
            yield return null;
            foreach (GameObject entry in entries)
            {
                Destroy(entry);
            }
        }
        #endregion
    }
}

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

        // Static State
        private static SceneLoader _activeSceneLoader;
        private static Zone _lastZone;
        private static Zone _currentZone;

        // Events
        public static event Action<Zone> leavingZone;
        public static event Action<Zone> zoneUpdated;

        #region PrivateStatic
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
            SetLastZone();
            yield return SceneManager.LoadSceneAsync(zone.GetSceneReference().SceneName);
            SetCurrentZone(zone);
        }

        public static void QueueScene(SceneQueueType sceneQueueType, SceneQueueData sceneQueueData)
        {
            if (_activeSceneLoader == null) { _activeSceneLoader = FindSceneLoader(); }
            if (_activeSceneLoader == null) { return; }
            _activeSceneLoader.StartLoadScene(sceneQueueType, sceneQueueData);
        }

        public static void ExitGame()
        {
            Application.Quit();
        }
        #endregion

        #region PrivateMethods
        private void StartLoadScene(SceneQueueType sceneQueueType, SceneQueueData sceneQueueData)
        {
            Zone zone = ReconcileZone(sceneQueueType, sceneQueueData.zoneOverride);
            if (zone == null) { return; }

            if (sceneQueueData.useFader)
            {
                // Standard Behaviour:  Load to GameOver scene while skipping session saving
                // From GameOver scene only player will be present, and we can save session to carry over player exp, etc.
                bool saveSession = sceneQueueType != SceneQueueType.GameOver;
                Fader.StartZoneFade(zone, new FaderEventTriggers(), saveSession);
            }
            else
            {
                StartCoroutine(LoadScene(zone, sceneQueueData.delayTime, sceneQueueData.sceneLoadedCallback));
            }
        }

        private Zone ReconcileZone(SceneQueueType sceneQueueType, Zone zoneOverride)
        {
            Zone zone = zoneOverride;
            if (zone == null)
            {
                zone = sceneQueueType switch
                {
                    SceneQueueType.Splash => splashScreen,
                    SceneQueueType.Start => startScreen,
                    SceneQueueType.New => newGame,
                    SceneQueueType.GameOver => gameOverScreen,
                    SceneQueueType.GameWin => gameWinScreen,
                    _ => zone
                };
            }
            return zone;
        }
        
        private IEnumerator LoadScene(Zone zone, float delayTime, Action sceneLoadedCallback)
        {
            if (zone == null) { yield break; }
            
            yield return new WaitForSeconds(delayTime);
            yield return SceneManager.LoadSceneAsync(zone.GetSceneReference().SceneName);
            SetCurrentZone(zone);
            sceneLoadedCallback?.Invoke();
        }
        #endregion
    }
}

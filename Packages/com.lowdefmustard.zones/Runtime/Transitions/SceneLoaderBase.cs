using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using LowDefMustard.Utils;

namespace LowDefMustard.Zones
{
    public abstract class SceneLoaderBase : MonoBehaviour
    {
        public Action<Zone, bool> sceneLoadFadeProvider;
        
        #region StaticFind
        protected const string sceneLoaderTag = "SceneLoader";
        public static SceneLoaderBase FindSceneLoader()
        {
            // Generic (non-typed) finder - Typed finder below
            var sceneLoaderGameObject = GameObject.FindGameObjectWithTag(sceneLoaderTag);
            return sceneLoaderGameObject != null ? sceneLoaderGameObject.GetComponent<SceneLoaderBase>() : null;
        }
        #endregion
        
        // Static State
        private static SceneLoaderBase _activeSceneLoaderBase;
        private static Zone _lastZone;
        private static Zone _currentZone;
        protected static bool IsCurrentlyLoading = false;
        
        // External Hooks
        public static Func<Zone> DemoZoneOverrideProvider;
        
        // Events
        public static event Action<Zone> leavingZone;
        public static event Action<Zone> zoneUpdated;
        
        #region UnityMethods
        protected virtual void Awake()
        {
            // SceneLoader is included in PersistentObjects and thus a singleton by standard implementation
            // So:  Establish sceneLoader in static state for public method calls
            // Note:  Typed version is instantiated below
            _activeSceneLoaderBase = this;
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
            if (IsCurrentlyLoading) { yield break; }
            
            IsCurrentlyLoading = true;
            SetLastZone();
            yield return SceneManager.LoadSceneAsync(zone.GetSceneReference().SceneName);
            SetCurrentZone(zone);
            IsCurrentlyLoading = false;
        }
        
        public static void QueueDelayedDestroy(IList<GameObject> entries)
        {
            if (_activeSceneLoaderBase == null) { _activeSceneLoaderBase = FindSceneLoader(); }
            if (_activeSceneLoaderBase == null) { return; }
            _activeSceneLoaderBase.StartDelayedDestroy(entries);
        }
        
        public static void ExitGame()
        {
            Application.Quit();
        }
        #endregion
        
        #region PrivateMethods
        protected static IEnumerator LoadScene(Zone zone, float delayTime, Action sceneLoadedCallback)
        {
            if (zone == null) { yield break; }

            IsCurrentlyLoading = true;
            yield return new WaitForSeconds(delayTime);
            yield return SceneManager.LoadSceneAsync(zone.GetSceneReference().SceneName);
            SetCurrentZone(zone);
            IsCurrentlyLoading = false;
            sceneLoadedCallback?.Invoke();
        }

        private void StartDelayedDestroy(IList<GameObject> entries)
        {
            StartCoroutine(DelayedDestroy(entries));
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
    
    public abstract class SceneLoaderBase<TSceneType> : SceneLoaderBase where TSceneType : struct, Enum
    {
        // Tunables
        [Header("Core Scene Listing")]
        [EnumKeyedCollection][SerializeField] private ZoneSceneTypeLookup<TSceneType> zoneSceneTypeLookup;
        
        // Static
        private static SceneLoaderBase<TSceneType> _activeSceneLoader;
        private static SceneLoaderBase<T> FindSceneLoader<T>()  where T : struct, Enum
        {
            // Generic (non-typed) finder - Typed finder below
            var sceneLoaderGameObject = GameObject.FindGameObjectWithTag(sceneLoaderTag);
            return sceneLoaderGameObject != null ? sceneLoaderGameObject.GetComponent<SceneLoaderBase<T>>() : null;
        }
        
        #region UnityMethods
        protected override void Awake()
        {
            base.Awake();
            _activeSceneLoader = this;
        }
        #endregion
        
        #region ProtectedAbstractMethods
        protected virtual bool IsNewGameSceneType(TSceneType sceneType) => true;
        protected virtual bool IsGameOverSceneType(TSceneType sceneType)  => true;
        protected virtual bool ShouldSaveSessionOnGameOver() => false;
        #endregion
        
        #region PublicMethods
        public static void QueueScene(TSceneType sceneType, SceneQueueData sceneQueueData)
        {
            if (IsCurrentlyLoading) { return; }
            
            if (_activeSceneLoader == null) { _activeSceneLoader = FindSceneLoader<TSceneType>(); }
            if (_activeSceneLoader == null) { return; }
            _activeSceneLoader.StartLoadScene(sceneType, sceneQueueData);
        }
        #endregion
        
        #region PrivateMethods
        private void StartLoadScene(TSceneType sceneType, SceneQueueData sceneQueueData)
        {
            Zone zone = ReconcileZone(sceneType);
            if (zone == null) { return; }

            if (sceneQueueData.useFader && sceneLoadFadeProvider != null)
            {
                // Standard Behaviour:  Load to GameOver scene while skipping session saving
                // From GameOver scene only player will be present, and we can save session to carry over player exp, etc.
                bool saveSession = true;
                if (IsGameOverSceneType(sceneType)) { saveSession = ShouldSaveSessionOnGameOver(); }
                sceneLoadFadeProvider.Invoke(zone, saveSession);
            }
            else
            {
                StartCoroutine(LoadScene(zone, sceneQueueData.delayTime, sceneQueueData.sceneLoadedCallback));
            }
        }

        private Zone ReconcileZone(TSceneType sceneType)
        {
            Zone zone = null;
            if (IsNewGameSceneType(sceneType))
            {
                zone = DemoZoneOverrideProvider?.Invoke();
                if (zone != null) { return zone; }
            }

            foreach ((TSceneType sceneType, Zone zone) zoneSceneTypePair in zoneSceneTypeLookup)
            {
                if (EqualityComparer<TSceneType>.Default.Equals(sceneType, zoneSceneTypePair.sceneType)) { return zoneSceneTypePair.zone; }
            }
            return zone;
        }
        #endregion
    }
}

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
        // Note:  Internal fields/methods for test visibility
        
        // State
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
        internal static SceneLoaderBase activeSceneLoaderBase;
        internal static Zone lastZone;
        internal static Zone currentZone;
        protected internal static bool isCurrentlyLoading = false;
        
        // External Hooks
        public static Func<Zone> demoZoneOverrideProvider;
        
        // Events
        public static event Action<Zone> leavingZone;
        public static event Action<Zone> zoneUpdated;
        
        #region UnityMethods
        protected virtual void Awake()
        {
            // SceneLoader is included in PersistentObjects and thus a singleton by standard implementation
            // So:  Establish sceneLoader in static state for public method calls
            // Note:  Typed version is instantiated below
            activeSceneLoaderBase = this;
        }
        #endregion
        
        #region GettersSetters
        public static Zone GetCurrentZone()
        {
            if (currentZone == null) { currentZone = Zone.GetFromSceneReference(SceneManager.GetActiveScene().name); }
            return currentZone;
        }
        
        internal static void SetLastZone()
        {
            lastZone = currentZone;
            leavingZone?.Invoke(lastZone);
        }

        internal static void SetCurrentZone(Zone zone)
        {
            currentZone = zone;
            zoneUpdated?.Invoke(currentZone);
        }
        
        public static void SetCurrentZoneToCurrentScene()
        {
            SetCurrentZone(Zone.GetFromSceneReference(SceneManager.GetActiveScene().name));
        }
        #endregion
        
        #region PublicMethods
        public static IEnumerator LoadNewSceneAsync(Zone zone)
        {
            if (isCurrentlyLoading) { yield break; }
            
            isCurrentlyLoading = true;
            SetLastZone();
            yield return SceneManager.LoadSceneAsync(zone.GetSceneReference().SceneName);
            SetCurrentZone(zone);
            isCurrentlyLoading = false;
        }
        
        public static void QueueDelayedDestroy(IList<GameObject> entries)
        {
            if (activeSceneLoaderBase == null) { activeSceneLoaderBase = FindSceneLoader(); }
            if (activeSceneLoaderBase == null) { return; }
            activeSceneLoaderBase.StartDelayedDestroy(entries);
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

            isCurrentlyLoading = true;
            yield return new WaitForSeconds(delayTime);
            yield return SceneManager.LoadSceneAsync(zone.GetSceneReference().SceneName);
            SetCurrentZone(zone);
            isCurrentlyLoading = false;
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
        // Note:  Internal fields/methods for test visibility
        
        // Tunables
        [Header("Core Scene Listing")]
        [EnumKeyedCollection][SerializeField] internal ZoneSceneTypeLookup<TSceneType> zoneSceneTypeLookup;
        
        // Static
        internal static SceneLoaderBase<TSceneType> activeSceneLoader;
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
            activeSceneLoader = this;
        }
        #endregion
        
        #region ProtectedAbstractMethods
        protected virtual bool IsNewGameSceneType(TSceneType sceneType) => false;
        protected virtual bool IsGameOverSceneType(TSceneType sceneType)  => false;
        protected virtual bool ShouldSaveSessionOnGameOver() => false;
        #endregion
        
        #region PublicMethods
        public static void QueueScene(TSceneType sceneType, SceneQueueData sceneQueueData)
        {
            if (isCurrentlyLoading) { return; }
            
            if (activeSceneLoader == null) { activeSceneLoader = FindSceneLoader<TSceneType>(); }
            if (activeSceneLoader == null) { return; }
            activeSceneLoader.StartLoadScene(sceneType, sceneQueueData);
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
                zone = demoZoneOverrideProvider?.Invoke();
                if (zone != null) { return zone; }
            }

            foreach ((TSceneType candidateType, Zone candidateZone) in zoneSceneTypeLookup)
            {
                if (EqualityComparer<TSceneType>.Default.Equals(sceneType, candidateType)) { return candidateZone; }
            }
            return zone;
        }
        #endregion
    }
}

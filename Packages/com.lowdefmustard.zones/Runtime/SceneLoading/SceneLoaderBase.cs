using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using LowDefMustard.Core;
using LowDefMustard.Utils;

namespace LowDefMustard.Zones
{
    public abstract partial class SceneLoaderBase : MonoBehaviour
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
        [AutoStaticsCleanup] internal static SceneLoaderBase activeSceneLoaderBase;
        [AutoStaticsCleanup] internal static Zone lastZone;
        [AutoStaticsCleanup] internal static Zone currentZone;
        [AutoStaticsCleanup] protected internal static bool isCurrentlyLoading = false;
        
        // External Hooks
        [AutoStaticsCleanup] public static Func<Zone> demoZoneOverrideProvider;
        
        // Events
        [AutoStaticsCleanup] public static event Action<Zone> leavingZone;
        [AutoStaticsCleanup] public static event Action<Zone> zoneUpdated;
        
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
            if (currentZone == null) { currentZone = Zone.GetFromSceneName(SceneManager.GetActiveScene().name); }
            return currentZone;
        }
        
        internal static void SetCurrentZone(Zone zone)
        {
            currentZone = zone;
            zoneUpdated?.Invoke(currentZone);
        }

        internal static void SetLastZone()
        {
            lastZone = currentZone;
            leavingZone?.Invoke(lastZone);
        }
        
        public static void SetCurrentZoneToCurrentScene()
        {
            SetCurrentZone(Zone.GetFromSceneName(SceneManager.GetActiveScene().name));
        }
        
        public static bool TryGetCurrentSceneGuid(out string sceneGuid)
        {
            Zone zone = GetCurrentZone();
            if (zone != null) { zone.GetSceneReference().TryGetSceneGuid(out sceneGuid); }
            else
            {
#if UNITY_EDITOR
                // Editor-only fallback for zone-less scenes (e.g. debug scenes)
                sceneGuid = UnityEditor.AssetDatabase.AssetPathToGUID(SceneManager.GetActiveScene().path);
#else
                sceneGuid = null;
#endif
            }
            if (!string.IsNullOrWhiteSpace(sceneGuid)) { return true; }
            
            LDMLog.Warning(LDMLogCategory.Zones, "Couldn't find the current scene's GUID.");
            sceneGuid = null;
            return false;
        }
        
        private static bool TryGetZoneSceneGuid(Zone zone, out string sceneGuid)
        {
            if (zone.GetSceneReference().TryGetSceneGuid(out sceneGuid)) { return true; }
            LDMLog.Warning(LDMLogCategory.Zones, $"Zone '{zone.name}' has no scene GUID, so its scene can't load. Staying in the current scene. Re-link the zone's scene in the editor.", zone);
            return false;
        }
        #endregion
        
        #region PublicMethods
        public static IEnumerator LoadNewSceneAsync(Zone zone)
        {
            if (zone == null) { LDMLog.Error(LDMLogCategory.Zones, "Cannot load a scene for a null zone. Ignoring the request."); yield break; }
            if (isCurrentlyLoading) { LDMLog.Info(LDMLogCategory.Zones, $"Already loading a scene. Ignoring the request to load zone '{zone.name}'."); yield break; }
            if (!TryGetZoneSceneGuid(zone, out string sceneGuid)) { yield break; }
            
            isCurrentlyLoading = true;
            SetLastZone(); // Before the load ~ leavingZone listeners need the old scene (e.g. MapCamera)
            bool isLoaded = false;
            yield return AddressableSceneLoader.LoadSceneAsync(sceneGuid, result => isLoaded = result);
            isCurrentlyLoading = false;
            TryCompleteZoneLoad(zone, isLoaded);
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
        protected static IEnumerator LoadScene(Zone zone, float delayTime, Action sceneLoadedCallback) // Note:  skips sceneLoadedCallback on fail
        {
            if (zone == null) { yield break; }
            if (!TryGetZoneSceneGuid(zone, out string sceneGuid)) { yield break; }

            isCurrentlyLoading = true;
            yield return new WaitForSeconds(delayTime);
            var isLoaded = false;
            yield return AddressableSceneLoader.LoadSceneAsync(sceneGuid, result => isLoaded = result);
            isCurrentlyLoading = false;
            if (!TryCompleteZoneLoad(zone, isLoaded)) { yield break; }
            sceneLoadedCallback?.Invoke();
        }

        private static bool TryCompleteZoneLoad(Zone zone, bool isLoaded)
        {
            if (!isLoaded)
            {
                LDMLog.Warning(LDMLogCategory.Zones, $"Zone '{zone.name}' didn't load - staying in the current scene.", zone);
                return false;
            }
            SetCurrentZone(zone);
            LDMLog.Important(LDMLogCategory.Zones, $"Loaded zone '{zone.name}'.");
            return true;
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
    
    public abstract partial class SceneLoaderBase<TSceneType> : SceneLoaderBase where TSceneType : struct, Enum
    {
        // Note:  Internal fields/methods for test visibility
        
        // Tunables
        [Header("Core Scene Listing")]
        [EnumKeyedCollection][SerializeField] internal ZoneSceneTypeLookup<TSceneType> zoneSceneTypeLookup;
        
        // Static
        [AutoStaticsCleanup] internal static SceneLoaderBase<TSceneType> activeSceneLoader;
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
            if (isCurrentlyLoading) { LDMLog.Info(LDMLogCategory.Zones, $"Already loading a scene. Ignoring the request to queue scene type '{sceneType}'."); return; }
            
            if (activeSceneLoader == null) { activeSceneLoader = FindSceneLoader<TSceneType>(); }
            if (activeSceneLoader == null) { LDMLog.Error(LDMLogCategory.Zones, $"No scene loader found (tag: '{sceneLoaderTag}'). Cannot queue scene type '{sceneType}'."); return; }
            activeSceneLoader.StartLoadScene(sceneType, sceneQueueData);
        }
        #endregion
        
        #region PrivateMethods
        private void StartLoadScene(TSceneType sceneType, SceneQueueData sceneQueueData)
        {
            Zone zone = ReconcileZone(sceneType);
            if (zone == null) { LDMLog.Error(LDMLogCategory.Zones, $"No zone mapped to scene type '{sceneType}'. Add it to the scene loader's zone scene type lookup.", this); return; }

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

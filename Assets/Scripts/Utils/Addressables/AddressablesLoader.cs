using System.Linq;
using UnityEngine;
using LowDefMustard.Utils;
using LowDefMustard.Zones;
using Frankie.Combat;
using Frankie.Inventory;
using Frankie.Quests;
using Frankie.Stats;

namespace Frankie.Utils.Addressables
{
    public class AddressablesLoader : MonoBehaviour
    {
        // State
        private bool isSingleton = false;

        #region UnityMethods
        private void Awake()
        {
            VerifySingleton();
            StartAllCacheBuilds(); // Async ~ never block here: this Awake can run inside a scene load (see AddressablesLookupCache)
        }

        private void OnDestroy()
        {
            if (!isSingleton) { return; }
            
            foreach (IAddressablesCacheLifecycle cache in GetCaches()) { cache.Release(); }
        }
        #endregion

        #region PublicMethods
        public static void StartAllCacheBuilds()
        {
            foreach (IAddressablesCacheLifecycle cache in GetCaches()) { cache.StartBuild(); }
        }

        public static bool AreAllCachesReady() => GetCaches().All(cache => cache.IsReady());
        
        internal static IAddressablesCacheLifecycle[] GetCaches() => new[]
        {
            // Every IAddressablesCache owner's cache ~ AddressablesLoaderTests checks none missing
            // Note:  Built per call, the owners' caches are replaced on each Play mode entry ([AutoStaticsCleanup])
            
            BattleAction.addressablesCache,
            CharacterProperties.addressablesCache,
            InventoryItem.addressablesCache,
            Quest.addressablesCache,
            Skill.addressablesCache,
            Zone.addressablesCache
        };
        #endregion

        #region PrivateMethods
        private void VerifySingleton()
        {
            // Singleton through standard approach: do not use persistent object spawner for addressables loader (want assets loaded in Awake)
            int numberOfAddressablesLoaders = FindObjectsByType<AddressablesLoader>().Length;
            if (numberOfAddressablesLoaders > 1)
            {
                isSingleton = false;
                gameObject.SetActive(false);
                Destroy(gameObject);
            }
            else
            {
                isSingleton = true;
                gameObject.transform.parent = null;
                DontDestroyOnLoad(gameObject);
            }
        }
        #endregion
    }
}

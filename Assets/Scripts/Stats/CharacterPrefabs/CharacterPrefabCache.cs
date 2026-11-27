using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using Unity.Scripting.LifecycleManagement;
using LowDefMustard.Core;
using LowDefMustard.Utils;
using UnityEngine.AddressableAssets;

namespace Frankie.Stats
{
    public static partial class CharacterPrefabCache
    {
        // Note:
        //  - Preloads during transitions via CharacterPrefabScenePreload
        //  - Users can start their own via StartPreload ~ lookups stay synchronous for the existing spawn call sites

        // State
        [AutoStaticsCleanup] private static AddressablesKeyedAssetCache<CharacterProperties, CharacterPrefabType, GameObject> _cache = new(LDMLogCategory.Stats, GetOwnerKey, GetReference);

        #region PublicMethods
        public static List<AsyncOperationHandle<GameObject>> StartPreload(IEnumerable<CharacterProperties> characters) => _cache.StartPreload(characters); // Non-blocking ~ safe during a scene load (e.g. from Awake / Start)
        public static IEnumerator Preload(IEnumerable<CharacterProperties> characters) => _cache.Preload(characters); // Blocking in use (Coroutine) ~ each yield is an Async handle
        public static bool IsReady(IEnumerable<CharacterProperties> characters) => _cache.IsReady(characters);
        public static bool TryGetPrefab(CharacterProperties characterProperties, CharacterPrefabType prefabType, out GameObject prefab) => _cache.TryGet(characterProperties, prefabType, out prefab);
        public static void ReleaseAllExcept(IEnumerable<CharacterProperties> keptCharacters) => _cache.ReleaseAllExcept(keptCharacters);
        public static void ReleaseAll() => _cache.ReleaseAll();
        #endregion
        
        #region PrivateMethods
        private static string GetOwnerKey(CharacterProperties characterProperties) => characterProperties.GetCharacterID();
        private static AssetReference GetReference(CharacterProperties characterProperties, CharacterPrefabType prefabType) => characterProperties.GetCharacterPrefabReference(prefabType);
        #endregion
    }
}

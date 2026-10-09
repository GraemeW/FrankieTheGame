using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace LowDefMustard.Utils
{
    [CreateAssetMenu(fileName = "New Game Properties", menuName = "GameProperties/New Game Properties", order = 1)]
    public sealed class GameProperties : ScriptableObject
    {
        // Note:  Read through the static accessors ~ they fall back to defaults when no instance is hooked up

        // Tunables
        [SerializeField][Tooltip("Pixels per world unit for the game's art")][Min(1f)] private float artPixelsPerUnit = _defaultArtPixelsPerUnit;
        
        // Const Fallbacks
        private const float _defaultArtPixelsPerUnit = 100f;
        
        // Static State (set after first read)
        [NoAutoStaticsCleanup] private static float? _cachedArtPixelsPerUnit;  

        
        #region UnityMethods
        private void OnValidate() => ClearCache();
        #endregion

        #region Getters
        public static float GetArtPixelsPerUnit(GameProperties gameProperties)
        {
            if (_cachedArtPixelsPerUnit.HasValue) { return _cachedArtPixelsPerUnit.Value; }
            if (gameProperties == null) { return _defaultArtPixelsPerUnit; }
            
            _cachedArtPixelsPerUnit = gameProperties.artPixelsPerUnit;
            return _cachedArtPixelsPerUnit.Value;
        }

        // Note:  Internal for test visibility
        internal static void ClearCache() => _cachedArtPixelsPerUnit = null;
        #endregion
    }
}

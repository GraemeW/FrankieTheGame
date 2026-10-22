using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace LowDefMustard.Rendering.Editor
{
    public static class BattleEffectPreviewSizes
    {
        // Notes:
        //  - Register a project's own (e.g. its UI slide sizes) from an [InitializeOnLoad] class
        //  - The default square is listed only while nothing is registered
        //  - Listed in registration order (the window saves the selected index) ~ re-registering a name replaces its size in place

        // Const Tunables
        private const string _defaultName = "Square (320x320)";
        private static readonly Vector2 _defaultSize = new(320f, 320f);

        // State
        [NoAutoStaticsCleanup] private static readonly List<BattleEffectPreviewSize> _presets = new();
        [NoAutoStaticsCleanup] private static readonly List<BattleEffectPreviewSize> _defaultPresets = new() { new BattleEffectPreviewSize(_defaultName, _defaultSize) };

        public static void Register(string name, Vector2 size)
        {
            var preset = new BattleEffectPreviewSize(name, size);
            int index = _presets.IndexOf(preset);
            if (index >= 0) { _presets[index] = preset; }
            else { _presets.Add(preset); }
        }

        public static bool Unregister(string name) => _presets.Remove(new BattleEffectPreviewSize(name, Vector2.zero));

        public static IReadOnlyList<BattleEffectPreviewSize> GetPresets() => _presets.Count > 0 ? _presets : _defaultPresets;
    }
}

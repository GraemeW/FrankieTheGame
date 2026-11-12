#if UNITY_EDITOR
using System.Collections.Generic;

namespace LowDefMustard.GameStateModifiers
{
    public sealed class GameStateModifierHandlerLookup
    {
        // Notes:
        //  - req'd for bulk tools to clean modifier entries without opening a scene per entry
        //  - only indexed zones are trusted ~ entries for any other zone fall back to the per-entry scene check

        // State
        private readonly HashSet<string> zoneNames = new();
        private readonly Dictionary<ZoneHandlerKey, HashSet<string>> modifierGUIDsByHandler = new();

        #region Getters
        public int zoneCount => zoneNames.Count;
        public int handlerCount => modifierGUIDsByHandler.Count;
        public bool HasZone(string zoneName) => zoneNames.Contains(zoneName ?? "");
        public bool HasHandler(string zoneName, string handlerGUID) => modifierGUIDsByHandler.ContainsKey(new ZoneHandlerKey(zoneName, handlerGUID));
        public bool IsLinked(string zoneName, string handlerGUID, string modifierGUID) => modifierGUIDsByHandler.TryGetValue(new ZoneHandlerKey(zoneName, handlerGUID), out HashSet<string> linkedModifierGUIDs) && linkedModifierGUIDs.Contains(modifierGUID);
        #endregion

        #region PublicMethods
        public void AddZone(string zoneName) => zoneNames.Add(zoneName ?? "");

        public void AddHandler(string zoneName, string handlerGUID, IEnumerable<string> modifierGUIDs)
        {
            if (string.IsNullOrWhiteSpace(handlerGUID)) { return; }
            AddZone(zoneName);

            var key = new ZoneHandlerKey(zoneName, handlerGUID);
            if (!modifierGUIDsByHandler.TryGetValue(key, out HashSet<string> linkedModifierGUIDs))
            {
                linkedModifierGUIDs = new HashSet<string>();
                modifierGUIDsByHandler[key] = linkedModifierGUIDs;
            }
            if (modifierGUIDs == null) { return; }
            foreach (string modifierGUID in modifierGUIDs) { if (!string.IsNullOrWhiteSpace(modifierGUID)) { linkedModifierGUIDs.Add(modifierGUID); } }
        }
        #endregion
    }
}
#endif

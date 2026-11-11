#if UNITY_EDITOR
using System;

namespace LowDefMustard.GameStateModifiers
{
    internal readonly struct ZoneHandlerKey : IEquatable<ZoneHandlerKey>
    {
        // Note: zoneName / handlerGUID keyed since handler GUIDs aren't guaranteed unique across zones (null zone name is treated as "")

        // Properties
        public readonly string zoneName;
        public readonly string handlerGUID;

        public ZoneHandlerKey(string zoneName, string handlerGUID)
        {
            this.zoneName = zoneName ?? "";
            this.handlerGUID = handlerGUID;
        }
        
        public bool Equals(ZoneHandlerKey other) => zoneName == other.zoneName && handlerGUID == other.handlerGUID;
        public override bool Equals(object obj) => obj is ZoneHandlerKey other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(zoneName, handlerGUID);
    }
}
#endif

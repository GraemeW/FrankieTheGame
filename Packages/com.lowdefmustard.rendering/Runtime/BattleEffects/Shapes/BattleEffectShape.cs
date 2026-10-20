using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Scripting.LifecycleManagement;

namespace LowDefMustard.Rendering
{
    [Serializable]
    public abstract class BattleEffectShape
    {
        // Notes:
        //  - Subclasses hold only their shape's own settings (position, scale, rotation, style, colour + timing live on BattleEffectItem)
        //  - Adding a shape:  a sealed subclass w/ a unique [BattleEffectShapeId] + an HLSL adapter named <ClassName>Distance in BattleEffectShapes.hlsl
        //      - The HLSL dispatch is generated from the subclasses (see BattleEffectDispatchGenerator)
        //      - Parameters are read by the adapter in the order they're written here
        //  - Setting names carry their unit as a suffix:
        //      - SU:  shape units (lengths in the shape's own space, where 1 = the item's scale)
        //      - Ratio:  one length over another (tooltip says which)
        //      - EU:  effect units (0-1 spans the effect's shorter side when aspect fit, or the whole element when stretched)
        //      - Fraction:  a 0-1 amount (signed where noted, e.g. a size change;  phases are fractions of a cycle)
        //      - Seconds / Degrees / Pixels (screen):  as named
        //      - Rate:  per second
        //      - PerEU:  per effect unit
        //      - none:  counts, IDs (e.g. a seed), enums, colours + toggles

        // Static
        [NoAutoStaticsCleanup] private static readonly Dictionary<Type, int> _shapeIds = new();

        // Abstract
        public abstract void GetParameters(BattleEffectFloatRange[] parameters);

        // Public
        public bool TryGetShapeId(out int shapeId)
        {
            Type shapeType = GetType();
            if (!_shapeIds.TryGetValue(shapeType, out shapeId))
            {
                shapeId = shapeType.GetCustomAttribute<BattleEffectShapeIdAttribute>()?.id ?? -1;
                _shapeIds[shapeType] = shapeId;
            }
            return shapeId >= 0;
        }
    }
}

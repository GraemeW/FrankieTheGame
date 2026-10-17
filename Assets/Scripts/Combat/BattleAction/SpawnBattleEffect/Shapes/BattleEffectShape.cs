using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Scripting.LifecycleManagement;

namespace Frankie.Combat
{
    [Serializable]
    public abstract class BattleEffectShape
    {
        // Notes:
        //  - Subclasses hold only their shape's own settings (position, scale, rotation, style, colour + timing live on BattleEffectItem)
        //  - Adding a shape:  a sealed subclass w/ a unique [BattleEffectShapeId] + an HLSL adapter named <ClassName>Distance in BattleEffectShapes.hlsl
        //      - The HLSL dispatch is generated from the subclasses (see BattleEffectDispatchGenerator)
        //      - Parameters are read by the adapter in the order they're written here

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

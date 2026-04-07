using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Core.GameStateModifiers
{
    public abstract class GameStateModifier : ScriptableObject
    {
        public static string GetGameStateModifierHandlerDataRef() => nameof(gameStateModifierHandlerData);
        public List<ZoneToGameObjectLinkData> gameStateModifierHandlerData = new();
    }
}

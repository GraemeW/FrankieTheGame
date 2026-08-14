using System;
using UnityEditor;
using LowDefMustard.GameStateModifiers;
using LowDefMustard.GameStateModifiers.Editor;
using LowDefMustard.Zones;
using LowDefMustard.Zones.Editor;

namespace Frankie.Core.GameStateModifiers
{
    [InitializeOnLoad]
    public static class GameStateModifierRegistration
    {
        static GameStateModifierRegistration()
        {
            GameStateModifier.ScenePathProvider = GetScenePath;
            GameStateModifierEditor.OpenSceneAndActProvider = OpenSceneAndAct;
        }
        
        private static bool GetScenePath(string zoneName, out string scenePath)
        {
            Zone.BuildCacheIfEmpty();
            
            scenePath = string.Empty;
            Zone zone = Zone.GetFromName(zoneName);
            if (zone == null) { return false; }
            
            scenePath = zone.GetSceneReference().GetScenePath();
            return !string.IsNullOrWhiteSpace(scenePath);
        }

        private static void OpenSceneAndAct(string zoneName, Action action)
        {
            ZoneTools.OpenSceneAndAct(zoneName, action);
        }
    }
}

#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace LowDefMustard.Zones.Editor
{
    public static class ZoneTools
    {
        public static void OpenSceneAndAct(string zoneName, Action onSceneOpen, bool suppressDialogs = false)
        {
            Zone zone = Zone.GetFromName(zoneName);
            OpenSceneAndAct(zone, onSceneOpen, suppressDialogs);
        }

        public static void OpenSceneAndAct(Zone zone, Action onSceneOpen, bool suppressDialogs = false)
        {
            if (zone == null) { return; }
            
            if (!suppressDialogs && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) { return; }
            bool didZoneOpen = OpenZone(zone, suppressDialogs);
            if (!didZoneOpen) { return; }
            onSceneOpen?.Invoke();
        }

        private static bool OpenZone(Zone zone, bool suppressDialogs)
        {
            if (zone == null) { return false; }

            string scenePath = zone.GetSceneReference().GetScenePath();
            if (string.IsNullOrEmpty(scenePath))
            {
                if (!suppressDialogs) { EditorUtility.DisplayDialog("Scene Not Found", $"Could not locate {zone.name} in the project.", "OK"); }
                return false;
            }
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            Debug.Log($"{zone.name} opened successfully.");
            return true;
        }
    }
}
#endif

#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Frankie.ZoneManagement.Editor
{
    public static class ZoneTools
    {
        public static void OpenSceneAndAct(string zoneName, Action onSceneOpen)
        {
            Zone zone = Zone.GetFromName(zoneName);
            OpenSceneAndAct(zone, onSceneOpen);
        }

        public static void OpenSceneAndAct(Zone zone, Action onSceneOpen)
        {
            if (zone == null) { return; }
            
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) { return; }
            bool didZoneOpen = OpenZone(zone);
            if (!didZoneOpen) { return; }
            onSceneOpen?.Invoke();
        }

        private static bool OpenZone(Zone zone)
        {
            if (zone == null) { return false; }

            string scenePath = zone.GetSceneReference().GetScenePath();
            if (string.IsNullOrEmpty(scenePath))
            {
                EditorUtility.DisplayDialog("Scene Not Found", $"Could not locate {zone.name} in the project.", "OK");
                return false;
            }
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            Debug.Log($"{zone.name} opened successfully.");
            return true;
        }
    }
}
#endif

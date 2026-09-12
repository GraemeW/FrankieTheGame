using System;
using UnityEngine;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace LowDefMustard.Localization
{
    public static class DefaultKeyGenerator
    {
        // State
        private static readonly System.Random _random = new();
        
        public static string GenerateKindaUniqueKey(Object targetObject, string propertyName, Type declaringType = null,  bool useParentNameStem = true)
        {
            string semiUniqueShortKey = _random.Next().ToString("x");
            
            // ReSharper disable once RedundantAssignment - false warning due to pragma 
            string kindaUniqueKey = semiUniqueShortKey; // Default fallback if exercised outside of Editor (unexpected)
            
#if UNITY_EDITOR
            string componentStem = declaringType != null ? $"{declaringType.Name}." : $"{targetObject.GetType().Name}.";
            string targetStem = "";
            string nameStem = targetObject.name;

            if (targetObject is GameObject castGameObject && castGameObject.TryGetComponent(out MonoBehaviour swapToMono)) { targetObject = swapToMono; }
            if (useParentNameStem && targetObject is MonoBehaviour castMonoBehaviour && castMonoBehaviour.transform.parent != null)
            {
                string parentName = castMonoBehaviour.transform.parent.name;
                if (!parentName.Contains("Canvas")) // Skip UI-most parent name
                {
                    nameStem = castMonoBehaviour.transform.parent.name;
                }
            }
            
            if (targetObject != null)
            {
                switch (targetObject)
                {
                    case ScriptableObject:
                        targetStem += $"SO.{nameStem}.";
                        break;
                    case MonoBehaviour targetMonoBehaviour when PrefabUtility.IsPartOfPrefabAsset(targetMonoBehaviour):
                        targetStem += $"Prefab.{nameStem}.";
                        break;
                    case MonoBehaviour targetMonoBehaviour:
                        targetStem += BuildGameObjectMonoSuffix(nameStem, targetMonoBehaviour.gameObject);
                        break;
                    case GameObject fallbackGameObject:
                        // Edge Case: unexpected path since, if it's localizable, it should have some sort of MonoBehaviour
                        targetStem += BuildGameObjectMonoSuffix(nameStem, fallbackGameObject);
                        break;
                }
            }

            string propertyNameStem = $"{(propertyName ?? "").Replace("localized", "")}.";
            kindaUniqueKey = $"{componentStem}{targetStem}{propertyNameStem}{semiUniqueShortKey}";
#endif
            
            return kindaUniqueKey;
        }

        private static string BuildGameObjectMonoSuffix(string nameStem, GameObject targetGameObject)
        {
            string suffix = string.Empty;
            
            // Secondary check on Prefab - within prefab stage utility, game object / mono will fall through
            PrefabStage prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null && prefabStage.IsPartOfPrefabContents(targetGameObject))
            {
                suffix += $"Prefab.{nameStem}.";
            }
                        
            suffix += "GO.";
            if (targetGameObject != null) { suffix += $"{targetGameObject.scene.name}.{nameStem}."; }
            else { suffix += $"{nameStem}."; }

            return suffix;
        }
    }
}

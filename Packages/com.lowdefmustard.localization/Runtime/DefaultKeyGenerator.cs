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
            
            if (targetObject is GameObject castGameObject) { targetObject = castGameObject.GetComponent<MonoBehaviour>(); }
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
                    {
                        GameObject targetGameObject =  targetMonoBehaviour.gameObject;
                        PrefabStage prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
                        if (prefabStage != null && prefabStage.IsPartOfPrefabContents(targetGameObject))
                        {
                            targetStem += $"Prefab.{nameStem}.";
                            break;
                        }
                        
                        targetStem += "GO.";
                        if (targetGameObject != null) { targetStem += $"{targetGameObject.scene.name}.{nameStem}."; }
                        else { targetStem += $"{nameStem}."; }
                        break;
                    }
                }
            }

            string propertyNameStem = $"{(propertyName ?? "").Replace("localized", "")}.";
            kindaUniqueKey = $"{componentStem}{targetStem}{propertyNameStem}{semiUniqueShortKey}";
#endif
            
            return kindaUniqueKey;
        }
    }
}

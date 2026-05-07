using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using Object = UnityEngine.Object;

namespace Frankie.Utils.Localization
{
    public interface ILocalizable
    {
        // ---------------------CRITICAL NOTES ON CONFIGURATION---------------------
        // 1 - For Scriptable Objects,
        //     A. ILocalizable should be placed on the parent-most object
        //     LocalizationDeletionHandler.OnWillDeleteAsset() does not trigger for scriptable objects that are childed to other scriptable objects!
        //     The parent-most object must take gather localization entries from all children for GetLocalizationEntries()
        //     B. In order to auto-set and auto-rename localized entries:
        //       - override iCachedName to link to a serialized cachedName backing field
        //       - create a custom inspector editor that calls TryLocalizedStandardEntries() during the editor's OnEnable()
        //       - pass all relevant propertyName-localizedString pairs to this method
        // 2 - For MonoBehaviours, the following must be manually configured:
        //     A. Add [ExecuteInEditMode] attribute to the class
        //     B. Include `ILocalizable.TriggerOnDestroy(this)` to the OnDestroy() method
        // Note that in the case of MonoBehaviours:
        //     - cleanup for prefabs/prefab variants is handled by LocalizationDeletionHandler.OnWillDeleteAsset()
        //     - cleanup for instanced objects in scenes is handled by OnDestroy()
        // ---------------------CRITICAL NOTES ON CONFIGURATION---------------------
        
        #region PublicMethodsProperties
        public string iCachedName { get => null; set => _ = value; } // Must include explicit backing field in implementation
        public LocalizationTableType localizationTableType { get; }
        public List<TableEntryReference> GetLocalizationEntries();
        public static event Action<LocalizationTableType, Object, ILocalizable> onBeforeDestroyedInEditor;

        public static void TriggerOnDestroy(ILocalizable localizable)
        {
#if UNITY_EDITOR
            if (localizable is not MonoBehaviour monoBehaviour) { return; }
            if (!FrankieNonEditorEditorTools.IsStandardEditorState(monoBehaviour.gameObject)) { return; }
            onBeforeDestroyedInEditor?.Invoke(localizable.localizationTableType, monoBehaviour.gameObject, localizable);
#endif
        }

        public void TryLocalizeStandardEntries(Object targetObject, List<(string propertyName, LocalizedString localizedString, bool setToName)> standardEntries, Action onRename = null)
        {
#if UNITY_EDITOR
            if (targetObject == null || string.IsNullOrWhiteSpace(targetObject.name)) { return; }
            if (string.IsNullOrWhiteSpace(iCachedName)) { iCachedName = targetObject.name; }
            
            ReconcileCachedName(targetObject, standardEntries, onRename);
            
            bool wasObjectDirtied = false;
            string id = targetObject.name;
            string typeName = targetObject.GetType().Name;
            
            foreach ((string propertyName, LocalizedString localizedString, bool setToName) standardEntry in standardEntries)
            {
                string key = GetStandardLocalizationKey(id, typeName, standardEntry.propertyName);
                Debug.Log($"id is {id}, key is {key}");
                if (standardEntry.setToName)
                {
                    wasObjectDirtied = wasObjectDirtied || LocalizationTool.TryLocalizeEntry(localizationTableType, standardEntry.localizedString, key, targetObject.name);
                }
                else
                {
                    wasObjectDirtied = wasObjectDirtied || LocalizationTool.InitializeLocalEntry(localizationTableType, standardEntry.localizedString, key);
                }
            }

            if (!wasObjectDirtied) { return; }
            EditorUtility.SetDirty(targetObject);
            AssetDatabase.SaveAssetIfDirty(targetObject);
#endif
        }

        public void ReconcileCachedName(Object targetObject, List<(string propertyName, LocalizedString _, bool __)> standardEntries, Action onRename)
        {
#if UNITY_EDITOR
            // iCachedName is null for default configuration, skips unless configured explicitly
            if (iCachedName == null || targetObject == null || string.IsNullOrWhiteSpace(targetObject.name)) { return; }
            if (targetObject.name == iCachedName) { return;}
            
            foreach ((string propertyName, LocalizedString _, bool __) standardEntry in standardEntries)
            {
                string typeName = targetObject.GetType().Name;
                TableEntryReference oldKey = GetStandardLocalizationKey(iCachedName, typeName, standardEntry.propertyName);
                string newKey = GetStandardLocalizationKey(targetObject.name, typeName, standardEntry.propertyName);
                LocalizationTool.MakeOrRenameKey(localizationTableType, oldKey, newKey);
            }
            
            iCachedName = targetObject.name;
            onRename?.Invoke();
            
            EditorUtility.SetDirty(targetObject);
            AssetDatabase.SaveAssetIfDirty(targetObject);
#endif
        }

        public static string GetStandardLocalizationKey(string id, string typeName, string propertyName)
        {
            string sanitizedPropertyName = (propertyName ?? "").Replace("localized", "");
            return sanitizedPropertyName.Contains("Name") ? $"{typeName}.{id}" : $"{typeName}.{id}.{sanitizedPropertyName}";
        }
        #endregion
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
#endif
using LowDefMustard.Utils;

namespace LowDefMustard.Localization
{
    // T is the caller's table-type enum (e.g. LocalizationTableType)
    // Project implementation should define its own empty alias interface (e.g. ILocalizable : ILocalizableBase<LocalizationTableType>)
    
    public interface ILocalizableBase<TTableType> : ILocalizableCore where TTableType : struct, Enum
    {
        // ---------------------CRITICAL NOTES ON CONFIGURATION---------------------
        // 1 - For Scriptable Objects,
        //     A. ILocalizableBase<T> should be placed on the parent-most object
        //     LocalizationDeletionHandler.OnWillDeleteAsset() does not trigger for scriptable objects that are childed to other scriptable objects!
        //     The parent-most object must take gather localization entries from all children for GetLocalizationEntries()
        //     B. In order to auto-set and auto-rename localized entries:
        //       - override iCachedName to link to a serialized cachedName backing field
        //       - create a custom inspector editor that calls TryLocalizedStandardEntries() during the editor's OnEnable()
        //       - pass all relevant propertyName-localizedString pairs to this method
        // 2 - For MonoBehaviours, the following can be manually configured:
        //     A. Add [ExecuteInEditMode] attribute to the class
        //     B. Include `ILocalizableCore.TriggerOnDestroy(this)` to the OnDestroy() method
        //     , in order to allow the localization entries to automatically delete on game object deletion
        //         ** if this is not necessary (e.g. for fixed UI elements), don't do it
        // Note that in the case of MonoBehaviours:
        //     - cleanup for prefabs/prefab variants is handled by LocalizationDeletionHandler.OnWillDeleteAsset()
        //     - cleanup for instanced objects in scenes is handled by OnDestroy()
        // ---------------------CRITICAL NOTES ON CONFIGURATION---------------------
        
        public TTableType localizationTableType { get; }
        Enum ILocalizableCore.localizationTableTypeValue => localizationTableType;

        public void TryLocalizeStandardEntries(Object targetObject, List<(string propertyName, LocalizedString localizedString, bool setToName)> standardEntries, Action onRename = null)
        {
            ILocalizableCore.TryLocalizeStandardEntries(this, targetObject, standardEntries, onRename);
        }

        public void ReconcileCachedName(Object targetObject, List<(string propertyName, LocalizedString _, bool __)> standardEntries, Action onRename)
        {
            ILocalizableCore.ReconcileCachedName(this, targetObject, standardEntries, onRename);
        }
    }
    
    public interface ILocalizableCore
    {
        // Non-Generic root for editor-only infra operations (without the need to know T at compile time)
        // Implementers should generally implement ILocalizableBase<TTableType> instead of this directly
        // Implement this ONLY when there are no project-specific TTableType available (e.g. classes shared across packages)
        //  - in this case, implementers must use LocalizableClassTableTypRegistry to supply their table type via GetType()
        
        public string iCachedName { get => null; set => _ = value; } // Must include explicit backing field in implementation
        public Enum localizationTableTypeValue { get; }
        public List<TableEntryReference> GetLocalizationEntries();
        public static string GetStandardLocalizationKey(string id, string typeName, string propertyName)
        {
            string sanitizedPropertyName = (propertyName ?? "").Replace("localized", "");
            return sanitizedPropertyName.Contains("Name") ? $"{typeName}.{id}" : $"{typeName}.{id}.{sanitizedPropertyName}";
        }

#if UNITY_EDITOR
        public static event Action<Enum, Object, ILocalizableCore> onBeforeDestroyedInEditor;
#endif

        public static void TriggerOnDestroy(ILocalizableCore localizable)
        {
#if UNITY_EDITOR
            if (localizable is not MonoBehaviour monoBehaviour) { return; }
            if (!EditorStateCheck.IsStandardEditorState(monoBehaviour.gameObject)) { return; }
            onBeforeDestroyedInEditor?.Invoke(localizable.localizationTableTypeValue, monoBehaviour.gameObject, localizable);
#endif
        }
        
        // Bridge-base of ILocalizableBase<T>.TryLocalizeStandardEntries (callable by those implementing ILocalizableCore directly)
        public static void TryLocalizeStandardEntries(ILocalizableCore localizable, Object targetObject, List<(string propertyName, LocalizedString localizedString, bool setToName)> standardEntries, Action onRename = null)
        {
#if UNITY_EDITOR
            if (targetObject == null || string.IsNullOrWhiteSpace(targetObject.name)) { return; }

            Enum tableType = localizable.localizationTableTypeValue;
            if (tableType == null) { Debug.LogWarning($"{targetObject.name} ({targetObject.GetType().Name}) has no table type registered"); return; }
            if (!LocalizationToolBridgeRegistry.TryGetBridge(tableType.GetType(), out ILocalizationToolBridge localizationToolBridge)) { Debug.LogWarning($"No localization bridge registered for table type '{tableType}'"); return; }

            if (string.IsNullOrWhiteSpace(localizable.iCachedName)) { localizable.iCachedName = targetObject.name; }
            ReconcileCachedName(localizable, targetObject, standardEntries, onRename);

            bool wasObjectDirtied = false;
            string id = targetObject.name;
            string typeName = targetObject.GetType().Name;

            foreach ((string propertyName, LocalizedString localizedString, bool setToName) standardEntry in standardEntries)
            {
                string key = GetStandardLocalizationKey(id, typeName, standardEntry.propertyName);
                if (localizationToolBridge.HasEnglishEntry(tableType, key)) { continue; }

                wasObjectDirtied = standardEntry.setToName
                    ? localizationToolBridge.TryLocalizeEntry(tableType, standardEntry.localizedString, key, targetObject.name) || wasObjectDirtied
                    : localizationToolBridge.InitializeLocalEntry(tableType, standardEntry.localizedString, key) || wasObjectDirtied;
            }

            if (!wasObjectDirtied) { return; }
            EditorUtility.SetDirty(targetObject);
            AssetDatabase.SaveAssetIfDirty(targetObject);
#endif
        }

        // Bridge-base of ILocalizableBase<T>.ReconcileCachedName (callable by those implementing ILocalizableCore directly)
        public static void ReconcileCachedName(ILocalizableCore localizable, Object targetObject, List<(string propertyName, LocalizedString _, bool __)> standardEntries, Action onRename)
        {
#if UNITY_EDITOR
            if (localizable.iCachedName == null || targetObject == null || string.IsNullOrWhiteSpace(targetObject.name)) { return; }
            if (targetObject.name == localizable.iCachedName) { return; }

            Enum tableType = localizable.localizationTableTypeValue;
            if (tableType == null) { return; }
            if (!LocalizationToolBridgeRegistry.TryGetBridge(tableType.GetType(), out ILocalizationToolBridge bridge)) { return; }

            foreach ((string propertyName, LocalizedString _, bool __) standardEntry in standardEntries)
            {
                string typeName = targetObject.GetType().Name;
                TableEntryReference oldKey = GetStandardLocalizationKey(localizable.iCachedName, typeName, standardEntry.propertyName);
                string newKey = GetStandardLocalizationKey(targetObject.name, typeName, standardEntry.propertyName);
                bridge.MakeOrRenameKey(tableType, oldKey, newKey);
            }

            localizable.iCachedName = targetObject.name;
            onRename?.Invoke();

            EditorUtility.SetDirty(targetObject);
            AssetDatabase.SaveAssetIfDirty(targetObject);
#endif
        }
    }
}

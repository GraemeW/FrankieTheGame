using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using Object = UnityEngine.Object;
using Frankie.Combat;
using Frankie.Stats;
using Frankie.Inventory;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Localization;
using UnityEditor.SceneManagement;
#endif

namespace Frankie.Utils.Localization
{
    public static class LocalizationTool
    {
        #region LocalizedStringSerializedProperties
        private const string _localizedStringSerializedKeyID = "m_TableEntryReference.m_KeyId";
        private const string _localizedStringSerializedKeyName = "m_TableEntryReference.m_Key";
        private const string _localizationFolder = "Assets/Localization";
        #endregion
        
        #region StringReferences
        private const string _englishRef = "en";
        private const string _initialOverwriteText = "Initial dummy text - to be replaced";
        private const string _tableChecksWorldObjectsRef = "ChecksWorldObjects";
        private const string _tableCoreRef = "Core";
        private const string _tableInventoryRef = "Inventory";
        private const string _tableQuestsRef = "Quests";
        private const string _tableSkillsRef = "Skills";
        private const string _tableSpeechRef = "Speech";
        private const string _tableUIRef = "UI";
        private const string _tableZonesRef = "Zones";
        #endregion
        
        #region RuntimeCompliant
        public static LocalizedString GetLocalizedString(LocalizationTableType localizationTableType, string key)
        {
            // Note: No safety on Localization Table loading, must be ensured via Unity settings
            var localizedString = new LocalizedString();
            string tableName = GetTableCollectionName(localizationTableType);
            localizedString.SetReference(tableName, key);
            return localizedString;
        }
        
        private static string GetTableCollectionName(LocalizationTableType localizationTableType)
        {
            return localizationTableType switch
            {
                LocalizationTableType.ChecksWorldObjects => _tableChecksWorldObjectsRef,
                LocalizationTableType.Core => _tableCoreRef,
                LocalizationTableType.Inventory => _tableInventoryRef,
                LocalizationTableType.Quests => _tableQuestsRef,
                LocalizationTableType.Skills => _tableSkillsRef,
                LocalizationTableType.Speech => _tableSpeechRef,
                LocalizationTableType.UI => _tableUIRef,
                LocalizationTableType.Zones => _tableZonesRef,
                _ => ""
            };
        }
        #endregion
        
#if UNITY_EDITOR
        // State
        private static bool _isLocaleInitialized = false;
        private static readonly Dictionary<LocalizationTableType, StringTable> _cachedEnglishTables = new();
        private static readonly Dictionary<LocalizationTableType, StringTableCollection> _cachedTableCollections = new();
        
        #region DirectoryManipulationMethods
        private static string GetTableCollectionPath(string tableCollectionName) => $"{_localizationFolder}/Table_{tableCollectionName}";
        private static void VerifyDirectoryExistence(string path)
        {
            if (!System.IO.Directory.Exists(path)) { System.IO.Directory.CreateDirectory(path); }
        }
        private static void VerifyLocalizationDirectoryExistence() => VerifyDirectoryExistence(_localizationFolder);

        private static StringTableCollection MakeLocalizationTable(string tableCollectionName, string tableCollectionPath)
        {
            StringTableCollection stringTableCollection = LocalizationEditorSettings.CreateStringTableCollection(tableCollectionName, tableCollectionPath);
            if (stringTableCollection == null) { return stringTableCollection; }
            
            Debug.Log($"Created StringTableCollection '{tableCollectionName}' at '{tableCollectionPath}'.");
            AssetDatabase.SaveAssetIfDirty(stringTableCollection);
            return stringTableCollection;
        }
        #endregion
        
        #region LocalizationTableInteraction
        public static void InitializeEnglishLocale(bool forceInitialization = false)
        {
            if (_isLocaleInitialized && !forceInitialization) { return; }

            LocalizationSettings.InitializationOperation.WaitForCompletion();
            LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.GetLocale(_englishRef);
            _isLocaleInitialized = true;
        }

        public static TableEntryReference GetTableEntryReferencedByID(LocalizationTableType localizationTableType, TableEntryReference ambiguousTableEntryReference)
        {
            if (!GetCachedTableCollection(localizationTableType, out StringTableCollection stringTableCollection)) { return SharedTableData.EmptyId; }
            return GetTableEntryReferencedByID(stringTableCollection.SharedData, ambiguousTableEntryReference);
        }

        public static TableEntryReference GetSerializedTableEntryKeyID(LocalizationTableType localizationTableType, SerializedProperty serializedProperty)
        {
            // KeyID Route
            SerializedProperty keyIDProperty = serializedProperty.FindPropertyRelative(_localizedStringSerializedKeyID);
            if (keyIDProperty == null) { return SharedTableData.EmptyId; }
            long keyID = keyIDProperty.longValue; 
            if (keyID != SharedTableData.EmptyId) { return keyID; }
            
            // Key Route
            SerializedProperty keyProperty = serializedProperty.FindPropertyRelative(_localizedStringSerializedKeyName);
            if (keyProperty == null) { return SharedTableData.EmptyId; }
            string key = keyProperty.stringValue;
            if (string.IsNullOrWhiteSpace(key)) { return SharedTableData.EmptyId; }

            TableEntryReference tableEntryReference = key;
            return GetTableEntryReferencedByID(localizationTableType, tableEntryReference);
        }
        
        public static bool GetOrMakeTableCollection(LocalizationTableType localizationTableType, out StringTableCollection stringTableCollection)
        {
            stringTableCollection = null;
            if (GetCachedTableCollection(localizationTableType, out stringTableCollection)) { return true; }
            
            VerifyLocalizationDirectoryExistence();
            
            string tableCollectionName = GetTableCollectionName(localizationTableType); 
            if (string.IsNullOrWhiteSpace(tableCollectionName)) { return false; }
            string tableCollectionPath = GetTableCollectionPath(tableCollectionName);
            if (string.IsNullOrWhiteSpace(tableCollectionPath)) { return false; }
            VerifyDirectoryExistence(tableCollectionPath);
            
            stringTableCollection = MakeLocalizationTable(tableCollectionName, tableCollectionPath);
            return stringTableCollection != null;
        }
        
        public static bool MakeOrRenameKey(LocalizationTableType localizationTableType, TableEntryReference tableEntryReference, string newKey)
        {
            if (!GetCachedEnglishTable(localizationTableType, out StringTable englishStringTable)) { return false; }
            
            Undo.RecordObject(englishStringTable, "Update Localization Key");
            Undo.RecordObject(englishStringTable.SharedData, "Update Localization Key");
            switch (tableEntryReference.ReferenceType)
            {
                case TableEntryReference.Type.Id:
                    englishStringTable.SharedData.RenameKey(tableEntryReference.KeyId, newKey);
                    break;
                case TableEntryReference.Type.Name:
                    englishStringTable.SharedData.RenameKey(tableEntryReference.Key, newKey);
                    break;
                case TableEntryReference.Type.Empty:
                {
                    if (!englishStringTable.SharedData.Contains(newKey))
                    {
                        englishStringTable.SharedData.AddKey(newKey);
                        break;
                    }
                    Debug.LogWarning($"Key '{newKey}' already exists in StringTableCollection.");
                    return false;
                }
                default:
                    return false;
            }
            
            DirtyStringTable(englishStringTable);
            return true;
        }

        public static string GetEnglishEntry(LocalizationTableType localizationTableType, TableEntryReference tableEntryReference)
        {
            if (!GetCachedEnglishTable(localizationTableType, out StringTable englishStringTable)) { return ""; }
            tableEntryReference = GetTableEntryReferencedByID(englishStringTable.SharedData, tableEntryReference);
            if (tableEntryReference.ReferenceType == TableEntryReference.Type.Empty) { return ""; }
            
            StringTableEntry stringTableEntry = englishStringTable.GetEntry(tableEntryReference.KeyId);
            return stringTableEntry?.Value ?? "";
        }
        
        public static bool AddUpdateEnglishEntry(LocalizationTableType localizationTableType, string keyName, string replacementText)
        {
            TableEntryReference tableEntryReference = keyName;
            return AddUpdateEnglishEntry(localizationTableType, tableEntryReference, replacementText);
        }
        
        public static bool AddUpdateEnglishEntry(LocalizationTableType localizationTableType, TableEntryReference tableEntryReference, string replacementText)
        {
            if (!GetCachedEnglishTable(localizationTableType, out StringTable englishStringTable)) { return false; }
            
            Undo.RecordObject(englishStringTable, "Update Localization Entry");
            Undo.RecordObject(englishStringTable.SharedData, "Update Localization Entry");
            long keyID;
            switch (tableEntryReference.ReferenceType)
            {
                case TableEntryReference.Type.Name:
                {
                    if (string.IsNullOrWhiteSpace(tableEntryReference)) { return false; }
                    keyID = englishStringTable.SharedData.GetId(tableEntryReference);
                    if (keyID == SharedTableData.EmptyId && !string.IsNullOrWhiteSpace(tableEntryReference.Key))
                    {
                        // KeyID doesn't exist, so make the entry via Key
                        englishStringTable.AddEntry(tableEntryReference.Key, replacementText);
                        break;
                    }
                    if (keyID == SharedTableData.EmptyId) { return false; }
                    
                    englishStringTable.AddEntry(keyID, replacementText);
                    break;
                }
                case TableEntryReference.Type.Id:
                {
                    keyID = tableEntryReference.KeyId;
                    englishStringTable.AddEntry(keyID, replacementText);
                    break;
                }
                case TableEntryReference.Type.Empty:
                default:
                    return false;
            }
            
            DirtyStringTable(englishStringTable);
            return true;
        }

        public static bool RemoveEntry(LocalizationTableType localizationTableType, TableEntryReference tableEntryReference)
        {
            if (!GetCachedTableCollection(localizationTableType, out StringTableCollection stringTableCollection)) { return false; }
            
            Undo.RecordObject(stringTableCollection, "Remove Localization Entry");
            Undo.RecordObject(stringTableCollection.SharedData, "Remove Localization Entry");
            long keyID;
            switch (tableEntryReference.ReferenceType)
            {
                case TableEntryReference.Type.Name:
                {
                    keyID = stringTableCollection.SharedData.GetId(tableEntryReference);
                    if (keyID == SharedTableData.EmptyId && !string.IsNullOrWhiteSpace(tableEntryReference.Key))
                    {
                        stringTableCollection.RemoveEntry(tableEntryReference.Key);
                        break;
                    }
                    if (keyID == SharedTableData.EmptyId) { return false; }
                    
                    stringTableCollection.RemoveEntry(keyID);
                    break;
                }
                case TableEntryReference.Type.Id:
                    keyID = tableEntryReference.KeyId;
                    stringTableCollection.RemoveEntry(keyID);
                    break;
                case TableEntryReference.Type.Empty:
                default:
                    return false;
            }
            
            DirtyStringTableCollection(stringTableCollection);
            return true;
        }
        #endregion
        
        #region LocalizedStringInteraction
        public static string ResolveKeyName(LocalizationTableType localizationTableType, LocalizedString localizedString, out TableEntryReference tableEntryReference)
        {
            bool englishTableFound = GetCachedEnglishTable(localizationTableType, out StringTable englishStringTable);
            
            string keyName;
            if (!localizedString.IsEmpty && englishTableFound)
            {
                tableEntryReference = localizedString.TableEntryReference;
                keyName = tableEntryReference.ResolveKeyName(englishStringTable.SharedData);
                if (keyName != null)
                {
                    tableEntryReference = GetTableEntryReferencedByID(englishStringTable.SharedData, tableEntryReference);
                    return keyName;
                }
            }
            
            tableEntryReference = new TableEntryReference();
            keyName = "";
            return keyName;
        }

        public static bool TryLocalizeEntry(LocalizationTableType localizationTableType, LocalizedString localizedString, string key, string value)
        {
            TableEntryReference tableEntryReference = key;
            if (localizedString != null && GetEnglishEntry(localizationTableType, localizedString.TableEntryReference) == value) { return false; }
            
            AddUpdateEnglishEntry(localizationTableType, tableEntryReference, value);
            SafelyUpdateReference(localizationTableType, localizedString, key);
            return true;
        }

        public static bool InitializeLocalEntry(LocalizationTableType localizationTableType, LocalizedString localizedString, string key)
        {
            localizedString ??= GetLocalizedString(localizationTableType, key);
            if (!localizedString.IsEmpty && !string.IsNullOrWhiteSpace(GetEnglishEntry(localizationTableType, localizedString.TableEntryReference))) { return false; }
            
            TableEntryReference tableEntryReference = key;
            AddUpdateEnglishEntry(localizationTableType, tableEntryReference, _initialOverwriteText);
            SafelyUpdateReference(localizationTableType, localizedString, key);
            return true;
        }

        public static bool SafelyUpdateReference(LocalizationTableType localizationTableType, LocalizedString localizedString, string newKey)
        {
            if (localizedString == null) { return false; }
            
            // Safely : Verify existence of entry, and update using long keyID only
            if (!GetCachedEnglishTable(localizationTableType, out StringTable englishStringTable)) {return false; }
            long newKeyID = englishStringTable.SharedData.GetId(newKey);
            if (newKeyID == SharedTableData.EmptyId) { return false; }
            
            localizedString.SetReference(englishStringTable.SharedData.TableCollectionNameGuid, newKeyID);
            return true;
        }
        #endregion
        
        #region ILocalizableInteraction
        public static List<TableEntryReference> GetStandardTableEntryReferences(LocalizationTableType localizationTableType, ILocalizable localizable)
        {
            List<TableEntryReference> tableEntryReferences = new();
            foreach (TableEntryReference ambiguousTableEntryReference in localizable.GetLocalizationEntries())
            {
                switch (ambiguousTableEntryReference.ReferenceType)
                {
                    case TableEntryReference.Type.Empty:
                    case TableEntryReference.Type.Id when ambiguousTableEntryReference.KeyId == SharedTableData.EmptyId:
                    case TableEntryReference.Type.Name when string.IsNullOrWhiteSpace(ambiguousTableEntryReference.Key):
                        continue;
                    default:
                    {
                        TableEntryReference tableEntryReference = LocalizationTool.GetTableEntryReferencedByID(localizationTableType, ambiguousTableEntryReference);
                        tableEntryReferences.Add(tableEntryReference);
                        break;
                    }
                }
            }
            return tableEntryReferences;
        }
        #endregion
        
        #region PrivateMethods
        private static void DirtyStringTable(StringTable stringTable)
        {
            EditorUtility.SetDirty(stringTable);
            EditorUtility.SetDirty(stringTable.SharedData);
            AssetDatabase.SaveAssetIfDirty(stringTable);
        }

        private static void DirtyStringTableCollection(StringTableCollection stringTableCollection)
        {
            EditorUtility.SetDirty(stringTableCollection);
            foreach (StringTable stringTable in stringTableCollection.StringTables)
            {
                EditorUtility.SetDirty(stringTable);
            }
            EditorUtility.SetDirty(stringTableCollection.SharedData);
            AssetDatabase.SaveAssetIfDirty(stringTableCollection);
        }
        
        private static TableEntryReference GetTableEntryReferencedByID(SharedTableData sharedTableData, TableEntryReference ambiguousTableEntryReference)
        {
            TableEntryReference tableEntryReferencedByID = new();
            switch (ambiguousTableEntryReference.ReferenceType)
            {
                case TableEntryReference.Type.Name:
                    tableEntryReferencedByID = sharedTableData.GetId(ambiguousTableEntryReference.Key);
                    if (tableEntryReferencedByID.KeyId ==  SharedTableData.EmptyId) { return new TableEntryReference(); }
                    break;
                case TableEntryReference.Type.Id:
                    tableEntryReferencedByID = ambiguousTableEntryReference.KeyId;
                    break;
            }
            return tableEntryReferencedByID;
        }
        
        private static bool GetCachedEnglishTable(LocalizationTableType localizationTableType, out StringTable englishStringTable)
        {
            if (_cachedEnglishTables.TryGetValue(localizationTableType, out englishStringTable)) { return true; }
            
            if (!GetCachedTableCollection(localizationTableType, out StringTableCollection stringTableCollection)) { return false; }
            if (!TryGetEnglishTable(stringTableCollection, out englishStringTable)) { return false; }
            
            _cachedEnglishTables.Add(localizationTableType, englishStringTable);
            return true;
        }

        private static bool GetCachedTableCollection(LocalizationTableType localizationTableType, out StringTableCollection stringTableCollection)
        {
            if (_cachedTableCollections.TryGetValue(localizationTableType, out stringTableCollection)) { return true; }
            
            if (!TryGetTableCollection(localizationTableType, out stringTableCollection)) { return false; }
            _cachedTableCollections.Add(localizationTableType, stringTableCollection);
            return true;
        }
        
        private static bool TryGetTableCollection(LocalizationTableType localizationTableType, out StringTableCollection stringTableCollection)
        {
            string stringTableName = GetTableCollectionName(localizationTableType);
            if (string.IsNullOrWhiteSpace(stringTableName))
            {
                stringTableCollection = null;
                return false; 
            }
            
            stringTableCollection = LocalizationEditorSettings.GetStringTableCollection(stringTableName);
            return stringTableCollection != null;
        }

        private static bool TryGetEnglishTable(StringTableCollection stringTableCollection, out StringTable englishStringTable)
        {
            englishStringTable = null;
            if (stringTableCollection == null) { return false; }
            
            englishStringTable = stringTableCollection.GetTable(_englishRef) as StringTable;
            return englishStringTable != null;
        }
        #endregion
#endif
    }
}

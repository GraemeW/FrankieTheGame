#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace Frankie.Utils
{
    public static class LocalizationTool
    {
        // State
        private static bool _isLocaleInitialized = false;
        private static readonly Dictionary<LocalizationTableType, StringTable> _cachedEnglishTables = new();
        private static readonly Dictionary<LocalizationTableType, StringTableCollection> _cachedTableCollections = new();
        
        #region StringReferences
        private const string _englishRef = "en";
        private const string _tableChecksWorldObjectsRef = "ChecksWorldObjects";
        private const string _tableCoreRef = "Core";
        private const string _tableInventoryRef = "Inventory";
        private const string _tableQuestsRef = "Quests";
        private const string _tableSkillsRef = "Skills";
        private const string _tableSpeechRef = "Speech";
        private const string _tableUIRef = "UI";
        private const string _tableZonesRef = "Zones";
        #endregion
        
        #region DirectoryManipulationMethods
        private const string _localizationFolder = "Assets/Localization";
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
        
        #region PublicMethods

        public static void InitializeEnglishLocale(bool forceInitialization = false)
        {
            if (_isLocaleInitialized && !forceInitialization) { return; }

            LocalizationSettings.InitializationOperation.WaitForCompletion();
            LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.GetLocale(_englishRef);
            _isLocaleInitialized = true;
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
                        return true;
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
            if (!EnsureTableEntryReferencedByID(englishStringTable.SharedData, ref tableEntryReference)) { return ""; }
            
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
        
        #region LocalizedStringSimplifiers
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
                    EnsureTableEntryReferencedByID(englishStringTable.SharedData, ref tableEntryReference);
                    return keyName;
                }
            }
            
            tableEntryReference = new TableEntryReference();
            keyName = "";
            return keyName;
        }

        public static bool SafelyUpdateReference(LocalizationTableType localizationTableType, LocalizedString localizedString, string newKey)
        {
            // Safely : Verify existence of entry, and update using long keyID only
            if (!GetCachedEnglishTable(localizationTableType, out StringTable englishStringTable)) {return false; }
            long newKeyID = englishStringTable.SharedData.GetId(newKey);
            if (newKeyID == SharedTableData.EmptyId) { return false; }
            
            localizedString.SetReference(englishStringTable.SharedData.TableCollectionNameGuid, newKeyID);
            return true;
        }

        public static bool SafelyUpdateReference(LocalizationTableType localizationTableType, LocalizedString localizedString, long newKeyID)
        {
            // Safely : Verify existence of entry, and update using long keyID only
            if (!GetCachedEnglishTable(localizationTableType, out StringTable englishStringTable)) {return false; }
            if (!englishStringTable.SharedData.Contains(newKeyID)) { return false; }
            
            localizedString.SetReference(englishStringTable.SharedData.TableCollectionNameGuid, newKeyID);
            return true;
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
        
        private static bool EnsureTableEntryReferencedByID(SharedTableData sharedTableData, ref TableEntryReference tableEntryReference)
        {
            switch (tableEntryReference.ReferenceType)
            {
                case TableEntryReference.Type.Name:
                    tableEntryReference = sharedTableData.GetId(tableEntryReference.Key);
                    return tableEntryReference.KeyId != SharedTableData.EmptyId;
                case TableEntryReference.Type.Id:
                    return tableEntryReference.KeyId != SharedTableData.EmptyId;
                case TableEntryReference.Type.Empty: 
                default:
                    return false;
            }
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
    }
}
#endif

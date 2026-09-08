using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Localization;
#endif

namespace LowDefMustard.Localization
{
    // TTableType is the caller's table-type enum (e.g. LocalizationTableType)
    // Project implementation should define its own empty alias interface (e.g. LocalizationTool : LocalizationToolBase<LocalizationTableType>)
    
    public abstract class LocalizationToolBase<TTableType> where TTableType: struct, Enum
    {
        // Const Tunables:  LocalizedStringSerializedProperties
        private const string _localizedStringSerializedKeyID = "m_TableEntryReference.m_KeyId";
        private const string _localizedStringSerializedKeyName = "m_TableEntryReference.m_Key";
        private const string _localizationFolder = "Assets/Localization";
        
        // English is used as the lookup locale for baseline table content
        private const string _englishRef = "en";
        
        // Const Tunables:  StringReferences
        private const string _initialOverwriteText = "Initial dummy text - to be replaced";
        
        // Type Look-up
        private static Dictionary<TTableType, string> _tableCollectionNames;
        
        #region Configuration
        public static void RegisterTableCollectionNames(IReadOnlyDictionary<TTableType, string> tableCollectionNames)
        {
            _tableCollectionNames ??= new Dictionary<TTableType, string>();
            foreach (KeyValuePair<TTableType, string> entry in tableCollectionNames) { _tableCollectionNames[entry.Key] = entry.Value; }
#if UNITY_EDITOR
            LocalizationToolBridgeRegistry.Register(typeof(TTableType), localizationToolBridge);
#endif
        }

        private static string GetTableCollectionName(TTableType tableType)
        {
            if (_tableCollectionNames != null && _tableCollectionNames.TryGetValue(tableType, out string tableCollectionName)) { return tableCollectionName;}
            Debug.LogWarning($"No table collection name registered for '{typeof(TTableType).Name}.{tableType}'");
            return "";
        }
        #endregion
        
        #region RuntimeCompliant
        public static LocalizedString MakeLocalizedString(TTableType tableType, string key)
        {
            // Note: No safety on Localization Table loading, must be ensured via Unity settings
            var localizedString = new LocalizedString();
            string tableName = GetTableCollectionName(tableType);
            localizedString.SetReference(tableName, key);
            return localizedString;
        }
        #endregion
        
        
#if UNITY_EDITOR
        // State
        private static ILocalizationToolBridge localizationToolBridge { get; } = new BridgeImplementation();
        private static readonly Dictionary<TTableType, StringTable> _cachedEnglishTables = new();
        private static readonly Dictionary<TTableType, StringTableCollection> _cachedTableCollections = new();

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
        private static bool HasTableEntry(TTableType tableType, ref TableEntryReference tableEntryReference) => HasTableEntry(tableType, ref tableEntryReference, out _);

        private static bool HasTableEntry(TTableType tableType, string key)
        {
            TableEntryReference tableEntryReference = key;
            return HasTableEntry(tableType, ref tableEntryReference, out _);
        }
        
        private static bool HasTableEntry(TTableType tableType, ref TableEntryReference tableEntryReference, out StringTable englishStringTable)
        {
            bool englishTableFound = GetCachedEnglishTable(tableType, out englishStringTable);
            if (!englishTableFound) { return false; }

            tableEntryReference = GetTableEntryReferencedByID(englishStringTable.SharedData, tableEntryReference);
            return tableEntryReference.ReferenceType != TableEntryReference.Type.Empty && tableEntryReference.KeyId != SharedTableData.EmptyId;
        }

        private static TableEntryReference GetTableEntryReferencedByID(TTableType tableType, TableEntryReference ambiguousTableEntryReference)
        {
            if (!GetCachedTableCollection(tableType, out StringTableCollection stringTableCollection)) { return SharedTableData.EmptyId; }
            return GetTableEntryReferencedByID(stringTableCollection.SharedData, ambiguousTableEntryReference);
        }
        
        private static TableEntryReference GetTableEntryReferencedByID(SharedTableData sharedTableData, TableEntryReference ambiguousTableEntryReference)
        {
            TableEntryReference tableEntryReferencedByID = new();
            switch (ambiguousTableEntryReference.ReferenceType)
            {
                case TableEntryReference.Type.Name:
                    tableEntryReferencedByID = sharedTableData.GetId(ambiguousTableEntryReference.Key);
                    if (tableEntryReferencedByID.KeyId == SharedTableData.EmptyId) { return new TableEntryReference(); }
                    break;
                case TableEntryReference.Type.Id:
                    tableEntryReferencedByID = ambiguousTableEntryReference.KeyId;
                    break;
            }
            return tableEntryReferencedByID;
        }

        private static TableEntryReference GetSerializedTableEntryKeyID(TTableType tableType, SerializedProperty serializedProperty)
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
            return GetTableEntryReferencedByID(tableType, tableEntryReference);
        }

        private static bool GetOrMakeTableCollection(TTableType tableType, out StringTableCollection stringTableCollection)
        {
            stringTableCollection = null;
            if (GetCachedTableCollection(tableType, out stringTableCollection)) { return true; }

            VerifyLocalizationDirectoryExistence();

            string tableCollectionName = GetTableCollectionName(tableType);
            if (string.IsNullOrWhiteSpace(tableCollectionName)) { return false; }
            string tableCollectionPath = GetTableCollectionPath(tableCollectionName);
            if (string.IsNullOrWhiteSpace(tableCollectionPath)) { return false; }
            VerifyDirectoryExistence(tableCollectionPath);

            stringTableCollection = MakeLocalizationTable(tableCollectionName, tableCollectionPath);
            return stringTableCollection != null;
        }

        public static bool MakeOrRenameKey(TTableType tableType, TableEntryReference tableEntryReference, string newKey)
        {
            if (!GetCachedEnglishTable(tableType, out StringTable englishStringTable)) { return false; }

            Undo.RecordObject(englishStringTable, "Update Localization Key");
            Undo.RecordObject(englishStringTable.SharedData, "Update Localization Key");

            bool tableEntryExists = HasTableEntry(tableType, ref tableEntryReference);
            if (!tableEntryExists) { tableEntryReference = new TableEntryReference(); } // Invalid table entry reference, reset to empty

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

        private static string GetEnglishEntry(TTableType tableType, TableEntryReference tableEntryReference)
        {
            if (!HasTableEntry(tableType, ref tableEntryReference, out StringTable englishStringTable)) { return ""; }
            StringTableEntry stringTableEntry = englishStringTable.GetEntry(tableEntryReference.KeyId);
            return stringTableEntry?.Value ?? "";
        }

        private static bool HasEnglishEntry(TTableType tableType, TableEntryReference tableEntryReference) => !string.IsNullOrWhiteSpace(GetEnglishEntry(tableType, tableEntryReference));

        private static bool AddUpdateEnglishEntry(TTableType tableType, string keyName, string replacementText)
        {
            TableEntryReference tableEntryReference = keyName;
            return AddUpdateEnglishEntry(tableType, tableEntryReference, replacementText);
        }

        public static bool AddUpdateEnglishEntry(TTableType tableType, TableEntryReference tableEntryReference, string replacementText)
        {
            if (!GetCachedEnglishTable(tableType, out StringTable englishStringTable)) { return false; }

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

        public static bool RemoveEntry(TTableType tableType, TableEntryReference tableEntryReference)
        {
            if (!GetCachedTableCollection(tableType, out StringTableCollection stringTableCollection)) { return false; }

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
        private static string ResolveKeyName(TTableType tableType, LocalizedString localizedString, out TableEntryReference tableEntryReference)
        {
            bool englishTableFound = GetCachedEnglishTable(tableType, out StringTable englishStringTable);

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

        public static bool TryLocalizeEntry(TTableType tableType, LocalizedString localizedString, string key, string value)
        {
            TableEntryReference tableEntryReference = key;
            if (localizedString != null && GetEnglishEntry(tableType, localizedString.TableEntryReference) == value) { return false; }

            AddUpdateEnglishEntry(tableType, tableEntryReference, value);
            SafelyUpdateReference(tableType, localizedString, key);
            return true;
        }

        public static bool InitializeLocalEntry(TTableType tableType, LocalizedString localizedString, string key)
        {
            localizedString ??= MakeLocalizedString(tableType, key);
            if (!localizedString.IsEmpty && !string.IsNullOrWhiteSpace(GetEnglishEntry(tableType, localizedString.TableEntryReference))) { return false; }

            TableEntryReference tableEntryReference = key;
            AddUpdateEnglishEntry(tableType, tableEntryReference, _initialOverwriteText);
            SafelyUpdateReference(tableType, localizedString, key);
            return true;
        }

        private static bool SafelyUpdateReference(TTableType tableType, LocalizedString localizedString, string newKey)
        {
            if (localizedString == null) { return false; }

            // Safely : Verify existence of entry, and update using long keyID only
            if (!GetCachedEnglishTable(tableType, out StringTable englishStringTable)) { return false; }
            long newKeyID = englishStringTable.SharedData.GetId(newKey);
            if (newKeyID == SharedTableData.EmptyId) { return false; }

            localizedString.SetReference(englishStringTable.SharedData.TableCollectionNameGuid, newKeyID);
            return true;
        }
        #endregion

        #region ILocalizableInteraction
        private static List<TableEntryReference> GetStandardTableEntryReferences(TTableType tableType, ILocalizableCore localizable)
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
                        TableEntryReference tableEntryReference = GetTableEntryReferencedByID(tableType, ambiguousTableEntryReference);
                        tableEntryReferences.Add(tableEntryReference);
                        break;
                    }
                }
            }
            return tableEntryReferences;
        }
        #endregion

        #region PrivateUtility
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

        private static bool GetCachedEnglishTable(TTableType tableType, out StringTable englishStringTable)
        {
            if (_cachedEnglishTables.TryGetValue(tableType, out englishStringTable)) { return true; }

            if (!GetCachedTableCollection(tableType, out StringTableCollection stringTableCollection)) { return false; }
            if (!TryGetEnglishTable(stringTableCollection, out englishStringTable)) { return false; }

            _cachedEnglishTables.Add(tableType, englishStringTable);
            return true;
        }

        private static bool GetCachedTableCollection(TTableType tableType, out StringTableCollection stringTableCollection)
        {
            if (_cachedTableCollections.TryGetValue(tableType, out stringTableCollection)) { return true; }

            if (!TryGetTableCollection(tableType, out stringTableCollection)) { return false; }
            _cachedTableCollections.Add(tableType, stringTableCollection);
            return true;
        }

        private static bool TryGetTableCollection(TTableType tableType, out StringTableCollection stringTableCollection)
        {
            string stringTableName = GetTableCollectionName(tableType);
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
        
        #region Bridge
        // Exposes the closed LocalizationToolBase<T> to editor that only knows T at runtime
        private sealed class BridgeImplementation : ILocalizationToolBridge
        {
#if UNITY_EDITOR
            public bool GetOrMakeTableCollection(Enum tableType, out StringTableCollection stringTableCollection) => LocalizationToolBase<TTableType>.GetOrMakeTableCollection((TTableType)tableType, out stringTableCollection);
            public TableEntryReference GetSerializedTableEntryKeyID(Enum tableType, SerializedProperty serializedProperty) => LocalizationToolBase<TTableType>.GetSerializedTableEntryKeyID((TTableType)tableType, serializedProperty);
            public bool HasTableEntry(Enum tableType, string key) => LocalizationToolBase<TTableType>.HasTableEntry((TTableType)tableType, key);
            public bool HasTableEntry(Enum tableType, ref TableEntryReference tableEntryReference) => LocalizationToolBase<TTableType>.HasTableEntry((TTableType)tableType, ref tableEntryReference);
            public TableEntryReference GetTableEntryReferencedByID(Enum tableType, TableEntryReference ambiguousTableEntryReference) => LocalizationToolBase<TTableType>.GetTableEntryReferencedByID((TTableType)tableType, ambiguousTableEntryReference);
            public bool MakeOrRenameKey(Enum tableType, TableEntryReference tableEntryReference, string newKey) => LocalizationToolBase<TTableType>.MakeOrRenameKey((TTableType)tableType, tableEntryReference, newKey);
            public string GetEnglishEntry(Enum tableType, TableEntryReference tableEntryReference) => LocalizationToolBase<TTableType>.GetEnglishEntry((TTableType)tableType, tableEntryReference);
            public bool HasEnglishEntry(Enum tableType, TableEntryReference tableEntryReference) => LocalizationToolBase<TTableType>.HasEnglishEntry((TTableType)tableType, tableEntryReference);
            public bool AddUpdateEnglishEntry(Enum tableType, string keyName, string replacementText) => LocalizationToolBase<TTableType>.AddUpdateEnglishEntry((TTableType)tableType, keyName, replacementText);
            public bool AddUpdateEnglishEntry(Enum tableType, TableEntryReference tableEntryReference, string replacementText) => LocalizationToolBase<TTableType>.AddUpdateEnglishEntry((TTableType)tableType, tableEntryReference, replacementText);
            public bool RemoveEntry(Enum tableType, TableEntryReference tableEntryReference) => LocalizationToolBase<TTableType>.RemoveEntry((TTableType)tableType, tableEntryReference);
            public string ResolveKeyName(Enum tableType, LocalizedString localizedString, out TableEntryReference tableEntryReference) => LocalizationToolBase<TTableType>.ResolveKeyName((TTableType)tableType, localizedString, out tableEntryReference);
            public bool SafelyUpdateReference(Enum tableType, LocalizedString localizedString, string newKey) => LocalizationToolBase<TTableType>.SafelyUpdateReference((TTableType)tableType, localizedString, newKey);
            public bool TryLocalizeEntry(Enum tableType, LocalizedString localizedString, string key, string value) => LocalizationToolBase<TTableType>.TryLocalizeEntry((TTableType)tableType, localizedString, key, value);
            public bool InitializeLocalEntry(Enum tableType, LocalizedString localizedString, string key) => LocalizationToolBase<TTableType>.InitializeLocalEntry((TTableType)tableType, localizedString, key);
            public List<TableEntryReference> GetStandardTableEntryReferences(Enum tableType, ILocalizableCore localizable) => LocalizationToolBase<TTableType>.GetStandardTableEntryReferences((TTableType)tableType, localizable);
#endif
        }
        #endregion
    }
}

using System;
using System.Collections.Generic;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Localization;
#endif

namespace LowDefMustard.Localization
{
    // Non-generic surface onto a single closed LocalizationToolBase<T> for LocalizationDeletionHandler && SimpleLocalizedStringDrawer
    public interface ILocalizationToolBridge
    {
#if UNITY_EDITOR
        bool GetOrMakeTableCollection(Enum tableType, out StringTableCollection stringTableCollection);
        TableEntryReference GetSerializedTableEntryKeyID(Enum tableType, SerializedProperty serializedProperty);
        bool HasTableEntry(Enum tableType, string key);
        bool HasTableEntry(Enum tableType, ref TableEntryReference tableEntryReference);
        TableEntryReference GetTableEntryReferencedByID(Enum tableType, TableEntryReference ambiguousTableEntryReference);
        bool MakeOrRenameKey(Enum tableType, TableEntryReference tableEntryReference, string newKey);
        string GetEnglishEntry(Enum tableType, TableEntryReference tableEntryReference);
        bool HasEnglishEntry(Enum tableType, TableEntryReference tableEntryReference);
        bool AddUpdateEnglishEntry(Enum tableType, string keyName, string replacementText);
        bool AddUpdateEnglishEntry(Enum tableType, TableEntryReference tableEntryReference, string replacementText);
        bool RemoveEntry(Enum tableType, TableEntryReference tableEntryReference);
        string ResolveKeyName(Enum tableType, LocalizedString localizedString, out TableEntryReference tableEntryReference);
        bool SafelyUpdateReference(Enum tableType, LocalizedString localizedString, string newKey);
        bool TryLocalizeEntry(Enum tableType, LocalizedString localizedString, string key, string value);
        bool InitializeLocalEntry(Enum tableType, LocalizedString localizedString, string key);
        List<TableEntryReference> GetStandardTableEntryReferences(Enum tableType, ILocalizableCore localizable);
#endif
    }
}

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine.Localization.Tables;

namespace Frankie.Core.Localization
{
    public static class LocalizationReferences
    {
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
        
        #region PublicMethods
        public static bool AddUpdateEnglishEntry(LocalizationTableType localizationTableType, string keyName, string replacementText)
        {
            if (!TryGetEnglishTable(localizationTableType, out StringTable englishStringTable)) { return false; }
            
            englishStringTable.AddEntry(keyName, replacementText);
            EditorUtility.SetDirty(englishStringTable);
            EditorUtility.SetDirty(englishStringTable.SharedData);
            AssetDatabase.SaveAssets();
            return true;
        }

        public static bool AddUpdateEnglishEntry(LocalizationTableType localizationTableType, TableEntryReference tableEntryReference, string replacementText)
        {
            if (!TryGetEnglishTable(localizationTableType, out StringTable englishStringTable)) { return false; }
            if (tableEntryReference.ReferenceType == TableEntryReference.Type.Empty) { return false; }
            
            englishStringTable.AddEntryFromReference(tableEntryReference, replacementText);
            EditorUtility.SetDirty(englishStringTable);
            EditorUtility.SetDirty(englishStringTable.SharedData);
            AssetDatabase.SaveAssets();
            return true;
        }
        #endregion
        
        #region PrivateMethods
        private static bool TryGetTableCollection(LocalizationTableType localizationTableType, out StringTableCollection stringTableCollection)
        {
            stringTableCollection = null;
            switch (localizationTableType)
            {
                case LocalizationTableType.ChecksWorldObjects:
                    stringTableCollection = LocalizationEditorSettings.GetStringTableCollection(_tableChecksWorldObjectsRef);
                    break;
                case LocalizationTableType.Core:
                    stringTableCollection = LocalizationEditorSettings.GetStringTableCollection(_tableCoreRef);
                    break;
                case LocalizationTableType.Inventory:
                    stringTableCollection = LocalizationEditorSettings.GetStringTableCollection(_tableInventoryRef);
                    break;
                case LocalizationTableType.Quests:
                    stringTableCollection = LocalizationEditorSettings.GetStringTableCollection(_tableQuestsRef);
                    break;
                case LocalizationTableType.Skills:
                    stringTableCollection = LocalizationEditorSettings.GetStringTableCollection(_tableSkillsRef);
                    break;
                case LocalizationTableType.Speech:
                    stringTableCollection = LocalizationEditorSettings.GetStringTableCollection(_tableSpeechRef);
                    break;
                case LocalizationTableType.UI:
                    stringTableCollection = LocalizationEditorSettings.GetStringTableCollection(_tableUIRef);
                    break;
                case LocalizationTableType.Zones:
                    stringTableCollection = LocalizationEditorSettings.GetStringTableCollection(_tableZonesRef);
                    break;
            }
            return stringTableCollection != null;
        }

        private static bool TryGetEnglishTable(LocalizationTableType localizationTableType, out StringTable englishStringTable)
        {
            englishStringTable = null;
            if (!TryGetTableCollection(localizationTableType, out StringTableCollection stringTableCollection)) { return false; }
            
            englishStringTable = stringTableCollection.GetTable(_englishRef) as StringTable;
            return englishStringTable != null;
        }
        #endregion
    }
}
#endif

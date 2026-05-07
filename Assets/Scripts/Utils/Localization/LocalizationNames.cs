using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using Frankie.Combat;
using Frankie.Inventory;
using Frankie.Quests;
using Frankie.Stats;

namespace Frankie.Utils.Localization
{
    public static class LocalizationNames
    {
        // State
        private static readonly System.Random _random = new();
        private static Dictionary<Stat, LocalizedString> _statNameCache;
        private static Dictionary<EquipLocation, LocalizedString> _equipLocationNameCache;
        
        #region PublicMethods
        public static string GenerateTypeSpecificKey(Object targetObject, string propertyName, Type declaringType = null, bool useParentNameStem = true)
        {
            switch (targetObject)
            {
                case CharacterProperties or Skill or InventoryItem or Quest:
                    string typeName = declaringType != null ? declaringType.Name : targetObject.GetType().Name;
                    return ILocalizable.GetStandardLocalizationKey(targetObject.name, typeName, propertyName);
                default:
                    return GenerateKindaUniqueKey(targetObject, propertyName, declaringType, useParentNameStem);
            }
        }
        
        public static string GetLocalizedName(Stat stat)
        {
            _statNameCache ??= BuildStatCache();
            return _statNameCache.ContainsKey(stat) ? _statNameCache[stat].GetSafeLocalizedString() : stat.ToString();
        }
        
        public static string GetLocalizedName(EquipLocation equipLocation)
        {
            _equipLocationNameCache ??= BuildEquipLocationCache();
            return _equipLocationNameCache.ContainsKey(equipLocation) ? _equipLocationNameCache[equipLocation].GetSafeLocalizedString() : equipLocation.ToString();
        }
        #endregion
        
        #region PrivateMethods
        private static string GetStatKey(Stat stat) => $"{nameof(Stat)}.{stat.ToString()}";
        private static string GetEquipLocationKey(EquipLocation equipLocation) => $"{nameof(EquipLocation)}.{equipLocation.ToString()}";
        
        private static Dictionary<Stat, LocalizedString> BuildStatCache()
        {
            var newStatNameCache = new Dictionary<Stat, LocalizedString>();
            foreach (Stat stat in Enum.GetValues(typeof(Stat)))
            {
                newStatNameCache.Add(stat, LocalizationTool.GetLocalizedString(LocalizationTableType.Core, GetStatKey(stat)));
            }
            return newStatNameCache;
        }
        
        private static Dictionary<EquipLocation, LocalizedString> BuildEquipLocationCache()
        {
            var newEquipLocationNameCache = new Dictionary<EquipLocation, LocalizedString>();
            foreach (EquipLocation equipLocation in Enum.GetValues(typeof(EquipLocation)))
            {
                newEquipLocationNameCache.Add(equipLocation, LocalizationTool.GetLocalizedString(LocalizationTableType.Inventory, GetEquipLocationKey(equipLocation)));
            }
            return newEquipLocationNameCache;
        }
        #endregion
        
#if UNITY_EDITOR
        #region EditorMethods
        private static string GenerateKindaUniqueKey(Object targetObject, string propertyName, Type declaringType = null,  bool useParentNameStem = true)
        {
            string componentStem = declaringType != null ? $"{declaringType.Name}." : $"{targetObject.GetType().Name}.";
            string targetStem = "";
            string nameStem = targetObject.name;
            
            if (targetObject is GameObject castGameObject) { targetObject = castGameObject.GetComponent<MonoBehaviour>(); }
            if (useParentNameStem && targetObject is MonoBehaviour castMonoBehaviour && castMonoBehaviour.transform.parent != null)
            {
                nameStem = castMonoBehaviour.transform.parent.name;
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
            string propertyNameStem = propertyName != null ? $"{propertyName}." : "";
            string semiUniqueShortKey = _random.Next().ToString("x");
            return $"{componentStem}{targetStem}{propertyNameStem}{semiUniqueShortKey}";
        }
        
        public static void GenerateDefaultEntries(LocalizationTableType localizationTableType)
        {
            // For generating default English entries (if ever need re-generation)   
            switch (localizationTableType)
            {
                case LocalizationTableType.Core:
                {
                    
                    foreach (Stat stat in Enum.GetValues(typeof(Stat)))
                    {
                        TableEntryReference tableEntryReference = GetStatKey(stat);
                        string englishStatName = stat.ToString();
                        if (stat == Stat.InitialLevel) { englishStatName = "Level"; }
                        LocalizationTool.AddUpdateEnglishEntry(localizationTableType, tableEntryReference, englishStatName);
                    }
                    break;
                }
                case LocalizationTableType.Inventory:
                {
                    foreach (EquipLocation equipLocation in Enum.GetValues(typeof(EquipLocation)))
                    {
                        TableEntryReference tableEntryReference = GetEquipLocationKey(equipLocation);
                        string englishEquipLocationName = equipLocation.ToString();
                        LocalizationTool.AddUpdateEnglishEntry(localizationTableType, tableEntryReference, englishEquipLocationName);
                    }
                    break;
                }
            }
        }
        #endregion
#endif
    }
}

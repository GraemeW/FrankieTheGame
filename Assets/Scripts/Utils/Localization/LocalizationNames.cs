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
using Frankie.Stats;
using Frankie.ZoneManagement;
using Frankie.Speech;
using Frankie.Combat;
using Frankie.Inventory;
using Frankie.Quests;

namespace Frankie.Utils.Localization
{
    public static class LocalizationNames
    {
        // State
        private static readonly System.Random _random = new();
        // Enum Translation Layer
        private static Dictionary<Stat, LocalizedString> _statNameCache;
        private static Dictionary<EquipLocation, LocalizedString> _equipLocationNameCache;
        // Status Effect Translation Layer
        private static Dictionary<Stat, LocalizedString> _statusEffectIncreaseNameCache;
        private static Dictionary<Stat, LocalizedString> _statusEffectDecreaseNameCache;
        
        #region PublicMethods
        public static string GetStandardLocalizationKey(string id, string typeName, string propertyName)
        {
            string sanitizedPropertyName = (propertyName ?? "").Replace("localized", "");
            return sanitizedPropertyName.Contains("Name") ? $"{typeName}.{id}" : $"{typeName}.{id}.{sanitizedPropertyName}";
        }
        
        // Enum Translations
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
        
        // Enum Derivatives
        public static string GetStatusEffectText(Stat stat, bool isBuff)
        {
            _statusEffectIncreaseNameCache ??= BuildStatusEffectCache(true);
            _statusEffectDecreaseNameCache ??= BuildStatusEffectCache(false);
            
            if (isBuff) { return _statusEffectIncreaseNameCache.ContainsKey(stat) ? _statusEffectIncreaseNameCache[stat].GetSafeLocalizedString() : ""; }
            else { return _statusEffectDecreaseNameCache.ContainsKey(stat) ? _statusEffectDecreaseNameCache[stat].GetSafeLocalizedString() : ""; } 
        }
        #endregion
        
        #region PrivateMethods
        private static string GetStatKey(Stat stat) => $"{nameof(Stat)}.{stat.ToString()}";
        private static string GetEquipLocationKey(EquipLocation equipLocation) => $"{nameof(EquipLocation)}.{equipLocation.ToString()}";
        private static string GetStatusEffectKey(Stat stat, bool isIncrease) => isIncrease ? $"StatusEffect.Buff.{stat.ToString()}" : $"StatusEffect.Debuff.{stat.ToString()}"; 
        
        private static Dictionary<Stat, LocalizedString> BuildStatCache()
        {
            var newStatNameCache = new Dictionary<Stat, LocalizedString>();
            foreach (Stat stat in Enum.GetValues(typeof(Stat)))
            {
                newStatNameCache.Add(stat, LocalizationTool.MakeLocalizedString(LocalizationTableType.Core, GetStatKey(stat)));
            }
            return newStatNameCache;
        }
        
        private static Dictionary<EquipLocation, LocalizedString> BuildEquipLocationCache()
        {
            var newEquipLocationNameCache = new Dictionary<EquipLocation, LocalizedString>();
            foreach (EquipLocation equipLocation in Enum.GetValues(typeof(EquipLocation)))
            {
                newEquipLocationNameCache.Add(equipLocation, LocalizationTool.MakeLocalizedString(LocalizationTableType.Inventory, GetEquipLocationKey(equipLocation)));
            }
            return newEquipLocationNameCache;
        }

        private static Dictionary<Stat, LocalizedString> BuildStatusEffectCache(bool isIncrease)
        {
            var newStatusEffectNameCache = new Dictionary<Stat, LocalizedString>();
            foreach (Stat stat in Enum.GetValues(typeof(Stat)))
            {
                if (!HasStatusEffectText(stat, isIncrease)) { continue; }
                newStatusEffectNameCache.Add(stat, LocalizationTool.MakeLocalizedString(LocalizationTableType.Core, GetStatusEffectKey(stat, isIncrease)));
            }
            return newStatusEffectNameCache;
        }
        #endregion
        
        #region StatusEffectEnglish
        private static bool HasStatusEffectText(Stat stat, bool isIncrease) => !string.IsNullOrWhiteSpace(GetEnglishStatusEffectText(stat, isIncrease));
        private static string GetEnglishStatusEffectText(Stat stat, bool isIncrease)
        {
            return stat switch
            {
                Stat.HP => isIncrease ? "+HP" : "-HP",
                Stat.AP => isIncrease ? "+AP" : "-AP",
                Stat.Brawn => isIncrease ? "STRONG" : "WEAK",
                Stat.Beauty => isIncrease ? "FETCHING" : "FOUL",
                Stat.Smarts => isIncrease ? "BRIGHT" : "DIM",
                Stat.Nimble => isIncrease ? "FAST" : "SLOW",
                Stat.Luck => isIncrease ? "BLESSED" : "JINXED",
                Stat.Pluck => isIncrease ? "BRAVE" : "COWARD",
                Stat.Stoic => isIncrease? "STURDY" : "FRAIL",
                _ => ""
            };
        }
        #endregion
        
#if UNITY_EDITOR
        #region KeyGeneration
        public static string GenerateTypeSpecificKey(Object targetObject, string propertyName, Type declaringType = null, bool useParentNameStem = true)
        {
            switch (targetObject)
            {
                case CharacterProperties or Zone or Dialogue or Skill or InventoryItem or Quest:
                    string typeName = declaringType != null ? declaringType.Name : targetObject.GetType().Name;
                    return GetStandardLocalizationKey(targetObject.name, typeName, propertyName);
                default:
                    return GenerateKindaUniqueKey(targetObject, propertyName, declaringType, useParentNameStem);
            }
        }
        
        private static string GenerateKindaUniqueKey(Object targetObject, string propertyName, Type declaringType = null,  bool useParentNameStem = true)
        {
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
            string semiUniqueShortKey = _random.Next().ToString("x");
            return $"{componentStem}{targetStem}{propertyNameStem}{semiUniqueShortKey}";
        }
        #endregion
        
        #region DefaultGeneration
        public static void GenerateDefaultEnumEntries(LocalizationTableType localizationTableType)
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

        public static void GenerateDefaultStatusEffectEntries()
        {
            foreach (Stat stat in Enum.GetValues(typeof(Stat)))
            {
                if (HasStatusEffectText(stat, true))
                {
                    TableEntryReference tableEntryReferenceIncrease = GetStatusEffectKey(stat, true);
                    string englishStatNameIncrease = GetEnglishStatusEffectText(stat, true);
                    LocalizationTool.AddUpdateEnglishEntry(LocalizationTableType.Core, tableEntryReferenceIncrease, englishStatNameIncrease);
                }

                if (HasStatusEffectText(stat, false))
                {
                    TableEntryReference tableEntryReferenceDecrease = GetStatusEffectKey(stat, false);
                    string englishStatNameDecrease = GetEnglishStatusEffectText(stat, false);
                    LocalizationTool.AddUpdateEnglishEntry(LocalizationTableType.Core, tableEntryReferenceDecrease, englishStatNameDecrease);
                }
            }
        }
        #endregion
#endif
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Frankie.Combat.Editor
{
    public static class EffectStrategyCatalog
    {
        // Notes:
        //  - Folders are relative to the battle actions root
        //  - An unmapped type / missing folder warns && falls back / gets created in uncategorized folder

        // Const Tunables
        private const string _battleActionsRoot = "Assets/Game/Combat/BattleActions";
        private const string _effectStrategiesFolder = "EffectStrategies";
        private const string _fallbackFolder = _effectStrategiesFolder + "/Uncategorized";
        private const string _valueFormat = "+0.##;-0.##;0";
        private static readonly (Type type, string folder)[] _typeFolders =
        {
            // Order here is also the make-new dropdown's order (matching the browser's sections)
            // OrderedDictionary not available until ≥Unity7
            (typeof(HealthEffect), _effectStrategiesFolder + "/DirectHP"),
            (typeof(HealthSequenceEffect), _effectStrategiesFolder + "/DirectHP"),
            (typeof(HealthOverTimeEffect), _effectStrategiesFolder + "/DoT"),
            (typeof(APEffect), _effectStrategiesFolder + "/DirectAP"),
            (typeof(CooldownSetEffect), _effectStrategiesFolder + "/SetCooldown"),
            (typeof(CooldownStopEffect), _effectStrategiesFolder + "/SetCooldown"),
            (typeof(ApplyPersistentStatEffect), _effectStrategiesFolder + "/PersistentStatMods"),
            (typeof(ApplyPermanentStatEffect), _effectStrategiesFolder + "/PermanentStatMods"),
            (typeof(RemovePersistentStatusEffect), _effectStrategiesFolder + "/OtherHealing"),
            (typeof(CallForHelp), _effectStrategiesFolder + "/CallForHelp"),
            (typeof(SpawnBattleEffect), "EffectStrategiesSpawnedArtwork"),
            (typeof(DoNothingEffect), _effectStrategiesFolder)
        };

        #region PublicMethods
        public static string GetCategoryLabel(EffectStrategyCategory category) => ObjectNames.NicifyVariableName(category.ToString());
        public static string GetTypeLabel(Type type) => ObjectNames.NicifyVariableName(type.Name);
        
        public static List<EffectStrategyCatalogEntry> Collect()
        {
            return AssetDatabase.FindAssets($"t:{nameof(EffectStrategy)}")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Distinct()
                .Select(AssetDatabase.LoadAssetAtPath<EffectStrategy>)
                .Where(strategy => strategy != null)
                .Select(Categorize)
                .OrderBy(entry => entry.category)
                .ThenBy(entry => entry.subOrder)
                .ThenBy(entry => entry.sortValue)
                .ThenBy(entry => entry.strategy.name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static EffectStrategyCatalogEntry Categorize(EffectStrategy strategy)
        {
            return strategy switch
            {
                HealthEffect health => HealthEntry(strategy, health.GetHealthChange(), 0, "Health"),
                HealthSequenceEffect sequence => HealthEntry(strategy, sequence.GetTotalHealthChange(), 1, "Sequence"),
                HealthOverTimeEffect overTime => HealthEntry(strategy, overTime.GetHealthChangePerTick(), 2, "Per tick"),
                APEffect ap => new EffectStrategyCatalogEntry(strategy, ap.GetAPChange() < 0f ? EffectStrategyCategory.APDrain : EffectStrategyCategory.APRestore, 0, Mathf.Abs(ap.GetAPChange()), $"AP {Format(ap.GetAPChange())}"),
                CooldownSetEffect => new EffectStrategyCatalogEntry(strategy, EffectStrategyCategory.Cooldown, 0, 0f, "Set"),
                CooldownStopEffect => new EffectStrategyCatalogEntry(strategy, EffectStrategyCategory.Cooldown, 1, 0f, "Stop"),
                ApplyPersistentStatEffect persistent => new EffectStrategyCatalogEntry(strategy, EffectStrategyCategory.PersistentStat, (int)persistent.GetStat(), -persistent.GetValue(), $"{persistent.GetStat()} {Format(persistent.GetValue())}"),
                ApplyPermanentStatEffect permanent => new EffectStrategyCatalogEntry(strategy, EffectStrategyCategory.PermanentStat, (int)permanent.GetStat(), -permanent.GetValue(), $"{permanent.GetStat()} {Format(permanent.GetValue())}"),
                RemovePersistentStatusEffect => new EffectStrategyCatalogEntry(strategy, EffectStrategyCategory.StatusRemoval, 0, 0f, GetTypeLabel(strategy.GetType())),
                CallForHelp => new EffectStrategyCatalogEntry(strategy, EffectStrategyCategory.CallForHelp, 0, 0f, GetTypeLabel(strategy.GetType())),
                SpawnBattleEffect => new EffectStrategyCatalogEntry(strategy, EffectStrategyCategory.BattleVisual, 0, 0f, GetTypeLabel(strategy.GetType())),
                _ => new EffectStrategyCatalogEntry(strategy, EffectStrategyCategory.Other, 0, 0f, GetTypeLabel(strategy.GetType()))
            };
        }

        public static List<Type> GetCreatableTypes()
        {
            // Mapped types first (in map order), then any unmapped ones alphabetically
            return TypeCache.GetTypesDerivedFrom<EffectStrategy>()
                .Where(type => !type.IsAbstract && !type.IsGenericType)
                .OrderBy(GetMapIndex)
                .ThenBy(GetTypeLabel, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        public static EffectStrategy Create(Type type)
        {
            if (type == null || type.IsAbstract || !typeof(EffectStrategy).IsAssignableFrom(type)) { return null; }

            string folder = GetFolder(type);
            BattleActionAssets.EnsureFolder(folder);
            string baseName = $"New {GetTypeLabel(type)}";
            string path = $"{folder}/{baseName}.asset";
            for (int i = 1; AssetDatabase.AssetPathExists(path); i++) { path = $"{folder}/{baseName} ({i}).asset"; }

            var strategy = (EffectStrategy)ScriptableObject.CreateInstance(type);
            AssetDatabase.CreateAsset(strategy, path);
            AssetDatabase.SaveAssets();
            return strategy;
        }
        #endregion

        #region PrivateMethods
        private static string Format(float value) => value.ToString(_valueFormat, CultureInfo.InvariantCulture);
        private static EffectStrategyCatalogEntry HealthEntry(EffectStrategy strategy, float healthChange, int subOrder, string prefix)
        {
            EffectStrategyCategory category = healthChange < 0f ? EffectStrategyCategory.Damage : EffectStrategyCategory.Healing;
            return new EffectStrategyCatalogEntry(strategy, category, subOrder, Mathf.Abs(healthChange), $"{prefix} {Format(healthChange)}");
        }

        private static string GetFolder(Type type)
        {
            int mapIndex = GetMapIndex(type);
            if (mapIndex < _typeFolders.Length) { return $"{_battleActionsRoot}/{_typeFolders[mapIndex].folder}"; }
            Debug.LogWarning($"No folder mapped for {type.Name} - creating it in {_fallbackFolder} (add the type to {nameof(EffectStrategyCatalog)}'s folder map)");
            return $"{_battleActionsRoot}/{_fallbackFolder}";
        }

        private static int GetMapIndex(Type type)
        {
            int mapIndex = Array.FindIndex(_typeFolders, entry => entry.type == type);
            return mapIndex < 0 ? _typeFolders.Length : mapIndex;
        }
        #endregion
    }
}

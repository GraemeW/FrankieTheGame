using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using LowDefMustard.Utils.Editor;
using LowDefMustard.Utils.Tests;
using LowDefMustard.Zones;
using Frankie.Combat;
using Frankie.Inventory;
using Frankie.Quests;
using Frankie.Stats;

namespace Frankie.Tests.Editor
{
    public class LabelLoadedDataHardReferenceTests
    {
        // Tested:
        //  - No label-loaded data type (or subclass) serializes a direct art reference (GameObject / Component / AudioClip / Sprite / Texture / RuntimeAnimatorController), incl. through nested serializable classes
        // Not Tested:
        //  - Data on disk ~ this checks the types;  a hard reference in a field type not listed here would slip through
        // Note:  The caches label-load these at startup, so a hard reference keeps its art resident from boot to quit ~ use an AssetReference (e.g. AssetReferenceGameObject, AssetReferenceAudioClip) instead

        // Const
        private static readonly Type[] _labelLoadedTypes = { typeof(CharacterProperties), typeof(Zone), typeof(InventoryItem), typeof(Skill), typeof(BattleAction), typeof(Quest) };
        private static readonly Type[] _hardReferenceTypes = { typeof(GameObject), typeof(Component), typeof(AudioClip), typeof(Sprite), typeof(Texture), typeof(RuntimeAnimatorController) };
        private const int _maxNestingDepth = 4;

        #region Helpers
        private static void CollectHardReferences(Type type, string path, int depth, HashSet<Type> visited, List<string> hardReferences)
        {
            if (depth > _maxNestingDepth || !visited.Add(type)) { return; }

            foreach (FieldInfo field in SerializedFieldUtility.GetSerializedFields(type))
            {
                Type elementType = SerializedFieldUtility.GetCollectionElementType(field.FieldType);
                string fieldPath = $"{path}.{field.Name}";
                if (_hardReferenceTypes.Any(hardReferenceType => hardReferenceType.IsAssignableFrom(elementType))) { hardReferences.Add(fieldPath); }
                else if (SerializedFieldUtility.IsInlineSerializable(elementType)) { CollectHardReferences(elementType, fieldPath, depth + 1, visited, hardReferences); }
            }
        }
        #endregion

        #region Tests
        [Test]
        public void LabelLoadedDataTypes_HaveNoDirectArtReferences()
        {
            List<Type> dataTypes = _labelLoadedTypes
                .SelectMany(labelLoadedType => TypeCache.GetTypesDerivedFrom(labelLoadedType).Prepend(labelLoadedType))
                .Where(type => !type.IsAbstract && !TestAssemblyCheck.IsTestAssemblyType(type))
                .Distinct()
                .ToList();

            var hardReferences = new List<string>();
            foreach (Type dataType in dataTypes) { CollectHardReferences(dataType, dataType.Name, 0, new HashSet<Type>(), hardReferences); }

            List<string> disallowed = hardReferences.Distinct().ToList();
            Assert.IsEmpty(disallowed, $"Direct art references on label-loaded data (keeps that art resident from boot) ~ use an AssetReference:\n{string.Join("\n", disallowed)}");
        }
        #endregion
    }
}

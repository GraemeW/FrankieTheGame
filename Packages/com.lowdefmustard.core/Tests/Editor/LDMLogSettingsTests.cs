using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LowDefMustard.Core.Tests.Editor
{
    public class LDMLogSettingsTests
    {
        // State
        private LDMLogSettings settings;

        #region Setup
        [SetUp]
        public void SetUp() => settings = ScriptableObject.CreateInstance<LDMLogSettings>();

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(settings);
        #endregion

        #region Tests
        [Test]
        public void SyncEntriesToEnum_CreatesOneEntryPerSingleBitCategory()
        {
            Assert.AreEqual(LDMLogSettings.GetSingleBitCategories().Count, settings.GetCategoryEntries().Count);
            foreach (LDMLogCategoryEntry entry in settings.GetCategoryEntries())
            {
                Assert.AreNotEqual(LDMLogCategory.None, entry.category);
                Assert.AreNotEqual(LDMLogCategory.All, entry.category);
            }
        }

        [Test]
        public void SyncEntriesToEnum_DropsStaleAndDuplicateEntries()
        {
            var serializedSettings = new SerializedObject(settings);
            SerializedProperty entries = serializedSettings.FindProperty(LDMLogSettings.categoryEntriesField);
            int originalCount = entries.arraySize;
            entries.arraySize = originalCount + 2;
            entries.GetArrayElementAtIndex(originalCount).FindPropertyRelative(LDMLogCategoryEntry.categoryField).enumValueFlag = 1 << 30;
            entries.GetArrayElementAtIndex(originalCount + 1).FindPropertyRelative(LDMLogCategoryEntry.categoryField).enumValueFlag = (int)LDMLogCategory.Combat;
            serializedSettings.ApplyModifiedPropertiesWithoutUndo();

            settings.SyncEntriesToEnum();

            Assert.AreEqual(originalCount, settings.GetCategoryEntries().Count);
        }

        [Test]
        public void SyncEntriesToEnum_SortsEntriesByBit()
        {
            for (int i = 1; i < settings.GetCategoryEntries().Count; i++)
            {
                Assert.Less((int)settings.GetCategoryEntries()[i - 1].category, (int)settings.GetCategoryEntries()[i].category);
            }
        }

        [Test]
        public void IsEnabled_AllCategoriesEnabledByDefault()
        {
            foreach (LDMLogCategory category in LDMLogSettings.GetSingleBitCategories()) { Assert.IsTrue(settings.IsEnabled(category), category.ToString()); }
        }

        [Test]
        public void IsEnabled_FalseForNone() => Assert.IsFalse(settings.IsEnabled(LDMLogCategory.None));

        [Test]
        public void IsEnabled_FalseForDisabledCategory()
        {
            settings.SetCategoryEnabled(LDMLogCategory.Combat, false);

            Assert.IsFalse(settings.IsEnabled(LDMLogCategory.Combat));
            Assert.IsTrue(settings.IsEnabled(LDMLogCategory.Stats));
        }

        [Test]
        public void IsEnabled_MultiFlagTrueWhenAnyFlagEnabled()
        {
            settings.SetCategoryEnabled(LDMLogCategory.Combat, false);

            Assert.IsTrue(settings.IsEnabled(LDMLogCategory.Combat | LDMLogCategory.Stats));
        }

        [Test]
        public void IsEnabled_MultiFlagFalseWhenAllFlagsDisabled()
        {
            settings.SetCategoryEnabled(LDMLogCategory.Combat | LDMLogCategory.Stats, false);

            Assert.IsFalse(settings.IsEnabled(LDMLogCategory.Combat | LDMLogCategory.Stats));
        }

        [Test]
        public void GetColorHex_MultiFlagUsesFirstCategoryInDeclarationOrder()
        {
            string combatHex = settings.GetColorHex(LDMLogCategory.Combat);
            string statsHex = settings.GetColorHex(LDMLogCategory.Stats);

            Assert.AreNotEqual(combatHex, statsHex);
            Assert.AreEqual(combatHex, settings.GetColorHex(LDMLogCategory.Stats | LDMLogCategory.Combat));
        }

        [Test]
        public void GetColorHex_WhiteForNone() => Assert.AreEqual("FFFFFF", settings.GetColorHex(LDMLogCategory.None));
        #endregion
    }
}

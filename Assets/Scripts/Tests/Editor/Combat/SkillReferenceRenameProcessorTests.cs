using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using LowDefMustard.Utils.Tests;
using Frankie.Combat;
using Frankie.Combat.Editor;
using Object = UnityEngine.Object;

namespace Frankie.Tests.Editor
{
    public class SkillReferenceRenameProcessorTests
    {
        // Tested:
        //  - Renaming a skill asset re-points [SkillReference] names to the new name (end to end, through AssetDatabase.RenameAsset)
        //  - Fields covered - pool entry, BattleAIPriority list element, SkillBranch slot;  other strings w/ the same value left alone
        //  - Old name still used by another skill - references left as they are
        //  - Moving a skill (same name) - nothing changes
        //  - Owner types (assets searched) - skill pools, BattleAI priorities, skill branches;  fields nested in serializable list entries
        // Not Tested:
        //  - Prefabs holding [SkillReference] fields - none today (components can't be found by type ~ all prefabs loaded when one exists)
        //  - [SerializeReference] fields - skipped (held type only known per instance;  none in use)

        // Const Tunables
        private const string _mainFolder = "Main";
        private const string _otherFolder = "Other";
        private const string _oldName = "SkillReferenceRenameProcessorTests_Old";
        private const string _newName = "SkillReferenceRenameProcessorTests_New";

        // Data Structures
        private class Host : ScriptableObject
        {
            [SkillReference] public string skillReference;
            public string otherName;
        }

        [Serializable]
        private class NestedEntry
        {
            [SkillReference] public string skillReference;
        }

        private class NestedHost : ScriptableObject
        {
            public List<NestedEntry> entries = new();
        }

        private class ObjectReferenceHost : ScriptableObject
        {
            public BattleAIPriority priority; // Points at another asset ~ not an owner itself
        }

        // State
        private readonly TestObjectTracker tracker = new();
        private TempAssetFolder tempFolder;

        #region Setup
        [TearDown]
        public void TearDown()
        {
            tracker.DestroyAll();
            tempFolder?.Dispose();
            tempFolder = null;
        }
        #endregion

        #region Helpers
        private static string GetString(Object target, string propertyPath)
        {
            using var serializedObject = new SerializedObject(target);
            return serializedObject.FindProperty(propertyPath).stringValue;
        }

        private static void SetPoolEntries(SkillPool skillPool, params string[] skillNames)
        {
            using var serializedObject = new SerializedObject(skillPool);
            SerializedProperty entries = serializedObject.FindProperty(SkillPool.skillProbabilityPairsField);
            entries.arraySize = skillNames.Length;
            for (int i = 0; i < skillNames.Length; i++)
            {
                entries.GetArrayElementAtIndex(i).FindPropertyRelative(SkillProbabilityPair.skillReferenceField).stringValue = skillNames[i];
                entries.GetArrayElementAtIndex(i).FindPropertyRelative(SkillProbabilityPair.probabilityWeightField).intValue = 1;
            }
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static string GetPoolEntry(SkillPool skillPool, int index) => GetString(skillPool, $"{SkillPool.skillProbabilityPairsField}.Array.data[{index}].{SkillProbabilityPair.skillReferenceField}");

        private SkillPool CreateSkillAndPool(out Skill skill)
        {
            tempFolder = new TempAssetFolder(nameof(SkillReferenceRenameProcessorTests));
            tempFolder.CreateSubfolder(_mainFolder);
            skill = tempFolder.CreateAsset<Skill>(_oldName, _mainFolder);
            var skillPool = tempFolder.CreateAsset<SkillPool>("SkillReferenceRenameProcessorTests_Pool", _mainFolder);
            SetPoolEntries(skillPool, _oldName, "SomeOtherSkill");
            AssetDatabase.SaveAssetIfDirty(skillPool);
            return skillPool;
        }

        private static IEnumerator WaitForDeferredFix()
        {
            // The processor defers through delayCall ~ let a few editor updates run
            yield return TestWait.Frames(5);
        }
        #endregion

        #region Tests
        [Test]
        public void Rename_PoolEntry_RenamedOthersKept()
        {
            SkillPool skillPool = tracker.Create<SkillPool>("SkillReferenceRenameProcessorTests_MemoryPool");
            SetPoolEntries(skillPool, _oldName, "SomeOtherSkill", _oldName);

            int updatedCount = SkillReferenceRenameProcessor.RenameSkillReferences(skillPool, _oldName, _newName);

            Assert.AreEqual(2, updatedCount);
            Assert.AreEqual(_newName, GetPoolEntry(skillPool, 0));
            Assert.AreEqual("SomeOtherSkill", GetPoolEntry(skillPool, 1));
            Assert.AreEqual(_newName, GetPoolEntry(skillPool, 2));
        }

        [Test]
        public void Rename_BattleAIPriorityElement_Renamed()
        {
            var priority = tracker.Create<BattleAIPriority>("SkillReferenceRenameProcessorTests_Priority");
            using (var serializedObject = new SerializedObject(priority))
            {
                SerializedProperty skillReferences = serializedObject.FindProperty(BattleAIPriority.skillReferencesField);
                skillReferences.arraySize = 2;
                skillReferences.GetArrayElementAtIndex(0).stringValue = "SomeOtherSkill";
                skillReferences.GetArrayElementAtIndex(1).stringValue = _oldName;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }

            SkillReferenceRenameProcessor.RenameSkillReferences(priority, _oldName, _newName);

            Assert.AreEqual("SomeOtherSkill", GetString(priority, $"{BattleAIPriority.skillReferencesField}.Array.data[0]"));
            Assert.AreEqual(_newName, GetString(priority, $"{BattleAIPriority.skillReferencesField}.Array.data[1]"));
        }

        [Test]
        public void Rename_SkillBranchSlot_Renamed()
        {
            tracker.CreateSkillTree("SkillReferenceRenameProcessorTests_Tree", out SkillBranch rootBranch);
            rootBranch.SetSkillName(SkillBranchMapping.Left, _oldName);

            SkillReferenceRenameProcessor.RenameSkillReferences(rootBranch, _oldName, _newName);

            Assert.AreEqual(_newName, GetString(rootBranch, SkillBranch.leftSkillReferenceField));
        }

        [Test]
        public void Rename_UntaggedStringWithSameValue_Kept()
        {
            var host = tracker.Create<Host>("SkillReferenceRenameProcessorTests_Host");
            host.skillReference = _oldName;
            host.otherName = _oldName;

            SkillReferenceRenameProcessor.RenameSkillReferences(host, _oldName, _newName);

            Assert.AreEqual(_newName, host.skillReference);
            Assert.AreEqual(_oldName, host.otherName);
        }

        [Test]
        public void OwnerTypes_GameTypes_SkillPoolPriorityBranch()
        {
            IEnumerable<Type> gameOwnerTypes = SkillReferenceRenameProcessor.GetOwnerTypes().Where(type => type.Namespace != typeof(SkillReferenceRenameProcessorTests).Namespace);

            CollectionAssert.AreEquivalent(new[] { typeof(SkillPool), typeof(BattleAIPriority), typeof(SkillBranch) }, gameOwnerTypes);
        }

        [Test]
        public void OwnerTypes_NestedListEntry_Found()
        {
            HashSet<Type> ownerTypes = SkillReferenceRenameProcessor.GetOwnerTypes();

            Assert.IsTrue(ownerTypes.Contains(typeof(NestedHost)));
            Assert.IsFalse(ownerTypes.Contains(typeof(ObjectReferenceHost)));
        }

        [UnityTest]
        public IEnumerator RenameAsset_Skill_PoolReferenceFollows()
        {
            SkillPool skillPool = CreateSkillAndPool(out Skill skill);
            LogAssert.Expect(LogType.Log, new Regex(Regex.Escape(_newName)));

            Assert.IsEmpty(AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(skill), _newName));
            yield return TestWait.Until(() => GetPoolEntry(skillPool, 0) == _newName);

            Assert.AreEqual(_newName, GetPoolEntry(skillPool, 0));
            Assert.AreEqual("SomeOtherSkill", GetPoolEntry(skillPool, 1));
            Assert.IsFalse(EditorUtility.IsDirty(skillPool), "saved to disk");
        }

        [UnityTest]
        public IEnumerator RenameAsset_OldNameStillUsed_ReferencesKept()
        {
            SkillPool skillPool = CreateSkillAndPool(out Skill skill);
            tempFolder.CreateSubfolder(_otherFolder);
            tempFolder.CreateAsset<Skill>(_oldName, _otherFolder); // Same name, other folder
            LogAssert.Expect(LogType.Log, new Regex("another skill is still named"));

            AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(skill), _newName);
            yield return WaitForDeferredFix();

            Assert.AreEqual(_oldName, GetPoolEntry(skillPool, 0));
        }

        [UnityTest]
        public IEnumerator MoveAsset_SameName_ReferencesKept()
        {
            SkillPool skillPool = CreateSkillAndPool(out Skill skill);
            string otherFolder = tempFolder.CreateSubfolder(_otherFolder);

            Assert.IsEmpty(AssetDatabase.MoveAsset(AssetDatabase.GetAssetPath(skill), $"{otherFolder}/{_oldName}.asset"));
            yield return WaitForDeferredFix();

            Assert.AreEqual(_oldName, GetPoolEntry(skillPool, 0));
        }
        #endregion
    }
}

#if UNITY_EDITOR
using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using LowDefMustard.Utils.Tests;
using Frankie.Combat;

namespace Frankie.Tests
{
    internal static class CombatTestBuilders
    {
        #region Skills
        public static Skill CreateSkill(this TestObjectTracker tracker, string skillName, float apCost = 0f)
        {
            var battleAction = tracker.Create<BattleAction>($"{skillName}_Action");
            SerializedFieldInjector.SetValue(battleAction, BattleAction.apCostField, apCost);
            var skill = tracker.Create<Skill>(skillName);
            SerializedFieldInjector.Set(skill, Skill.battleActionField, battleAction);
            return skill;
        }

        // Existing project skills - req'd for methods that resolve skills by name through the Skill addressables cache
        public static Skill[] LoadAddressableSkills(int count, Func<Skill, bool> match = null)
        {
            Skill[] skills = AssetDatabase.FindAssets($"t:{nameof(Skill)}")
                .Select(guid => AssetDatabase.LoadAssetAtPath<Skill>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(skill => skill != null && Skill.GetSkillFromName(skill.name) == skill && (match == null || match(skill)))
                .Take(count).ToArray();
            Assert.AreEqual(count, skills.Length, $"Needs {count} addressable project skills");
            return skills;
        }
        #endregion

        #region SkillPools
        public static SkillPool CreateSkillPool(this TestObjectTracker tracker, string poolName, params (Skill skill, int probabilityWeight)[] pairs)
        {
            var skillPool = tracker.Create<SkillPool>(poolName);
            skillPool.SetSkills(pairs);
            return skillPool;
        }

        public static void SetSkills(this SkillPool skillPool, params (Skill skill, int probabilityWeight)[] pairs)
        {
            using var serializedObject = new SerializedObject(skillPool);
            SerializedProperty skillProbabilityPairsProperty = serializedObject.FindProperty(SkillPool.skillProbabilityPairsField);
            Assert.IsNotNull(skillProbabilityPairsProperty, $"{nameof(SkillPool)} has no serialized field {SkillPool.skillProbabilityPairsField}");
            skillProbabilityPairsProperty.arraySize = pairs.Length;
            for (int i = 0; i < pairs.Length; i++)
            {
                SerializedProperty element = skillProbabilityPairsProperty.GetArrayElementAtIndex(i);
                element.FindPropertyRelative(SkillProbabilityPair.skillField).objectReferenceValue = pairs[i].skill;
                element.FindPropertyRelative(SkillProbabilityPair.probabilityWeightField).intValue = pairs[i].probabilityWeight;
            }
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
        
        public static void SetSkills(this SkillPool skillPool, params Skill[] skills) => skillPool.SetSkills(skills.Select(skill => (skill, 1)).ToArray()); // Weight 1 each
        #endregion

        #region SkillTrees
        // Branch slots hold skill names (use LoadAddressableSkills since test-created skills never resolve)
        public static SkillTree CreateSkillTree(this TestObjectTracker tracker, string treeName, out SkillBranch rootBranch)
        {
            var skillTree = tracker.Create<SkillTree>(treeName);
            rootBranch = tracker.Track(skillTree.CreateRootSkillBranchIfMissing());
            return skillTree;
        }

        public static SkillBranch CreateChildBranch(this TestObjectTracker tracker, SkillTree skillTree, SkillBranch parentBranch, SkillBranchMapping skillBranchMapping)
        {
            return tracker.Track(skillTree.CreateChildSkillBranch(parentBranch, skillBranchMapping));
        }
        
        public static void SetSkills(this SkillBranch skillBranch, Skill up = null, Skill left = null, Skill right = null, Skill down = null)
        {
            skillBranch.SetSkill(up?.name, SkillBranchMapping.Up);
            skillBranch.SetSkill(left?.name, SkillBranchMapping.Left);
            skillBranch.SetSkill(right?.name, SkillBranchMapping.Right);
            skillBranch.SetSkill(down?.name, SkillBranchMapping.Down);
        }

        // Tree building records undo steps against throwaway objects ~ call in TearDown before DestroyAll
        public static void ClearUndo(this SkillTree skillTree)
        {
            if (skillTree == null) { return; }
            foreach (SkillBranch skillBranch in skillTree.GetAllBranches()) { Undo.ClearUndo(skillBranch); }
            Undo.ClearUndo(skillTree);
        }
        #endregion

        #region Participants
        public static SkillHandler BuildSkillHandler(this TestObjectTracker tracker, string participantName, SkillSet skillSet)
        {
            // Inactive until the skill set is injected to ensure Awake / OnEnable see the configured handler
            GameObject participantObject = tracker.Track(new GameObject(participantName));
            participantObject.SetActive(false);
            participantObject.AddComponent<CombatParticipant>(); // Pulls in BaseStats ~ no character properties, so 0 AP and 0 in every stat (traits pass at tree level 0 only, unless the unlock multiplier is 0)
            var skillHandler = participantObject.AddComponent<SkillHandler>();
            SerializedFieldInjector.Set(skillHandler, SkillHandler.skillSetField, skillSet);
            participantObject.SetActive(true);
            return skillHandler;
        }
        
        public static Skill[] GetSelectionSkills(this SkillHandler skillHandler)
        {
            skillHandler.GetSkillsForCurrentSelection(out Skill up, out Skill left, out Skill right, out Skill down);
            return new[] { up, left, right, down };
        }
        #endregion

        #region BattleEvents
        // As raised when a participant commits an action
        public static void RaiseQueuedAction(CombatParticipant sender)
        {
            BattleEventBus<BattleQueueUpdatedEvent>.Raise(new BattleQueueUpdatedEvent(new BattleSequence(null, new BattleActionData(sender))));
        }
        #endregion
    }
}
#endif

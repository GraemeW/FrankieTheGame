using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Scripting.LifecycleManagement;
using LowDefMustard.Utils;
using Frankie.Combat;

namespace Frankie.Tests.Editor
{
    public class SkillCacheTests
    {
        // Tested:
        //  - GetSkillFromName - builds the cache once, later lookups reuse it (e.g. a drop target's accept check on every drag update)
        //  - Reset - next lookup rebuilds
        //  - StartBuild - async start, the next lookup completes it (one build), IsReady follows
        //  - A lookup during an Addressables scene load, before the build finishes - warns without blocking (WaitForCompletion there deadlocks builds)
        // Not Tested:
        //  - Reset on Skill import / move / delete - AddressablesCacheInvalidatorTests covers the invalidator
        // Note:  Detects builds by the cache's build log (Info)

        // Const Tunables
        private const int _repeatedLookups = 25;
        [NoAutoStaticsCleanup] private static readonly Regex _buildLog = new("Built the 'Skill' cache");
        [NoAutoStaticsCleanup] private static readonly Regex _sceneLoadReadLog = new("'Skill' cache was read during a scene load");

        #region Setup
        [SetUp]
        public void SetUp()
        {
            Skill.addressablesCache.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            AddressablesSceneLoadState.isLoadingScene = false;
        }
        #endregion

        #region Helpers
        private static Skill LoadAnySkill() => CombatTestBuilders.LoadAddressableSkills(1)[0];
        #endregion

        #region Tests
        [Test]
        public void GetSkillFromName_RepeatedLookups_BuildsOnce()
        {
            LogAssert.Expect(LogType.Log, _buildLog);
            Skill skill = LoadAnySkill();

            for (int i = 0; i < _repeatedLookups; i++) { Assert.AreSame(skill, Skill.GetSkillFromName(skill.name)); }

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Reset_NextLookup_Rebuilds()
        {
            LogAssert.Expect(LogType.Log, _buildLog);
            Skill skill = LoadAnySkill();

            Skill.addressablesCache.Reset();

            LogAssert.Expect(LogType.Log, _buildLog);
            Assert.AreSame(skill, Skill.GetSkillFromName(skill.name));
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void StartBuild_ThenLookup_CompletesOneBuild()
        {
            LogAssert.Expect(LogType.Log, _buildLog);
            Skill skill = LoadAnySkill();
            Skill.addressablesCache.Reset();

            Skill.addressablesCache.StartBuild();
            Skill.addressablesCache.StartBuild();
            LogAssert.Expect(LogType.Log, _buildLog);
            Assert.AreSame(skill, Skill.GetSkillFromName(skill.name));

            Assert.IsTrue(Skill.addressablesCache.IsReady());
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Lookup_DuringSceneLoadBeforeBuildFinishes_LogsWarningWithoutBlocking()
        {
            Skill skill = LoadAnySkill();
            Skill.addressablesCache.Reset();
            Skill.addressablesCache.StartBuild();
            if (Skill.addressablesCache.IsReady()) { Assert.Inconclusive("The Skill label load finished synchronously, so there is no in-flight build to guard"); }

            AddressablesSceneLoadState.isLoadingScene = true;
            LogAssert.Expect(LogType.Warning, _sceneLoadReadLog);
            Skill.addressablesCache.BuildIfEmpty();
            Assert.IsFalse(Skill.addressablesCache.IsReady());

            AddressablesSceneLoadState.isLoadingScene = false;
            LogAssert.Expect(LogType.Log, _buildLog);
            Assert.AreSame(skill, Skill.GetSkillFromName(skill.name));
        }
        #endregion
    }
}

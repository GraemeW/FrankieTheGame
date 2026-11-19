using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Scripting.LifecycleManagement;
using Frankie.Combat;

namespace Frankie.Tests.Editor
{
    public class SkillCacheTests
    {
        // Tested:
        //  - GetSkillFromName - builds the cache once, later lookups reuse it (e.g. a drop target's accept check on every drag update)
        //  - ResetCache - next lookup rebuilds
        // Not Tested:
        //  - Reset on Skill import / move / delete - AddressablesCacheInvalidatorTests covers the invalidator
        // Note:  Detects builds by the cache's build log (Info)

        // Const Tunables
        private const int _repeatedLookups = 25;
        [NoAutoStaticsCleanup] private static readonly Regex _buildLog = new("Built the 'Skill' cache");

        #region Setup
        [SetUp]
        public void SetUp()
        {
            Skill.ResetCache();
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
        public void ResetCache_NextLookup_Rebuilds()
        {
            LogAssert.Expect(LogType.Log, _buildLog);
            Skill skill = LoadAnySkill();

            Skill.ResetCache();

            LogAssert.Expect(LogType.Log, _buildLog);
            Assert.AreSame(skill, Skill.GetSkillFromName(skill.name));
            LogAssert.NoUnexpectedReceived();
        }
        #endregion
    }
}

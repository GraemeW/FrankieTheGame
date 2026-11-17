using System;
using System.Linq;
using NUnit.Framework;
using Frankie.Combat;

namespace Frankie.Tests.Editor
{
    public class SkillHandlerPoolSlotMappingTests
    {
        // Tested:
        //  - SkillHandler.poolSlotCount vs SkillBranchMapping - one slot per direction, values contiguous from 0

        #region Tests
        [Test]
        public void PoolSlotCount_MatchesSkillBranchMappingValues_ContiguousFromZero()
        {
            int[] mappingValues = Enum.GetValues(typeof(SkillBranchMapping)).Cast<int>().OrderBy(value => value).ToArray();

            CollectionAssert.AreEqual(Enumerable.Range(0, SkillHandler.poolSlotCount), mappingValues);
        }
        #endregion
    }
}

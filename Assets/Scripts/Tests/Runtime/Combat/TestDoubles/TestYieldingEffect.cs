#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using LowDefMustard.Utils.Tests;
using Frankie.Combat;

namespace Frankie.Tests
{
    // Test double for EffectStrategy -- counts starts / completions, and yields for `framesToYield` frames between them

    public sealed class TestYieldingEffect : EffectStrategy
    {
        public int framesToYield = 1;
        public int startedCount = 0;
        public int completedCount = 0;

        public override IEnumerator StartEffect(CombatParticipant sender, IList<BattleEntity> recipients, DamageType damageType)
        {
            startedCount++;
            yield return TestWait.Frames(framesToYield);
            completedCount++;
        }
    }
}
#endif

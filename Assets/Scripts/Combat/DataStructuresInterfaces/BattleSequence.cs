using Frankie.Inventory;
using System.Collections.Generic;

namespace Frankie.Combat
{
    public struct BattleSequence
    {
        public IBattleActionUser battleAction;
        public CombatParticipant sender;
        public IEnumerable<CombatParticipant> recipients;
    }
}
using Frankie.Inventory;

namespace Frankie.Combat
{
    public struct BattleSequence
    {
        public BattleAction battleAction;
        public CombatParticipant sender;
        public CombatParticipant recipient;
    }
}
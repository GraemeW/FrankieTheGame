using Frankie.Inventory;

namespace Frankie.Combat
{
    public struct BattleSequence
    {
        public BattleActionType battleActionType;
        public CombatParticipant sender;
        public CombatParticipant recipient;
        public Skill skill;
        public ActionItem actionItem;
    }
}
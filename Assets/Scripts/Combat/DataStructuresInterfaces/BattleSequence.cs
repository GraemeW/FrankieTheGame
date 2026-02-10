namespace Frankie.Combat
{
    public struct BattleSequence
    {
        public readonly IBattleActionSuper battleActionSuper;
        public readonly BattleActionData battleActionData;

        public BattleSequence(IBattleActionSuper battleActionSuper, BattleActionData battleActionData)
        {
            this.battleActionSuper = battleActionSuper;
            this.battleActionData = battleActionData;
        }
    }
}

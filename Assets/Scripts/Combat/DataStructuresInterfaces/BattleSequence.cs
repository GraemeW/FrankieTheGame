namespace Frankie.Combat
{
    public struct BattleSequence
    {
        public IBattleActionSuper battleActionSuper;
        public BattleActionData battleActionData;

        public BattleSequence(IBattleActionSuper battleActionSuper, BattleActionData battleActionData)
        {
            this.battleActionSuper = battleActionSuper;
            this.battleActionData = battleActionData;
        }
    }
}

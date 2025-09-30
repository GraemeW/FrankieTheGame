namespace Frankie.Combat
{
    public struct BattleEntityRemovedFromBoardEvent : IBattleEvent
    {
        public BattleEventType battleEventType => BattleEventType.BattleEntityRemovedFromBoard;

        public BattleEntity battleEntity;
        public int row;
        public int column;

        public BattleEntityRemovedFromBoardEvent(BattleEntity battleEntity, int row, int column)
        {
            this.battleEntity = battleEntity;
            this.row = row;
            this.column = column;
        }
    }
}

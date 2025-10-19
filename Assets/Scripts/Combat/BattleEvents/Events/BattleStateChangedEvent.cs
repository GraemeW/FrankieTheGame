using System.Collections.Generic;

namespace Frankie.Combat
{
    public class BattleStateChangedEvent : IBattleEvent
    {
        public BattleEventType battleEventType => BattleEventType.BattleStateChanged;

        public BattleState battleState { get; private set; }
        public BattleOutcome battleOutcome { get; private set; }
        public IList<BattleEntity> characters { get; private set; }
        public IList<BattleEntity> enemies { get; private set; }

        public BattleStateChangedEvent(BattleState battleState, BattleOutcome battleOutcome, IList<BattleEntity> characters, IList<BattleEntity> enemies)
        {
            this.battleState = battleState;
            this.battleOutcome = battleOutcome;
            this.characters = characters;
            this.enemies = enemies;
        }
    }
}

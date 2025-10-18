using System.Collections.Generic;

namespace Frankie.Combat
{
    public class BattleStateChangedEvent : IBattleEvent
    {
        public BattleEventType battleEventType => BattleEventType.BattleStateChanged;

        public BattleState battleState { get; private set; }
        public BattleOutcome battleOutcome { get; private set; }
        public List<BattleEntity> characters { get; private set; }
        public List<BattleEntity> enemies { get; private set; }

        public BattleStateChangedEvent(BattleState battleState, BattleOutcome battleOutcome, List<BattleEntity> characters, List<BattleEntity> enemies)
        {
            this.battleState = battleState;
            this.battleOutcome = battleOutcome;
            this.characters = characters;
            this.enemies = enemies;
        }
    }
}

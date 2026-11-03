using System.Collections.Generic;
using Frankie.Zones;
using LowDefMustard.Core;

namespace Frankie.Combat
{
    public struct BattleStagingEvent : IBattleEvent
    {
        public BattleEventType battleEventType => BattleEventType.BattleStaging;

        public BattleStagingType battleStagingType { get; private set; }
        public bool optionalParametersSet { get; private set; }
        private readonly IList<BattleEntity> playerEntities;
        private readonly IList<BattleEntity> enemyEntities;
        private readonly TransitionType transitionType;

        public BattleStagingEvent(BattleStagingType battleStagingType, IList<BattleEntity> playerEntities, IList<BattleEntity> enemyEntities, TransitionType transitionType)
        {
            optionalParametersSet = true;
            this.battleStagingType = battleStagingType;
            this.playerEntities = playerEntities;
            this.enemyEntities = enemyEntities;
            this.transitionType = transitionType;
        }

        public BattleStagingEvent(BattleStagingType battleStagingType)
        {
            optionalParametersSet = false;
            this.battleStagingType = battleStagingType;
            playerEntities = new List<BattleEntity>();
            enemyEntities = new List<BattleEntity>();
            transitionType = TransitionType.None;
        }

        public IList<BattleEntity> GetPlayerEntities()
        {
            if (!optionalParametersSet) { LDMLog.Warning(LDMLogCategory.Combat, $"Optional {nameof(BattleStagingEvent)} parameters read before they were set. Returning their defaults."); }
            return playerEntities;
        }

        public IList<BattleEntity> GetEnemyEntities()
        {
            if (!optionalParametersSet) { LDMLog.Warning(LDMLogCategory.Combat, $"Optional {nameof(BattleStagingEvent)} parameters read before they were set. Returning their defaults."); }
            return enemyEntities;
        }

        public TransitionType GetTransitionType()
        {
            if (!optionalParametersSet) { LDMLog.Warning(LDMLogCategory.Combat, $"Optional {nameof(BattleStagingEvent)} parameters read before they were set. Returning their defaults."); }
            return transitionType;
        }
    }
}

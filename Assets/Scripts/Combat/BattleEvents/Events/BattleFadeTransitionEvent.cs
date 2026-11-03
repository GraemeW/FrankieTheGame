using System.Collections.Generic;
using Frankie.Zones;
using LowDefMustard.Core;

namespace Frankie.Combat
{
    public struct BattleFadeTransitionEvent : IBattleEvent
    {
        public BattleEventType battleEventType => BattleEventType.BattleFadeTransition;
        public BattleFadePhase fadePhase { get; private set; }
        public readonly bool optionalParametersSet;
        private readonly IList<CombatParticipant> enemies;
        private readonly TransitionType transitionType;

        public BattleFadeTransitionEvent(BattleFadePhase fadePhase, List<CombatParticipant> enemies, TransitionType transitionType)
        {
            optionalParametersSet = true;
            this.fadePhase = fadePhase;
            this.enemies = enemies;
            this.transitionType = transitionType;
        }

        public BattleFadeTransitionEvent(BattleFadePhase fadePhase)
        {
            optionalParametersSet = false;
            enemies = new List<CombatParticipant>();
            this.fadePhase = fadePhase;
            transitionType = TransitionType.None;
        }

        public IList<CombatParticipant> GetEnemies()
        {
            if (!optionalParametersSet) { LDMLog.Warning(LDMLogCategory.Combat, $"Optional {nameof(BattleFadeTransitionEvent)} parameters read before they were set. Returning their defaults."); }
            return enemies;
        }

        public TransitionType GetTransitionType()
        {
            if (!optionalParametersSet) { LDMLog.Warning(LDMLogCategory.Combat, $"Optional {nameof(BattleFadeTransitionEvent)} parameters read before they were set. Returning their defaults."); }
            return transitionType;
        }
    }
}

using System.Collections.Generic;
using Frankie.ZoneManagement;

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
            if (!optionalParametersSet) { UnityEngine.Debug.Log("Warning:  Accessing optional event parameters when they have not been set!");}
            return enemies;
        }

        public TransitionType GetTransitionType()
        {
            if (!optionalParametersSet) { UnityEngine.Debug.Log("Warning:  Accessing optional event parameters when they have not been set!");}
            return transitionType;
        }
    }
}

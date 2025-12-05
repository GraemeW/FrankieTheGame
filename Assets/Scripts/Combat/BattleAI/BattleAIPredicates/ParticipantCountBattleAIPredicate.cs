using System.Linq;
using UnityEngine;

namespace Frankie.Combat
{
    [CreateAssetMenu(fileName = "New Participant Count Predicate", menuName = "BattleAI/Predicates/ParticipantCount")]
    public class ParticipantCountBattleAIPredicate : BattleAIPredicate
    {
        [SerializeField] private bool countLocalAllies = true;
        [SerializeField] private bool countLocalFoes = false;
        [SerializeField][Tooltip("Including the caller, >= value is true")][Min(0)] private int countThreshold = 2;

        public override bool? Evaluate(BattleAI battleAI)
        {
            countThreshold = Mathf.Max(countThreshold, 0);
            
            int participantCount = 0;
            if (countLocalAllies) { participantCount += battleAI.GetLocalAllies().Count(x => !x.combatParticipant.IsDead()); }
            if (countLocalFoes) { participantCount += battleAI.GetLocalFoes().Count(x => !x.combatParticipant.IsDead()); }

            return participantCount >= countThreshold;
        }
    }
}

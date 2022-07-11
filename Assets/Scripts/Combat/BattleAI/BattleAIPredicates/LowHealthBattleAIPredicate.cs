using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Combat
{
    [CreateAssetMenu(fileName = "New Low Health Predicate", menuName = "BattleAI/Predicates/LowHealth")]
    public class LowHealthBattleAIPredicate : BattleAIPredicate
    {
        [SerializeField][Tooltip("Enable for allies, disable for foes")] bool checkAllies = true;
        [SerializeField] float minHP = 30f;
        [SerializeField][Tooltip("Enable:  all party members must hit minHP criteria")] bool requireAllPartyMembers = false;

        public override bool? Evaluate(BattleAI battleAI)
        {
            bool criteriaMet = false;
            bool partyCriteria = true;
            List<CombatParticipant> checkParticipants = checkAllies ? battleAI.GetLocalAllies() : battleAI.GetLocalFoes();

            foreach (CombatParticipant combatParticipant in checkParticipants)
            {
                if (combatParticipant.GetHP() <= minHP)
                {
                    if (!requireAllPartyMembers) { criteriaMet = true; break; }
                }
                else
                {
                    partyCriteria = false;
                }
            }
            if (requireAllPartyMembers && partyCriteria) { criteriaMet = true; }

            return criteriaMet;
        }
    }
}

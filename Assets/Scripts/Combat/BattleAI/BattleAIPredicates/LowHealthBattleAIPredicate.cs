using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Combat
{
    [CreateAssetMenu(fileName = "New Low Health Predicate", menuName = "BattleAI/Predicates/LowHealth")]
    public class LowHealthBattleAIPredicate : BattleAIPredicate
    {
        [SerializeField][Tooltip("Enable for allies, disable for foes")] private bool checkAllies = true;
        [SerializeField] private float minHP = 30f;
        [SerializeField][Tooltip("Enable:  all party members must hit minHP criteria")] private bool requireAllPartyMembers = false;

        public override bool? Evaluate(BattleAI battleAI)
        {
            bool criteriaMet = false;
            bool partyCriteria = true;
            List<BattleEntity> checkParticipants = checkAllies ? battleAI.GetLocalAllies() : battleAI.GetLocalFoes();

            foreach (BattleEntity battleEntity in checkParticipants)
            {
                if (battleEntity.combatParticipant.GetHP() <= minHP)
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

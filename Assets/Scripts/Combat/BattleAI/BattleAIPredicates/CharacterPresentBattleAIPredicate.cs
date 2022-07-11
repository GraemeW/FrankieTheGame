using Frankie.Stats;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Combat
{
    [CreateAssetMenu(fileName = "New Character Present Predicate", menuName = "BattleAI/Predicates/CharacterPresent")]
    public class CharacterPresentBattleAIPredicate : BattleAIPredicate
    {
        [SerializeField] CharacterProperties characterProperties = null;

        public override bool? Evaluate(BattleAI battleAI)
        {
            if (characterProperties == null) { return false; }

            foreach (CombatParticipant combatParticipant in battleAI.GetLocalAllies())
            {
                if (combatParticipant.GetCharacterProperties() == characterProperties) { return true; }
            }
            foreach (CombatParticipant combatParticipant in battleAI.GetLocalFoes())
            {
                if (combatParticipant.GetCharacterProperties() == characterProperties) { return true; }
            }

            return false;
        }
    }
}

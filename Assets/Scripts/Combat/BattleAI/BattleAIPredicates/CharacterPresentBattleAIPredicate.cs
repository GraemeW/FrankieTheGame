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

            foreach (BattleEntity combatParticipant in battleAI.GetLocalAllies())
            {
                if (combatParticipant.combatParticipant.GetCharacterProperties() == characterProperties) { return true; }
            }
            foreach (BattleEntity combatParticipant in battleAI.GetLocalFoes())
            {
                if (combatParticipant.combatParticipant.GetCharacterProperties() == characterProperties) { return true; }
            }

            return false;
        }
    }
}

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

            foreach (BattleEntity battleEntity in battleAI.GetLocalAllies())
            {
                CombatParticipant combatParticipant = battleEntity.combatParticipant;
                if (combatParticipant.GetCharacterProperties().GetCharacterNameID() == characterProperties.GetCharacterNameID()) { return true; }
            }
            foreach (BattleEntity battleEntity in battleAI.GetLocalFoes())
            {
                CombatParticipant combatParticipant = battleEntity.combatParticipant;
                if (combatParticipant.GetCharacterProperties().GetCharacterNameID() == characterProperties.GetCharacterNameID()) { return true; }
            }

            return false;
        }
    }
}

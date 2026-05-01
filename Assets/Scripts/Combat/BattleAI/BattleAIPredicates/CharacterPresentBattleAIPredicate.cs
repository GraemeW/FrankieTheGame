using Frankie.Stats;
using UnityEngine;

namespace Frankie.Combat
{
    [CreateAssetMenu(fileName = "New Character Present Predicate", menuName = "BattleAI/Predicates/CharacterPresent")]
    public class CharacterPresentBattleAIPredicate : BattleAIPredicate
    {
        [SerializeField] private CharacterProperties characterProperties;

        public override bool? Evaluate(BattleAI battleAI)
        {
            if (characterProperties == null) { return false; }

            foreach (BattleEntity battleEntity in battleAI.GetLocalAllies())
            {
                CombatParticipant combatParticipant = battleEntity.combatParticipant;
                if (CharacterProperties.AreCharacterPropertiesMatched(characterProperties, combatParticipant.GetCharacterProperties())) { return !combatParticipant.IsDead(); }
            }
            foreach (BattleEntity battleEntity in battleAI.GetLocalFoes())
            {
                CombatParticipant combatParticipant = battleEntity.combatParticipant;
                if (CharacterProperties.AreCharacterPropertiesMatched(characterProperties, combatParticipant.GetCharacterProperties())) { return !combatParticipant.IsDead();}
            }
            
            return false;
        }
    }
}

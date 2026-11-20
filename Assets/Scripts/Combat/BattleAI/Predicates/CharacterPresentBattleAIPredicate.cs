using Frankie.Stats;
using UnityEngine;

namespace Frankie.Combat
{
    [CreateAssetMenu(fileName = "New Character Present Predicate", menuName = "Combat/BattleAI/Predicates/CharacterPresent", order = 6)]
    public class CharacterPresentBattleAIPredicate : BattleAIPredicate
    {
        // Tunables
        [SerializeField] private CharacterProperties characterProperties;
        
        // Editor Field References
        internal const string characterPropertiesField = nameof(characterProperties);

        // Public Methods
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

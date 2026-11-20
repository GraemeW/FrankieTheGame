using System.Linq;
using UnityEngine;
using Frankie.Stats;

namespace Frankie.Combat
{
    [CreateAssetMenu(fileName = "New Specific Character Target Priority", menuName = "Combat/BattleAI/TargetPriority/SpecificCharacter", order = 7)]
    public class TargetSpecificCharacter : TargetPriority
    {
        // Tunables
        [SerializeField] private CharacterProperties characterProperties;
        
        // Editor Field References
        internal const string characterPropertiesField = nameof(characterProperties);

        // Public Methods
        public override bool SetTarget(BattleAI battleAI, BattleActionData battleActionData, Skill skill)
        {
            if (characterProperties == null) { return false; }

            foreach (BattleEntity battleEntity in battleAI.GetLocalAllies().Where(battleEntity => CharacterProperties.AreCharacterPropertiesMatched(battleEntity.combatParticipant.GetCharacterProperties(), characterProperties)))
            {
                battleActionData.SetTargets(battleEntity);
                skill.SetTargets(TargetingNavigationType.Hold, battleActionData, battleAI.GetLocalAllies(), battleAI.GetLocalFoes());
                return true;
            }
            foreach (BattleEntity battleEntity in battleAI.GetLocalFoes().Where(battleEntity => CharacterProperties.AreCharacterPropertiesMatched(battleEntity.combatParticipant.GetCharacterProperties(), characterProperties)))
            {
                battleActionData.SetTargets(battleEntity);
                skill.SetTargets(TargetingNavigationType.Hold, battleActionData, battleAI.GetLocalAllies(), battleAI.GetLocalFoes());
                return true;
            }
            return false;
        }
    }
}

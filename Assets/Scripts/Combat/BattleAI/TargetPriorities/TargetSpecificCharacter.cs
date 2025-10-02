using UnityEngine;
using Frankie.Stats;

namespace Frankie.Combat
{
    [CreateAssetMenu(fileName = "New Specific Character Target Priority", menuName = "BattleAI/TargetPriority/SpecificCharacter")]
    public class TargetSpecificCharacter : TargetPriority
    {
        [SerializeField] CharacterProperties characterProperties = null;

        public override bool SetTarget(BattleAI battleAI, BattleActionData battleActionData, Skill skill)
        {
            if (characterProperties == null) { return false; }

            foreach (BattleEntity battleEntity in battleAI.GetLocalAllies())
            {
                if (battleEntity.combatParticipant.GetCharacterProperties() == characterProperties)
                {
                    battleActionData.SetTargets(battleEntity);
                    skill.GetTargets(null, battleActionData, battleAI.GetLocalAllies(), battleAI.GetLocalFoes());
                    return true;
                }
            }
            foreach (BattleEntity battleEntity in battleAI.GetLocalFoes())
            {
                if (battleEntity.combatParticipant.GetCharacterProperties() == characterProperties)
                {
                    battleActionData.SetTargets(battleEntity);
                    skill.GetTargets(null, battleActionData, battleAI.GetLocalAllies(), battleAI.GetLocalFoes());
                    return true;
                }
            }

            return false;
        }
    }
}

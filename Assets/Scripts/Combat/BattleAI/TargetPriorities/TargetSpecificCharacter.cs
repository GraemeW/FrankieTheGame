using Frankie.Stats;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Combat
{
    [CreateAssetMenu(fileName = "New Specific Character Target Priority", menuName = "BattleAI/TargetPriority/SpecificCharacter")]
    public class TargetSpecificCharacter : TargetPriority
    {
        [SerializeField] CharacterProperties characterProperties = null;

        public override bool SetTarget(BattleAI battleAI, BattleActionData battleActionData, Skill skill)
        {
            if (characterProperties == null) { return false; }

            foreach (CombatParticipant combatParticipant in battleAI.GetLocalAllies())
            {
                if (combatParticipant.GetCharacterProperties() == characterProperties)
                {
                    battleActionData.SetTargets(combatParticipant);
                    skill.GetTargets(null, battleActionData, battleAI.GetLocalAllies(), battleAI.GetLocalFoes());
                    return true;
                }
            }
            foreach (CombatParticipant combatParticipant in battleAI.GetLocalFoes())
            {
                if (combatParticipant.GetCharacterProperties() == characterProperties)
                {
                    battleActionData.SetTargets(combatParticipant);
                    skill.GetTargets(null, battleActionData, battleAI.GetLocalAllies(), battleAI.GetLocalFoes());
                    return true;
                }
            }

            return false;
        }
    }
}

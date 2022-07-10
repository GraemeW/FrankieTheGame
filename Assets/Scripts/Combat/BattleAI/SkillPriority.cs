using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Combat
{
    public abstract class SkillPriority : ScriptableObject
    {
        public abstract Skill GetSkill(SkillHandler skillHandler, List<Skill> skillsToExclude, float probabilityToTraverseSkillTree);
    }
}

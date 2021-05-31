using Frankie.Inventory;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Combat
{
    public struct BattleAction
    {
        public BattleActionType battleActionType { get; }
        public Skill skill { get; }
        public ActionItem actionItem { get; }
        public string name { get; }
        public bool isFriendly { get; }

        public static BattleAction None { get; } = new BattleAction();

        public BattleAction(Skill skill)
        {
            battleActionType = BattleActionType.Skill;
            this.skill = skill;
            this.name = Skill.GetSkillNamePretty(skill.name);
            this.actionItem = null;
            this.isFriendly = skill.IsFriendly();
        }

        public BattleAction(ActionItem actionItem)
        {
            battleActionType = BattleActionType.ActionItem;
            this.actionItem = actionItem;
            this.name = actionItem.GetDisplayName();
            this.skill = null;
            this.isFriendly = actionItem.IsFriendly();
        }
    }
}

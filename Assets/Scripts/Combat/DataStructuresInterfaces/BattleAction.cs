using Frankie.Inventory;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Combat
{
    public class BattleAction
    {
        public BattleActionType battleActionType { get; } = BattleActionType.None;
        public Skill skill { get; } = null;
        public ActionItem actionItem { get; } = null;
        public string name { get; } = null;

        public BattleAction(Skill skill)
        {
            battleActionType = BattleActionType.Skill;
            this.skill = skill;
            this.name = Skill.GetSkillNamePretty(skill.name);
            this.actionItem = null;
        }

        public BattleAction(ActionItem actionItem)
        {
            battleActionType = BattleActionType.ActionItem;
            this.actionItem = actionItem;
            this.name = InventoryItem.GetItemNamePretty(actionItem.name);
            this.skill = null;
        }

    }

}

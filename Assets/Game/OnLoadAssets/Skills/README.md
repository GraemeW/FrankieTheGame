# Assets:  Game - Skills

Skills are the primary actions a character may take in combat.  They may also be used in-world via the Abilities UI menu.  They are effectively an encapsulation of [Battle Actions](../BattleActions/), with several additional properties.  See, for example, the `Instill Self Doubt` skill below:

<img src="../../../../InfoTools/Documentation/Game/OnLoadAssets/Skills/SkillExample.png" width="400">

A high-level summary on Skills, Battle Actions and their overall role in the combat system is described in [Game/Combat](../../Combat/).

## Skills: Quick Start Guide

### Make the Skill

1. Navigate to this [Skills](./) directory (or any sub-directories within)
2. Right click and select `Create` -> `Skills` -> `New Skill`

<img src="../../../../InfoTools/Documentation/Game/OnLoadAssets/Skills/NewSkillMenu.png" width="500">

### Configure the Skill

Set:
* Battle Action: to the paired battle action for the skill
  * See [Battle Actions](../BattleActions/) in [OnLoadAssets](../) for further detail on Battle Action construction
* Stat: to the relevant character stat for the skill
* Detail: to the desired flavour text for the skill, which will appear in the in-game UI, as below

<img src="../../../../InfoTools/Documentation/Game/OnLoadAssets/Skills/SkillDetailUI.png" width="500">

## Skill - Stat Relevance

As described in [Game/Combat](../../Combat/), a skill's [Stat](../../../Scripts/Stats/Stat.cs) parameter plays a key role in gating access to the next skill node in the character's [SkillTree](../../Combat/SkillTrees/).

Access to a given skill node in the skill tree requires that `stat > n * statRequirement`, where:
* `n` : refers to the node depth in the skill tree
* `statRequirement` : is a pre-defined variable configured on the [character](../../CharacterObjects/)
  * , as part of its [SkillHandler](../../../Scripts/Combat/Skills/SkillHandler.cs) component

As an example, see an example skill tree, where `statRequirement = 10`:

<img src="../../../../InfoTools/Documentation/Game/OnLoadAssets/Skills/SkillTreeStatGates.png" width="700">

From the root/center node, n = 1:
* At a `Luck` stat `1 * 10 = 10`, the character gains access to the skill `GentlePraise`
  * The node north of root node (`n=2`) contains the skills `SmileMenacingly` and `SpillHotCoffee`
* At a `Beauty` stat of `2 * 10 = 20`, the character gains access to `SmileMenacingly`
  * The node west of this node (`n=3`) contains the skills `WordsOfEncouragement` and `BlatherOn`
* At a `Smarts` stat of `3 * 10 = 30`, the character gains access to `WordsOfEncouragement`

As seen on the above skill tree, skill stats **only** unlock new branches if such branches actually exist.  Or: a skill may be present, the character may have sufficient stat to unlock a new node, but the tree may have reached a terminal node position.

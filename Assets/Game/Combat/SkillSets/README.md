# Assets:  Game - Skill Sets

Skill sets are defined per-[character](../../CharacterObjects/), granting each character access to a unique set of [skills](../../OnLoadAssets/).  A character's Skill Handler takes either kind:
* **Skill trees** ([SkillTrees-Characters](./SkillTrees-Characters/)):  (generally) for playable characters - a tree of interconnected skill nodes, with up to four [skills](../../OnLoadAssets/Skills/) per node
* **Skill pools** ([SkillPools-NPCs](./SkillPools-NPCs/)):  (generally) for NPCs - a flat list of skills, each with a probability weight

Above are not strict requirements, as an NPC's BattleAI can still parse a tree, and a playable character can still use a pool (see [below](#skill-pools--quick-start-guide) for detail).

Both trees and pools are built in the **Skill Set Editor** (`Window` -> `Skill Set Editor`, or double click a skill tree / skill pool).  Skills can be typed into a skill field (w/ autocomplete), or dragged in from the editor's skills panel.

Trees and pools both store skills by name, looked up at runtime through the `Skill` addressables group ([OnLoadAssets/Skills](../../OnLoadAssets/Skills/)), so:
* only skills in this addressables group can be added (others are refused)
* renaming a skill (in the Project window) updates every tree, pool and BattleAI priority that uses it ~ the Console lists what changed
  * note that only assets under `Assets/` are updated (never scenes or packages)

On - Why names rather than direct references? This addresses a constant risk with runtime vs. project/editor SOs.  Skill sets (and BattleAI priorities) aren't addressable, while skills are.  A direct reference from a non-addressable asset bundles its own copy of the skill in builds, so the same skill could exist as two different objects (e.g. a priority's skill never matching the tree's).  Looking skills up by name always returns the one "true" addressable copy.

See for example the test skill tree below:

<img src="../../../../InfoTools/Documentation/Game/Combat/SkillTreeExample.png" width="800">

For more detail on:
* combat system & skill tree navigation -- see [Game/Combat](../)
* skills & how skill/stats impact branch access -- see [Game/OnLoadAssets/Skills](../../OnLoadAssets/Skills/)
* battle actions that define skill behaviour -- see [Game/OnLoadAssets/BattleActions](../../OnLoadAssets/BattleActions/)

## Skill Trees: Quick Start Guide

### Make the Skill Tree

1. Navigate to the [SkillTrees-Characters](./SkillTrees-Characters/) directory
2. Right click and select `Create` -> `Combat` -> `Skills` -> `New Skill Tree`

<img src="../../../../InfoTools/Documentation/Game/Combat/NewSkillTreeMenu.png" width="500">

### Configure the Skill Tree

1. Rename the new skill tree & double click on it, which will:
   * open the Skill Set Editor
   * create the root node of the new skill tree
2. Add skills to the root node
   * simply type the name of skills in the relevant directional field, or drag a skill in from the skills panel
   * if no skill matches the typed name, it will revert to no skill
3. Add branches from the root node
   * click on the + button beside any existing skill
4. Add skills to the branched nodes
5. Add more branches to the branched nodes
6. . . . and so on . . . 

As usual, **cmd+s** or **ctrl+s** to save the asset -- this will also save all of the sub-nodes under the main tree asset.

### Example Tree Construction

For example, consider a fresh tree below:

<img src="../../../../InfoTools/Documentation/Game/Combat/SkillTreeFresh.png" width="400">

Let's add the `Bite` skill to the `left` direction:

<img src="../../../../InfoTools/Documentation/Game/Combat/SkillTreeAddSkill.png" width="400">

Now let's branch to the left:

<img src="../../../../InfoTools/Documentation/Game/Combat/SkillTreeAddBranch.png" width="600">

Let's add a few more skills:
* `SqueakSqueak` on the `right` direction of the root node
* `Neigh` on the `down` direction of the branched node
  
<img src="../../../../InfoTools/Documentation/Game/Combat/SkillTreeAddMoreSkills.png" width="600">

Now let's branch on both of these new skills:

<img src="../../../../InfoTools/Documentation/Game/Combat/SkillTreeAddMoreBranches.png" width="800">

And onward:

<img src="../../../../InfoTools/Documentation/Game/Combat/SkillTreeOnward.png" width="1000">

### Standard Root / Branch Configuration Guidance

The below guidance should be followed in order to make it simpler for new players to catch onto the battle system.  The intent is to make selection directions relatively intuitive with some amount of muscle memory. 

#### Root Nodes

The following paradigm should be followed for root nodes:
* **Up**:  Primary Stat - Brawn
  * Map to a physical damage skill - this should usually be [Wallop](../../OnLoadAssets/Skills/General/Wallop.asset)
* **Right**:  Primary Stat - Smarts
  * Map to friendly buff or heal skill
* **Left**:  Primary Stat - Luck
  * Map to enemy debuff or utility skill
* **Down**:  Primary Stat - Beauty 
  * Map to magical damage skill

See [Stats.cs](../../../Scripts/Stats/Stat.cs) for Stat Enum details.

#### Branch Nodes

The following logic should be applied when branching:
* **Up**:  Increase base amount of the skill
  * e.g. increase the amount of damage dealt, the amount healed, the stat amount buffed, etc.
* **Right**:  Modify the base skill by adjusting crit multiplier, cooldown, length applied, etc.
* **Left**:  Add new capability or utility to the skill
  * e.g. convert a damage skill to life-leach or AP-leach, slow down enemy, add DoT, etc.
* **Down**:  Adjust the targeting of the skill
  * e.g. convert single target to column target to row target to all target

## Skill Pools:  Quick Start Guide

The much simpler skill pools are generally intended for NPCs:  the battle AI picks from the pool by weight, rather than navigating a tree.  A playable character can still be given a pool (i.e. instead of a tree), and they will get four random skills from it, re-rolled after each of their actions.  

### Make the Skill Pool

1. Navigate to the [SkillPools-NPCs](./SkillPools-NPCs/) directory
2. Right click and select `Create` -> `Combat` -> `Skills` -> `New Skill Pool`

### Configure the Skill Pool

1. Rename the new skill pool & double click on it to open the Skill Set Editor
   * the pool shows as a single card, with one row per entry
2. Click `+` to add an entry, then set its skill (type the name, or drag a skill in from the skills panel)
3. Set each entry's probability weight
   * the share beside it shows how often that skill is picked (e.g. weights 1 and 3 -> 25% and 75%)
   * entries with no skill, or a weight of 0, are flagged `never picked`
4. Click `-` on a row to remove that entry

For example, see below:

<img src="../../../../InfoTools/Documentation/Game/Combat/SkillPoolExample.png" width="600">

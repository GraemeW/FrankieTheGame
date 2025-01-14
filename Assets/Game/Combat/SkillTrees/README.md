# Assets:  Game - Skill Trees

Skill trees are defined per-[character](../../CharacterObjects/), granting each character access to a unique set of [skills](../../OnLoadAssets/).  They comprise a tree of interconnected skill nodes, with up to four [skills](../../OnLoadAssets/Skills/) per node.  

See for example the test skill tree below:

<img src="../../../../InfoTools/Documentation/Game/Combat/SkillTreeExample.png" width="800">

For more detail on:
* combat system & skill tree navigation -- see [Game/Combat](../)
* skills & how skill/stats impact branch access -- see [Game/OnLoadAssets/Skills](../../OnLoadAssets/Skills/)
* battle actions that define skill behaviour -- see [Game/OnLoadAssets/BattleActions](../../OnLoadAssets/BattleActions/)

## Skill Trees: Quick Start Guide

### Make the Skill Tree

1. Navigate to this [SkillTrees](./) directory
   * , then to [Characters](./Characters/): for playable character trees
   * , or to [NPCs](./NPCs/): for non-playable character trees
2. Right click and select `Create` -> `Skills` -> `New Skill Tree`

<img src="../../../../InfoTools/Documentation/Game/Combat/NewSkillTreeMenu.png" width="500">

### Configure the Skill Tree

1. Rename the new skill tree & double click on it, which will:
   * open the skill tree editor
   * create the root node of the new skill tree
2. Add skills to the root node
   * simply type the name of skills in the relevant directional field
   * if no skill matches the typed name, it will rever to 'No Skill'
3. Add branches from the root node
   * click on the + button beside any existing skill
4. Add skills to the branched nodes
5. Add more branches to the branched nodes
6. . . . and so on . . . 

As usual, **cmd+s** or **ctrl+s** to save the asset -- this will also save all of the sub-nodes under the main tree asset.

### Example Tree Construction

For example, consider a fresh tree below:

<img src="../../../../InfoTools/Documentation/Game/Combat/SkillTreeFresh.png" width="250">

Let's add the `Bite` skill to the `left` direction:

<img src="../../../../InfoTools/Documentation/Game/Combat/SkillTreeAddSkill.png" width="250">

Now let's branch to the left:

<img src="../../../../InfoTools/Documentation/Game/Combat/SkillTreeAddBranch.png" width="500">

Let's add a few more skills:
* `SqueakSqueak` on the `right` direction of the root node
* `Neigh` on the `down` direction of the branched node
  
<img src="../../../../InfoTools/Documentation/Game/Combat/SkillTreeAddMoreSkills.png" width="500">

Now let's branch on both of these new skills:

<img src="../../../../InfoTools/Documentation/Game/Combat/SkillTreeAddMoreBranches.png" width="800">

And onward:

<img src="../../../../InfoTools/Documentation/Game/Combat/SkillTreeOnward.png" width="1000">

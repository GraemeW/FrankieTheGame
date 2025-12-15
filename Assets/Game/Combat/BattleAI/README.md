# Assets:  Game - Battle AI

The Battle AI is a component that lives on any [NPC](../../CharacterObjects/NPCs/) or [Assist Characters](../../CharacterObjects/PCs/Assist/) for automated skill selection in combat.  For example, see the BattleAI component on [PhilRamen](../../CharacterObjects/PCs/Assist/PhilRamen/PhilRamenAssist.prefab) below:

<img src="../../../../InfoTools/Documentation/Game/Combat/BattleAIComponentExample.png" width="500">

For more general information on the combat system and associated skills/skill trees, see [Game/Combat](../).

## Battle AI Base Logic

### Traverse the Tree

The `Probability To Traverse Skill Tree` field defines how likely the NPC will move to a viable next node in its [SkillTree](../SkillTrees/). So:
* at 0.8: there is an 80% probability that the NPC will favor a node deeper in its skill tree
  * or - the NPC will generally use heavier hitting skills more frequently
* at 0.2: there is a 20% probability that the NPC will favor a node deeper in its skill tree
  * or - the NPC will tend to use the less heavy skills 

The probability applies at each node, and so reaching level 3, 4, 5, etc. nodes deep into the skill tree becomes less and less likely.

### Skill Selection

Once the NPC has arrived at its selected node depth, and if `Use Random Selection On No Priorities` is toggled `Enable`, the NPC will randomly select a skill from the corresponding node.

Note that if the NPC is configured such that:
* it has no valid Battle AI Priorities
* `Use Random Selection On No Priorities = False`
, the NPC will never select a skill.

This is by design, as there are instances where it is preferred that the AI halt attacks when AI priorities are no longer valid.

## AI Priorities

In lieu of the default behavior described above, NPCs may be programmed with AI priorities.  Simply, a list of priorities with specific conditions is provided as a list in order of preference.

As an example, consider the BattleAI Priority [BattleAI_RamenHeal-LowPartyHealth](./PhilRamen/BattleAI_RamenHeal-LowPartyHealth.asset) for PhilRamen:

<img src="../../../../InfoTools/Documentation/Game/Combat/BattleAIPredicateExample.png" width="400">

Since this is the first item in PhilRamen's Battle AI priorities (as above), it takes precedence, and so if the following conditions are met:
* the character [Maya](../../CharacterObjects/NPCs/MiscNPCs/Maya/Maya.prefab) is present in the combat

**AND** 

* one of PhilRamen's party members has `health < 42` 

, then PhilRamen will use the skill [ShareWarmRamen](../../OnLoadAssets/Skills/Characters/Ramen/ShareWarmRamen.asset) on the character [Frankie](../../CharacterObjects/PCs/Frankie/Frankie.prefab) (and only Frankie).


## AI Priorities: Quick Start Guide

###  Make the Battle AI Priority

1. Navigate to this [BattleAI](./) directory
2. Create a sub-folder within this directory for the NPC
3. Right click and select `Create` -> `BattleAI` -> `BattleAIPriority`

<img src="../../../../InfoTools/Documentation/Game/Combat/NewBattleAIPriorityMenu.png" width="500">

### Set the Skill(s)

Under skills, add the skills of interest from [OnLoadAssets/Skills](../../OnLoadAssets/Skills/), noting:
* the skill must also be in the NPC's [skill tree](../SkillTrees/)
* if more than one skill is provided, a random skill from the list will be selected (if conditions are met)

### Create and Set the Target(s)

1. Navigate to the [TargetPriorities](./TargetPriorities/) directory
2. For individual character targeting, right click and select `Create` -> `BattleAI` -> `TargetPriority` -> `SpecificTarget`
3. Assign the relevant [CharacterProperties](../../OnLoadAssets/CharacterProperties/) asset to the target priority

For example, see below for [TargetFrankie](./TargetPriorities/TargetFrankie.asset):

<img src="../../../../InfoTools/Documentation/Game/Combat/BattleAITargetPriority.png" width="350">

4. Add the targets of interest, with the highest priority targets at the top of the list
   * i.e. the first entry will take target precedence

Select `Default to Random Target` if the skill should be used even if the target priority is not present in combat, or if the skill should be used even when there are no target priorities.

### Create and Set the Conditions

[BattleAIPredicates](./BattleAIPredicates/) follow the same logic as [GamePredicates](../../Predicates/), using a [conjunctive normal form](https://en.wikipedia.org/wiki/Conjunctive_normal_form) (CNF) of predicate boolean evaluators to check for some arbitrary game condition.

1. On the BattleAIPriority, build out the condition check according to CNF
   * i.e. setting the number of AND and OR conditions that need to be checked together
2. Navigate to the [BattleAIPredicates](./BattleAIPredicates/) folder
3. Right click and select `Create` -> `BattleAI` -> `Predicates` -> *and select the relevant predicate*
   * if the desired predicate does not exist, a new predicate script derivative of the [BattleAIPredicate](../../../Scripts/Combat/BattleAI/BattleAIPredicates/BattleAIPredicate.cs) abstract class may need to be developed
4. Configure the predicate
   * e.g. such as with [MayaPresent](./BattleAIPredicates/MayaPresent.asset) 'CharacterPresent' predicate

<img src="../../../../InfoTools/Documentation/Game/Combat/BattleAIPredicateCharPresent.png" width="350">

   * e.g. such as with [AnyPartyHealthSub42](./BattleAIPredicates/AnyPartyHealthSub42.asset) `LowHealth` predicate

<img src="../../../../InfoTools/Documentation/Game/Combat/BattleAIPredicateLowHealth.png" width="350">

5. Attach the predicates in their relevant positions in the Skill Condition check on the BattleAIPrority (from step 1)

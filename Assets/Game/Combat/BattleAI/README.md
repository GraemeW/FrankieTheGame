# Assets:  Game - Battle AI

The Battle AI is a component that lives on any [NPC](../../CharacterObjects/NPCs/) or [Assist Characters](../../CharacterObjects/PCs/Assist/) for automated skill selection in combat.  For example, see the BattleAI component on [PhilRamen](../../CharacterObjects/PCs/Assist/PhilRamen/PhilRamenAssist.prefab) below:

<img src="../../../../InfoTools/Documentation/Game/Combat/BattleAIComponentExample.png" width="400">

For more general information on the combat system and associated skills/skill trees, see [Game/Combat](../).

## Battle AI Base Logic

### Skill Selection

If no Battle AI Priority applies and `Use Random Selection On No Priorities` is enabled, the NPC rolls a random skill from its [skill set](../SkillSets/):
* skill pool:  weighted by each entry's probability weight
* skill tree:  every filled slot across all branches counts equally (i.e. a skill in several slots is picked more often)

`Filter Skills By AP` (off by default) additionally skips any skill the NPC can't currently afford.

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
3. Right click and select `Create` -> `Combat` -> `BattleAI` -> `BattleAIPriority`

<img src="../../../../InfoTools/Documentation/Game/Combat/NewBattleAIPriorityMenu.png" width="500">

### Set the Skill(s)

Under `Skill References`, add the skills of interest from [OnLoadAssets/Skills](../../OnLoadAssets/Skills/), noting:
* skills are stored by name (same as [skill sets](../SkillSets/)), so only skills in the `Skill` addressables group are accepted
  * note that renaming a skill updates it here automatically
* the skill must also be in the NPC's [skill set](../SkillSets/) (skill pool or skill tree) for the AI to be able to call it
* if more than one skill is provided, a random skill from the list will be selected (if conditions are met)

### Create and Set the Target(s)

1. Navigate to the [TargetPriorities](./TargetPriorities/) directory
2. For individual character targeting, right click and select `Create` -> `Combat` -> `BattleAI` -> `TargetPriority` -> `SpecificCharacter`
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
3. Right click and select `Create` -> `Combat` -> `BattleAI` -> `Predicates` -> *and select the relevant predicate*
   * if the desired predicate does not exist, a new predicate script derivative of the [BattleAIPredicate](../../../Scripts/Combat/BattleAI/Predicates/BattleAIPredicate.cs) abstract class may need to be developed
4. Configure the predicate
   * e.g. such as with [MayaPresent](./BattleAIPredicates/MayaPresent.asset) 'CharacterPresent' predicate

<img src="../../../../InfoTools/Documentation/Game/Combat/BattleAIPredicateCharPresent.png" width="350">

   * e.g. such as with [AnyPartyHealthSub42](./BattleAIPredicates/AnyPartyHealthSub42.asset) `LowHealth` predicate

<img src="../../../../InfoTools/Documentation/Game/Combat/BattleAIPredicateLowHealth.png" width="350">

5. Attach the predicates in their relevant positions in the Skill Condition check on the BattleAIPrority (from step 1)

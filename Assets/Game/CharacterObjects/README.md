# Assets: Game - Character Objects

This directory houses all of the character objects (AKA actors), their corresponding stat progression and wearable objects (placed on characters) in Frankie.  

## Progression

The [Progression](./Progression.asset) scriptable object is a list of characters, as defined by their [Character Properties](../OnLoadAssets/CharacterProperties/) scriptable objects, mapping to their corresponding [stats](../../Scripts/Stats/Stat.cs).  

In other words, [Progression](./Progression.asset) defines how much health a character has, or how much experience a monster gives upon defeat, or how brawny a character might be, etc.  The progression scriptable object is linked && consumed by the [BaseStats](../../Scripts/Stats/BaseStats.cs) component, which lives on every combat-ready character.  NPCs that do not engage in combat (and thus are not present on Progression) are flagged as such by setting their `Has Progression Stats` parameter to `False` on their [CharacterProperties](../OnLoadAssets/CharacterProperties/).

### Note on Stat Growth:

For playable characters, the stats in progression:
* define the starting stats at level 1
* are used as an input to roll for stat growth on each level up

In contrast, for non-playable characters, the stats are fixed and do not increment during level-up.  

This behaviour is flagged using the property `Increment Stats on Level Up` on [CharacterProperties](../OnLoadAssets/CharacterProperties/).

### Summary of Key Progression Stats

The default parameters to include for any new character include:
* Health Points (HP)
* Action Points (AP)
* Experience Reward
  * i.e. experience disbursed on character defeat
  * generally more relevant for NPCs / enemies
* Experience to Level Up
  * can be modified e.g. to allow for more rapid character growth
* Core Stats (increment amount on level-up):
  * Brawn, Beauty, Smarts, Nimble, Luck, Pluck, Stoic

### Progression Editor

For stat adjustment and game balance activities, it is recommended to make use of the [Progression Editor](../../Scripts/Stats/Editor/ProgressionEditor.cs).  This tool can be accessed on the Unity Toolbar, navigating to:
* Tools -> Progression Editor

, as below:

<img src="../../../InfoTools/Documentation/Game/CharacterObjects/ProgressionEditorToolbar.png" width="150">

Once the Progression Editor window is opened, select the [Progression](./Progression.asset) scriptable object, as below:

<img src="../../../InfoTools/Documentation/Game/CharacterObjects/ProgressionEditorSelectAsset.png" width="300">

#### Reconcile Characters

Once the [Progression](./Progression.asset) asset is selected, a character list will be populated on the left-hand side of the tool (as shown in [Basic Usage](#basic-usage--updating-stats)).  

If any characters have [CharacterProperties](../OnLoadAssets/CharacterProperties/) SOs already defined, but do not yet have entries in [Progression](./Progression.asset), click the `Reconcile Characters` button.  This will auto-generate entries for these missing characters if their `Has Progression Stats` parameter is set to enabled.  The new entries are set with some dummy default stats that can be adjusted as needed, per below.

#### Basic Usage:  Updating Stats

An example snapshot of the Progression Editor in action is provided below:

<img src="../../../InfoTools/Documentation/Game/CharacterObjects/ProgressionEditorSnapshot.png" width="800">

Briefly:
* select the character(s) of interest from the list view on the left-hand side of the tool
  * character cards will automatically appear on the right-hand side of the tool
* edit the stats of the character(s) as desired
* any edits will automatically be reflected in the [Progression](./Progression.asset) Asset
  * *this is done using the standard method of marking the asset dirty*
  * *in some cases, changes may not be reflected immediately in file editors/source control*
  * *to force the process, open [Progression](./Progression.asset) in Unity and ctrl/cmd+s to force save*

The Progression Editor tool is hooked up to Unity's Undo/Redo system, so if any mistakes are made, simply undo them (ctrl/cmd+z).

#### Removing Characters from Progression

To remove character(s):
* select the characters
* click the `Remove Selected Characters` button

This is likewise hooked up to Unity's Undo/Redo system, so don't panic if you mistakenly remove an entry.

#### Simulated Stats for Playable Characters

Playable characters have stat growth as a function of their level.  Since stat increases vary based on rolls that happen on each level up event, the stat value at any given level is non-deterministic.

In order to allow for simplier cross-comparisons, simulated stat cards are provided to the right of the initial/stat growth cards for playable characters.  Simply change the `Simulated Level` parameter, and the simulated stats will be generated and then updated on the corresponding card.

## Character Objects 

Character objects are broken down into:
* [PCs](./PCs/): Playable characters, which can be added to the player's [Party](../../Scripts/Stats/Party/) and controlled by the player
* [NPCs](./NPCs/): Non-playable characters, which notably have some form of intelligence to control themselves, whether in the world or in combat
  * e.g. in fixed movements through [NPCMover](../../Scripts/Control/NPC/NPCMover.cs) via [patrol paths](../../Scripts/Control/NPC/PatrolPath.cs)
  * , or to chase the player through [NPCChaser](../../Scripts/Control/NPC/NPCChaser.cs)
  * , or in combat to attack the player with the [BattleAI](../../Scripts/Combat/BattleAI/BattleAI.cs) and [BattleAIPredicates](../../Scripts/Combat/BattleAI/BattleAIPredicates/)

### New Character Creation Quick Start:  Playable Characters

#### Initial Setup

* Create a prefab variant from the standard [Character Prefab](./PCs/Character.prefab)

<img src="../../../InfoTools/Documentation/Game/CharacterObjects/NewPrefabVariant.png" width="400">

* Rename the prefab variant to the character, and create a folder for it in [PCs](./PCs/) - move the prefab variant to this new folder
* Adjust the `Unique Identifier` parameter under the `Saveable Entity` for the variant, as below
  * For recurring characters (ongoing presence in the game):  Set to some constant value, such as the character name
    * *this is critical to ensure character progress is maintained across the game with save progress*
  * For non-recurring & multi-copy characters:  Keep blank/empty
    * *this is critical to ensure unique identifiers are generated on spawn*
    * *this is especially pertinent if multiple copies of the entity may be spawned simultaneously*

<img src="../../../InfoTools/Documentation/Game/CharacterObjects/SaveableEntityUniqueIdentifier.png" width="400">

* Adjust the default sprite image for this character (Character Sprite -> Sprite Renderer -> Sprite) - e.g. like below for [Lucy](./PCs/Lucy/Lucy.prefab)
  * *Note:  Sprite artwork should follow the style guide, per [StyleGuide](../../../InfoTools/StyleGuide/README.md#game-object--world-artwork-pixel-art)*
  * *Note:  Sprite import settings should be adjusted, per [Game/WorldObjects](../WorldObjects/README.md#sprite-import--setup)*

<img src="../../../InfoTools/Documentation/Game/CharacterObjects/LucySpriteOverride.png" width="450">

* Adjust the BoxCollider2D on the prefab variant (as needed to line-up to the attached sprite image)
* In [OnLoadAssets/CharacterProperties/](../OnLoadAssets/CharacterProperties/), create a new entry for the new character & rename it accordingly

<img src="../../../InfoTools/Documentation/Game/CharacterObjects/NewCharacterProperties.png" width="550">

* Link the prefab variant for the character to the new character properties scriptable object - e.g. like below for [Lucy](./PCs/Lucy/Lucy.prefab)
  * *Note: Ignore the Character NPC Prefab entry for now -- this will be addressed later*

<img src="../../../InfoTools/Documentation/Game/CharacterObjects/LucyCharacterProperties.png" width="350">

* Link the character properties to the BaseStats component on your character prefab variant - e.g. like below for [Lucy](./PCs/Lucy/Lucy.prefab)

<img src="../../../InfoTools/Documentation/Game/CharacterObjects/LucyBaseStatsLink.png" width="350">

* Create a new entry for the character properties using the [Progression Editor](#progression-editor), as detailed above
  * This is done simply by clicking on the `Reconcile Characters` button
* This will update the [Progression](./Progression.asset) asset accordingly, for example as below:

<img src="../../../InfoTools/Documentation/Game/CharacterObjects/LucyProgression.png" width="400">

#### Animator Setup

* Create an animator override controller referencing the standard [Character Animator Controller](./PCs/PCAnimatorController.controller)

<img src="../../../InfoTools/Documentation/Game/CharacterObjects/NewAnimatorOverrideController.png" width="500">

* Move the animator override controller to the folder you made for the new character in [PCs](./PCs/)
* Attach the animator override controller as the animator for the new character - e.g. like below for [Lucy](./PCs/Lucy/Lucy.prefab)

<img src="../../../InfoTools/Documentation/Game/CharacterObjects/LucyAnimatorOverride.png" width="350">

* It is now necessary to create override animation clips to replace 16 animation states on the character - e.g. like below for [Lucy](./PCs/Lucy/Lucy.prefab)

<img src="../../../InfoTools/Documentation/Game/CharacterObjects/LucyAnimationStates.png" width="350">

* Thus: 
  * Open an animation tab (in Unity:  Window -> Animation -> Animation)
  * Open the new character prefab variant game object
  * For each animation state in the override controller: in the animation window, select 'Create New Clip' (as below)
  * Link the new clip to the animator override controller (e.g. as above for Lucy)

<img src="../../../InfoTools/Documentation/Game/CharacterObjects/LucyAnimationNewClip.png" width="450">

* Note:  
  * For typical 2-frame walk animation, samples is usually set to:  4
  * If samples is not visible in the animation window, click the … and click 'Show Sample Rate'

<img src="../../../InfoTools/Documentation/Game/CharacterObjects/ShowSampleRate.png" width="250">

* Keep in mind: 
  * For the standard PC/NPC prefab variant, the sprite renderer (and character sprite property) are childed to the main game object - e.g. like below for [Lucy](./PCs/Lucy/Lucy.prefab)
  * Do NOT just drag images directly onto the animation, as this will attach to the parent game object
  * Instead, add the sprite property from the childed object (as below), and then drag the sprites onto this property

<img src="../../../InfoTools/Documentation/Game/CharacterObjects/LucyChildedCharacterSpriteProperty.png" width="500">

* Once all the new clips are bound the the override object, open the prefab's animator controller and delete the same new clips from the controller itself
  * otherwise these new clips will appear in the override controller in addition to the standard dummy animations

#### Combat Setup

* In the [Game/Combat/SkillTrees/Characters](../Combat/SkillTrees/Characters/) directory create a new skill tree

<img src="../../../InfoTools/Documentation/Game/CharacterObjects/NewSkillTree.png" width="600">

* Use the custom [Skill Tree Editor](../../Scripts/Combat/Skills/SkillTree.cs) to build up the character's skill tree
  * *See [SkillTrees](../../Game/Combat/SkillTrees/) for more information on skill tree construction*
* Attach the skill tree to the Skill Handler component on the character - e.g. like below for [Lucy](./PCs/Lucy/Lucy.prefab)

<img src="../../../InfoTools/Documentation/Game/CharacterObjects/LucySkillHandler.png" width="400">

* *Optional Step:* 
* Hook up 'enemy' combat parameters - e.g. like below for [Lucy](./PCs/Lucy/Lucy.prefab)
  * `Combat Sprite`: the image displayed on the [BattleCanvas](../UI/Combat/Battle%20Canvas.prefab) during combat **against** this character (i.e. when faced as an enemy in combat)
  * `Battle Entity Type`:  if the combat participant falls into a category of 'Standard', 'Mook' or 'Boss'
    * this primarily impacts the sprite size when faced as an enemy in combat
  * `Sprite Scale Fine Tune`:  additional parameter to scale the sprite size
  * `Combat Audio`: the music played during combat against this character
  * Moving Background Properties:
    * `Tile Sprite Image`: the tiled image to display during combat against this character
    * `Shader Material`: the shader to apply to sed tiled image during combat against this character
* This step is noted as optional here, because (typically) you will not face a playable character in combat as an enemy

<img src="../../../InfoTools/Documentation/Game/CharacterObjects/LucyOptionalCombatParameters.png" width="350">

#### Character NPC Prefab

As noted above in Character [Initial Setup](#initial-setup), the [Character Properties](../OnLoadAssets/CharacterProperties/) scriptable objects include a link to a Character Prefab, as well as a Character NPC Prefab.

The Character NPC Prefab is generally populated for NPCs (duh), as discussed in the [NPC Creation Quick Start](#new-character-creation-quick-start--non-playable-characters).  However, it can also be populated for playable characters!

Notably, **both** the Character Prefab and Character NPC Prefab should be populated for any characters that can act as both a member of Frankie's party, as well as live as an NPC in the world.  This is particularly important in the context of the [CharacterNPCSwapper](../../Scripts/Stats/CharacterNPCSwapper.cs) component, which can be used to:
- A) recruit an NPC from the world into Frankie's party
- B) take a character from Frankie's party to place them in the world

For creating new Playable Character NPC prefabs, it is recommended to build a variant off of: [CharacterNPC](./PCs/CharacterNPC.prefab)

### New Character Creation Quick Start:  Non-Playable Characters

#### Setup:  Deltas to Playable Characters

The character creation process for NPCs is nearly identical to that of playable characters above, with a few notable exceptions:
* The prefabs to build variants off of are:
  * [NonPlayableCharacter](./NPCs/NonPlayableCharacter.prefab): for NPCs that do not engage in combat
  * [NPCCombatReady](./NPCs/NPCCombatReady.prefab):  for NPCs that do engage in combat
* The *optional* step of hooking up combat parameters in [Combat Setup](#combat-setup) is **no longer optional**
* A host of new NPC-related components are now configurable -- such as state machine behavior, move parameters, chase parameters, loot tables, etc.
  * For spawnable enemies, it is **critical** to set the NPC State Handler:
    * `Will Force Combat`:  `Enable`
    * `Will Destroy If Invisible`:  `Enable`

### Summary of Key Components

A brief summary of the configurable components on the character prefabs noted above is provided below:

|                                    Component                                     |  PC   | PC-NPC |  NPC  | NPC-CR |       |                                                   Detail                                                    |
| :------------------------------------------------------------------------------: | :---: | :----: | :---: | :----: | :---: | :---------------------------------------------------------------------------------------------------------: |
|        [CharacterSpriteLink](../../Scripts/Stats/CharacterSpriteLink.cs)         |   X   |   X    |   X   |   X    |       |        Root-level link to sprite/animator, for cached reference & announcing animation state updates        |
|                  [BaseStats](../../Scripts/Stats/BaseStats.cs)                   |   X   |   X    |   X   |   X    |       |                Link to character properties, progression & interface to all character stats                 |
| [CombatParticipant](../../Scripts/Combat/CombatParticipant/CombatParticipant.cs) |   X   |   X    |       |   X    |       |                          Combat behaviour/methods & interface to the battle system                          |
|           [SkillHandler](../../Scripts/Combat/Skills/SkillHandler.cs)            |   X   |   X    |       |   X    |       |                               Link to skill tree & interface to skill system                                |
|                 [Experience](../../Scripts/Stats/Experience.cs)                  |   X   |   ~    |       |        |       |                       Character experience & level-up behaviour (disabled on PC-NPC)                        |
|                 [Knapsack](../../Scripts/Inventory/Knapsack.cs)                  |   X   |   X    |       |        |       | Character inventory, incl. methods/interface for adjustment + predicate evaluators (e.g. for quests/speech) |
|                  [Equipment](../../Scripts/Stats/Experience.cs)                  |   X   |   X    |       |        |       |              Character equipment, incl. methods/interface for adjustment + stat modifications               |
|        [CharacterNPCSwapper](../../Scripts/Stats/CharacterNPCSwapper.cs)         |   X   |   X    |       |        |       |                   Methods for swapping between character in party <-> character in world                    |
|            [WearablesLink](../../Scripts/Inventory/WearablesLink.cs)             |   X   |   X    |       |        |       |              Methods for probing/interacting with wearables + link to wearables root transform              |
|         [NPCStateHandler](../../Scripts/Control/NPC/NPCStateHandler.cs)          |       |   X    |   X   |   X    |       |                NPC state, with player state listeners and methods to adjust player/NPC state                |
|                [NPCMover](../../Scripts/Control/NPC/NPCMover.cs)                 |       |   X    |   X   |   X    |       |           NPC world move properties / methods, including momvement along pre-defined patrol paths           |
|              [BattleAI](../../Scripts/Combat/BattleAI/BattleAI.cs)               |       |   X    |   X   |   X    |       |  Logic for NPCs during battle (i.e. skill selection, combat priorities), interfacing to the battle system   |
|     [NPCCollisionHandler](../../Scripts/Control/NPC/NPCCollisionHandler.cs)      |       |        |   X   |   X    |       |     State changes as a function of character collisions, flesibility to trigger arbitrary Unity Events      |
|            [LootDispenser](../../Scripts/Inventory/LootDispenser.cs)             |       |        |       |   X    |       |                            Loot tables & logic for randomly providing loot/cash                             |
|             [SaveableEntity](../../Scripts/Saving/SaveableEntity.cs)             |   X   |   X    |   X   |   X    |       |          For interfacing with the save system - defines the character state as an item to be saved          |


## Wearables

See [Wearables](./Wearables/)

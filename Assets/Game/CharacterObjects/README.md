# Assets: Game - Character Objects

This directory houses all of the character objects (AKA actors), their corresponding stat progression and wearable objects (placed on characters) in Frankie.  

## Progression

//TODO:

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
* Adjust the default sprite image for this character (Character Sprite -> Sprite Renderer -> Sprite) - e.g. like below for [Lucy](./PCs/Lucy/Lucy.prefab)

<img src="../../../InfoTools/Documentation/Game/CharacterObjects/LucySpriteOverride.png" width="450">

* In [OnLoadAssets/CharacterProperties/](../OnLoadAssets/CharacterProperties/), create a new entry for the new character & rename it accordingly

<img src="../../../InfoTools/Documentation/Game/CharacterObjects/NewCharacterProperties.png" width="550">

* Link the prefab variant for the character to the new character properties scriptable object - e.g. like below for [Lucy](./PCs/Lucy/Lucy.prefab)
  * *Note: Ignore the Character NPC Prefab entry for now -- this will be addressed later*

<img src="../../../InfoTools/Documentation/Game/CharacterObjects/LucyCharacterProperties.png" width="350">

* Link the character properties to the BaseStats component on your character prefab variant - e.g. like below for [Lucy](./PCs/Lucy/Lucy.prefab)

<img src="../../../InfoTools/Documentation/Game/CharacterObjects/LucyBaseStatsLink.png" width="350">

* Open up the [Progression](./Progression.asset) scriptable object
* Create a new entry for the new character
* Link the character properties scriptable object to the new entry
* Fill the stats for the new character as desired - e.g. like below for [Lucy](./PCs/Lucy/Lucy.prefab)
  * *See above for more detail on progression stats*

<img src="../../../InfoTools/Documentation/Game/CharacterObjects/LucyProgression.png" width="400">

<br/>

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

<br/>

#### Combat Setup

* In the [Game/Combat/SkillTrees/Characters](../Combat/SkillTrees/Characters/) directory create a new skill tree

<img src="../../../InfoTools/Documentation/Game/CharacterObjects/NewSkillTree.png" width="600">

* Use the custom [Skill Tree Editor](../../Scripts/Combat/Skills/SkillTree.cs) to build up the character's skill tree
  * *See [SkillTrees](../../Game/Combat/SkillTrees/) for more information on skill tree construction*
* Attach the skill tree to the Skill Handler component on the character - e.g. like below for [Lucy](./PCs/Lucy/Lucy.prefab)

<img src="../../../InfoTools/Documentation/Game/CharacterObjects/LucySkillHandler.png" width="400">

<br/>

* *Optional Step:* 
* Hook up 'enemy' combat parameters - e.g. like below for [Lucy](./PCs/Lucy/Lucy.prefab)
  * Combat Sprite: the image displayed on the [BattleCanvas](../UI/Combat/Battle%20Canvas.prefab) during combat **against** this character
  * Combat Audio: the music played during combat against this character
  * Moving background properties:
    * Tile Sprite Image: the tiled image to display during combat against this character
    * Shader Material: the shader to apply to sed tiled image during combat against this character

<img src="../../../InfoTools/Documentation/Game/CharacterObjects/LucyOptionalCombatParameters.png" width="350">

<br/>

#### Character NPC Prefab

//TODO:

### New Character Creation Quick Start:  Non-Playable Characters

//TODO:

### Summary of Key Character Components

//TODO: 


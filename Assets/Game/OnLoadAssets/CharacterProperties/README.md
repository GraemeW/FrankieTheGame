# Assets:  Game - Character Properties

CharacterProperties are lightweight scriptable objects used to link to [Playable Character](../../CharacterObjects/PCs/) and/or [NPC](../../CharacterObjects/NPCs/) prefabs.  

They are loaded as Addressables in [OnLoadAssets](../) in order to allow character/NPC instantiation at runtime.

## Progression Integration

CharacterProperties are notably linked in the [Progression](../../CharacterObjects/Progression.asset) scriptable object, in order to allow each character to derive its core stats -- detailed further in [CharacterObjects](../../CharacterObjects/).

## Character Properties: Quick Start Guide

### Make the Character Property

1.  Navigate to this [CharacterProperties](./) directory
2.  Right click and select `Create` -> `Characters` -> `New Character`

<img src="../../../../InfoTools/Documentation/Game/OnLoadAssets/CharacterProperties/NewCharacterPropertiesMenu.png" width="500">

### Configure the Character Property

Attach:
* `Character Prefab` to the relevant [Playable Character](../../CharacterObjects/PCs/) prefab
* `Character NPC Prefab` to the relevant [NPC](../../CharacterObjects/NPCs/) prefab

, for example as shown for Tilly below:

<img src="../../../../InfoTools/Documentation/Game/OnLoadAssets/CharacterProperties/CharacterPropertiesTilly.png" width="300">

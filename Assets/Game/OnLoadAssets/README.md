# Assets: Game - OnLoadAssets

OnLoadAssets are files loaded during run-time via the Unity [Addressables](../../Scripts/Core/AddressablesHandling/) system.  These are scriptable objects that are not necessarily present in a given scene, but are nonetheless critical for game functionality.

Generally, each category/directory of OnLoadAssets includes a dictionary look-up that is built during initial game loading.  In this manner, the full set of game data are available and addressable (ba-dum-chah) throughout any portion of the game.

A summary is provided below:

|                   Directory                   |       |                                                                                                                          Detail                                                                                                                          |
| :-------------------------------------------: | :---: | :------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------: |
| [CharacterProperties](./CharacterProperties/) |       |            Light-weight link to the [playable character](../CharacterObjects/PCs/) and [non-playable character](../CharacterObjects/NPCs/) prefabs, which further link to character stat [progression](../CharacterObjects/Progression.asset)            |
|               [Zones](./Zones/)               |       |       Scene reference, zone audio and related parameters per Zone - used by the [Zone](../../Scripts/Zones/) system to allow for inter- and intra-scene player travel, with custom UnityUI editor for linking together [ZoneNodes](../ZoneNodes/)        |
|       [BattleActions](./BattleActions/)       |       |                Including parameters to trigger different effects on characters (whether by item or skill), such as damage, healing, status, etc. <br/> *Note: battle actions are further wrapped by inventory items or skills, as below*                 |
|              [Skills](./Skills/)              |       |                                                  Skill-based BattleAction wrapper, with a) flavour text detailing the skill (for UI/lore) and b) corresponding stat for interfacing w/ the skill system                                                  |
|           [Inventory](./Inventory/)           |       | Item-based BattleAction wrapper, with a) flavour text, b) item details such as consumability and for interfacing with shops, equipment (if [equippable](./Inventory/EquipableItems/)), wearable system (if [wearable](./Inventory/WearableItems/)), etc. |
|              [Quests](./Quests/)              |       |                                                              Including quest description, childed quest objectives (each including flavour text) and linked quest rewards (inventory items)                                                              |

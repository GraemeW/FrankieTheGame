# Frankie The Game

A 2D bird's eye old-school RPG adventure that follows Frankie, as he deals with the trials and tribulations of being an underpaid, overworked white collar office laborer.  

Frankie's day-to-day monotony is interrupted by a near-death experience involving a rather spicy cup-noodle-ramen, leading to an understanding that life is not as Plain Jane as it once seemed.  Dark forces seek to corrupt those around him and shatter his understanding of the universe.

## Game Features

Frankie has danger:

![](/InfoTools/Images/HorseDanger.png)

, it has friendship:

![](/InfoTools/Images/FrankieFriendship.gif)

, it has vast landscapes:

![](/InfoTools/Images/VastOverworld.png)

, it has thrilling combat:

![](/InfoTools/Images/ThrillingCombatToo.png)

, and most of all, it has heart:

![](/InfoTools/Images/LucySmooch.png)

## World Construction - Scenes

Scenes are located in: [Scenes](./Assets/Scenes/)

Each major city/area exterior & interior are allocated to their own scenes.  For interior scenes, multiple rooms may share the same scene, and individual rooms are toggled on/off as defined by the behaviour in the [Zones](/Assets/Scripts/Zones/) system.  Maintaining character state during transitions throughout scenes is handled via the singleton pattern on the [Player](/Assets/Scripts/Core//Player.cs) object.

## World Construction - Game Assets

Unity game assets are located in: [Game](./Assets/Game/)

The addressables that contain key game data (zone properties, character properties, actions/skills, quests, items, etc.) are located in [OnLoadAssets](/Assets/Game/OnLoadAssets/), which are loaded via the aforementioned [Addressables Loading System](/Assets/Scripts/Core/AddressablesHandling/) in the Core namespace.

N.B. Artwork and music associated with these assets are not, by default, pushed to GIT.  They are backed up separately.

## Scripts - Key Namespaces

Scripts are located in: [Scripts](./Assets/Scripts/)

* [Core](/Assets/Scripts/Core/):  Camera, scene management, addressables loading
    * *incl.: [Predicates](/Assets/Scripts/Predicates/), logic for any state-based conditional evaluations
* [Control](/Assets/Scripts/Control/):  Player and NPC -to- world interface, including player/NPC state machines
    * *incl. [Check Interactions](/Assets/Scripts/CheckInteractions/)*
    * *incl. [World Interactables](/Assets/Scripts/World/)*
    * *notable namespace exceptions*:
        * *[BattleController](/Assets/Scripts/Control/Controllers/BattleController.cs) -- included in [Combat](/Assets/Scripts/Combat/) namespace*
        * *[DialogueController](/Assets/Scripts/Control/Controllers/DialogueController.cs) -- included in [Speech](/Assets/Scripts/Speech/) namespace*
* [Zones](/Assets/Scripts/Zones/):  Scene (room/worldspace) properties/references and scene-to-scene transitions
    * *incl. [Map/World Camera](/Assets/Scripts/Zones/Map/)*
* [Stats](/Assets/Scripts/Stats/):  Character, enemy and NPC game stats, party tracker/behaviors
* [Combat](/Assets/Scripts/Combat/):  Battle logistics, combat participant, enemy combat AI, actions/skills, status effects, enemy spawners
* [Speech](/Assets/Scripts/Speech/):  Dialogue nodes/trees, triggers (via predicates), NPC conversant AI
* [Inventory](/Assets/Scripts/Inventory/):  Items, equipment/wearables, knapsack/wallet, loot dispensing, shops
* [Quests](/Assets/Scripts/Quests/):  Tracker/status, objectives, givers, rewards
* [UI](/Assets/Scripts/UI/):  Main menus, world menus, speech text, stats/inventories, combat/skills menus
* [Sound](/Assets/Scripts/Sound/):  Music and sound effects
* [Saving](/Assets/Scripts/Saving/):  Flexible to any arbitrary game/player/NPC state parameters
* [Utilities](/Assets/Scripts/Utils/):
    * *incl. [Misc Functional](/Assets/Scripts/Utils/Functional/), such as circular buffers, lazy initializations, list extensions, seriablizable vectors, etc.*
    * *incl. [Shader Support](/Assets/Scripts/Utils//Shaders/)*
    * *incl. [UIBox](/Assets/Scripts/Utils/UIBox/) & [UIMisc](/Assets/Scripts/Utils/UIMisc/), support code for key UI elements (such as the text/menu scroll box used in all key UI)*
* [Settings](/Assets/Scripts/PlayerPrefs/):  Game settings, such as master/world/battle volume

## Scripts - Tool Extensions

* [CustomRuleTiles](/Assets/Scripts/CustomRuleTiles/):  Extensions on Unity base rule tiles, incl. rule-match siblings, random siblings, random animation rule tiles

## Getting involved?

This is a passion project, and I'm not currently looking for any support for this project.  

That stated, if you have similar game development sensibilities/interests && strong pixel art capabilities or programming prowess, feel free to drop me a message and I'd be happy to chat.

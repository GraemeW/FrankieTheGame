# Assets - Scripts : Game Logic

## NameSpaces

The game logic for Frankie is broken down into the below high-level organization *(as defined by their corresponding namespaces)*:

|     Namespace      |                                                                          Detail                                                                          |       |                              Key Folders                              |
| :----------------: |:--------------------------------------------------------------------------------------------------------------------------------------------------------:| :---: |:---------------------------------------------------------------------:|
|      **Core**      |               Player singleton, player state machine, persistent object spawner, game state modifier framework, predicate logic/evaluators               |       |           [Core](./Core/) <br/> [Predicates](./Predicates/)           |
|     **Saving**     |                          Save file manager _(backed by com.lowdefmustard.saving)_, and PlayerPrefs interfacing (game settings)                           |       |                          [Saving](./Saving/)                          |
|     **Sound**      |                                  Sound control for background music, music overrides, battle music, misc. sound effects                                  |       |                           [Sound](./Sound/)                           |
|   **Rendering**    |                                     Custom shader code, and display resolution / window logic, control + monitoring                                      |       |                       [Rendering](./Rendering/)                       |
|       **UI**       |                     UI-specific logic + menu interfacing _(backed by com.lowdefmustard.uibox)_ <br/> Grouped by `.UI` sub-namespaces                     |       |                              [UI](./UI/)                              |
|    **Control**     |   Object control/interaction _(backed by com.lowdefmustard.control)_, including: <br/> Player input/controllers, actor movement and NPC state handler    |       | [Control](./Control/) <br/> [CheckInteractions](./CheckInteractions/) |
|     **Stats**      |          Character attributes (e.g. impacting Combat), experience/level handlers & progression, party/party assist logic & relevant interfacing          |       |                           [Stats](./Stats/)                           |
|     **Combat**     | Combat participant configuration/state, battle actions/skills & status effects, battle logistics (battle mat rewards, NPC battle AI), world NPC spawners |       |                          [Combat](./Combat/)                          |
|   **Inventory**    |            Items (incl. wearables), wallet, inventory & equipment configuration/state, item-to-skill/action logic, shops/shopping, enemy loot            |       |                       [Inventory](./Inventory/)                       |
|     **Quests**     |                                     Quest configurations incl. objectives/rewards, quest givers, quest list & state                                      |       |                          [Quests](./Quests/)                          |
|     **Speech**     |               Dialogue system, including support for complex dialogue trees via Dialogue Nodes, Triggers - interfacing w/ predicate logic                |       |                          [Speech](./Speech/)                          |
|     **World**      |                      Specific actor-object world interaction scripts - usually including public functions to call via Unity Events                       |       |                           [World](./World/)                           |
|     **Zones**      |                            Intra- and inter-zone (scene) attributes & transition logic _(backed by com.lowdefmustard.zones)_                             |       |                           [Zones](./Zones/)                           |
|     **Utils**      |                  Debugger & editor tools, Addressables Loader & Localization Tools _(backed by com.lowdefmustard.utils / localization)_                  |       |                           [Utils](./Utils/)                           |

## Standalone Packages

Several of the above namespaces implement common Low Def Mustard packages (as noted above) -- see each package's own README for full details.

|              Package               |                                                                              Summary                                                                              |                                Corresponds To                                |
| :---------------------------------: |:-------------------------------------------------------------------------------------------------------------------------------------------------------------:|:------------------------------------------------------------------------------:|
| `com.lowdefmustard.utils`           | General-purpose data structures/extensions, custom attribute drawers, the Predicates/Condition system, weighted probability, and the Sprite Animation Generator editor tool. | **Utils** (fully) · base framework used by **Core**'s predicate logic |
| `com.lowdefmustard.control`         | Controller/input-receiver framework, top-down movement & warp handling, collider-driven `MoveMesh` navmesh generation, A* pathfinding, and patrol paths.        | **Control** (movement/pathfinding/input, not actor state machines) |
| `com.lowdefmustard.localization`    | `SimpleLocalizedString` inspector authoring workflow, `ILocalizable` entry-ownership contract, and automatic cleanup of orphaned localization entries.          | Cross-cutting — used wherever `[SimpleLocalizedString]` fields appear (**Core**, **Speech**, **UI**, **Quests**, **Stats**, **Inventory**, **Zones**, ...) |
| `com.lowdefmustard.ruletiles`       | Custom Tilemap `RuleTile`s: sibling-tile recognition across separate tile assets, and random/animated rule tiles.                                              | Cross-cutting tile-authoring tool, not tied to a single namespace above |
| `com.lowdefmustard.saving`          | Save-data serialization, the `SaveableEntity`/`ISaveable` framework, and the `SaveEditor` in-editor save inspector/mutator tool.                                | **Saving** (core mechanics + editor tooling, not project-specific save-slot management) |
| `com.lowdefmustard.uibox`           | Input-driven UI box architecture: cursor-navigable state machine, choice widgets, and `TextScanBox` scrolling/typewriter text.                                  | **UI** (the `UIBox` architecture specifically) |
| `com.lowdefmustard.zones`           | Zone/scene graph, Addressables-backed scene transitions & fading, and the `ZoneEditor`/`MultiZoneViewer` editor tools.                                          | **Zones** (graph, transitions, editor tooling, not the mini-map or zone-specific handlers) |

### Package Dependencies

Internal (`com.lowdefmustard.*`) dependencies between the packages above:

- **`utils`** — no internal dependencies
- **`ruletiles`** — no internal dependencies
- **`saving`** — no internal dependencies
- **`control`** — depends on `utils`
- **`localization`** — depends on `utils`
- **`uibox`** — depends on `control`, `utils`
- **`zones`** — depends on `utils`, `localization`

All packages are currently at version `0.1.0`. See `Packages/com.lowdefmustard.<name>/README.md` for each package's full contents, assembly structure, and design notes.

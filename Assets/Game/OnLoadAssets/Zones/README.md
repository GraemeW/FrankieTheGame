# Assets:  Game - Zones

Zone scriptable objects serve three primary purposes:
* as wrappers to [Scenes](../../../Scenes/):
  * containing key scene properties, such as linked music to play on scene load & map update behaviour
* as functional maps of the interior of the scene, enabling Frankie to traverse within the scene (e.g. through doors, elevators, fireman poles, etc.)
* as links between multiple zones, enabling Frankie to traverse from scene-to-scene

Naturally it should be understood that any time a new Scene is created, a new Zone should be created to pair with it.  

## Zones:  Quest Start Guide

### Make the Zone

1. Navigate to this [Zones](./) directory (or any sub-directories within)
2. Right click and select `Create` -> `Zones` -> `New Zone`

<img src="../../../../InfoTools/Documentation/Game/OnLoadAssets/Zones/NewZoneMenu.png" width="500">

### Make the Scene

See [Scenes](../../../Scenes/) for more detail on creating a new scene.

### Configure the Zone

Set:
* Scene Reference:  to link to the paired scene
* Update Map:
  * true - if the player map should update (e.g. exterior scene)
  * false - if the player map should pause from last screen (e.g. interior scene)
* Zone Audio:  to link to the audio to play when the scene is loaded
* Is Zone Audio Looping:
  * true - if the audio file should loop continuously
  * false - if the audio should stop after one play

The remaining parameters relate to the Zone Editor and Zone Node properties.  

An example Zone configuration is shown below for the Office Interior scene:

<img src="../../../../InfoTools/Documentation/Game/OnLoadAssets/Zones/ZoneOfficeInteriorConfig.png" width="300">

## Zone Editor

Double click on the Zone scriptable object to open up the Zone Editor.  An example new zone is shown below:

<img src="../../../../InfoTools/Documentation/Game/OnLoadAssets/Zones/ExampleZoneA.png" width="600">

Upon opening the zone in the zone editor and saving, a child ZoneNode will be created, as shown above.  We will refer to this as the `root` ZoneNode.  The `root` ZoneNode's ID is auto-populated, but it can be overridden for descriptive clarity, as below:

<img src="../../../../InfoTools/Documentation/Game/OnLoadAssets/Zones/ExampleZoneB.png" width="300">

*N.B.  You must remember to save (cmd/ctrl + s) after making changes in the zone editor for the scriptable object asset to be updated!*

### Setting up Across-Zone Nodes

We need to have an entry/exit point for the new zone (scene) to some other zone (scene).  As such, it is typical practice to use the root node to link for this purpose.

Click on the `+` sign on the `root` ZoneNode to create a second child ZoneNode.  Click on the new ZoneNode and rename the Override ID to something meaningful to indicate transition out of this zone -- in this example, we name it `SceneToExampleZoneExterior`, as below:

<img src="../../../../InfoTools/Documentation/Game/OnLoadAssets/Zones/ExampleZoneC.png" width="600">

Click on this second child node and view its properties in the inspector -- to allow for transition to another scene, update parameters:
* Linked Zone:  with the new zone/scene to load
* Linked Zone Node:  with the specific zone node location to load into the scene

Continuing the above example, we can hook up the `SceneToExampleZoneExterior` ZoneNode to the `OfficeInterior` zone, and have the player appear at the `OfficeInteriorDoor`:

<img src="../../../../InfoTools/Documentation/Game/OnLoadAssets/Zones/ExampleZoneD.png" width="300">

This effectively sets the logic to exit the `ExampleZone` scene -- by interacting with the ZoneNode `MainZoneDoor` the player will transition to the OfficeInterior zone (scene), and then be positioned just inside the ZoneNode labeled `OfficeInteriorDoor`.  In a similar fashion, and other zones can now link to enter the `ExampleZone` by creating those zone node links in their respective zone editors.

### Setting up Within-Zone Nodes

ZoneNodes are also used for travel **within** a zone.  For example, consider if we want to have Frankie enter a door to travel from the living room to the bathroom -- in this case, we don't want separate scenes for each room, but we do want to move from the living room into the bathroom within the scene (and to toggle game objects accordingly).  This is zone via simple zone nodes without populating the `Linked Zone/Node` fields noted above.

Continuing the above example, let's add a new *orphan* zone node by:
* once again, click on the `+` sign on the `root` ZoneNode
  * this will generate a third ZoneNode, which we will name `LivingRoomDoor`
* click `link` on the `root` ZoneNode
* click `unlink` on the the `LivingRoomDoor` ZoneNode
, as below:

<img src="../../../../InfoTools/Documentation/Game/OnLoadAssets/Zones/ExampleZoneE.png" width="600">

, which creates a new independent ZoneNode:

<img src="../../../../InfoTools/Documentation/Game/OnLoadAssets/Zones/ExampleZoneF.png" width="600">

From here we can link this new ZoneNode to a new `BathroomDoor` ZoneNode by clicking the `+` sign on the `LivingRoomDoor` and updating the new ZoneNode Override ID accordingly:

<img src="../../../../InfoTools/Documentation/Game/OnLoadAssets/Zones/ExampleZoneG.png" width="600">

We usually want bi-directional movement between ZoneNodes (it would be weird if Frankie could travel from the living room to the bathroom, but could not travel back from the bathroom to the living room).  To enable this:
* click `link` on the `BathroomDoor` ZoneNode
* click `child` on the `LivingRoomDoor` ZoneNode
, as below:

<img src="../../../../InfoTools/Documentation/Game/OnLoadAssets/Zones/ExampleZoneH.png" width="600">

Finally, note that a single ZoneNode **can** access multiple other ZoneNodes -- consider the case of an elevator that reaches multiple floors.  To explore this idea, let's:
* re-name the `LivingRoomDoor` ZoneNode to `ElevatorDoor`
* click the `+` sign on the `LivingRoomDoor` ZoneNode twice
* name the new ZoneNodes (e.g. `BasementDoor` and `RoofDoor`)
* back-link the new ZoneNodes to enable bi-directional travel

The final zone as mapped out then looks like:

<img src="../../../../InfoTools/Documentation/Game/OnLoadAssets/Zones/ExampleZoneI.png" width="600">


### Conditional Zone Nodes

In some cases, access to certain ZoneNodes needs to be gated by conditions (e.g. story progression, party members present, items in inventory, etc.).  This can be accomplished using [Predicates](../../Predicates/), by setting the `Additional Properties - Condition` setting on the ZoneNode in the Inspector.

Continuing the above example, we can restrict access to the `Bathroom` ZoneNode so that the party can only enter the bathroom if Frankie is currently in the party by attaching the relevant predicate:

<img src="../../../../InfoTools/Documentation/Game/OnLoadAssets/Zones/ExampleZoneJ.png" width="300">

## Zone Node Integrations

As discussed above, Zones are effectively wrappers to the scenes, and ZoneNodes help to map the interior of the scene.  Functionally, ZoneNodes are consumed by a ZoneHandler component to physically warp Frankie and his team around the game.  

Standard implementations of the ZoneHandler component can be found on the prefabs in [WorldObjects/_ZoneNodes/](../../WorldObjects/_ZoneNodes/).  These prefabs include interaction components/colliders (i.e. so Frankie can trigger the ZoneHandler->ZoneNode), sound boxes (e.g. a opening door sound) and other relevant tunables -- such as with the SimpleZoneNode below:

<img src="../../../../InfoTools/Documentation/Game/OnLoadAssets/Zones/SimpleZoneNode.png" width="600">

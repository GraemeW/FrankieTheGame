# Assets:  Game - Map

The game map is a UI element accessible from the [WorldOptions](../UI/World/WorldOptions.prefab) menu.  The map itself lives on an image in the [MapSuper](./MapSuper.prefab) prefab that is spawned when selected.  [MapSuper](./MapSuper.prefab) is itself an extension of the standard [UIBox](../../Scripts/Utils/UIBox/) UI element.

## Map Elements

The key map elements in [this directory](./) include:
* [MapSuper](./MapSuper.prefab):  UI box that displays the map, spawned on selection via [WorldOptions](../UI/World/WorldOptions.prefab) 
  * [MapDisplay](./MapDisplay.prefab):  childed to [MapSuper](./MapSuper.prefab), includes UI elements living in the UIBox
  * [MapRenderTexture](./MapRenderTexture.renderTexture):  map texture loaded into the `Map` Raw Image (childed to [MapDisplay](./MapDisplay.prefab))
  * [FrankieIndicator](./FrankieIndicator.prefab):  flashing image of Frankie's face to indicate player location
* [MapCamera](./MapCamera.prefab):  camera used to generate a snapshot of the scene/zone

## Camera Snapshots to Map Updates

In order to update the map:
* Upon spawning, [MapSuper](./MapSuper.prefab) will spawn a [MapCamera](./MapCamera.prefab)
* [MapCamera](./MapCamera.prefab) will take a low-res + limited FoV snapshot of the current scene/zone
* [MapCamera](./MapCamera.prefab) will write load this snapshot into [MapRenderTexture](./MapRenderTexture.renderTexture)
* The [MapRenderTexture](./MapRenderTexture.renderTexture) will in turn be rendered on the `Map` Raw Image in [MapDisplay](./MapDisplay.prefab)

, as illustrated below:

<img src="../../../InfoTools/Documentation/Game/Map/UItoCameraLink.png" width="600">

## Map Update Pausing & Scene Transition Snapshots

While the player is indoors, we wish to have the map locked to a snapshot of the exterior scene with the [Frankie Indicator](./FrankieIndicator.prefab) flashing over the building the player entered.

This functionality is defined by the `updateMap` parameter set during [Zone Configuration](../OnLoadAssets/Zones/README.md#configure-the-zone), where we can configure:
* exterior scenes:  update map
* interior scenes:  do not update map

Practically, this is enabled via an additional [MapCamera](./MapCamera.prefab) that lives on [PersistentObjects](../Core/README.md#persistent-objects-singleton).  This copy of [MapCamera](./MapCamera.prefab) subscribes to scene transition events, and takes a scene snapshot right before the scene transition.

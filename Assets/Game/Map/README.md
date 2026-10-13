# Assets:  Game - Map

The game map is a UI element accessible from the [WorldOptions](../UI/MainMenus/InWorld/WorldOptions.prefab) menu.  The map itself is shown by the [MapSuper](./MapSuper.prefab) prefab that is spawned when selected.  [MapSuper](./MapSuper.prefab) is itself an extension of the standard [UIBox](../../../Packages/com.lowdefmustard.uibox) UI element.

## Map Elements

The key map elements in [this directory](./) include:
* [MapSuper](./MapSuper.prefab):  UI box that displays the map, spawned on selection via [WorldOptions](../UI/MainMenus/InWorld/WorldOptions.prefab) 
  * [MapSuper.uxml](./MapSuper.uxml) / [MapSuper.uss](./MapSuper.uss):  UI Toolkit layout + style of the box (frame, map, flavour text, player indicator)
  * [MapRenderTexture](./MapRenderTexture.renderTexture):  map texture shown by the box's `MapImageElement` (hooked up on [MapSuper](./MapSuper.prefab))
  * the player indicator:  a `FlashingElement` showing Frankie's face (`--map-indicator-image`) to indicate player location
* [MapCamera](./MapCamera.prefab):  camera used to generate a snapshot of the scene/zone

## Camera Snapshots to Map Updates

In order to update the map:
* Upon spawning, [MapSuper](./MapSuper.prefab) will spawn a [MapCamera](./MapCamera.prefab)
* [MapCamera](./MapCamera.prefab) will take a low-res + limited FoV snapshot of the current scene/zone
* [MapCamera](./MapCamera.prefab) will write load this snapshot into [MapRenderTexture](./MapRenderTexture.renderTexture)
* The [MapRenderTexture](./MapRenderTexture.renderTexture) will in turn be rendered as the background of the `MapImageElement` in [MapSuper.uxml](./MapSuper.uxml)

, as illustrated below:

<img src="../../../InfoTools/Documentation/Game/Map/UItoCameraLink.png" width="600">

## Map Update Pausing & Scene Transition Snapshots

While the player is indoors, we wish to have the map locked to a snapshot of the exterior scene with the player indicator flashing over the building the player entered.

This functionality is defined by the `updateMap` parameter set during [Zone Configuration](../OnLoadAssets/Zones/README.md#configure-the-zone), where we can configure:
* exterior scenes:  update map
* interior scenes:  do not update map

Practically, this is enabled via an additional [MapCamera](./MapCamera.prefab) that lives on [PersistentObjects](../Core/README.md#persistent-objects-singleton).  This copy of [MapCamera](./MapCamera.prefab) subscribes to scene transition events, and takes a map snapshot right before the scene transition.  In this manner, the [MapRenderTexture](./MapRenderTexture.renderTexture) is updated in the background without needing to spawn the [MapSuper](./MapSuper.prefab) UI element.
